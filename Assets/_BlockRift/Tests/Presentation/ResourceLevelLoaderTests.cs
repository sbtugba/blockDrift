using System;
using System.IO;
using System.Linq;
using BlockRift.Core;
using BlockRift.Presentation;
using NUnit.Framework;

namespace BlockRift.Tests
{
    /// <summary>Unity Resources üzerinden seviye yükleme servisinin EditMode testleridir.</summary>
    public sealed class ResourceLevelLoaderTests
    {
        [Test]
        public void MissingResourceDoesNotReadUnimportedSourceFile()
        {
            // Kaynak dosyası mevcut olsa da import edilmemiş içerik paketlenmiş Resources değildir.
            string id = "unimported-" + Guid.NewGuid().ToString("N");
            string path = Path.Combine("Assets", "_BlockRift", "Content", "Resources", "Levels", id + ".json");
            try
            {
                File.WriteAllText(path, LevelJsonSerializer.Serialize(LevelOne.Create()));
                Assert.That(UnityEngine.Resources.Load<UnityEngine.TextAsset>("Levels/" + id), Is.Null);
                Assert.Throws<FileNotFoundException>(() => ResourceLevelLoader.LoadLevel(id));
            }
            finally
            {
                File.Delete(path);
                if (File.Exists(path + ".meta")) File.Delete(path + ".meta");
            }
        }

        [Test]
        public void ResourceLevelLoaderLoadsLevel001Successfully()
        {
            var level = ResourceLevelLoader.LoadLevel("level-001");
            Assert.That(level, Is.Not.Null);
            Assert.That(level.Id, Is.EqualTo("level-001"));
            Assert.That(level.Width, Is.EqualTo(6));
            Assert.That(level.Height, Is.EqualTo(6));
            Assert.That(level.Exit, Is.EqualTo(new Cell(5, 0)));
            Assert.That(level.Pieces.Count, Is.EqualTo(7));

            var reference = LevelOne.Create();
            Assert.That(level.Pieces.Select(p => p.Id).OrderBy(x => x),
                Is.EqualTo(reference.Pieces.Select(p => p.Id).OrderBy(x => x)));

            var proof = BoardSolver.Solve(level, BoardState.CreateInitial(level));
            Assert.That(proof.Status, Is.EqualTo(SearchStatus.Solved));
            Assert.That(proof.Path.Count, Is.EqualTo(8));
        }

        [Test]
        public void ResourceLevelLoaderImplementsILevelProvider()
        {
            ILevelProvider provider = new ResourceLevelLoader();
            var level = provider.GetLevel("level-001");
            Assert.That(level, Is.Not.Null);
            Assert.That(level.Id, Is.EqualTo("level-001"));
        }

        [Test]
        public void ResourceLevelLoaderThrowsFileNotFoundForNonExistentLevel()
        {
            Assert.Throws<FileNotFoundException>(() => ResourceLevelLoader.LoadLevel("non_existent_level_99999"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void ResourceLevelLoaderThrowsArgumentExceptionForInvalidId(string invalidId)
        {
            Assert.Throws<ArgumentException>(() => ResourceLevelLoader.LoadLevel(invalidId));
        }
    }
}
