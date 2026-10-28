using System;
using BlockRift.Core;
using BlockRift.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlockRift.Tests
{
    /// <summary>Model üretmeden birim ölçü, parça pivotu, sahne kökleri ve mevcut kamera entegrasyonunu sınar.</summary>
    public sealed class BoardVisualLayoutTests
    {
        private Scene scene;
        private Transform root;
        private BoardVisualLayout layout;

        [SetUp] public void OpenScene()
        {
            scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/BlockRift_Gameplay.unity");
            root = scene.GetRootGameObjects()[0].transform;
            layout = root.Find("BoardRoot").GetComponent<BoardVisualLayout>();
        }

        [TearDown] public void CloseScene()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
        }

        [TestCase(5)] [TestCase(6)]
        public void CellCentersAreCenteredAndExactlyOneUnitApart(int size)
        {
            var grid = new BoardGridLayout(size);
            float first = -(size - 1) * 0.5f;
            Assert.That(grid.CellCenter(new Cell(0, 0)), Is.EqualTo(new Vector3(first, 0, first)));
            Assert.That(grid.CellCenter(new Cell(size - 1, size - 1)), Is.EqualTo(new Vector3(-first, 0, -first)));
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Vector3 point = grid.CellCenter(new Cell(x, y));
                Assert.That(point, Is.EqualTo(new Vector3(first + x, 0, first + y)));
                if (x + 1 < size) Assert.That(grid.CellCenter(new Cell(x + 1, y)) - point, Is.EqualTo(Vector3.right));
                if (y + 1 < size) Assert.That(grid.CellCenter(new Cell(x, y + 1)) - point, Is.EqualTo(Vector3.forward));
            }
        }

        [TestCase(5)] [TestCase(6)]
        public void EveryApprovedShapeUsesFootprintBaseCenterAtEveryVisualPlacement(int size)
        {
            var grid = new BoardGridLayout(size);
            int shapes = 0;
            for (int width = 1; width <= 4; width++)
            for (int height = 1; height <= 4; height++)
            {
                if (!ShapeCatalog.IsAllowed(width, height)) continue;
                shapes++;
                for (int y = 0; y <= size - height; y++)
                for (int x = 0; x <= size - width; x++)
                {
                    Vector3 average = Vector3.zero;
                    for (int py = 0; py < height; py++)
                    for (int px = 0; px < width; px++) average += grid.CellCenter(new Cell(x + px, y + py));
                    Assert.That(grid.PieceBaseCenter(new Cell(x, y), width, height), Is.EqualTo(average / (width * height)));
                }
            }
            Assert.That(shapes, Is.EqualTo(9));
        }

        [Test] public void InvalidVisualCoordinatesAndNonFiniteHeightAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BoardGridLayout(4));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BoardGridLayout(6, float.NaN));
            var grid = new BoardGridLayout(6);
            Assert.Throws<ArgumentOutOfRangeException>(() => grid.CellCenter(new Cell(-1, 0)));
            Assert.Throws<ArgumentOutOfRangeException>(() => grid.CellCenter(new Cell(0, 6)));
            Assert.Throws<ArgumentOutOfRangeException>(() => grid.PieceBaseCenter(new Cell(int.MaxValue, 0), 2, 1));
            Assert.Throws<InvalidOperationException>(() => default(BoardGridLayout).CellCenter(new Cell(0, 0)));
        }

        [TestCase(5)] [TestCase(6)]
        public void WorldMappingPreservesUnitsUnderTranslatedAndYawRotatedBoard(int size)
        {
            layout.SetBoardSize(size);
            layout.transform.position = new Vector3(3, 2, -7);
            layout.transform.rotation = Quaternion.Euler(0, 37, 0);
            Assert.That(layout.ValidateSetup(out var issue), Is.True, issue);
            Vector3 first = layout.CellToWorld(new Cell(0, 0));
            Vector3 next = layout.CellToWorld(new Cell(1, 0));
            Assert.That(Vector3.Distance(first, next), Is.EqualTo(1).Within(0.00001));
            var piece = new PieceDefinition("view-only", PieceKind.Obstacle, 2, 2, new Cell(0, 0));
            Vector3 center = layout.PieceToWorld(piece, new Cell(1, 1));
            Vector3 expected = layout.transform.TransformPoint(layout.Grid.PieceBaseCenter(new Cell(1, 1), 2, 2));
            Assert.That(Vector3.Distance(center, expected), Is.LessThan(0.00001));
            Assert.That(piece.Start, Is.EqualTo(new Cell(0, 0)), "Core tanımı değiştirilmemeli");
        }

        [Test] public void InvalidRootScaleOrShiftedPiecesRootIsReported()
        {
            Assert.That(layout.ValidateSetup(out var issue), Is.True, issue);
            root.localScale = new Vector3(1, 1, 2);
            Assert.That(layout.ValidateSetup(out _), Is.False);
            Assert.Throws<InvalidOperationException>(() => layout.CellToWorld(new Cell(0, 0)));
            root.localScale = Vector3.one;
            root.Find("BoardRoot/PiecesRoot").localPosition = Vector3.right;
            Assert.That(layout.ValidateSetup(out _), Is.False);
        }

        [Test] public void InvalidInspectorSizeIsReportedAndCameraCanStillRefresh()
        {
            var serialized = new SerializedObject(layout);
            serialized.FindProperty("boardSize").intValue = 4;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(layout.ValidateSetup(out _), Is.False);
            Assert.That(layout.TryGetPresentationBounds(out _), Is.False);
            var framing = root.GetComponentInChildren<GameplayViewportLayout>();
            Assert.DoesNotThrow(() => framing.ApplyLayout(1080, 1920, new Rect(0, 0, 1080, 1920)));
        }

        [Test] public void EmptySuppliedViewsCanBePlacedRepeatedlyWithoutMeshesOrCoreMutation()
        {
            // Yalnız test sırasında boş Transform kullanılır; sahneye geçici taş/mesh kaydedilmez.
            var view = new GameObject("TestEmptyView").transform;
            try
            {
                var piece = new PieceDefinition("p", PieceKind.Obstacle, 2, 1, new Cell(0, 0));
                layout.PlacePieceView(view, piece, new Cell(1, 2));
                layout.PlacePieceView(view, piece, new Cell(2, 2));
                Assert.That(view.parent, Is.SameAs(root.Find("BoardRoot/PiecesRoot")));
                Assert.That(view.position, Is.EqualTo(layout.PieceToWorld(piece, new Cell(2, 2))));
                Assert.That(view.localScale, Is.EqualTo(Vector3.one));
                Assert.That(piece.Start, Is.EqualTo(new Cell(0, 0)));
                layout.PlaceBoardView(view); layout.PlaceBoardView(view);
                Assert.That(view.parent, Is.SameAs(root.Find("BoardRoot/BoardVisualsRoot")));
                Assert.That(view.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(view.GetComponent<MeshFilter>(), Is.Null);
            }
            finally { UnityEngine.Object.DestroyImmediate(view.gameObject); }
        }

        [Test] public void RejectedViewLeavesHierarchyAndTransformUntouched()
        {
            var piece = new PieceDefinition("p", PieceKind.Target, 1, 1, new Cell(0, 0));
            var pieces = root.Find("BoardRoot/PiecesRoot");
            Assert.Throws<ArgumentException>(() => layout.PlacePieceView(pieces, piece, new Cell(0, 0)));
            Assert.That(pieces.parent, Is.EqualTo(layout.transform));
            var view = new GameObject("TestInvalidView").transform;
            try
            {
                view.localScale = Vector3.one * 100;
                Assert.Throws<ArgumentException>(() => layout.PlacePieceView(view, piece, new Cell(0, 0)));
                Assert.That(view.parent, Is.Null);
                Assert.That(view.localScale.x, Is.EqualTo(100));
            }
            finally { UnityEngine.Object.DestroyImmediate(view.gameObject); }
        }

        [TestCase(5, 1080, 1920)] [TestCase(6, 1080, 1920)]
        [TestCase(5, 1080, 2520)] [TestCase(6, 1080, 2520)]
        [TestCase(5, 1536, 2048)] [TestCase(6, 1536, 2048)]
        public void ExistingCameraFramesAdapterBoundsForBothBoardSizes(int size, int width, int height)
        {
            layout.SetBoardSize(size);
            var framing = root.GetComponentInChildren<GameplayViewportLayout>();
            framing.ApplyLayout(width, height, new Rect(width * 0.02f, height * 0.04f, width * 0.96f, height * 0.9f));
            Assert.That(framing.UsesBoardLayout(layout), Is.True);
            var camera = root.GetComponentInChildren<Camera>();
            var safe = root.Find("UIRoot/GameplayCanvas/SafeArea").GetComponent<RectTransform>();
            var area = safe.Find("BoardViewport").GetComponent<RectTransform>();
            Vector2 span = safe.anchorMax - safe.anchorMin;
            Vector2 min = safe.anchorMin + Vector2.Scale(area.anchorMin, span);
            Vector2 max = safe.anchorMin + Vector2.Scale(area.anchorMax, span);
            Bounds bounds = layout.PresentationBounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 point = bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 projected = camera.WorldToViewportPoint(layout.transform.TransformPoint(point));
                Assert.That(projected.x, Is.InRange(min.x, max.x));
                Assert.That(projected.y, Is.InRange(min.y, max.y));
                Assert.That(projected.z, Is.InRange(camera.nearClipPlane, camera.farClipPlane));
            }
        }

        [Test] public void BoundsChecksDetectPivotScaleAxisAndFramingProblems()
        {
            var piece = new Bounds(new Vector3(0, 0.15f, 0), new Vector3(2, 0.3f, 1));
            Assert.That(VisualModelChecks.ValidatePieceBounds(piece, 2, 1, out _), Is.True);
            var shifted = piece; shifted.center += Vector3.right * 0.2f;
            Assert.That(VisualModelChecks.ValidatePieceBounds(shifted, 2, 1, out _), Is.False);
            Assert.That(VisualModelChecks.ValidatePieceBounds(piece, 1, 2, out _), Is.False);
            var huge = piece; huge.size *= 100;
            Assert.That(VisualModelChecks.ValidatePieceBounds(huge, 2, 1, out _), Is.False);
            var tall = new Bounds(new Vector3(0, 0.5f, 0), new Vector3(2, 1, 1));
            Assert.That(VisualModelChecks.ValidatePieceBounds(tall, 2, 1, out _), Is.False);
            var board = new Bounds(new Vector3(0, -0.09f, 0), new Vector3(6.24f, 0.18f, 6.24f));
            Assert.That(VisualModelChecks.ValidateBoardBounds(board, 6, out _), Is.True);
            Assert.That(VisualModelChecks.ValidateBoardBounds(board, 5, out _), Is.False);
            board.center += Vector3.up * 0.09f;
            Assert.That(VisualModelChecks.ValidateBoardBounds(board, 6, out _), Is.False);
        }
    }
}
