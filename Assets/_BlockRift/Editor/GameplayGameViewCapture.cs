using System;
using System.IO;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BlockRift.Presentation;

namespace BlockRift.Editor
{
    /// <summary>Unity'nin gerçek Game View'ını portre çözünürlükte açar ve ScreenCapture ile kanıt görüntüsü alır.</summary>
    [InitializeOnLoad]
    public static class GameplayGameViewCapture
    {
        private const string Pending = "BlockRift.ArtCapture.Pending";
        private const string Request = "Temp/BlockRift.GameView.request";
        private static double captureAt;
        private static bool requested;
        private static DateTime previousImageTime;
        private static double diagnosticAt;

        static GameplayGameViewCapture() => EditorApplication.update += Tick;

        /// <summary>Grafik destekli izole Unity çalışmasında Game View/Play Mode yakalamasını başlatır.</summary>
        public static void CaptureBatch() => Begin();

        /// <summary>Ana projede kaydedilmiş entegre sahneyi Game View'da gösterir; kirli sahneyi ezmez.</summary>
        [MenuItem("Block Rift/Visuals/Open Integrated Game View")]
        public static void Begin()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Önce mevcut Play Mode'u durdurun.");
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (active.isDirty)
                throw new InvalidOperationException("Önce mevcut sahne değişikliklerini kaydedin. Sahne ezilmedi.");
            // Diskten aktarılan prefab'lar, açık editörün AssetDatabase'ine girmeden sahne açılamaz.
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (string path in new[] { GameplayArtAssets.Models.TrimEnd('/'), GameplayArtAssets.Materials.TrimEnd('/'),
                GameplayArtAssets.Prefabs.TrimEnd('/'), "Assets/_BlockRift/Content" })
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceSynchronousImport);
            var catalog = AssetDatabase.LoadAssetAtPath<VisualPrefabCatalog>(GameplayArtAssets.CatalogPath);
            if (!catalog || catalog.Entries.Count < 8)
                throw new InvalidOperationException("Entegre görsel katalog henüz import edilmedi.");
            int expectedMeshes = 0;
            foreach (var entry in catalog.Entries)
            {
                if (!entry.Prefab)
                    throw new InvalidOperationException("Eksik prefab: " + entry.AssetId);
                expectedMeshes += entry.Prefab.GetComponentsInChildren<MeshFilter>(true).Length;
            }
            EditorSceneManager.OpenScene(GameplaySceneSetup.ScenePath, OpenSceneMode.Single);
            var board = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()[0].transform.Find("BoardRoot");
            if (board && board.parent.GetComponent<LevelOneGameplay>())
            {
                // Gerçek Level 1 artık katalogdaki her şeklin tek örneğini içeren sanat testi değildir.
                expectedMeshes = board.Find("BoardVisualsRoot").GetComponentsInChildren<MeshFilter>().Length;
                var level = BlockRift.Core.LevelOne.Create();
                expectedMeshes += level.Pieces.Sum(p => catalog.Entries.First(e => !e.IsBoard &&
                    e.AssetId == (p.Kind == BlockRift.Core.PieceKind.Target ? "Special_1x1_Star" : "Block_" + p.Width + "x" + p.Height)).Prefab.GetComponentsInChildren<MeshFilter>().Length);
                expectedMeshes += level.FixedCells.Count * catalog.Entries.First(e => e.AssetId == "Obstacle_1x1_Stone").Prefab.GetComponentsInChildren<MeshFilter>().Length;
                var markers = board.Find("MarkersRoot");
                if (markers)
                    expectedMeshes += markers.GetComponentsInChildren<MeshFilter>().Length;
            }
            if (!board || board.GetComponentsInChildren<MeshFilter>().Length != expectedMeshes)
                throw new InvalidOperationException("Game View yakalanmadı: katalogdaki gerçek mesh'ler sahneye bağlanamadı.");
            var gameType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            var view = EditorWindow.GetWindow(gameType);
            // Unity 6'nın editör API'si içseldir; başarısızlık sessizce başka çözünürlüğe dönüştürülmez.
            var sizesType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            object sizes = singleton.GetProperty("instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy).GetValue(null);
            var groupType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizeGroupType");
            object group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { Enum.Parse(groupType, "Standalone") });
            var sizeType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSize");
            var sizeKind = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizeType");
            object resolution = Activator.CreateInstance(sizeType, new object[] { Enum.Parse(sizeKind, "FixedResolution"), 1080, 1920, "Block Rift 1080x1920" });
            int index = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { resolution });
            gameType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(view, index);
            view.Show();
            view.Focus();
            captureAt = 0;
            requested = false;
            SessionState.SetBool(Pending, true);
            SessionState.SetBool(Pending + ".Background", Application.runInBackground);
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            if (!Application.isBatchMode && File.Exists(Request) && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
            {
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.isPlaying = false;
                    return;
                }
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;
                File.Delete(Request);
                try
                {
                    Begin();
                }
                catch (Exception e) { Debug.LogException(e); }
            }
            if (!SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying)
                return;
            // Codex öndeyken de Game View'ın gerçek oyuncu karesi üretebilmesi gerekir.
            Application.runInBackground = true;
            EditorApplication.QueuePlayerLoopUpdate();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            if (EditorApplication.timeSinceStartup >= diagnosticAt)
            {
                File.WriteAllText("Temp/BlockRift.GameView.status.json", "{\"playing\":true,\"paused\":" +
                    EditorApplication.isPaused.ToString().ToLowerInvariant() + ",\"requested\":" + requested.ToString().ToLowerInvariant() + "}");
                diagnosticAt = EditorApplication.timeSinceStartup + 2;
            }
            if (captureAt == 0)
                captureAt = EditorApplication.timeSinceStartup + 5;
            if (EditorApplication.timeSinceStartup < captureAt)
                return;
            const string output = "docs/Gameplay_6x6_GameView.png";
            Directory.CreateDirectory("docs");
            if (!requested)
            {
                previousImageTime = File.Exists(output) ? File.GetLastWriteTimeUtc(output) : DateTime.MinValue;
                ScreenCapture.CaptureScreenshot(Path.GetFullPath(output));
                requested = true;
                captureAt = EditorApplication.timeSinceStartup + 3;
                return;
            }
            if (!File.Exists(output) || File.GetLastWriteTimeUtc(output) <= previousImageTime)
                return;
            Directory.CreateDirectory("TestResults/ArtIntegration");
            File.WriteAllText("TestResults/ArtIntegration/gameview.json",
                "{\"source\":\"Unity Game View ScreenCapture\",\"width\":" + Screen.width + ",\"height\":" + Screen.height + ",\"playMode\":true}");
            SessionState.SetBool(Pending, false);
            Application.runInBackground = SessionState.GetBool(Pending + ".Background", false);
            Debug.Log("GAME_VIEW_CAPTURED " + Path.GetFullPath(output));
            if (Application.isBatchMode)
            {
                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += () => EditorApplication.Exit(0);
            }
        }
    }
}
