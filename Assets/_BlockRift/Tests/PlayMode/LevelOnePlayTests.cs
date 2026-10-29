using System.Collections;
using System.Linq;
using BlockRift.Core;
using BlockRift.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace BlockRift.Tests
{
    /// <summary>Gerçek Gameplay sahnesinde state, cihaz girdisi ve sanat bağlarını doğrular.</summary>
    public sealed class LevelOnePlayTests : InputTestFixture
    {
        private LevelOneGameplay game;
        private ProgressionSceneIsolation isolation;
        [UnityTearDown]
        public IEnumerator CleanupProgress()
        {
            isolation?.Dispose();
            yield return null;
        }
        [UnitySetUp]
        public IEnumerator Load()
        {
            isolation = new ProgressionSceneIsolation();
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/BlockRift_Gameplay.unity", new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
#else
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("BlockRift_Gameplay");
#endif
            yield return null;
            yield return null;
            game = Object.FindAnyObjectByType<LevelOneGameplay>();
            Assert.That(game, Is.Not.Null);
        }
        private Vector2 Screen(string id, Vector3 offset = default) => game.GameplayCamera.WorldToScreenPoint(game.Views[id].position + offset);
        [UnityTest]
        public IEnumerator SceneStartLoadsPackagedJsonAndRestartRestoresItsState()
        {
            // Bu test Editor PlayMode ve Development test player içinde aynı Resources yolunu kullanır.
            var asset = Resources.Load<TextAsset>(ResourceLevelLoader.LevelsResourceFolder + game.LevelId);
            Assert.That(asset, Is.Not.Null, "Paketlenmiş JSON bulunamadı");
            var expected = LevelJsonSerializer.Deserialize(asset.text);
            Assert.That(LevelJsonSerializer.Serialize(game.Session.Level), Is.EqualTo(LevelJsonSerializer.Serialize(expected)));
            var initial = BoardSolver.Key(BoardState.CreateInitial(expected));
            Assert.That(BoardSolver.Key(game.Session.State), Is.EqualTo(initial));
            var start = Screen("C");
            var end = Screen("C", Vector3.forward);
            Assert.That(game.BeginPointer(start), Is.True);
            game.DragPointer(end);
            game.EndPointer(end);
            Assert.That(game.Session.MoveCount, Is.EqualTo(1));
            Assert.That(BoardSolver.Key(game.Session.State), Is.Not.EqualTo(initial));
            game.Restart();
            Assert.That(BoardSolver.Key(game.Session.State), Is.EqualTo(initial));
            Assert.That(game.Session.MoveCount, Is.Zero);
            Assert.That(game.Session.Won, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ForbiddenAxesDoNotMoveViewsOrSpendMoves()
        {
            foreach (string id in new[] { "A", "B", "C", "E" })
            {
                var piece = game.Session.Level.Pieces.Single(p => p.Id == id);
                var before = game.Views[id].position;
                var key = BoardSolver.Key(game.Session.State);
                var start = Screen(id);
                var end = Screen(id, piece.Width > piece.Height ? Vector3.forward : Vector3.right);
                Assert.That(game.BeginPointer(start), Is.True, id);
                game.DragPointer(end);
                Assert.That(game.Views[id].position, Is.EqualTo(before), id + " yasak önizleme");
                game.EndPointer(end);
                Assert.That(game.Session.MoveCount, Is.Zero);
                Assert.That(BoardSolver.Key(game.Session.State), Is.EqualTo(key));
            }
            yield return null;
        }
        [UnityTest]
        public IEnumerator SquareMovesOnBothAxesAndTouchRejectsForbiddenAxis()
        {
            foreach (var direction in new[] { Vector3.right, Vector3.forward })
            {
                var start = Screen("F");
                var end = Screen("F", direction);
                Assert.That(game.BeginPointer(start), Is.True);
                game.DragPointer(end);
                game.EndPointer(end);
                Assert.That(game.Session.MoveCount, Is.EqualTo(1));
                game.Restart();
            }
            var touch = InputSystem.AddDevice<Touchscreen>();
            var origin = Screen("A");
            var sideways = Screen("A", Vector3.right);
            var initial = BoardSolver.Key(game.Session.State);
            var position = game.Views["A"].position;
            InputSystem.QueueStateEvent(touch, new TouchState { touchId = 2, phase = UnityEngine.InputSystem.TouchPhase.Began, position = origin });
            yield return null;
            InputSystem.QueueStateEvent(touch, new TouchState { touchId = 2, phase = UnityEngine.InputSystem.TouchPhase.Moved, position = sideways });
            yield return null;
            Assert.That(game.Views["A"].position, Is.EqualTo(position));
            InputSystem.QueueStateEvent(touch, new TouchState { touchId = 2, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = sideways });
            yield return null;
            Assert.That(BoardSolver.Key(game.Session.State), Is.EqualTo(initial));
            Assert.That(game.Session.MoveCount, Is.Zero);
            InputSystem.RemoveDevice(touch);
        }
        [UnityTest]
        public IEnumerator RealModelsPaletteAndRestartMatchAuthoritativeState()
        {
            Assert.That(game.Session.Level.Width, Is.EqualTo(6));
            Assert.That(game.Views.Count, Is.EqualTo(7));
            foreach (var piece in game.Session.Level.Pieces)
            {
                var view = game.Views[piece.Id];
                Assert.That(view.position, Is.EqualTo(game.Layout.PieceToWorld(piece, piece.Start)));
                Assert.That(view.GetComponentInChildren<MeshFilter>().sharedMesh.vertexCount, Is.GreaterThan(100));
                foreach (var mat in view.GetComponentsInChildren<MeshRenderer>().SelectMany(r => r.sharedMaterials))
                    Assert.That(mat.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));
            }
            Assert.That(game.Views["B"].GetComponentInChildren<MeshRenderer>().sharedMaterial.name, Is.EqualTo("BR_Plastic_Green"));
            Assert.That(game.Views["A"].GetComponentInChildren<MeshRenderer>().sharedMaterial.name, Is.EqualTo("BR_Plastic_Blue"));
            string[,] expectedColors = { { "C", "Yellow" }, { "D", "Red" }, { "E", "Purple" }, { "F", "Orange" } };
            for (int i = 0; i < 4; i++)
                foreach (var renderer in game.Views[expectedColors[i, 0]].GetComponentsInChildren<MeshRenderer>())
                    foreach (var material in renderer.sharedMaterials)
                        Assert.That(material.name, Is.EqualTo("BR_Plastic_" + expectedColors[i, 1]));
            var starMaterials = game.Views["star"].GetComponentsInChildren<MeshRenderer>().SelectMany(r => r.sharedMaterials).Select(m => m.name).ToArray();
            Assert.That(starMaterials.Any(n => n.Contains("Ivory")), Is.True);
            Assert.That(starMaterials.Any(n => n.Contains("Gold")), Is.True);
            var shared = game.Views["A"].GetComponentInChildren<MeshRenderer>().sharedMaterial;
            var clone = Object.Instantiate(game.Views["A"].gameObject);
            clone.SetActive(false);
            Assert.That(clone.GetComponentInChildren<MeshRenderer>(true).sharedMaterial, Is.SameAs(shared));
            Object.Destroy(clone);
            var origin = Screen("C");
            var end = Screen("C", Vector3.forward);
            Assert.That(game.BeginPointer(origin), Is.True);
            game.DragPointer(end);
            game.EndPointer(end);
            Assert.That(game.Session.MoveCount, Is.EqualTo(1));
            game.Restart();
            Assert.That(game.Session.MoveCount, Is.Zero);
            Assert.That(MovementEngine.Position(game.Session.State, "C"), Is.EqualTo(new Cell(2, 0)));
            yield return null;
        }
        [UnityTest]
        public IEnumerator MouseDeviceDragProducesCoreMove()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            var start = Screen("C");
            var end = Screen("C", Vector3.forward);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = start }.WithButton(MouseButton.Left));
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = end }.WithButton(MouseButton.Left));
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = end });
            yield return null;
            Assert.That(MovementEngine.Position(game.Session.State, "C"), Is.EqualTo(new Cell(2, 1)));
            Assert.That(game.Session.MoveCount, Is.EqualTo(1));
            InputSystem.RemoveDevice(mouse);
        }
        [UnityTest]
        public IEnumerator TouchDeviceDragProducesCoreMove()
        {
            var touch = InputSystem.AddDevice<Touchscreen>();
            var start = Screen("C");
            var end = Screen("C", Vector3.forward);
            InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Began, position = start });
            yield return null;
            InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Moved, position = end });
            yield return null;
            InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = end });
            yield return null;
            Assert.That(game.Session.MoveCount, Is.EqualTo(1));
            Assert.That(MovementEngine.Position(game.Session.State, "C"), Is.EqualTo(new Cell(2, 1)));
            InputSystem.RemoveDevice(touch);
        }
        [UnityTest]
        public IEnumerator UiAndStoneCannotSelectAndReturnToOriginIsNoOp()
        {
            var button = Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b => b.name == "LevelOneRestart");
            Assert.That(game.BeginPointer(RectTransformUtility.WorldToScreenPoint(null, button.transform.position)), Is.False);
            Assert.That(game.BeginPointer(game.GameplayCamera.WorldToScreenPoint(game.Layout.CellToWorld(game.Session.Level.FixedCells[0]))), Is.False);
            var start = Screen("C");
            var moved = Screen("C", Vector3.forward);
            Assert.That(game.BeginPointer(start), Is.True);
            game.DragPointer(moved);
            game.DragPointer(start);
            game.EndPointer(start);
            Assert.That(game.Session.MoveCount, Is.Zero);
            yield return null;
        }
        [UnityTest]
        public IEnumerator VerifiedSolutionWinsBeforeReleaseAndLocksInput()
        {
            var proof = BoardSolver.Solve(game.Session.Level, game.Session.State);
            foreach (var move in proof.Path)
            {
                var start = Screen(move.PieceId);
                var end = Screen(move.PieceId, (move.Axis == MoveAxis.Horizontal ? Vector3.right : Vector3.forward) * move.Distance);
                Assert.That(game.BeginPointer(start), Is.True);
                game.DragPointer(end);
                if (move.Equals(proof.Path.Last()))
                    Assert.That(game.Session.Won, Is.True, "Bırakma beklenmemeli");
                game.EndPointer(end);
                yield return null;
            }
            Assert.That(game.Session.Won, Is.True);
            Assert.That(game.Session.MoveCount, Is.EqualTo(proof.Path.Count));
            Assert.That(game.BeginPointer(Screen("C")), Is.False);
            game.Restart();
            Assert.That(game.Session.Won, Is.False);
            Assert.That(game.Session.MoveCount, Is.Zero);
        }
        [UnityTest]
        public IEnumerator DragAxisStaysLockedAndRestartButtonResetsPreview()
        {
            var start = Screen("C");
            var first = Screen("C", Vector3.forward);
            var sideways = Screen("C", Vector3.right);
            Assert.That(game.BeginPointer(start), Is.True);
            game.DragPointer(first);
            game.DragPointer(sideways);
            game.EndPointer(sideways);
            Assert.That(game.Session.MoveCount, Is.Zero, "İkinci eksene geçmek yerine başlangıca döner");
            Assert.That(MovementEngine.Position(game.Session.State, "C"), Is.EqualTo(new Cell(2, 0)));
            game.BeginPointer(start);
            game.DragPointer(first);
            Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b => b.name == "LevelOneRestart").onClick.Invoke();
            Assert.That(game.Views["C"].position, Is.EqualTo(game.Layout.PieceToWorld(game.Session.Level.Pieces.Single(p => p.Id == "C"), new Cell(2, 0))));
            game.EndPointer(first);
            Assert.That(game.Session.MoveCount, Is.Zero);
            yield return null;
        }
    }
}
