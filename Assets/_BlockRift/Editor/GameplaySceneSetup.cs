using System;
using System.IO;
using BlockRift.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BlockRift.Editor
{
    /// <summary>Boş gameplay sahne iskeletini kurar; model, input veya oyun kuralı oluşturmaz.</summary>
    public static class GameplaySceneSetup
    {
        public const string ScenePath = "Assets/Scenes/BlockRift_Gameplay.unity";

        /// <summary>Batch doğrulama için tek giriş; var olan sahneyi asla ezmez.</summary>
        [MenuItem("Block Rift/Scene/Create Gameplay Foundation")]
        public static void Create()
        {
            if (File.Exists(ScenePath))
                throw new InvalidOperationException("Gameplay sahnesi zaten mevcut; otomatik üzerine yazılmaz.");
            Scene previous = SceneManager.GetActiveScene();
            // Batch başlangıcında boş Untitled sahnesi vardır; Unity buna additive sahne açmaz.
            bool batchStartup = Application.isBatchMode && string.IsNullOrEmpty(previous.path);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                batchStartup ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var root = Node("GameplayRoot");
            var cameraRig = Node("CameraRig", root);
            var cameraNode = Node("Main Camera", cameraRig);
            cameraNode.gameObject.tag = "MainCamera";
            var camera = cameraNode.gameObject.AddComponent<Camera>();
            cameraNode.rotation = Quaternion.Euler(60, 0, 0);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.075f, 0.11f, 1);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100;
            camera.orthographic = true;
            camera.allowHDR = false;
            cameraNode.gameObject.AddComponent<AudioListener>();
            var cameraData = cameraNode.gameObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = false;
            cameraData.antialiasing = AntialiasingMode.None;
            var environment = Node("EnvironmentRoot", root);
            var lightNode = Node("Key Light", environment);
            lightNode.rotation = Quaternion.Euler(50, -30, 0);
            var light = lightNode.gameObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1, 0.95f, 0.87f);
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.65f;
            lightNode.gameObject.AddComponent<UniversalAdditionalLightData>();
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.45f, 0.5f, 0.6f);
            RenderSettings.ambientEquatorColor = new Color(0.25f, 0.3f, 0.38f);
            RenderSettings.ambientGroundColor = new Color(0.16f, 0.18f, 0.23f);
            RenderSettings.sun = light;
            var board = Node("BoardRoot", root);
            Node("BoardVisualsRoot", board);
            Node("PiecesRoot", board);
            Node("FixedGeometryRoot", board);
            Node("MarkersRoot", board);
            Node("EffectsRoot", board);
            var ui = Node("UIRoot", root);
            var canvasNode = new GameObject("GameplayCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasNode.layer = 5;
            canvasNode.transform.SetParent(ui, false);
            var canvas = canvasNode.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasNode.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            var safe = Area("SafeArea", canvasNode.transform, Vector2.zero, Vector2.one);
            Area("TopHUD", safe, new Vector2(0.04f, 0.82f), new Vector2(0.96f, 0.98f));
            var viewport = Area("BoardViewport", safe, new Vector2(0.04f, 0.22f), new Vector2(0.96f, 0.80f));
            Area("BottomControls", safe, new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.20f));
            Area("OverlayRoot", safe, Vector2.zero, Vector2.one);
            var layout = cameraRig.gameObject.AddComponent<GameplayViewportLayout>();
            var data = new SerializedObject(layout);
            data.FindProperty("boardCamera").objectReferenceValue = camera;
            data.FindProperty("boardRoot").objectReferenceValue = board;
            data.FindProperty("safeAreaRoot").objectReferenceValue = safe;
            data.FindProperty("boardViewport").objectReferenceValue = viewport;
            data.ApplyModifiedPropertiesWithoutUndo();
            GameplayVisualIntegrationSetup.ConfigureRoots(root);
            layout.ApplyLayout(1080, 1920, new Rect(0, 0, 1080, 1920));
            PlayerSettings.defaultScreenWidth = 1080;
            PlayerSettings.defaultScreenHeight = 1920;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            if (previous.IsValid())
            {
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
            Debug.Log("Block Rift: gameplay foundation saved: " + ScenePath);
        }

        private static Transform Node(string name, Transform parent = null)
        {
            var node = new GameObject(name).transform;
            if (parent)
                node.SetParent(parent, false);
            return node;
        }

        private static RectTransform Area(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var node = new GameObject(name, typeof(RectTransform));
            node.layer = 5;
            var rect = node.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
    }
}
