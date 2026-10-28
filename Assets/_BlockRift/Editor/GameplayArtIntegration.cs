using System;
using System.Collections.Generic;
using System.IO;
using BlockRift.Core;
using BlockRift.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BlockRift.Editor
{
    /// <summary>Onaylı FBX'leri URP prefab'larına bağlar; tek statik sanat test dizilimi üretir, level generator değildir.</summary>
    public static class GameplayArtIntegration
    {
        public const string Models = "Assets/_BlockRift/Art/Models/Gameplay/";
        public const string Prefabs = "Assets/_BlockRift/Art/Prefabs/Gameplay/";
        public const string Materials = "Assets/_BlockRift/Art/Materials/Gameplay/";
        public const string CatalogPath = "Assets/_BlockRift/Content/GameplayVisualCatalog.asset";
        private static readonly string[] Ids = { "Block_1x1", "Block_2x1", "Block_3x1", "Block_4x1", "Block_2x2", "Special_1x1_Star", "Obstacle_1x1_Stone", "Board_6x6" };
        private static readonly int[] Widths = { 1, 2, 3, 4, 2, 1, 1, 6 };
        private static readonly int[] Heights = { 1, 1, 1, 1, 2, 1, 1, 6 };
        // Blender'ın 205 konumlu fit denetiminden gelen görsel test dizilimi; oynanabilir bölüm iddiası yoktur.
        private static readonly Cell[] Origins = { new Cell(5, 2), new Cell(3, 2), new Cell(0, 2), new Cell(0, 0), new Cell(0, 4), new Cell(5, 0), new Cell(4, 0) };

        [Serializable] private sealed class Manifest { public MaterialData[] materials; }
        [Serializable] private sealed class MaterialData { public string name; public float[] linear_color; public float metallic, roughness; }

        /// <summary>Mevcut Gameplay sahnesini yalnız açık batch komutuyla günceller; kaynak .blend dosyalarını değiştirmez.</summary>
        public static void Build()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("İzole batch bakım girişi.");
            Directory.CreateDirectory(Prefabs); Directory.CreateDirectory(Materials);
            Directory.CreateDirectory("Assets/_BlockRift/Content");
            AssetDatabase.Refresh();
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText("ArtProduction/UnityIntegration/export_manifest.json"));
            var materials = CreateMaterials(manifest);
            var prefabs = new List<GameObject>();
            for (int i = 0; i < Ids.Length; i++)
            {
                string path = Models + Ids[i] + ".fbx";
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (!importer) throw new InvalidOperationException("Eksik gerçek FBX: " + path);
                importer.globalScale = 1; importer.useFileUnits = true; importer.bakeAxisConversion = true;
                importer.importAnimation = false; importer.importCameras = false; importer.importLights = false;
                importer.addCollider = false; importer.meshCompression = ModelImporterMeshCompression.Off;
                importer.importNormals = ModelImporterNormals.Import;
                importer.importTangents = ModelImporterTangents.CalculateMikk;
                importer.isReadable = true;
                importer.SaveAndReimport();
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>(true))
                    foreach (var source in renderer.sharedMaterials)
                    {
                        string key = source.name;
                        if (!materials.TryGetValue(key, out Material material)) throw new InvalidOperationException("Eksik URP materyal: " + key);
                        importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), key), material);
                    }
                importer.SaveAndReimport();
                model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var wrapper = new GameObject(Ids[i]);
                try
                {
                    var child = (GameObject)PrefabUtility.InstantiatePrefab(model);
                    child.name = "Model"; child.transform.SetParent(wrapper.transform, false);
                    Bounds bounds = MeasureBounds(wrapper.transform);
                    bool valid = i == 7 ? VisualModelChecks.ValidateBoardBounds(bounds, 6, out var issue)
                        : VisualModelChecks.ValidatePieceBounds(bounds, Widths[i], Heights[i], out issue);
                    Debug.Log($"ART_BOUNDS {Ids[i]} center={bounds.center:F6} size={bounds.size:F6} min={bounds.min:F6} max={bounds.max:F6}");
                    if (!valid) throw new InvalidOperationException(Ids[i] + ": " + issue);
                    var prefab = PrefabUtility.SaveAsPrefabAsset(wrapper, Prefabs + Ids[i] + ".prefab");
                    prefabs.Add(prefab);
                }
                finally { UnityEngine.Object.DestroyImmediate(wrapper); }
            }
            var catalog = AssetDatabase.LoadAssetAtPath<VisualPrefabCatalog>(CatalogPath);
            if (!catalog) { catalog = ScriptableObject.CreateInstance<VisualPrefabCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
            var serialized = new SerializedObject(catalog);
            var entries = serialized.FindProperty("entries"); entries.arraySize = Ids.Length;
            for (int i = 0; i < Ids.Length; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("assetId").stringValue = Ids[i];
                entry.FindPropertyRelative("width").intValue = Widths[i]; entry.FindPropertyRelative("height").intValue = Heights[i];
                entry.FindPropertyRelative("board").boolValue = i == 7; entry.FindPropertyRelative("prefab").objectReferenceValue = prefabs[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var scene = EditorSceneManager.OpenScene(GameplaySceneSetup.ScenePath, OpenSceneMode.Single);
            var root = scene.GetRootGameObjects()[0].transform;
            var layout = root.Find("BoardRoot").GetComponent<BoardVisualLayout>();
            if (root.Find("BoardRoot/BoardVisualsRoot").childCount != 0 || root.Find("BoardRoot/PiecesRoot").childCount != 0)
                throw new InvalidOperationException("Mevcut modellerin üzerine otomatik yazılmaz.");
            var layoutData = new SerializedObject(layout);
            layoutData.FindProperty("visualCatalog").objectReferenceValue = catalog; layoutData.ApplyModifiedPropertiesWithoutUndo();
            layout.SetBoardSize(6);
            var board = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[7], scene); layout.PlaceBoardView(board.transform);
            for (int i = 0; i < 7; i++)
            {
                var view = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[i], scene);
                var definition = new PieceDefinition(Ids[i], i == 5 ? PieceKind.Target : PieceKind.Obstacle, Widths[i], Heights[i], Origins[i]);
                layout.PlacePieceView(view.transform, definition, Origins[i]);
            }
            root.GetComponentInChildren<GameplayViewportLayout>().ApplyLayout(1080, 1920, new Rect(0, 0, 1080, 1920));
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("ART_INTEGRATION_COMPLETE: 1 real board, 7 real pieces. Imported normals preserved; shading REVIEW REQUIRED.");
        }

        private static Dictionary<string, Material> CreateMaterials(Manifest manifest)
        {
            var result = new Dictionary<string, Material>(StringComparer.Ordinal);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) throw new InvalidOperationException("URP Lit bulunamadı.");
            foreach (var source in manifest.materials)
            {
                string path = Materials + source.name + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!mat) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
                mat.shader = shader;
                // Blender shader renkleri lineerdir; Unity materyal renk alanı sRGB gösterimle saklanır.
                var c = source.linear_color; mat.SetColor("_BaseColor", new Color(c[0], c[1], c[2], c[3]).gamma);
                mat.SetFloat("_Metallic", source.metallic); mat.SetFloat("_Smoothness", 1 - source.roughness);
                if (source.name == "BR_Stone_Final_URP")
                {
                    const string tex = "Assets/_BlockRift/Art/Textures/GameplayPreview/Stone_1x1_";
                    var surface = AssetImporter.GetAtPath(tex + "Surface.png") as TextureImporter;
                    var normal = AssetImporter.GetAtPath(tex + "Normal.png") as TextureImporter;
                    if (!surface || !normal) throw new InvalidOperationException("Onaylı taş haritaları eksik.");
                    surface.textureType = TextureImporterType.Default; surface.textureShape = TextureImporterShape.Texture2D;
                    surface.sRGBTexture = false; surface.SaveAndReimport();
                    normal.textureType = TextureImporterType.NormalMap; normal.textureShape = TextureImporterShape.Texture2D;
                    normal.sRGBTexture = false; normal.SaveAndReimport();
                    var baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(tex + "Surface.png");
                    var normalMap = AssetDatabase.LoadAssetAtPath<Texture2D>(tex + "Normal.png");
                    if (!baseMap || !normalMap) throw new InvalidOperationException("Taş haritalarının Texture2D import ayarları geçersiz.");
                    mat.SetTexture("_BaseMap", baseMap);
                    mat.SetTexture("_BumpMap", normalMap);
                    mat.SetFloat("_BumpScale", 0.85f); mat.EnableKeyword("_NORMALMAP");
                }
                EditorUtility.SetDirty(mat); result.Add(source.name, mat);
            }
            return result;
        }

        /// <summary>Grafik destekli importta taş haritalarını kesin olarak bağlar; sessiz null texture kabul etmez.</summary>
        public static void FinishMaterials()
        {
            const string tex = "Assets/_BlockRift/Art/Textures/GameplayPreview/";
            foreach (string file in new[] { "Stone_1x1_Surface.png", "Stone_1x1_Normal.png" })
            {
                AssetDatabase.ImportAsset(tex + file, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(tex + file)) Debug.Log("TEXTURE_DIAGNOSTIC " + file + " " + asset.GetType().Name + " " + asset.name);
            }
            CreateMaterials(JsonUtility.FromJson<Manifest>(File.ReadAllText("ArtProduction/UnityIntegration/export_manifest.json")));
            AssetDatabase.SaveAssets();
        }

        /// <summary>Gerçek mesh köşelerini wrapper yerel uzayında ölçer; imported rotation/scale'i hesaba katar.</summary>
        public static Bounds MeasureBounds(Transform wrapper)
        {
            bool found = false; Bounds bounds = default;
            foreach (var filter in wrapper.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!filter.sharedMesh) throw new InvalidOperationException("Eksik mesh.");
                Matrix4x4 matrix = wrapper.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                foreach (Vector3 vertex in filter.sharedMesh.vertices)
                {
                    Vector3 point = matrix.MultiplyPoint3x4(vertex);
                    if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; } else bounds.Encapsulate(point);
                }
            }
            if (!found) throw new InvalidOperationException("Gerçek mesh bulunamadı.");
            return bounds;
        }
    }
}
