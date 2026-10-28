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
        [UnitySetUp] public IEnumerator Load()
        {
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/BlockRift_Gameplay.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
#else
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("BlockRift_Gameplay");
#endif
            yield return null; yield return null;
            game=Object.FindAnyObjectByType<LevelOneGameplay>(); Assert.That(game,Is.Not.Null);
        }
        private Vector2 Screen(string id,Vector3 offset=default) => game.GameplayCamera.WorldToScreenPoint(game.Views[id].position+offset);
        [UnityTest] public IEnumerator RealModelsPaletteAndRestartMatchAuthoritativeState()
        {
            Assert.That(game.Session.Level.Width,Is.EqualTo(6)); Assert.That(game.Views.Count,Is.EqualTo(3));
            foreach(var piece in game.Session.Level.Pieces)
            {
                var view=game.Views[piece.Id]; Assert.That(view.position,Is.EqualTo(game.Layout.PieceToWorld(piece,piece.Start)));
                Assert.That(view.GetComponentInChildren<MeshFilter>().sharedMesh.vertexCount,Is.GreaterThan(100));
                foreach(var mat in view.GetComponentsInChildren<MeshRenderer>().SelectMany(r=>r.sharedMaterials))
                    Assert.That(mat.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));
            }
            Assert.That(game.Views["gate"].GetComponentInChildren<MeshRenderer>().sharedMaterial.name,Is.EqualTo("BR_Plastic_Green"));
            Assert.That(game.Views["blue"].GetComponentInChildren<MeshRenderer>().sharedMaterial.name,Is.EqualTo("BR_Plastic_Blue"));
            var starMaterials=game.Views["star"].GetComponentsInChildren<MeshRenderer>().SelectMany(r=>r.sharedMaterials).Select(m=>m.name).ToArray();
            Assert.That(starMaterials.Any(n=>n.Contains("Ivory")),Is.True); Assert.That(starMaterials.Any(n=>n.Contains("Gold")),Is.True);
            var shared=game.Views["blue"].GetComponentInChildren<MeshRenderer>().sharedMaterial;
            var clone=Object.Instantiate(game.Views["blue"].gameObject); clone.SetActive(false);
            Assert.That(clone.GetComponentInChildren<MeshRenderer>(true).sharedMaterial,Is.SameAs(shared)); Object.Destroy(clone);
            var origin=Screen("blue"); var end=Screen("blue",Vector3.forward);
            Assert.That(game.BeginPointer(origin),Is.True); game.DragPointer(end); game.EndPointer(end);
            Assert.That(game.Session.MoveCount,Is.EqualTo(1)); game.Restart();
            Assert.That(game.Session.MoveCount,Is.Zero);
            Assert.That(MovementEngine.Position(game.Session.State,"blue"),Is.EqualTo(new Cell(1,3)));
            yield return null;
        }
        [UnityTest] public IEnumerator MouseDeviceDragProducesCoreMove()
        {
            var mouse=InputSystem.AddDevice<Mouse>();
            var start=Screen("blue"); var end=Screen("blue",Vector3.forward);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=start}.WithButton(MouseButton.Left)); yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=end}.WithButton(MouseButton.Left)); yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=end}); yield return null;
            Assert.That(MovementEngine.Position(game.Session.State,"blue"),Is.EqualTo(new Cell(1,4)));
            Assert.That(game.Session.MoveCount,Is.EqualTo(1)); InputSystem.RemoveDevice(mouse);
        }
        [UnityTest] public IEnumerator TouchDeviceDragProducesCoreMove()
        {
            var touch=InputSystem.AddDevice<Touchscreen>();
            var start=Screen("blue"); var end=Screen("blue",Vector3.forward);
            InputSystem.QueueStateEvent(touch,new TouchState{touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Began,position=start}); yield return null;
            InputSystem.QueueStateEvent(touch,new TouchState{touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Moved,position=end}); yield return null;
            InputSystem.QueueStateEvent(touch,new TouchState{touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=end}); yield return null;
            Assert.That(game.Session.MoveCount,Is.EqualTo(1));
            Assert.That(MovementEngine.Position(game.Session.State,"blue"),Is.EqualTo(new Cell(1,4))); InputSystem.RemoveDevice(touch);
        }
        [UnityTest] public IEnumerator UiAndStoneCannotSelectAndReturnToOriginIsNoOp()
        {
            var button=Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.name=="LevelOneRestart");
            Assert.That(game.BeginPointer(RectTransformUtility.WorldToScreenPoint(null,button.transform.position)),Is.False);
            Assert.That(game.BeginPointer(game.GameplayCamera.WorldToScreenPoint(game.Layout.CellToWorld(game.Session.Level.FixedCells[0]))),Is.False);
            var start=Screen("blue"); var moved=Screen("blue",Vector3.forward);
            Assert.That(game.BeginPointer(start),Is.True);
            game.DragPointer(moved); game.DragPointer(start); game.EndPointer(start);
            Assert.That(game.Session.MoveCount,Is.Zero); yield return null;
        }
        [UnityTest] public IEnumerator VerifiedSolutionWinsBeforeReleaseAndLocksInput()
        {
            var proof=BoardSolver.Solve(game.Session.Level,game.Session.State);
            foreach(var move in proof.Path)
            {
                var start=Screen(move.PieceId);
                var end=Screen(move.PieceId,(move.Axis==MoveAxis.Horizontal?Vector3.right:Vector3.forward)*move.Distance);
                Assert.That(game.BeginPointer(start),Is.True); game.DragPointer(end);
                if(move.Equals(proof.Path.Last())) Assert.That(game.Session.Won,Is.True,"Bırakma beklenmemeli");
                game.EndPointer(end); yield return null;
            }
            Assert.That(game.Session.Won,Is.True); Assert.That(game.Session.MoveCount,Is.EqualTo(proof.Path.Count));
            Assert.That(game.BeginPointer(Screen("blue")),Is.False);
            game.Restart(); Assert.That(game.Session.Won,Is.False); Assert.That(game.Session.MoveCount,Is.Zero);
        }
        [UnityTest] public IEnumerator DragAxisStaysLockedAndRestartButtonResetsPreview()
        {
            var start=Screen("blue"); var first=Screen("blue",Vector3.forward);
            var sideways=Screen("blue",Vector3.right);
            Assert.That(game.BeginPointer(start),Is.True); game.DragPointer(first);
            game.DragPointer(sideways); game.EndPointer(sideways);
            Assert.That(game.Session.MoveCount,Is.Zero,"İkinci eksene geçmek yerine başlangıca döner");
            Assert.That(MovementEngine.Position(game.Session.State,"blue"),Is.EqualTo(new Cell(1,3)));
            game.BeginPointer(start); game.DragPointer(first);
            Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.name=="LevelOneRestart").onClick.Invoke();
            Assert.That(game.Views["blue"].position,Is.EqualTo(game.Layout.PieceToWorld(game.Session.Level.Pieces.Single(p=>p.Id=="blue"),new Cell(1,3))));
            game.EndPointer(first); Assert.That(game.Session.MoveCount,Is.Zero);
            yield return null;
        }
    }
}
