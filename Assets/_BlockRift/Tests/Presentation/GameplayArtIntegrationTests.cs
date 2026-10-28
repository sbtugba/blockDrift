using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BlockRift.Core;
using BlockRift.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlockRift.Tests
{
    /// <summary>Gerçek FBX/prefab/material verisini ve tek sanat test tahtasının yerleşimini denetler.</summary>
    public sealed class GameplayArtIntegrationTests
    {
        private const string Art = "Assets/_BlockRift/Art/";
        private Scene scene;
        private Transform root;

        [SetUp] public void OpenScene()
        {
            scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/BlockRift_Gameplay.unity");
            root = scene.GetRootGameObjects()[0].transform;
        }
        [TearDown] public void CloseScene() => EditorSceneManager.ClosePreviewScene(scene);

        [TestCase("Block_1x1",1,1)] [TestCase("Block_2x1",2,1)]
        [TestCase("Block_3x1",3,1)] [TestCase("Block_4x1",4,1)]
        [TestCase("Block_2x2",2,2)] [TestCase("Special_1x1_Star",1,1)]
        [TestCase("Obstacle_1x1_Stone",1,1)] [TestCase("Board_6x6",6,6)]
        public void RealPrefabsHaveExpectedPivotsMetricsAndUrpMaterials(string id, int width, int height)
        {
            string modelId = id == "Board_6x6" ? "Board_6x6_Frameless" : id;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "Prefabs/Gameplay/" + modelId + ".prefab");
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.transform.localScale, Is.EqualTo(Vector3.one));
            Bounds bounds = BlockRift.Editor.GameplayArtIntegration.MeasureBounds(prefab.transform);
            bool valid = id == "Board_6x6" ? VisualModelChecks.ValidateBoardBounds(bounds, 6, out var issue)
                : VisualModelChecks.ValidatePieceBounds(bounds, width, height, out issue);
            Assert.That(valid, Is.True, issue);
            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                Assert.That(filter.sharedMesh.vertexCount, Is.GreaterThan(100));
                Assert.That(AssetDatabase.GetAssetPath(filter.sharedMesh), Does.EndWith("/Gameplay/" + modelId + ".fbx"));
                Assert.That(filter.sharedMesh.normals.Length, Is.EqualTo(filter.sharedMesh.vertexCount));
                Assert.That(filter.sharedMesh.tangents.Length, Is.EqualTo(filter.sharedMesh.vertexCount));
                foreach (var uv in filter.sharedMesh.uv) Assert.That(float.IsNaN(uv.x) || float.IsNaN(uv.y), Is.False);
            }
            foreach (var renderer in prefab.GetComponentsInChildren<MeshRenderer>(true))
            foreach (var material in renderer.sharedMaterials)
            {
                Assert.That(material, Is.Not.Null);
                Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));
                Assert.That(AssetDatabase.GetAssetPath(material), Does.StartWith(Art + "Materials/Gameplay/"));
            }
            var importer = (ModelImporter)AssetImporter.GetAtPath(Art + "Models/Gameplay/" + modelId + ".fbx");
            Assert.That(importer.globalScale, Is.EqualTo(1));
            Assert.That(importer.importNormals, Is.EqualTo(ModelImporterNormals.Import));
            Assert.That(importer.addCollider, Is.False);
        }

        [Test] public void CatalogHasOnlyRequestedAssetsAndNoFakeFiveByFiveBoard()
        {
            var layout = root.Find("BoardRoot").GetComponent<BoardVisualLayout>();
            Assert.That(layout.VisualCatalog, Is.Not.Null);
            Assert.That(layout.VisualCatalog.Entries.Count, Is.EqualTo(8));
            Assert.That(layout.VisualCatalog.TryGetBoard(6, out _), Is.True);
            Assert.That(layout.VisualCatalog.TryGetBoard(5, out _), Is.False);
            Assert.That(layout.VisualCatalog.TryGetPiece("missing-model", out _), Is.False);
            Assert.That(new BoardGridLayout(5).CellCenter(new Cell(0, 0)), Is.EqualTo(new Vector3(-2,0,-2)));
        }

        [Test] public void AuthoredTestLayoutHasNoCellOverlapAndActualMeshVolumesAreSeparated()
        {
            var boardRoot = root.Find("BoardRoot");
            var layout = boardRoot.GetComponent<BoardVisualLayout>();
            var pieces = boardRoot.Find("PiecesRoot");
            var board = boardRoot.Find("BoardVisualsRoot").GetChild(0);
            Bounds boardBounds = BlockRift.Editor.GameplayArtIntegration.MeasureBounds(board);
            Assert.That(boardBounds.max.y, Is.EqualTo(-0.001f).Within(0.00001));
            var occupied = new HashSet<Cell>(); var allBounds = new List<Bounds>();
            foreach (Transform view in pieces)
            {
                Assert.That(layout.VisualCatalog.TryGetPiece(view.name, out var entry), Is.True, view.name);
                // Bu ters hesap yalnız test beklentisidir; runtime input/world-grid sistemi değildir.
                int x = Mathf.RoundToInt(view.localPosition.x + 3 - entry.Width * 0.5f);
                int y = Mathf.RoundToInt(view.localPosition.z + 3 - entry.Height * 0.5f);
                Assert.That(view.position, Is.EqualTo(layout.PieceToWorld(new PieceDefinition(view.name,
                    PieceKind.Obstacle, entry.Width, entry.Height, new Cell(x,y)), new Cell(x,y))));
                for (int py = 0; py < entry.Height; py++) for (int px = 0; px < entry.Width; px++)
                { Assert.That(x+px, Is.InRange(0,5)); Assert.That(y+py, Is.InRange(0,5)); Assert.That(occupied.Add(new Cell(x+px,y+py)), Is.True); }
                Bounds local = BlockRift.Editor.GameplayArtIntegration.MeasureBounds(view);
                var bounds = new Bounds(local.center + view.localPosition, local.size);
                Assert.That(bounds.min.y - boardBounds.max.y, Is.GreaterThan(0.0009f));
                Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(-3.00001f));
                Assert.That(bounds.max.x, Is.LessThanOrEqualTo(3.00001f));
                Assert.That(bounds.min.z, Is.GreaterThanOrEqualTo(-3.00001f));
                Assert.That(bounds.max.z, Is.LessThanOrEqualTo(3.00001f));
                foreach (Bounds other in allBounds)
                {
                    float overlapX = Mathf.Min(bounds.max.x,other.max.x)-Mathf.Max(bounds.min.x,other.min.x);
                    float overlapZ = Mathf.Min(bounds.max.z,other.max.z)-Mathf.Max(bounds.min.z,other.min.z);
                    Assert.That(overlapX <= 0.00001f || overlapZ <= 0.00001f, Is.True, "Pozitif hacimsel XZ çakışması");
                }
                allBounds.Add(bounds);
                Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(view.gameObject), Is.Not.Null);
            }
            var level=LevelOne.Create();
            Assert.That(pieces.childCount, Is.EqualTo(level.Pieces.Count+level.FixedCells.Count));
            Assert.That(occupied.Count, Is.EqualTo(level.Pieces.Sum(p=>p.Width*p.Height)+level.FixedCells.Count));
        }

        [Test] public void StoneUsesApprovedLinearSurfaceAndNormalMapSettings()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(Art + "Materials/Gameplay/BR_Stone_Final_URP.mat");
            Assert.That(mat.GetFloat("_Smoothness"), Is.EqualTo(0.13f).Within(0.00001));
            Assert.That(mat.GetFloat("_BumpScale"), Is.EqualTo(0.85f));
            Assert.That(mat.IsKeywordEnabled("_NORMALMAP"), Is.True);
            var normal = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(mat.GetTexture("_BumpMap")));
            var surface = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(mat.GetTexture("_BaseMap")));
            Assert.That(normal.textureType, Is.EqualTo(TextureImporterType.NormalMap));
            Assert.That(normal.sRGBTexture, Is.False); Assert.That(surface.sRGBTexture, Is.False);
        }

        [TestCase(1080,1920)] [TestCase(1080,2400)] [TestCase(1080,2520)] [TestCase(1536,2048)]
        public void ActualRendererBoundsStayInsideSafeBoardViewport(int width, int height)
        {
            var framing = root.GetComponentInChildren<GameplayViewportLayout>();
            framing.ApplyLayout(width,height,new Rect(width*0.02f,height*0.04f,width*0.95f,height*0.91f));
            var camera = root.GetComponentInChildren<Camera>();
            var safe = root.Find("UIRoot/GameplayCanvas/SafeArea").GetComponent<RectTransform>();
            var area = safe.Find("BoardViewport").GetComponent<RectTransform>();
            Vector2 span=safe.anchorMax-safe.anchorMin;
            Vector2 min=safe.anchorMin+Vector2.Scale(area.anchorMin,span), max=safe.anchorMin+Vector2.Scale(area.anchorMax,span);
            foreach (var renderer in root.Find("BoardRoot").GetComponentsInChildren<MeshRenderer>())
            {
                var bounds=renderer.bounds;
                for(int i=0;i<8;i++)
                {
                    var point=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    var projected=camera.WorldToViewportPoint(point);
                    Assert.That(projected.x,Is.InRange(min.x,max.x)); Assert.That(projected.y,Is.InRange(min.y,max.y));
                    Assert.That(projected.z,Is.InRange(camera.nearClipPlane,camera.farClipPlane));
                }
            }
        }
    }
}
