using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BlockRift.Core;
using BlockRift.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BlockRift.Tests
{
    /// <summary>Şema-1 ek görsel alanlarını ve domain'den bağımsız roundtrip sözleşmesini denetler.</summary>
    public sealed class LevelVisualDataTests
    {
        private VisualPrefabCatalog Catalog => AssetDatabase.LoadAssetAtPath<VisualPrefabCatalog>("Assets/_BlockRift/Content/GameplayVisualCatalog.asset");
        private static string Json => File.ReadAllText("Assets/_BlockRift/Content/Resources/Levels/level-001.json");

        [Test]
        public void LevelOneColorsRoundtripWithoutChangingCoreDefinition()
        {
            var data = LevelVisualData.Deserialize(Json);
            Assert.That(data.VisualKeys.Count, Is.EqualTo(6));
            string[] ids = { "A", "B", "C", "D", "E", "F" };
            string[] keys = { "blue", "green", "yellow", "red", "purple", "orange" };
            for (int i = 0; i < ids.Length; i++)
                Assert.That(data.RequireKey(ids[i]), Is.EqualTo(keys[i]));
            var copy = LevelVisualData.Deserialize(data.Serialize());
            Assert.That(copy.VisualKeys, Is.EquivalentTo(data.VisualKeys));
            Assert.That(LevelJsonSerializer.Serialize(copy.Definition), Is.EqualTo(LevelJsonSerializer.Serialize(LevelOne.Create())));
            Assert.That(copy.Definition.SchemaVersion, Is.EqualTo(1));
            Assert.That(copy.Definition.RulesVersion, Is.EqualTo(2));
            var star = data.Definition.Pieces.Single(p => p.Kind == PieceKind.Target);
            Assert.That(Catalog.ResolveMaterial(star, data), Is.Null, "Yıldız prefab materyallerini korur");
        }

        [Test]
        public void LegacyJsonRemainsReadableButMissingColorIsExplicitAtRendering()
        {
            var data = LevelVisualData.Deserialize(LevelJsonSerializer.Serialize(LevelOne.Create()));
            Assert.That(data.VisualKeys, Is.Empty);
            Assert.Throws<InvalidOperationException>(() => Catalog.ResolveMaterial(data.Definition.Pieces.Single(p => p.Id == "A"), data));
            Assert.That(LevelVisualData.Deserialize(data.Serialize()).VisualKeys, Is.Empty);
        }

        [TestCase("null")]
        [TestCase("123")]
        [TestCase("\"\"")]
        [TestCase("\" \"")]
        public void InvalidVisualValueFails(string value) => Assert.Throws<FormatException>(() =>
            LevelVisualData.Deserialize(Json.Replace("\"blue\"", value)));

        [Test]
        public void UnknownKeyIsNotReplacedWithDefaultColor()
        {
            var data = LevelVisualData.Deserialize(Json.Replace("\"blue\"", "\"typo\""));
            Assert.Throws<InvalidOperationException>(() => Catalog.ResolveMaterial(data.Definition.Pieces.Single(p => p.Id == "A"), data));
        }

        [TestCase("schemaVersion", 1, 99)]
        [TestCase("rulesVersion", 2, 99)]
        public void ExistingVersionValidationStillApplies(string field, int oldVersion, int version)
        {
            string invalid = Json.Replace("\"" + field + "\": " + oldVersion, "\"" + field + "\": " + version);
            if (field == "schemaVersion")
                Assert.Throws<FormatException>(() => LevelVisualData.Deserialize(invalid));
            else
                Assert.Throws<LevelValidationException>(() => LevelVisualData.Deserialize(invalid));
        }

        [Test]
        public void VisualKeysDoNotChangeSolverStateOrMovement()
        {
            var first = LevelVisualData.Deserialize(Json);
            var second = LevelVisualData.Deserialize(Json.Replace("\"blue\"", "\"orange\""));
            Assert.That(LevelJsonSerializer.Serialize(first.Definition), Is.EqualTo(LevelJsonSerializer.Serialize(second.Definition)));
            var move = new MoveCommand("C", MoveAxis.Vertical, 1);
            Assert.That(BoardSolver.Key(MovementEngine.Apply(first.Definition, BoardState.CreateInitial(first.Definition), move).State),
                Is.EqualTo(BoardSolver.Key(MovementEngine.Apply(second.Definition, BoardState.CreateInitial(second.Definition), move).State)));
        }

        [Test]
        public void SameShapeDifferentColorsAndDifferentShapesSameColorUseSharedAssets()
        {
            var level = LevelOne.Create();
            var data = new LevelVisualData(level, new Dictionary<string, string> { { "A", "red" }, { "C", "red" }, { "D", "blue" } });
            var a = level.Pieces.Single(p => p.Id == "A");
            var c = level.Pieces.Single(p => p.Id == "C");
            Assert.That(Catalog.ResolveMaterial(a, data), Is.SameAs(Catalog.ResolveMaterial(c, data)));
            var alternate = new LevelVisualData(level, new Dictionary<string, string> { { "A", "green" } });
            var otherId = new PieceDefinition("another-level-id", PieceKind.Obstacle, a.Width, a.Height, new Cell(0, 0));
            Assert.That(Catalog.ResolvePiece(otherId).Prefab, Is.SameAs(Catalog.ResolvePiece(a).Prefab));
            Assert.That(Catalog.ResolveMaterial(a, alternate), Is.Not.SameAs(Catalog.ResolveMaterial(a, data)));
        }

        [TestCase("width")]
        [TestCase("prefab")]
        public void InvalidCatalogEntryFailsWithoutChangingSourceAsset(string field)
        {
            var clone = UnityEngine.Object.Instantiate(Catalog);
            try
            {
                var edit = new SerializedObject(clone);
                var entry = edit.FindProperty("entries").GetArrayElementAtIndex(0);
                if (field == "width")
                    entry.FindPropertyRelative(field).intValue = 2;
                else
                    entry.FindPropertyRelative(field).objectReferenceValue = null;
                edit.ApplyModifiedPropertiesWithoutUndo();
                Assert.Throws<InvalidOperationException>(() => clone.ResolvePiece(new PieceDefinition("any", PieceKind.Obstacle, 1, 1, new Cell(0, 0))));
                Assert.That(Catalog.ResolvePiece(new PieceDefinition("any", PieceKind.Obstacle, 1, 1, new Cell(0, 0))).Width, Is.EqualTo(1));
            }
            finally { UnityEngine.Object.DestroyImmediate(clone); }
        }
    }
}
