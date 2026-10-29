using System;
using System.IO;
using System.Linq;
using BlockRift.Core;
using BlockRift.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlockRift.Editor
{
    /// <summary>Onaylı mesh'leri mevcut prefab GUID'lerine bağlayan açık batch bakım komutudur; eski dosyaları silmez.</summary>
    public static class ApprovedAssetMigration
    {
        private const string Art = "Assets/_BlockRift/Art/";
        private const string Report = "TestResults/AssetMigration";
        private static Material neutral, cream;

        public static void Build()
        {
            if (!Application.isBatchMode)
                throw new InvalidOperationException("Yalnız izole batch bakım komutu.");
            Directory.CreateDirectory(Report);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            neutral = AssetDatabase.LoadAssetAtPath<Material>(Art + "Materials/Gameplay/BR_Neutral_Candy.mat");
            if (!neutral)
                neutral = AssetDatabase.LoadAssetAtPath<GameObject>(GameplayArtAssets.Prefabs + "Block_1x1.prefab").GetComponentInChildren<MeshRenderer>().sharedMaterial;
            cream = AssetDatabase.LoadAssetAtPath<Material>(Art + "Materials/Gameplay/MAT_Board_Cream.mat");
            if (!cream)
                throw new InvalidOperationException("Krem materyal bulunamadı.");
            foreach (string shape in new[] { "2x1", "3x1", "4x1", "2x2" })
                ReplacePiece("Block_" + shape, Import("Block_" + shape + "_Unified", neutral));
            string[] names = { "Blue", "Green", "Red", "Yellow", "Purple", "Orange" };
            string[] colors = { "#087FFF", "#12DA45", "#FF3838", "#FFD12A", "#9735F5", "#FF9B21" };
            for (int i = 0; i < names.Length; i++)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(Art + "Materials/Gameplay/BR_Plastic_" + names[i] + ".mat");
                if (!mat)
                    throw new InvalidOperationException("Eksik renk: " + names[i]);
                ColorUtility.TryParseHtmlString(colors[i], out var color);
                mat.SetColor("_BaseColor", color);
                mat.SetFloat("_Metallic", 0);
                mat.SetFloat("_Smoothness", .8f);
                EditorUtility.SetDirty(mat);
            }
            var catalog = AssetDatabase.LoadAssetAtPath<VisualPrefabCatalog>(GameplayArtAssets.CatalogPath);
            foreach (int size in new[] { 5, 6 })
            {
                var sand = SandMaterial(size);
                var cells = Import($"Board_{size}x{size}_Cells", cream);
                var terrain = Import($"Sand_BoardTransition_{size}x{size}", sand);
                string path = GameplayArtAssets.Prefabs + (size == 6 ? "Board_6x6_Frameless" : "Board_5x5_Mobile") + ".prefab";
                GameObject root = File.Exists(path) ? PrefabUtility.LoadPrefabContents(path) : new GameObject("Board_" + size + "x" + size + "_Mobile");
                bool loaded = File.Exists(path);
                try
                {
                    while (root.transform.childCount > 0)
                        UnityEngine.Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
                    AddModel(root, cells, "Cells");
                    AddModel(root, terrain, "Sand");
                    var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                    var data = new SerializedObject(catalog);
                    var entries = data.FindProperty("entries");
                    int index = Enumerable.Range(0, entries.arraySize).Where(i => entries.GetArrayElementAtIndex(i).FindPropertyRelative("assetId").stringValue == "Board_" + size + "x" + size).DefaultIfEmpty(-1).First();
                    if (index < 0)
                    {
                        index = entries.arraySize;
                        entries.InsertArrayElementAtIndex(index);
                    }
                    var e = entries.GetArrayElementAtIndex(index);
                    e.FindPropertyRelative("assetId").stringValue = "Board_" + size + "x" + size;
                    e.FindPropertyRelative("width").intValue = size;
                    e.FindPropertyRelative("height").intValue = size;
                    e.FindPropertyRelative("board").boolValue = true;
                    e.FindPropertyRelative("prefab").objectReferenceValue = prefab;
                    data.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(catalog);
                }
                finally { if (loaded) PrefabUtility.UnloadPrefabContents(root); else UnityEngine.Object.DestroyImmediate(root); }
            }
            AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.OpenScene(GameplaySceneSetup.ScenePath, OpenSceneMode.Single);
            var layout = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<BoardVisualLayout>()).Single();
            var framing = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameplayViewportLayout>()).Single();
            var cam = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>()).Single();
            cam.transform.rotation = Quaternion.Euler(90, 0, 0);
            cam.orthographic = true;
            var fd = new SerializedObject(framing);
            fd.FindProperty("usePerspective").boolValue = false;
            fd.ApplyModifiedPropertiesWithoutUndo();
            // Katalog seçimi yalnız tahta görselini yeniler; authored Level 1 ve PiecesRoot korunur.
            int boardSize = LevelOne.Create().Width;
            if (!catalog.TryGetBoard(boardSize, out var boardEntry))
                throw new InvalidOperationException("Tahta prefabı eksik.");
            var boardViews = layout.transform.Find("BoardVisualsRoot");
            while (boardViews.childCount > 0)
                UnityEngine.Object.DestroyImmediate(boardViews.GetChild(0).gameObject);
            var board = (GameObject)PrefabUtility.InstantiatePrefab(boardEntry.Prefab, scene);
            layout.PlaceBoardView(board.transform);
            var ld = new SerializedObject(layout);
            ld.FindProperty("activeBoardPrefab").objectReferenceValue = boardEntry.Prefab;
            ld.ApplyModifiedPropertiesWithoutUndo();
            layout.SetBoardSize(boardSize);
            framing.ApplyLayout(1080, 1920, new Rect(0, 0, 1080, 1920));
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            File.WriteAllText(Report + "/migration.txt", "Updated: 4 horizontal/square prefabs; nested vertical prefabs inherit meshes.\nBoards: real 5x5 + 6x6 cells and sand; no support panel.\nCamera: orthographic 90 degrees; palette six URP colors.\nNo asset deletion.\n");
            Debug.Log("APPROVED_ASSET_MIGRATION_COMPLETE");
        }

        private static GameObject Import(string name, Material material)
        {
            string path = GameplayArtAssets.Models + name + ".fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.globalScale = 1;
            importer.useFileUnits = true;
            importer.bakeAxisConversion = true;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.isReadable = true;
            importer.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>(true))
                foreach (var source in renderer.sharedMaterials)
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), source.name), material);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        private static void ReplacePiece(string id, GameObject model)
        {
            string path = GameplayArtAssets.Prefabs + id + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                while (root.transform.childCount > 0)
                    UnityEngine.Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
                AddModel(root, model, "Model");
                var shape = id.Substring(6).Split('x');
                if (!VisualModelChecks.ValidatePieceBounds(GameplayArtAssets.MeasureBounds(root.transform), int.Parse(shape[0]), int.Parse(shape[1]), out string issue))
                    throw new InvalidOperationException(id + ": " + issue);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        private static void AddModel(GameObject root, GameObject model, string name)
        {
            var child = (GameObject)PrefabUtility.InstantiatePrefab(model);
            child.name = name;
            child.transform.SetParent(root.transform, false);
        }
        private static Material SandMaterial(int size)
        {
            string folder = Art + "Textures/Sand/" + size + "/";
            foreach (string name in new[] { "Color", "Normal" })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(folder + "Sand_" + name + ".png");
                importer.textureType = name == "Normal" ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = name != "Normal";
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 1024;
                importer.SaveAndReimport();
            }
            string path = Art + "Materials/Gameplay/MAT_Mobile_Sand_" + size + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Metallic", 0);
            mat.SetFloat("_Smoothness", .17f);
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "Sand_Color.png"));
            mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "Sand_Normal.png"));
            mat.SetFloat("_BumpScale", .75f);
            mat.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
