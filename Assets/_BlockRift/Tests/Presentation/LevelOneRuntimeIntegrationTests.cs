using System;
using System.IO;
using System.Linq;
using BlockRift.Core;
using BlockRift.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace BlockRift.Tests
{
    /// <summary>
    /// LevelOneGameplay'in çalışma zamanında LevelOne.Create yerine ILevelProvider ve JSON
    /// üzerinden başlatıldığını, sessiz geri dönüş (fallback) olmadığını ve yaşam döngüsünü doğrular.
    /// </summary>
    public sealed class LevelOneRuntimeIntegrationTests
    {
        private sealed class FakeProvider : ILevelProvider
        {
            private readonly LevelDefinition level;
            public FakeProvider(LevelDefinition level) => this.level = level;
            public LevelDefinition GetLevel(string levelId) => level;
        }

        private sealed class ThrowingProvider : ILevelProvider
        {
            private readonly Exception exception;
            public ThrowingProvider(Exception exception) => this.exception = exception;
            public LevelDefinition GetLevel(string levelId) => throw exception;
        }

        [Test]
        public void DefaultRuntimeLoadsLevelFromJsonDirectly()
        {
            var go = new GameObject("TestGameplay");
            try
            {
                var gameplay = go.AddComponent<LevelOneGameplay>();
                TestProgressStore.OpenFixtures(gameplay, "level-001", "custom-runtime-5x5", "missing", "invalid");
                Assert.That(string.IsNullOrEmpty(gameplay.LevelId), Is.True, "ID sahne veya çağıran tarafından sağlanmalı");

                // InitializeLevel çağrıldığında ResourceLevelLoader üzerinden JSON yüklenir
                gameplay.LoadLevel("level-001");

                Assert.That(gameplay.Session, Is.Not.Null);
                Assert.That(gameplay.Session.Level.Id, Is.EqualTo("level-001"));
                Assert.That(gameplay.Session.Level.RulesVersion, Is.EqualTo(MovementEngine.RulesVersion));
                Assert.That(gameplay.Session.Level.Width, Is.EqualTo(6));
                Assert.That(gameplay.Session.Level.Height, Is.EqualTo(6));
                Assert.That(gameplay.Session.Level.Exit, Is.EqualTo(new Cell(5, 0)));
                Assert.That(gameplay.Session.Level.Pieces.Count, Is.EqualTo(7));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RuntimeBypassesLevelOneCreateWhenCustomProviderInjected()
        {
            var go = new GameObject("TestGameplay");
            try
            {
                var gameplay = go.AddComponent<LevelOneGameplay>();
                TestProgressStore.OpenFixtures(gameplay, "level-001", "custom-runtime-5x5", "missing", "invalid");

                // Farklı bir test bölümü üretiyoruz (LevelOne'dan tamamen farklı 5x5)
                var customLevel = new LevelDefinition("custom-runtime-5x5", 1, MovementEngine.RulesVersion, 5, 5,
                    new Cell(4, 0),
                    new[] { new PieceDefinition("star", PieceKind.Target, 1, 1, new Cell(0, 0)) },
                    Array.Empty<Cell>(), Array.Empty<Cell>());

                gameplay.SetLevelProvider(new FakeProvider(customLevel));
                gameplay.LoadLevel(customLevel.Id);

                // Session.Level'ın LevelOne.Create değil, enjekte edilen provider'dan geldiği kanıtlanır
                Assert.That(gameplay.Session.Level.Id, Is.EqualTo("custom-runtime-5x5"));
                Assert.That(gameplay.Session.Level.Width, Is.EqualTo(5));
                Assert.That(gameplay.Session.Level.Pieces.Count, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RuntimeThrowsFileNotFoundWhenJsonIsMissingAndNeverFallsBackToLevelOne()
        {
            var go = new GameObject("TestGameplay");
            try
            {
                var gameplay = go.AddComponent<LevelOneGameplay>();
                TestProgressStore.OpenFixtures(gameplay, "level-001", "custom-runtime-5x5", "missing", "invalid");
                gameplay.SetLevelProvider(new ThrowingProvider(new FileNotFoundException("level-99999.json bulunamadı")));

                // JSON bulunamadığında LevelOne.Create'e sessiz dönüş yapılmamalı, hata fırlatılmalıdır
                Assert.Throws<FileNotFoundException>(() => gameplay.LoadLevel("missing"));
                Assert.That(gameplay.Session, Is.Null, "Hata durumunda oturum başlatılmamalı");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RuntimeThrowsWhenJsonDataIsInvalidAndNeverFallsBackToLevelOne()
        {
            var go = new GameObject("TestGameplay");
            try
            {
                var gameplay = go.AddComponent<LevelOneGameplay>();
                TestProgressStore.OpenFixtures(gameplay, "level-001", "custom-runtime-5x5", "missing", "invalid");
                var validationResult = BoardValidator.Validate(null);
                gameplay.SetLevelProvider(new ThrowingProvider(new LevelValidationException("Geçersiz veri", validationResult)));

                Assert.Throws<LevelValidationException>(() => gameplay.LoadLevel("invalid"));
                Assert.That(gameplay.Session, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RuntimeRestartRestoresExactJsonInitialState()
        {
            var go = new GameObject("TestGameplay");
            try
            {
                var gameplay = go.AddComponent<LevelOneGameplay>();
                TestProgressStore.OpenFixtures(gameplay, "level-001", "custom-runtime-5x5", "missing", "invalid");
                gameplay.LoadLevel("level-001");

                // Başlangıç durumu
                var initialPosC = MovementEngine.Position(gameplay.Session.State, "C");
                Assert.That(initialPosC, Is.EqualTo(new Cell(2, 0)));

                // Hamle yap
                var result = gameplay.Session.Commit(new MoveCommand("C", MoveAxis.Vertical, 1));
                Assert.That(result.Changed, Is.True);
                Assert.That(gameplay.Session.MoveCount, Is.EqualTo(1));
                Assert.That(MovementEngine.Position(gameplay.Session.State, "C"), Is.EqualTo(new Cell(2, 1)));

                // Restart çağır
                gameplay.Restart();
                Assert.That(gameplay.Session.MoveCount, Is.EqualTo(0));
                Assert.That(MovementEngine.Position(gameplay.Session.State, "C"), Is.EqualTo(new Cell(2, 0)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RuntimeSolverReplayAchievesWinOnJsonLevel()
        {
            var go = new GameObject("TestGameplay");
            try
            {
                var gameplay = go.AddComponent<LevelOneGameplay>();
                TestProgressStore.OpenFixtures(gameplay, "level-001", "custom-runtime-5x5", "missing", "invalid");
                gameplay.LoadLevel("level-001");

                var proof = BoardSolver.Solve(gameplay.Session.Level, gameplay.Session.State);
                Assert.That(proof.Status, Is.EqualTo(SearchStatus.Solved));
                Assert.That(proof.Path.Count, Is.EqualTo(8));

                foreach (var move in proof.Path)
                {
                    var res = gameplay.Session.Commit(move);
                    Assert.That(res.Changed, Is.True);
                }

                Assert.That(gameplay.Session.Won, Is.True);
                Assert.That(gameplay.Session.MoveCount, Is.EqualTo(8));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
