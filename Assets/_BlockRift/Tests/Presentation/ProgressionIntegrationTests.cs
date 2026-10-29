using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BlockRift.App;
using BlockRift.Core;
using BlockRift.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace BlockRift.Tests
{
    /// <summary>Testlere özel bellek kaydı; kullanıcı persistentDataPath dosyasına erişmez.</summary>
    public sealed class TestProgressStore : IProgressStore
    {
        public LevelProgression.Snapshot Data; public bool Fail; public int Writes;
        public LevelProgression.Snapshot Load() => Data;
        public void Save(LevelProgression.Snapshot data)
        {
            if (Fail)
                throw new IOException("Test write failure");
            Data = data;
            Writes++;
        }
        public static string Manifest(params string[] ids) => LevelJsonSerializer.WriteJsonObject(new Dictionary<string, object> { { "schemaVersion", 1 }, { "levels", ids.Cast<object>().ToList() } });
        public static void OpenFixtures(LevelOneGameplay game, params string[] ids) => game.ConfigureProgression(Manifest(ids), new TestProgressStore { Data = new LevelProgression.Snapshot(1, ids, ids[0]) });
    }

    /// <summary>İlerleme bildiriminin mevcut controller ve GameSession bağlantısını doğrular.</summary>
    public sealed class ProgressionIntegrationTests
    {
        private GameObject host; private LevelOneGameplay game; private TestProgressStore store; private Provider provider;
        private sealed class Provider : ILevelProvider
        {
            public bool Fail;
            public LevelDefinition GetLevel(string id)
            {
                if (Fail)
                    throw new FileNotFoundException(id);
                return new LevelDefinition(id, 1, 2, 5, 5, new Cell(4, 0), new[] { new PieceDefinition("goal", PieceKind.Target, 1, 1, new Cell(0, 0)) }, Array.Empty<Cell>(), Array.Empty<Cell>());
            }
        }
        [SetUp]
        public void Setup()
        {
            host = new GameObject("ProgressionIntegration");
            game = host.AddComponent<LevelOneGameplay>();
            store = new TestProgressStore();
            provider = new Provider();
            game.ConfigureProgression(TestProgressStore.Manifest("chapter-z", "chapter-a"), store);
            game.SetLevelProvider(provider);
        }
        [TearDown] public void Cleanup() => UnityEngine.Object.DestroyImmediate(host);
        private void Win() => game.Session.Commit(new MoveCommand("goal", MoveAxis.Horizontal, 4));

        [Test]
        public void WinRestartReplayAndNextUseExistingSessionAndLoader()
        {
            game.InitializeLevel();
            Assert.That(game.LevelId, Is.EqualTo("chapter-z"));
            Assert.Throws<InvalidOperationException>(() => game.LoadLevel("chapter-a"));
            Win();
            Assert.That(store.Data.Completed, Is.EqualTo(new[] { "chapter-z" }));
            int writes = store.Writes;
            game.Restart();
            Win();
            Assert.That(store.Writes, Is.EqualTo(writes));
            var old = game.Session;
            Assert.That(game.LoadNextLevel(), Is.True);
            old.Restart();
            old.Commit(new MoveCommand("goal", MoveAxis.Horizontal, 4));
            Assert.That(game.Progression.State.Completed.Count, Is.EqualTo(1));
            Win();
            Assert.That(game.LoadNextLevel(), Is.False);
            Assert.That(game.LevelId, Is.EqualTo("chapter-a"));
        }
        [Test]
        public void FailedLoadAndFailedWinSaveNeverReportProgressSuccess()
        {
            game.InitializeLevel();
            var session = game.Session;
            provider.Fail = true;
            Assert.Throws<FileNotFoundException>(() => game.LoadLevel("chapter-z"));
            Assert.That(game.Session, Is.SameAs(session));
            provider.Fail = false;
            store.Fail = true;
            Assert.Throws<IOException>(() => game.LoadLevel("chapter-z"));
            Assert.That(game.Session, Is.SameAs(session));
            Assert.Throws<IOException>(() => Win());
            Assert.That(game.Progression.State.Completed, Is.Empty);
            Assert.That(game.ProgressError, Does.Contain("kaydedilemedi"));
            store.Fail = false;
            game.PersistCompletion();
            Assert.That(game.Progression.State.Completed.Count, Is.EqualTo(1));
            Assert.That(game.ProgressError, Is.Null);
        }
        [Test]
        public void PersistedAccessibleSelectionOverridesInspectorDefault()
        {
            store.Data = new LevelProgression.Snapshot(1, new[] { "chapter-z" }, "chapter-a");
            game.ConfigureProgression(TestProgressStore.Manifest("chapter-z", "chapter-a"), store);
            game.InitializeLevel();
            Assert.That(game.LevelId, Is.EqualTo("chapter-a"));
            Assert.That(game.Session.MoveCount, Is.Zero);
        }
        [Test]
        public void RealManifestPublishesOnlyExistingLevelOne()
        {
            var catalog = new LevelProgression(ResourceLevelLoader.LoadPublishedManifest());
            Assert.That(catalog.Levels, Is.EqualTo(new[] { "level-001" }));
            foreach (var id in catalog.Levels)
                Assert.That(ResourceLevelLoader.LoadLevel(id).Id, Is.EqualTo(id));
            Assert.Throws<ArgumentException>(() => catalog.Selection("level-002"));
        }
    }
}
