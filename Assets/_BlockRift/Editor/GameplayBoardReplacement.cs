using System;
using BlockRift.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlockRift.Editor
{
    /// <summary>Yalnız yeni board prefab'ını ve onaylı perspektif sahne kadrajını bağlar; parçalara/Core'a yazmaz.</summary>
    public static class GameplayBoardReplacement
    {
        public const string ModelPath = GameplayArtIntegration.Models + "Board_6x6_Frameless.fbx";
        public const string PrefabPath = GameplayArtIntegration.Prefabs + "Board_6x6_Frameless.prefab";

        /// <summary>İzole batch bakımında mevcut sahnenin tahta görselini değiştirir, parça Transform'larını korur.</summary>
        public static void Apply()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Bu bakım girişi yalnız izole batch doğrulamasında kullanılır.");
            AssetDatabase.Refresh();
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (!importer) throw new InvalidOperationException("Yeni onaylı FBX bulunamadı.");
            importer.globalScale = 1; importer.useFileUnits = true; importer.bakeAxisConversion = true;
            importer.importAnimation = false; importer.importLights = false; importer.importCameras = false;
            importer.addCollider = false; importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.importNormals = ModelImporterNormals.Import; importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.isReadable = true; importer.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>(true))
            foreach (var source in renderer.sharedMaterials)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(GameplayArtIntegration.Materials + source.name + ".mat");
                if (!material) throw new InvalidOperationException("Onaylı URP materyali eksik: " + source.name);
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),source.name),material);
            }
            importer.SaveAndReimport();
            var wrapper = new GameObject("Board_6x6_Frameless");
            GameObject prefab;
            try
            {
                var child = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
                child.name = "Model"; child.transform.SetParent(wrapper.transform,false);
                Bounds bounds = GameplayArtIntegration.MeasureBounds(wrapper.transform);
                if (!VisualModelChecks.ValidateBoardBounds(bounds,6,out var issue)) throw new InvalidOperationException(issue);
                Debug.Log($"NEW_BOARD_BOUNDS center={bounds.center:F6} size={bounds.size:F6} min={bounds.min:F6} max={bounds.max:F6}");
                prefab = PrefabUtility.SaveAsPrefabAsset(wrapper,PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(wrapper); }
            var catalog = AssetDatabase.LoadAssetAtPath<VisualPrefabCatalog>(GameplayArtIntegration.CatalogPath);
            var data = new SerializedObject(catalog);
            var entries = data.FindProperty("entries"); bool replaced = false;
            for (int i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                if (!entry.FindPropertyRelative("board").boolValue || entry.FindPropertyRelative("width").intValue != 6) continue;
                entry.FindPropertyRelative("prefab").objectReferenceValue = prefab; replaced = true;
            }
            if (!replaced) throw new InvalidOperationException("Mevcut 6×6 katalog eşlemesi bulunamadı.");
            data.ApplyModifiedPropertiesWithoutUndo();
            var scene = EditorSceneManager.OpenScene(GameplaySceneSetup.ScenePath,OpenSceneMode.Single);
            var root = scene.GetRootGameObjects()[0].transform;
            var boardRoot = root.Find("BoardRoot");
            var pieces = boardRoot.Find("PiecesRoot");
            var positions = new Vector3[pieces.childCount];
            for (int i = 0; i < positions.Length; i++) positions[i] = pieces.GetChild(i).localPosition;
            var framing = root.GetComponentInChildren<GameplayViewportLayout>();
            var camera = root.GetComponentInChildren<Camera>();
            camera.transform.rotation = Quaternion.Euler(50,0,0);
            camera.orthographic = false; camera.fieldOfView = 32;
            var frameData = new SerializedObject(framing);
            frameData.FindProperty("usePerspective").boolValue = true;
            frameData.FindProperty("verticalFieldOfView").floatValue = 32;
            frameData.FindProperty("framingPadding").floatValue = 1.04f;
            frameData.ApplyModifiedPropertiesWithoutUndo();
            var area = root.Find("UIRoot/GameplayCanvas/SafeArea/BoardViewport").GetComponent<RectTransform>();
            area.anchorMin = new Vector2(0.04f,0.30f); area.anchorMax = new Vector2(0.96f,0.80f);
            area.offsetMin = area.offsetMax = Vector2.zero;
            framing.ApplyLayout(1080,1920,new Rect(0,0,1080,1920));
            var visualRoot = boardRoot.Find("BoardVisualsRoot");
            if (visualRoot.childCount != 1) throw new InvalidOperationException("Sahnede tek board görseli beklenir.");
            UnityEngine.Object.DestroyImmediate(visualRoot.GetChild(0).gameObject);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
            boardRoot.GetComponent<BoardVisualLayout>().PlaceBoardView(instance.transform);
            for (int i = 0; i < positions.Length; i++)
                if (pieces.GetChild(i).localPosition != positions[i]) throw new InvalidOperationException("Parça konumu değişti.");
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log($"BOARD_REPLACEMENT_COMPLETE perspective={ !camera.orthographic } euler={camera.transform.eulerAngles:F3} fov={camera.fieldOfView} position={camera.transform.position:F5}");
        }
    }
}
