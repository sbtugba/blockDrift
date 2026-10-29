using System;
using System.IO;
using BlockRift.App;
using BlockRift.Core;
using NUnit.Framework;

namespace BlockRift.Tests
{
    /// <summary>Yayın sırası, aktif oturum kanıtı ve gerçek dosya kaydı regresyonları.</summary>
    public sealed class ProgressionTests
    {
        private const string Manifest = "{\"schemaVersion\":1,\"levels\":[\"chapter-z\",\"chapter-a\"]}";
        private string folder;
        private string PathToSave => Path.Combine(folder, "progress.json");
        [SetUp]
        public void Setup()
        {
            folder = Path.Combine(Path.GetTempPath(), "BlockRiftProgressTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
        }
        [TearDown]
        public void Cleanup()
        {
            if (!System.IO.Path.GetFullPath(folder).StartsWith(System.IO.Path.GetFullPath(System.IO.Path.GetTempPath()), StringComparison.OrdinalIgnoreCase))
                throw new Exception("Unsafe test path");
            Directory.Delete(folder, true);
        }
        private static GameSession Session(string id) => new GameSession(new LevelDefinition(id, 1, 2, 5, 5, new Cell(4, 0), new[] { new PieceDefinition("goal", PieceKind.Target, 1, 1, new Cell(0, 0)) }, Array.Empty<Cell>(), Array.Empty<Cell>()));
        private static void Win(GameSession s) => s.Commit(new MoveCommand("goal", MoveAxis.Horizontal, 4));

        [Test]
        public void ManifestOrderUnlockWinReplayAndFinalLevel()
        {
            var p = new LevelProgression(Manifest);
            Assert.That(p.State.Selected, Is.EqualTo("chapter-z"));
            Assert.That(p.IsUnlocked("chapter-z"), Is.True);
            Assert.That(p.IsUnlocked("chapter-a"), Is.False);
            Assert.Throws<InvalidOperationException>(() => p.Selection("chapter-a"));
            Assert.Throws<ArgumentException>(() => p.Selection("chapter-missing"));
            var s = Session("chapter-z");
            p.Activate(s, p.Selection(s.Level.Id));
            Assert.That(p.Completion(s), Is.Null);
            Win(s);
            var completed = p.Completion(s);
            Assert.That(completed, Is.Not.Null);
            p.AcceptCompletion(s, completed);
            Assert.That(p.Completion(s), Is.Null);
            Assert.That(p.IsUnlocked("chapter-a"), Is.True);
            Assert.That(p.NextLevel(), Is.EqualTo("chapter-a"));
            s.Restart();
            Win(s);
            Assert.That(p.Completion(s), Is.Null);
            var next = Session("chapter-a");
            p.Activate(next, p.Selection("chapter-a"));
            Assert.That(p.Completion(s), Is.Null);
            Win(next);
            p.AcceptCompletion(next, p.Completion(next));
            Assert.That(p.NextLevel(), Is.Null);
            Assert.That(p.State.Completed.Count, Is.EqualTo(2));
            Assert.That(p.IsUnlocked("chapter-z"), Is.True);
        }

        [Test]
        public void StaleWonSessionCannotCompleteNewActiveSession()
        {
            var p = new LevelProgression(Manifest);
            var old = Session("chapter-z");
            p.Activate(old, p.Selection("chapter-z"));
            Win(old);
            var fresh = Session("chapter-z");
            p.Activate(fresh, p.Selection("chapter-z"));
            Assert.That(p.Completion(old), Is.Null);
            Assert.That(p.State.Completed, Is.Empty);
        }

        [TestCase("{}")]
        [TestCase("{broken")]
        [TestCase("{\"schemaVersion\":2,\"levels\":[\"x\"]}")]
        [TestCase("{\"schemaVersion\":1,\"levels\":[]}")]
        [TestCase("{\"schemaVersion\":1,\"levels\":[\"x\",\"x\"]}")]
        [TestCase("{\"schemaVersion\":1,\"levels\":[\"\"]}")]
        public void InvalidManifestIsExplicit(string json) => Assert.Throws<FormatException>(() => new LevelProgression(json));

        [Test]
        public void CatalogRemovalAndLockedSelectionReconcileDeterministically()
        {
            var p = new LevelProgression(Manifest, new LevelProgression.Snapshot(1, new[] { "removed", "chapter-z" }, "removed"));
            Assert.That(p.State.Completed, Is.EqualTo(new[] { "chapter-z" }));
            Assert.That(p.State.Selected, Is.EqualTo("chapter-a"));
            Assert.That(p.ReconciliationNotice, Is.Not.Null.And.Not.Empty);
            var locked = new LevelProgression(Manifest, new LevelProgression.Snapshot(1, Array.Empty<string>(), "chapter-a"));
            Assert.That(locked.State.Selected, Is.EqualTo("chapter-z"));
        }

        [Test]
        public void SaveLoadRoundtripReopenAndInterruptedTemp()
        {
            var store = new JsonProgressStore(PathToSave);
            Assert.That(store.Load(), Is.Null);
            store.Save(new LevelProgression.Snapshot(1, new[] { "chapter-z" }, "chapter-a"));
            File.WriteAllText(PathToSave + ".tmp", "interrupted");
            var loaded = new JsonProgressStore(PathToSave).Load();
            Assert.That(loaded.Completed, Is.EqualTo(new[] { "chapter-z" }));
            Assert.That(loaded.Selected, Is.EqualTo("chapter-a"));
            Assert.That(new LevelProgression(Manifest, loaded).State.Selected, Is.EqualTo("chapter-a"));
        }

        [TestCase("broken")]
        [TestCase("")]
        [TestCase("   ")]
        public void BackupRecoveryIsExplicitAndCorruptBothNeverResetsSilently(string corrupt)
        {
            var store = new JsonProgressStore(PathToSave);
            store.Save(new LevelProgression.Snapshot(1, Array.Empty<string>(), "chapter-z"));
            store.Save(new LevelProgression.Snapshot(1, new[] { "chapter-z" }, "chapter-a"));
            File.WriteAllText(PathToSave, corrupt);
            var recovered = store.Load();
            Assert.That(recovered.Selected, Is.EqualTo("chapter-z"));
            Assert.That(store.RecoveryNotice, Is.Not.Null.And.Not.Empty);
            File.WriteAllText(PathToSave + ".bak", "broken");
            Assert.Throws<FormatException>(() => store.Load());
        }

        [Test]
        public void UnsupportedSaveVersionIsNeverSilentlyRecovered()
        {
            var store = new JsonProgressStore(PathToSave);
            store.Save(new LevelProgression.Snapshot(1, Array.Empty<string>(), "chapter-z"));
            store.Save(new LevelProgression.Snapshot(1, Array.Empty<string>(), "chapter-z"));
            File.WriteAllText(PathToSave, "{\"saveSchemaVersion\":99,\"completed\":[],\"selected\":\"chapter-z\"}");
            Assert.Throws<NotSupportedException>(() => store.Load());
        }

        [Test]
        public void WriteFailurePreservesPreviousGoodSave()
        {
            var store = new JsonProgressStore(PathToSave);
            store.Save(new LevelProgression.Snapshot(1, Array.Empty<string>(), "chapter-z"));
            Directory.CreateDirectory(PathToSave + ".tmp");
            Assert.That(() => store.Save(new LevelProgression.Snapshot(1, new[] { "chapter-z" }, "chapter-a")), Throws.Exception);
            Assert.That(store.Load().Selected, Is.EqualTo("chapter-z"));
        }
    }
}
