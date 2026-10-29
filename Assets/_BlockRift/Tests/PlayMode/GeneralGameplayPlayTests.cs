using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BlockRift.Core;
using BlockRift.App;
using BlockRift.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace BlockRift.Tests
{
    /// <summary>Gerçek sahnede farklı test JSON'larını mevcut prefablarla oynatır; yeni oyun içeriği yayınlamaz.</summary>
    public sealed class GeneralGameplayPlayTests
    {
        private LevelOneGameplay game;
        private ProgressionSceneIsolation isolation;
        [UnityTearDown]
        public IEnumerator CleanupProgress()
        {
            isolation?.Dispose();
            yield return null;
        }

        private sealed class JsonProvider : ILevelProvider, ILevelVisualProvider
        {
            public readonly Dictionary<string, string> Json = new Dictionary<string, string>();
            private LevelVisualData loaded;
            public LevelVisualData GetVisualData(string id)
            {
                if (!Json.TryGetValue(id, out var json))
                    throw new FileNotFoundException(id);
                return loaded != null && loaded.Definition.Id == id ? loaded : (loaded = LevelVisualData.Deserialize(json));
            }
            public LevelDefinition GetLevel(string id)
            {
                loaded = null;
                return GetVisualData(id).Definition;
            }
        }

        private static LevelDefinition Fixture() => new LevelDefinition("smoke-alpha", 1, MovementEngine.RulesVersion, 6, 6,
            new Cell(5, 5), new[] {
                new PieceDefinition("nova", PieceKind.Target, 1, 1, new Cell(0, 5)),
                new PieceDefinition("rail", PieceKind.Obstacle, 2, 1, new Cell(0, 0)),
                new PieceDefinition("mast", PieceKind.Obstacle, 1, 2, new Cell(5, 0)),
                new PieceDefinition("box", PieceKind.Obstacle, 2, 2, new Cell(2, 0)) },
            new[] { new Cell(4, 3) }, Array.Empty<Cell>());

        [UnitySetUp]
        public IEnumerator OpenScene()
        {
            isolation = new ProgressionSceneIsolation();
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/BlockRift_Gameplay.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return SceneManager.LoadSceneAsync("BlockRift_Gameplay");
#endif
            yield return null;
            yield return null;
            game = UnityEngine.Object.FindAnyObjectByType<LevelOneGameplay>();
            Assert.That(game, Is.Not.Null);
        }

        private JsonProvider ConfigureFixture()
        {
            var provider = new JsonProvider();
            string manifest = LevelJsonSerializer.WriteJsonObject(new Dictionary<string, object> { { "schemaVersion", 1 }, { "levels", new List<object> { "smoke-alpha", "smoke-beta", "color-fixture" } } });
            var fixtureStore = new JsonProgressStore(isolation.SavePath);
            fixtureStore.Save(new LevelProgression.Snapshot(1, new[] { "smoke-alpha", "smoke-beta", "color-fixture" }, "smoke-alpha"));
            game.ConfigureProgression(manifest, fixtureStore);
            provider.Json["smoke-alpha"] = new LevelVisualData(Fixture(), new Dictionary<string, string> { { "rail", "blue" }, { "mast", "green" }, { "box", "red" } }).Serialize();
            provider.Json["smoke-beta"] = LevelJsonSerializer.Serialize(new LevelDefinition("smoke-beta", 1, MovementEngine.RulesVersion, 6, 6,
                new Cell(3, 0), new[] { new PieceDefinition("ember", PieceKind.Target, 1, 1, new Cell(1, 0)) }, Array.Empty<Cell>(), Array.Empty<Cell>()));
            game.SetLevelProvider(provider);
            return provider;
        }

        private Vector2 Screen(string id, Vector3 offset = default) => game.GameplayCamera.WorldToScreenPoint(game.Views[id].position + offset);
        private void Drag(string id, Vector3 offset)
        {
            var start = Screen(id);
            var end = Screen(id, offset);
            Assert.That(game.BeginPointer(start), Is.True, id);
            game.DragPointer(end);
            game.EndPointer(end);
        }

        [UnityTest]
        public IEnumerator DifferentIdsAndShapesUseCoreAndExistingModels()
        {
            ConfigureFixture();
            game.LoadLevel("smoke-alpha");
            yield return null;
            Assert.That(game.Views.Keys, Is.EquivalentTo(new[] { "nova", "rail", "mast", "box" }));
            foreach (var piece in game.Session.Level.Pieces)
                Assert.That(game.Views[piece.Id].position, Is.EqualTo(game.Layout.PieceToWorld(piece, piece.Start)));
            Assert.That(game.Layout.transform.Find("PiecesRoot").childCount, Is.EqualTo(5));
            Assert.That(game.Layout.transform.Find("MarkersRoot/ExitVisual").position, Is.EqualTo(game.Layout.CellToWorld(new Cell(5, 5)) + Vector3.up * .004f));
            Drag("rail", Vector3.forward);
            Drag("mast", Vector3.right);
            Drag("rail", Vector3.right * 3);
            Assert.That(game.Session.MoveCount, Is.Zero, "Yasak eksenler ve box çarpışması hamle değildir");
            Drag("box", Vector3.right);
            Drag("rail", Vector3.right * 5);
            Assert.That(MovementEngine.Position(game.Session.State, "rail"), Is.EqualTo(new Cell(1, 0)));
            Drag("mast", Vector3.forward * 20);
            Assert.That(MovementEngine.Position(game.Session.State, "mast"), Is.EqualTo(new Cell(5, 4)));
            var start = Screen("box");
            var moved = Screen("box", Vector3.forward);
            game.BeginPointer(start);
            game.DragPointer(moved);
            game.DragPointer(start);
            game.EndPointer(start);
            Assert.That(game.Session.MoveCount, Is.EqualTo(3));
            game.Restart();
            Assert.That(BoardSolver.Key(game.Session.State), Is.EqualTo(BoardSolver.Key(BoardState.CreateInitial(Fixture()))));
            Assert.That(game.Session.MoveCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator GenericTargetWinsDuringDragAndReloadCancelsPreview()
        {
            ConfigureFixture();
            game.LoadLevel("smoke-alpha");
            yield return null;
            var start = Screen("nova");
            var end = Screen("nova", Vector3.right * 5);
            Assert.That(game.BeginPointer(start), Is.True);
            game.DragPointer(end);
            Assert.That(game.Session.Won, Is.True, "Başka kimlikteki hedef de bırakmadan kazanmalı");
            game.EndPointer(end);
            Assert.That(game.Session.MoveCount, Is.EqualTo(1));
            Assert.That(game.BeginPointer(Screen("rail")), Is.False);
            game.Restart();
            game.BeginPointer(Screen("box"));
            game.DragPointer(Screen("box", Vector3.forward));
            game.LoadLevel("smoke-beta");
            yield return null;
            game.EndPointer(end);
            Assert.That(game.Views.Keys, Is.EquivalentTo(new[] { "ember" }));
            Assert.That(game.Layout.transform.Find("PiecesRoot").childCount, Is.EqualTo(1));
            Assert.That(game.Session.MoveCount, Is.Zero);
            Assert.That(game.Session.Won, Is.False);
            Drag("ember", Vector3.right * 2);
            Assert.That(game.Session.Won, Is.True);
            game.Restart();
            Assert.That(MovementEngine.Position(game.Session.State, "ember"), Is.EqualTo(new Cell(1, 0)));
            Assert.That(UnityEngine.Object.FindObjectsByType<LevelOneGameplay>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator MissingVisualKeyIsExplicitAndPreservesCurrentScene()
        {
            var fixtureStore = new JsonProgressStore(isolation.SavePath);
            fixtureStore.Save(new LevelProgression.Snapshot(1, new[] { "level-001", "smoke-alpha" }, "level-001"));
            game.ConfigureProgression("{\"schemaVersion\":1,\"levels\":[\"level-001\",\"smoke-alpha\"]}", fixtureStore);
            var provider = new JsonProvider();
            provider.Json["smoke-alpha"] = LevelJsonSerializer.Serialize(Fixture());
            game.SetLevelProvider(provider);
            var session = game.Session;
            var view = game.Views["star"];
            var error = Assert.Throws<InvalidOperationException>(() => game.LoadLevel("smoke-alpha"));
            StringAssert.Contains("visualKey", error.Message);
            Assert.That(game.Session, Is.SameAs(session));
            Assert.That(game.Views["star"], Is.SameAs(view));
            Assert.That(game.LevelId, Is.EqualTo("level-001"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator JsonColorsShareMaterialsWithoutPerLevelPaletteOrMaterialMutation()
        {
            var provider = ConfigureFixture();
            var fixture = new LevelDefinition("color-fixture", 1, MovementEngine.RulesVersion, 6, 6, new Cell(5, 5), new[]{
                new PieceDefinition("sun",PieceKind.Target,1,1,new Cell(0,5)),
                new PieceDefinition("left",PieceKind.Obstacle,1,2,new Cell(4,0)),
                new PieceDefinition("right",PieceKind.Obstacle,1,2,new Cell(5,0)),
                new PieceDefinition("small",PieceKind.Obstacle,1,1,new Cell(0,0)),
                new PieceDefinition("copy",PieceKind.Obstacle,1,1,new Cell(1,0))}, Array.Empty<Cell>(), Array.Empty<Cell>());
            provider.Json[fixture.Id] = new LevelVisualData(fixture, new Dictionary<string, string> { { "left", "blue" }, { "right", "red" }, { "small", "blue" }, { "copy", "blue" } }).Serialize();
            game.LoadLevel(fixture.Id);
            yield return null;
            Material Mat(string id) => game.Views[id].GetComponentInChildren<MeshRenderer>().sharedMaterial;
            var blue = Mat("left");
            var red = Mat("right");
            var originalColor = blue.color;
            Assert.That(Mat("small"), Is.SameAs(blue));
            Assert.That(Mat("copy"), Is.SameAs(blue));
            Assert.That(red, Is.Not.SameAs(blue));
            Assert.That(game.Views["left"].GetComponentInChildren<MeshFilter>().sharedMesh,
                Is.SameAs(game.Views["right"].GetComponentInChildren<MeshFilter>().sharedMesh));
            game.Restart();
            Assert.That(Mat("left"), Is.SameAs(blue));
            Assert.That(Mat("right"), Is.SameAs(red));
            provider.Json[fixture.Id] = new LevelVisualData(fixture, new Dictionary<string, string> { { "left", "red" }, { "right", "red" }, { "small", "blue" }, { "copy", "blue" } }).Serialize();
            game.LoadLevel(fixture.Id);
            yield return null;
            Assert.That(Mat("left"), Is.SameAs(red));
            Assert.That(Mat("small"), Is.SameAs(blue));
            Assert.That(Mat("copy"), Is.SameAs(blue));
            Assert.That(blue.color, Is.EqualTo(originalColor), "Bir parça için renk seçmek paylaşılan materyali değiştirmez");
            game.Restart();
            Assert.That(Mat("left"), Is.SameAs(red));
            // Başka bölüme geçerken materyal nesnelerini değiştirmeden farklı anahtarlar seçilir.
            game.LoadLevel("smoke-alpha");
            yield return null;
            Assert.That(blue.color, Is.EqualTo(originalColor));
            Assert.That(red.name, Is.EqualTo("BR_Plastic_Red"));
            Assert.That(game.Views.ContainsKey("left"), Is.False);
            var session = game.Session;
            provider.Json[fixture.Id] = new LevelVisualData(fixture, new Dictionary<string, string> { { "left", "unknown" }, { "right", "red" }, { "small", "blue" }, { "copy", "blue" } }).Serialize();
            Assert.Throws<InvalidOperationException>(() => game.LoadLevel(fixture.Id));
            Assert.That(game.Session, Is.SameAs(session));
            Assert.That(game.Views.ContainsKey("rail"), Is.True);
        }
    }
}
