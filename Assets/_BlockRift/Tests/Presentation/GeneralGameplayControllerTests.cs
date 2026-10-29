using System;
using System.Collections.Generic;
using System.IO;
using BlockRift.Core;
using BlockRift.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlockRift.Tests
{
    /// <summary>Üretim bölümü eklemeden genel controller sözleşmesini sınar.</summary>
    public sealed class GeneralGameplayControllerTests
    {
        private GameObject host;
        private LevelOneGameplay game;
        private JsonProvider provider;

        private sealed class JsonProvider : ILevelProvider
        {
            public readonly Dictionary<string, string> Content = new Dictionary<string, string>();
            public string Requested;
            public LevelDefinition GetLevel(string id)
            {
                Requested = id;
                if (!Content.TryGetValue(id, out var json))
                    throw new FileNotFoundException(id);
                return LevelJsonSerializer.Deserialize(json);
            }
        }

        [SetUp]
        public void Create()
        {
            host = new GameObject("GeneralControllerTest");
            game = host.AddComponent<LevelOneGameplay>();
            TestProgressStore.OpenFixtures(game, "fixture-alpha", "fixture-beta", "missing", "broken");
            provider = new JsonProvider();
            provider.Content["fixture-alpha"] = LevelJsonSerializer.Serialize(Fixture());
            provider.Content["fixture-beta"] = LevelJsonSerializer.Serialize(new LevelDefinition("fixture-beta", 1, MovementEngine.RulesVersion, 5, 5,
                new Cell(4, 0), new[] { new PieceDefinition("comet", PieceKind.Target, 1, 1, new Cell(0, 0)) }, Array.Empty<Cell>(), Array.Empty<Cell>()));
            game.SetLevelProvider(provider);
        }
        [TearDown] public void Destroy() => UnityEngine.Object.DestroyImmediate(host);

        private static LevelDefinition Fixture() => new LevelDefinition("fixture-alpha", 1, MovementEngine.RulesVersion, 5, 5,
            new Cell(4, 4), new[] {
                new PieceDefinition("hero", PieceKind.Target, 1, 1, new Cell(0, 4)),
                new PieceDefinition("bridge", PieceKind.Obstacle, 2, 1, new Cell(2, 0)),
                new PieceDefinition("pillar", PieceKind.Obstacle, 1, 2, new Cell(4, 0)) },
            new[] { new Cell(2, 3) }, Array.Empty<Cell>());

        private void Load(string id) => game.LoadLevel(id);

        [Test]
        public void RequestedIdAndArbitraryPiecesComeFromJson()
        {
            Load("fixture-alpha");
            Assert.That(provider.Requested, Is.EqualTo("fixture-alpha"));
            Assert.That(game.LevelId, Is.EqualTo("fixture-alpha"));
            Assert.That(game.Session.Level.Width, Is.EqualTo(5));
            Assert.That(game.Session.Level.Pieces.Count, Is.EqualTo(3));
            Assert.That(game.Session.Level.FixedCells, Is.EqualTo(new[] { new Cell(2, 3) }));
            foreach (var piece in game.Session.Level.Pieces)
                Assert.That(MovementEngine.Position(game.Session.State, piece.Id), Is.EqualTo(piece.Start));
        }

        [Test]
        public void CoreMovementRestrictionsCollisionBoundaryAndNoOpArePreserved()
        {
            Load("fixture-alpha");
            Assert.That(game.Session.Commit(new MoveCommand("bridge", MoveAxis.Vertical, 1)).Changed, Is.False);
            Assert.That(game.Session.Commit(new MoveCommand("bridge", MoveAxis.Horizontal, 5)).Changed, Is.False, "pillar yolu kapatır");
            Assert.That(game.Session.MoveCount, Is.Zero);
            Assert.That(game.Session.Commit(new MoveCommand("bridge", MoveAxis.Horizontal, -1)).Changed, Is.True);
            game.Session.Commit(new MoveCommand("bridge", MoveAxis.Horizontal, -20));
            Assert.That(MovementEngine.Position(game.Session.State, "bridge"), Is.EqualTo(new Cell(0, 0)));
            game.Session.Commit(new MoveCommand("bridge", MoveAxis.Horizontal, 20));
            Assert.That(MovementEngine.Position(game.Session.State, "bridge"), Is.EqualTo(new Cell(2, 0)));
            Assert.That(game.Session.MoveCount, Is.EqualTo(3));
        }

        [Test]
        public void ActiveExitWinsAndRestartRestoresExactInitialState()
        {
            Load("fixture-alpha");
            var initial = BoardSolver.Key(game.Session.State);
            game.Session.Commit(new MoveCommand("hero", MoveAxis.Horizontal, 20));
            Assert.That(game.Session.Won, Is.True);
            Assert.That(MovementEngine.Position(game.Session.State, "hero"), Is.EqualTo(new Cell(4, 4)));
            game.Restart();
            Assert.That(game.Session.Won, Is.False);
            Assert.That(game.Session.MoveCount, Is.Zero);
            Assert.That(BoardSolver.Key(game.Session.State), Is.EqualTo(initial));
        }

        [Test]
        public void ReloadChangesPieceCountAndRestartUsesNewLevel()
        {
            Load("fixture-alpha");
            game.Session.Commit(new MoveCommand("hero", MoveAxis.Horizontal, 4));
            Load("fixture-beta");
            Assert.That(game.Session.Level.Pieces.Count, Is.EqualTo(1));
            Assert.That(game.Session.Won, Is.False);
            Assert.That(game.Session.MoveCount, Is.Zero);
            game.Session.Commit(new MoveCommand("comet", MoveAxis.Horizontal, 4));
            Assert.That(game.Session.Won, Is.True);
            game.Restart();
            Assert.That(MovementEngine.Position(game.Session.State, "comet"), Is.EqualTo(new Cell(0, 0)));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void EmptyIdIsRejectedBeforeProvider(string id)
        {
            Assert.Throws<ArgumentException>(() => Load(id));
            Assert.That(provider.Requested, Is.Null);
            Assert.That(game.Session, Is.Null);
        }

        [Test]
        public void MissingOrInvalidJsonIsExplicitAndLeavesActiveSessionIntact()
        {
            Load("fixture-alpha");
            var active = game.Session;
            Assert.Throws<FileNotFoundException>(() => Load("missing"));
            provider.Content["broken"] = "{broken";
            Assert.Throws<FormatException>(() => Load("broken"));
            Assert.That(game.Session, Is.SameAs(active));
            Assert.That(game.LevelId, Is.EqualTo("fixture-alpha"));
        }

        [Test]
        public void SceneConfiguresOneControllerAndInitialJsonId()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/BlockRift_Gameplay.unity");
            try
            {
                var controllers = scene.GetRootGameObjects()[0].GetComponentsInChildren<LevelOneGameplay>(true);
                Assert.That(controllers.Length, Is.EqualTo(1));
                Assert.That(new SerializedObject(controllers[0]).FindProperty("levelId").stringValue, Is.EqualTo("level-001"));
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(controllers[0].gameObject), Is.Zero);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
