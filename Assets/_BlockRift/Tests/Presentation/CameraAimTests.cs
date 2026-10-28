using System.Collections.Generic;
using BlockRift.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlockRift.Tests
{
    /// <summary>Kameranın gerçek tahta merkezine üstten baktığını ve hafif perspektif oranını sınar.</summary>
    public sealed class CameraAimTests
    {
        private Scene scene;
        private Transform root;
        [SetUp] public void Open()
        {
            scene=EditorSceneManager.OpenPreviewScene("Assets/Scenes/BlockRift_Gameplay.unity");
            root=scene.GetRootGameObjects()[0].transform;
        }
        [TearDown] public void Close()=>EditorSceneManager.ClosePreviewScene(scene);

        [TestCase(5,1080,1920)] [TestCase(6,1080,1920)]
        [TestCase(5,1080,2520)] [TestCase(6,1080,2520)]
        [TestCase(5,1536,2048)] [TestCase(6,1536,2048)]
        public void CameraLooksAtGeometryCenterFromAboveAndRetainsUiSpace(int size,int width,int height)
        {
            var board=root.Find("BoardRoot");
            var poses=new Dictionary<Transform,Matrix4x4>();
            foreach(var transform in board.GetComponentsInChildren<Transform>())poses[transform]=transform.localToWorldMatrix;
            var framing=root.GetComponentInChildren<GameplayViewportLayout>();
            var camera=root.GetComponentInChildren<Camera>();
            board.GetComponent<BoardVisualLayout>().SetBoardSize(size);
            framing.ApplyLayout(width,height,new Rect(0,0,width,height));
            Vector3 target=framing.FocusPoint;
            Assert.That(camera.orthographic,Is.False);
            Assert.That(camera.transform.position.y,Is.GreaterThan(target.y));
            Assert.That(camera.transform.forward.y,Is.LessThan(0));
            Assert.That(Vector3.Dot(camera.transform.forward,(target-camera.transform.position).normalized),Is.GreaterThan(.99999f));
            Assert.That(Quaternion.Angle(camera.transform.rotation,Quaternion.LookRotation(target-camera.transform.position,Vector3.up)),Is.LessThan(.001));
            Assert.That(camera.transform.eulerAngles.x,Is.InRange(70f,80f));
            Assert.That(Mathf.DeltaAngle(camera.transform.eulerAngles.y,0),Is.EqualTo(0).Within(.001));
            Assert.That(Mathf.DeltaAngle(camera.transform.eulerAngles.z,0),Is.EqualTo(0).Within(.001));
            var actual=BlockRift.Editor.GameplayArtIntegration.MeasureBounds(board.Find("BoardVisualsRoot").GetChild(0));
            Assert.That(Vector3.Distance(target,board.TransformPoint(actual.center)),Is.LessThan(.00001));
            float half=size*.5f;
            Vector3 frontA=camera.WorldToViewportPoint(board.TransformPoint(new Vector3(-half,0,-half)));
            Vector3 frontB=camera.WorldToViewportPoint(board.TransformPoint(new Vector3(half,0,-half)));
            Vector3 backA=camera.WorldToViewportPoint(board.TransformPoint(new Vector3(-half,0,half)));
            Vector3 backB=camera.WorldToViewportPoint(board.TransformPoint(new Vector3(half,0,half)));
            float ratio=(backB.x-backA.x)/(frontB.x-frontA.x);
            Assert.That(ratio,Is.InRange(.94f,1.01f),"Üst/alt kenar farkı hafif perspective sınırında kalmalı");
            foreach(var item in poses)Assert.That(item.Key.localToWorldMatrix,Is.EqualTo(item.Value));
        }

        [Test] public void ActualSixBySixBoardOccupies85To90PercentAtReferenceResolution()
        {
            var framing=root.GetComponentInChildren<GameplayViewportLayout>();
            framing.ApplyLayout(1080,1920,new Rect(0,0,1080,1920));
            var camera=root.GetComponentInChildren<Camera>();
            float min=1,max=0,minY=1,maxY=0;
            foreach(var filter in root.Find("BoardRoot/BoardVisualsRoot").GetComponentsInChildren<MeshFilter>())
            foreach(var vertex in filter.sharedMesh.vertices)
            {
                var point=camera.WorldToViewportPoint(filter.transform.TransformPoint(vertex));
                min=Mathf.Min(min,point.x);max=Mathf.Max(max,point.x);minY=Mathf.Min(minY,point.y);maxY=Mathf.Max(maxY,point.y);
            }
            Assert.That(max-min,Is.InRange(.85f,.90f));
            var safe=root.Find("UIRoot/GameplayCanvas/SafeArea");
            Assert.That(maxY,Is.LessThan(safe.Find("TopHUD").GetComponent<RectTransform>().anchorMin.y));
            Assert.That(minY,Is.GreaterThan(safe.Find("BottomControls").GetComponent<RectTransform>().anchorMax.y));
            Assert.That(camera.WorldToViewportPoint(framing.FocusPoint).x,Is.EqualTo(.5f).Within(.00001));
        }

        [Test] public void OffAxisFramingStillCoversTheExistingBackground()
        {
            var framing=root.GetComponentInChildren<GameplayViewportLayout>();
            framing.ApplyLayout(1080,1920,new Rect(0,0,1080,1920));
            var camera=root.GetComponentInChildren<Camera>();
            var background=root.Find("Background");
            background.GetComponent<BackgroundSpriteFitter>().SendMessage("LateUpdate");
            var plane=new Plane(background.forward,background.position);
            for(int x=0;x<=1;x++)for(int y=0;y<=1;y++)
            {
                var ray=camera.ViewportPointToRay(new Vector3(x,y,0));
                Assert.That(plane.Raycast(ray,out float depth),Is.True);
                var local=background.InverseTransformPoint(ray.GetPoint(depth));
                Assert.That(Mathf.Abs(local.x),Is.LessThan(.50001));Assert.That(Mathf.Abs(local.y),Is.LessThan(.50001));
            }
        }
    }
}
