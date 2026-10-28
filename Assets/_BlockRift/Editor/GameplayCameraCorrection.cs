using System;
using System.Collections.Generic;
using System.IO;
using BlockRift.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlockRift.Editor
{
    /// <summary>Mevcut model/prefab verisini değiştirmeden merkez hedefli üstten kamera kadrajını kaydeder.</summary>
    public static class GameplayCameraCorrection
    {
        [Serializable] private sealed class Metrics
        {
            public Vector3 position, forward, euler, target;
            public float fov, distance, aimDot, boardWidthPercent, artWidthPercent, topToBottomWidthRatio;
        }

        /// <summary>İzole batch doğrulaması için yalnız kamera ve BoardViewport yerleşimini düzenler.</summary>
        public static void Apply()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Bu bakım girişi yalnız izole batch doğrulaması içindir.");
            var scene=EditorSceneManager.OpenScene(GameplaySceneSetup.ScenePath,OpenSceneMode.Single);
            var root=scene.GetRootGameObjects()[0].transform;
            var board=root.Find("BoardRoot");
            var poses=new Dictionary<Transform,Matrix4x4>();
            foreach(var item in board.GetComponentsInChildren<Transform>(true))poses[item]=item.localToWorldMatrix;
            var framing=root.GetComponentInChildren<GameplayViewportLayout>();
            var camera=root.GetComponentInChildren<Camera>();
            camera.transform.rotation=Quaternion.Euler(78,0,0);
            var data=new SerializedObject(framing);
            data.FindProperty("focusRoot").objectReferenceValue=board.Find("BoardVisualsRoot");
            data.FindProperty("usePerspective").boolValue=true;
            data.FindProperty("verticalFieldOfView").floatValue=22;
            data.FindProperty("framingPadding").floatValue=1.02f;
            data.ApplyModifiedPropertiesWithoutUndo();
            var area=root.Find("UIRoot/GameplayCanvas/SafeArea/BoardViewport").GetComponent<RectTransform>();
            area.anchorMin=new Vector2(.01f,.26f);area.anchorMax=new Vector2(.99f,.82f);
            area.offsetMin=area.offsetMax=Vector2.zero;
            framing.ApplyLayout(1080,1920,new Rect(0,0,1080,1920));
            var target=framing.FocusPoint;
            var metrics=new Metrics {position=camera.transform.position,forward=camera.transform.forward,euler=camera.transform.eulerAngles,
                target=target,fov=camera.fieldOfView,distance=Vector3.Distance(camera.transform.position,target),
                aimDot=Vector3.Dot(camera.transform.forward,(target-camera.transform.position).normalized),
                boardWidthPercent=ProjectedWidth(camera,board.Find("BoardVisualsRoot"))*100,
                artWidthPercent=ProjectedWidth(camera,board)*100};
            Vector3 a=camera.WorldToViewportPoint(board.TransformPoint(new Vector3(-3,0,-3)));
            Vector3 b=camera.WorldToViewportPoint(board.TransformPoint(new Vector3(3,0,-3)));
            Vector3 c=camera.WorldToViewportPoint(board.TransformPoint(new Vector3(-3,0,3)));
            Vector3 d=camera.WorldToViewportPoint(board.TransformPoint(new Vector3(3,0,3)));
            metrics.topToBottomWidthRatio=(d.x-c.x)/(b.x-a.x);
            if(metrics.aimDot<.99999f||camera.transform.position.y<=target.y||camera.transform.forward.y>=0)
                throw new InvalidOperationException("Kamera tahtaya üstten/merkezden bakmıyor.");
            if(metrics.boardWidthPercent<85||metrics.boardWidthPercent>90)
                throw new InvalidOperationException("Hedef 1080×1920 tahta genişliği %85–90 aralığında değil: "+metrics.boardWidthPercent);
            foreach(var item in poses)
                if(item.Key.localToWorldMatrix!=item.Value)throw new InvalidOperationException("Model Transform'u değişti: "+item.Key.name);
            root.Find("Background").GetComponent<BackgroundSpriteFitter>().SendMessage("LateUpdate");
            Directory.CreateDirectory("TestResults/CameraAim");
            File.WriteAllText("TestResults/CameraAim/metrics.json",JsonUtility.ToJson(metrics,true));
            EditorSceneManager.SaveScene(scene);
            Debug.Log("CAMERA_CORRECTION "+JsonUtility.ToJson(metrics));
        }

        private static float ProjectedWidth(Camera camera,Transform root)
        {
            float min=float.PositiveInfinity,max=float.NegativeInfinity;
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
            foreach(var vertex in filter.sharedMesh.vertices)
            {
                var point=camera.WorldToViewportPoint(filter.transform.TransformPoint(vertex));
                min=Mathf.Min(min,point.x);max=Mathf.Max(max,point.x);
            }
            return max-min;
        }
    }
}
