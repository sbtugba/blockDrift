using BlockRift.Presentation;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BlockRift.Tests
{
    /// <summary>Gerçek kayıtlı sahnede kamera projeksiyonunu ve boş sunum iskeletini doğrular.</summary>
    public sealed class GameplaySceneTests
    {
        private Scene scene;
        private Transform root;

        [SetUp] public void OpenFoundation()
        {
            // Preview sahnesi kullanıcının açık/kaydedilmemiş sahnesine dokunmaz.
            scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/BlockRift_Gameplay.unity");
            root = scene.GetRootGameObjects()[0].transform;
        }

        [TearDown] public void CloseFoundation()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
        }

        [Test] public void FoundationContainsPresentationAndApprovedModelRoots()
        {
            Assert.That(root.name, Is.EqualTo("GameplayRoot"));
            // Entegrasyon öncesi boşluk şartının yerini gerçek ve tek test tahtasının varlığı alır.
            Assert.That(root.Find("BoardRoot/PiecesRoot").childCount, Is.EqualTo(BlockRift.Core.LevelOne.Create().Pieces.Count+BlockRift.Core.LevelOne.Create().FixedCells.Count));
            Assert.That(root.Find("BoardRoot/BoardVisualsRoot").childCount, Is.EqualTo(1));
            Assert.That(root.Find("BoardRoot").GetComponentsInChildren<MeshFilter>(true).Length,
                Is.EqualTo(2+BlockRift.Core.LevelOne.Create().Pieces.Count+BlockRift.Core.LevelOne.Create().FixedCells.Count));
            var background = root.Find("Background");
            Assert.That(background, Is.Not.Null);
            Assert.That(background.GetComponent<BackgroundSpriteFitter>().enabled, Is.True);
            Assert.That(background.GetComponent<MeshRenderer>().sharedMaterial.shader.name,
                Is.EqualTo("Universal Render Pipeline/Unlit"));
            Assert.That(root.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(root.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
            Assert.That(root.GetComponentsInChildren<AudioListener>(true).Length, Is.EqualTo(1));
            var scaler = root.GetComponentInChildren<CanvasScaler>();
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1080, 1920)));
            Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(root.Find("UIRoot/GameplayCanvas/SafeArea/TopHUD"), Is.Not.Null);
            Assert.That(root.Find("UIRoot/GameplayCanvas/SafeArea/BottomControls"), Is.Not.Null);
        }

        [TestCase(5, 1080, 1920)] [TestCase(6, 1080, 1920)]
        [TestCase(5, 1080, 2400)] [TestCase(6, 1080, 2400)]
        [TestCase(5, 1080, 2520)] [TestCase(6, 1080, 2520)]
        [TestCase(5, 1536, 2048)] [TestCase(6, 1536, 2048)]
        [TestCase(5, 1920, 1080)] [TestCase(6, 1920, 1080)]
        [TestCase(5, 720, 1280)] [TestCase(6, 720, 1280)]
        public void BoardFrameAndPieceHeightStayInsideReservedSafeViewport(int boardSize, int width, int height)
        {
            // Asimetrik çentik/home-indicator ve yan insetlerle aynı runtime kadraj yolunu sınarız.
            var safePixels = new Rect(width * 0.02f, height * 0.035f, width * 0.95f, height * 0.91f);
            var layout = root.GetComponentInChildren<GameplayViewportLayout>();
            layout.ApplyLayout(width, height, safePixels);
            var camera = root.GetComponentInChildren<Camera>();
            var safe = root.Find("UIRoot/GameplayCanvas/SafeArea").GetComponent<RectTransform>();
            var viewport = safe.Find("BoardViewport").GetComponent<RectTransform>();
            Vector2 safeSpan = safe.anchorMax - safe.anchorMin;
            Vector2 min = safe.anchorMin + Vector2.Scale(viewport.anchorMin, safeSpan);
            Vector2 max = safe.anchorMin + Vector2.Scale(viewport.anchorMax, safeSpan);
            Assert.That(safe.anchorMin.x, Is.EqualTo(safePixels.xMin / width).Within(0.00001));
            Assert.That(safe.anchorMax.y, Is.EqualTo(safePixels.yMax / height).Within(0.00001));
            Assert.That(camera.orthographic, Is.False);
            var board = root.Find("BoardRoot");
            for (int i = 0; i < 8; i++)
            {
                float half = (boardSize + 0.24f) * 0.5f;
                var point = board.TransformPoint(new Vector3((i & 1) == 0 ? -half : half,
                    (i & 2) == 0 ? -0.18f : 0.3f, (i & 4) == 0 ? -half : half));
                Vector3 projected = camera.WorldToViewportPoint(point);
                Assert.That(projected.x, Is.InRange(min.x, max.x), "Tahta yatay kadraj dışında");
                Assert.That(projected.y, Is.InRange(min.y, max.y), "Tahta HUD/kontrol alanında");
                Assert.That(projected.z, Is.InRange(camera.nearClipPlane, camera.farClipPlane));
            }
        }
    }
}
