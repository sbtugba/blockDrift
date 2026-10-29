using System.Collections.Generic;
using BlockRift.Core;
using BlockRift.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlockRift.Tests
{
    /// <summary>Yeni board'un birim grid/pivot sözleşmesini ve iki projeksiyonda gerçek kadrajı doğrular.</summary>
    public sealed class BoardPerspectiveTests
    {
        private Scene scene;
        private Transform root;
        [SetUp] public void OpenScene()
        {
            scene=EditorSceneManager.OpenPreviewScene("Assets/Scenes/BlockRift_Gameplay.unity");
            root=scene.GetRootGameObjects()[0].transform;
        }
        [TearDown] public void CloseScene() => EditorSceneManager.ClosePreviewScene(scene);

        [TestCase(true,5,1080,1920,30f)] [TestCase(true,6,1080,1920,35f)]
        [TestCase(true,5,1080,2520,35f)] [TestCase(true,6,1080,2520,30f)]
        [TestCase(true,5,1536,2048,30f)] [TestCase(true,6,1536,2048,35f)]
        [TestCase(true,5,1920,1080,35f)] [TestCase(true,6,1920,1080,30f)]
        [TestCase(false,5,1080,1920,32f)] [TestCase(false,6,1080,1920,32f)]
        [TestCase(false,5,1080,2520,32f)] [TestCase(false,6,1536,2048,32f)]
        public void BothProjectionsKeepBoundsAndBackgroundVisible(bool perspective,int size,int width,int height,float fov)
        {
            var framing=root.GetComponentInChildren<GameplayViewportLayout>();
            var settings=new SerializedObject(framing);
            settings.FindProperty("usePerspective").boolValue=perspective;
            settings.FindProperty("verticalFieldOfView").floatValue=fov;
            settings.ApplyModifiedPropertiesWithoutUndo();
            var camera=root.GetComponentInChildren<Camera>();
            camera.transform.rotation=Quaternion.Euler(perspective?78:90,0,0);
            // Projection seçimi adaptör kontrolünden önce uygulanır.
            framing.ApplyLayout(width,height,new Rect(0,0,width,height));
            var layout=root.Find("BoardRoot").GetComponent<BoardVisualLayout>();
            layout.SetBoardSize(size);
            framing.ApplyLayout(width,height,new Rect(width*.03f,height*.04f,width*.94f,height*.91f));
            Assert.That(camera.orthographic,Is.EqualTo(!perspective));
            if(perspective)Assert.That(camera.fieldOfView,Is.EqualTo(fov));
            var safe=root.Find("UIRoot/GameplayCanvas/SafeArea").GetComponent<RectTransform>();
            var area=safe.Find("BoardViewport").GetComponent<RectTransform>();
            Vector2 span=safe.anchorMax-safe.anchorMin;
            Vector2 min=safe.anchorMin+Vector2.Scale(area.anchorMin,span),max=safe.anchorMin+Vector2.Scale(area.anchorMax,span);
            var bounds=layout.PresentationBounds;
            for(int i=0;i<8;i++)
            {
                var corner=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                var projected=camera.WorldToViewportPoint(layout.transform.TransformPoint(corner));
                Assert.That(projected.x,Is.InRange(min.x,max.x));Assert.That(projected.y,Is.InRange(min.y,max.y));
                Assert.That(projected.z,Is.InRange(camera.nearClipPlane,camera.farClipPlane));
            }
            Assert.That(Vector3.Distance(layout.CellToWorld(new Cell(0,0)),layout.CellToWorld(new Cell(1,0))),Is.EqualTo(1).Within(.00001));
            var background=root.Find("Background");
            background.GetComponent<BackgroundSpriteFitter>().SendMessage("LateUpdate");
            var plane=new Plane(background.forward,background.position);
            for(int x=0;x<=1;x++)for(int y=0;y<=1;y++)
            {
                var ray=camera.ViewportPointToRay(new Vector3(x,y,0));
                Assert.That(plane.Raycast(ray,out float depth),Is.True);
                var local=background.InverseTransformPoint(ray.GetPoint(depth));
                Assert.That(Mathf.Abs(local.x),Is.LessThanOrEqualTo(.50001f));
                Assert.That(Mathf.Abs(local.y),Is.LessThanOrEqualTo(.50001f));
            }
        }

        [Test] public void ActiveBoardHasIndependentCellsAndSandWithoutSupportPanel()
        {
            var layout=root.Find("BoardRoot").GetComponent<BoardVisualLayout>();
            Assert.That(layout.VisualCatalog.TryGetBoard(6,out var entry),Is.True);
            Assert.That(AssetDatabase.GetAssetPath(entry.Prefab),Does.EndWith("Board_6x6_Frameless.prefab"));
            var board=root.Find("BoardRoot/BoardVisualsRoot").GetChild(0);
            Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(board.gameObject),Is.SameAs(entry.Prefab));
            Assert.That(board.localPosition,Is.EqualTo(Vector3.zero));Assert.That(board.localScale,Is.EqualTo(Vector3.one));
            var meshes=board.GetComponentsInChildren<MeshFilter>();Assert.That(meshes.Length,Is.EqualTo(2));
            int triangles=0;
            foreach(var mesh in meshes)triangles+=mesh.sharedMesh.triangles.Length/3;
            Assert.That(triangles,Is.EqualTo(16128+3600));
            var bounds=BlockRift.Editor.GameplayArtAssets.MeasureBounds(board.Find("Cells"));
            Assert.That(bounds.size.x,Is.EqualTo(5.97f).Within(.00001));Assert.That(bounds.size.z,Is.EqualTo(5.97f).Within(.00001));
            Assert.That(bounds.min.y,Is.EqualTo(-.061f).Within(.00001));Assert.That(bounds.max.y,Is.EqualTo(-.001f).Within(.00001));
        }

        [Test] public void SceneUsesApprovedTopDownAngleAndReservesUpperAndLowerUi()
        {
            var camera=root.GetComponentInChildren<Camera>();
            Assert.That(camera.orthographic,Is.True);
            Assert.That(Quaternion.Angle(camera.transform.rotation,Quaternion.Euler(90,0,0)),Is.LessThan(.001));
            Assert.That(camera.fieldOfView,Is.InRange(15f,35f));
            var safe=root.Find("UIRoot/GameplayCanvas/SafeArea");
            var area=safe.Find("BoardViewport").GetComponent<RectTransform>();
            var top=safe.Find("TopHUD").GetComponent<RectTransform>();
            var bottom=safe.Find("BottomControls").GetComponent<RectTransform>();
            Assert.That(area.anchorMax.y,Is.LessThanOrEqualTo(top.anchorMin.y));
            Assert.That(area.anchorMin.y,Is.GreaterThanOrEqualTo(bottom.anchorMax.y));
            Assert.That((area.anchorMin.y+area.anchorMax.y)*.5f,Is.GreaterThan(.5f));
        }

        [Test] public void NewBoardRetainsEveryVerifiedCellVertexAtTheSameGridCoordinate()
        {
            var previous=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_BlockRift/Art/Prefabs/Gameplay/Board_6x6.prefab");
            var current=root.Find("BoardRoot/BoardVisualsRoot").GetChild(0);
            var oldPoints=CellPositions(previous.transform);
            var newPoints=CellPositions(current);
            Assert.That(newPoints.Count,Is.GreaterThan(1000));
            CollectionAssert.AreEquivalent(oldPoints,newPoints,"Yeni hücre geometrisi ve merkezleri önceki doğrulanmış grid ile aynı olmalı.");
        }

        private static HashSet<Vector3Int> CellPositions(Transform board)
        {
            var result=new HashSet<Vector3Int>();
            foreach(var filter in board.GetComponentsInChildren<MeshFilter>())
            {
                if(!filter.name.Contains("Cells"))continue;
                var matrix=board.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                foreach(var vertex in filter.sharedMesh.vertices)
                {
                    var point=matrix.MultiplyPoint3x4(vertex);
                    result.Add(new Vector3Int(Mathf.RoundToInt(point.x*100000),Mathf.RoundToInt(point.y*100000),Mathf.RoundToInt(point.z*100000)));
                }
            }
            return result;
        }

        [TestCase("Block_1x1",1,1,5,2)] [TestCase("Block_2x1",2,1,3,2)]
        [TestCase("Block_3x1",3,1,0,2)] [TestCase("Block_4x1",4,1,0,0)]
        [TestCase("Block_2x2",2,2,0,4)] [TestCase("Obstacle_1x1_Stone",1,1,4,0)]
        [TestCase("Special_1x1_Star",1,1,5,0)]
        public void ExistingPieceTransformsAndBasePivotsArePreserved(string id,int width,int height,int x,int y)
        {
            var layout=root.Find("BoardRoot").GetComponent<BoardVisualLayout>();
            // Level 1 bütün katalog şekillerini sahnede tutmak zorunda değildir.
            // Önceki yedi asset/pivot senaryosu gerçek prefab'ı preview sahnesine yerleştirerek korunur.
            Assert.That(layout.VisualCatalog.TryGetPiece(id,out var entry),Is.True);
            var piece=((GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(entry.Prefab,scene)).transform;
            layout.PlacePieceView(piece,new PieceDefinition(id,PieceKind.Obstacle,width,height,new Cell(x,y)),new Cell(x,y));
            Assert.That(piece.localPosition,Is.EqualTo(layout.Grid.PieceBaseCenter(new Cell(x,y),width,height)));
            Assert.That(piece.localScale,Is.EqualTo(Vector3.one));
            Assert.That(Quaternion.Angle(piece.localRotation,Quaternion.identity),Is.LessThan(.001));
            var bounds=BlockRift.Editor.GameplayArtAssets.MeasureBounds(piece);
            Assert.That(bounds.min.y,Is.EqualTo(0).Within(.00001));
        }
    }
}
