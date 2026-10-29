using System;
using BlockRift.Core;
using BlockRift.Presentation;
using NUnit.Framework;
using UnityEditor;

namespace BlockRift.Tests
{
    /// <summary>Mevcut katalog üzerinde tür/boyut ve renk anahtarı sözleşmesini doğrular.</summary>
    public sealed class VisualMappingTests
    {
        private VisualPrefabCatalog catalog;
        [SetUp] public void Load() => catalog = AssetDatabase.LoadAssetAtPath<VisualPrefabCatalog>("Assets/_BlockRift/Content/GameplayVisualCatalog.asset");

        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(3, 1)]
        [TestCase(4, 1)]
        [TestCase(1, 2)]
        [TestCase(1, 3)]
        [TestCase(1, 4)]
        [TestCase(2, 2)]
        public void ShapeSelectsSamePrefabRegardlessOfPieceId(int width, int height)
        {
            var first = catalog.ResolvePiece(new PieceDefinition("X", PieceKind.Obstacle, width, height, new Cell(0, 0)));
            var second = catalog.ResolvePiece(new PieceDefinition("K", PieceKind.Obstacle, width, height, new Cell(0, 0)));
            Assert.That(first.Prefab, Is.SameAs(second.Prefab));
            Assert.That(first.AssetId, Is.EqualTo("Block_" + width + "x" + height));
            Assert.That(first.Width, Is.EqualTo(width));
            Assert.That(first.Height, Is.EqualTo(height));
        }

        [Test]
        public void StarAndStoneUseSpecialPrefabs()
        {
            var star = catalog.ResolvePiece(new PieceDefinition("anything", PieceKind.Target, 1, 1, new Cell(0, 0)));
            Assert.That(star.AssetId, Is.EqualTo("Special_1x1_Star"));
            Assert.That(catalog.ResolveFixedStone().AssetId, Is.EqualTo("Obstacle_1x1_Stone"));
        }

        [Test]
        public void UnsupportedThreeByThreeFailsExplicitly() => Assert.Throws<ArgumentException>(() =>
            catalog.ResolvePiece(new PieceDefinition("bad", PieceKind.Obstacle, 3, 3, new Cell(0, 0))));

        [TestCase("blue", "BR_Plastic_Blue")]
        [TestCase("green", "BR_Plastic_Green")]
        [TestCase("yellow", "BR_Plastic_Yellow")]
        [TestCase("red", "BR_Plastic_Red")]
        [TestCase("purple", "BR_Plastic_Purple")]
        [TestCase("orange", "BR_Plastic_Orange")]
        public void ColorKeyResolvesExistingSharedMaterial(string key, string expected)
        {
            var material = catalog.ResolveColor(key);
            Assert.That(material.name, Is.EqualTo(expected));
            Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));
            Assert.That(catalog.ResolveColor(key), Is.SameAs(material));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("unknown")]
        public void MissingOrUnknownColorFailsExplicitly(string key) => Assert.Throws<InvalidOperationException>(() => catalog.ResolveColor(key));
    }
}
