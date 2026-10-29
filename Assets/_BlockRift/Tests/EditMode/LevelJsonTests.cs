using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using BlockRift.Core;

namespace BlockRift.Tests
{
    /// <summary>Veri odaklı bölüm serileştirme, yükleme, şema ve invariant doğrulama testleridir.</summary>
    public sealed class LevelJsonTests
    {
        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(2)]
        [TestCase(99)]
        public void UnsupportedSchemaIsRejectedExplicitly(int version)
        {
            string json = LevelJsonSerializer.Serialize(LevelOne.Create()).Replace("\"schemaVersion\": 1", "\"schemaVersion\": " + version);
            Assert.Throws<FormatException>(() => LevelJsonSerializer.Deserialize(json));
        }

        [Test]
        public void SerializeLevelOneAndDeserializeMatchesExactDefinition()
        {
            var original = LevelOne.Create();
            string json = LevelJsonSerializer.Serialize(original);

            Assert.That(string.IsNullOrWhiteSpace(json), Is.False);
            Assert.That(json.Contains("\"schemaVersion\": 1"), Is.True);
            Assert.That(json.Contains("\"rulesVersion\": 2"), Is.True);

            var loaded = LevelJsonSerializer.Deserialize(json);

            Assert.That(loaded.Id, Is.EqualTo(original.Id));
            Assert.That(loaded.SchemaVersion, Is.EqualTo(original.SchemaVersion));
            Assert.That(loaded.RulesVersion, Is.EqualTo(original.RulesVersion));
            Assert.That(loaded.Width, Is.EqualTo(original.Width));
            Assert.That(loaded.Height, Is.EqualTo(original.Height));
            Assert.That(loaded.Exit, Is.EqualTo(original.Exit));

            Assert.That(loaded.Pieces.Count, Is.EqualTo(original.Pieces.Count));
            for (int i = 0; i < original.Pieces.Count; i++)
            {
                var o = original.Pieces[i];
                var l = loaded.Pieces.FirstOrDefault(p => p.Id == o.Id);
                Assert.That(l, Is.Not.Null, $"Parça bulunamadı: {o.Id}");
                Assert.That(l.Kind, Is.EqualTo(o.Kind));
                Assert.That(l.Width, Is.EqualTo(o.Width));
                Assert.That(l.Height, Is.EqualTo(o.Height));
                Assert.That(l.Start, Is.EqualTo(o.Start));
            }

            Assert.That(loaded.FixedCells.Count, Is.EqualTo(original.FixedCells.Count));
            foreach (var cell in original.FixedCells)
            {
                Assert.That(loaded.FixedCells.Contains(cell), Is.True);
            }

            Assert.That(loaded.Triggers.Count, Is.EqualTo(original.Triggers.Count));

            var proof = BoardSolver.Solve(loaded, BoardState.CreateInitial(loaded));
            Assert.That(proof.Status, Is.EqualTo(SearchStatus.Solved));
            Assert.That(proof.Path.Count, Is.EqualTo(8));

            var state = BoardState.CreateInitial(loaded);
            foreach (var move in proof.Path)
                state = MovementEngine.Apply(loaded, state, move).State;
            Assert.That(MovementEngine.IsWon(loaded, state), Is.True);
        }

        [Test]
        public void ResourcesLevel001JsonMatchesLevelOne()
        {
            string path = Path.Combine("Assets", "_BlockRift", "Content", "Resources", "Levels", "level-001.json");
            Assert.That(File.Exists(path), Is.True, $"Seviye JSON dosyası bulunamadı: {path}");

            string json = File.ReadAllText(path);
            var loaded = LevelJsonSerializer.Deserialize(json);
            var reference = LevelOne.Create();

            Assert.That(loaded.Id, Is.EqualTo(reference.Id));
            Assert.That(loaded.Width, Is.EqualTo(reference.Width));
            Assert.That(loaded.Height, Is.EqualTo(reference.Height));
            Assert.That(loaded.Exit, Is.EqualTo(reference.Exit));
            Assert.That(loaded.Pieces.Count, Is.EqualTo(reference.Pieces.Count));

            var proof = BoardSolver.Solve(loaded, BoardState.CreateInitial(loaded));
            Assert.That(proof.Status, Is.EqualTo(SearchStatus.Solved));
            Assert.That(proof.Path.Count, Is.EqualTo(8));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void DeserializeRejectsNullOrWhitespace(string invalidJson)
        {
            Assert.Catch<ArgumentException>(() => LevelJsonSerializer.Deserialize(invalidJson));
        }

        [TestCase("{")]
        [TestCase("{ id: 123 }")]
        [TestCase("not a json")]
        [TestCase("{\"id\": \"test\"")]
        public void DeserializeRejectsMalformedJson(string malformed)
        {
            Assert.Throws<FormatException>(() => LevelJsonSerializer.Deserialize(malformed));
        }

        [TestCase("{\"schemaVersion\":1,\"rulesVersion\":2,\"width\":6,\"height\":6,\"exit\":{\"x\":5,\"y\":0},\"pieces\":[]}", "id")]
        [TestCase("{\"id\":\"test\",\"rulesVersion\":2,\"width\":6,\"height\":6,\"exit\":{\"x\":5,\"y\":0},\"pieces\":[]}", "schemaVersion")]
        [TestCase("{\"id\":\"test\",\"schemaVersion\":1,\"width\":6,\"height\":6,\"exit\":{\"x\":5,\"y\":0},\"pieces\":[]}", "rulesVersion")]
        [TestCase("{\"id\":\"test\",\"schemaVersion\":1,\"rulesVersion\":2,\"height\":6,\"exit\":{\"x\":5,\"y\":0},\"pieces\":[]}", "width")]
        [TestCase("{\"id\":\"test\",\"schemaVersion\":1,\"rulesVersion\":2,\"width\":6,\"exit\":{\"x\":5,\"y\":0},\"pieces\":[]}", "height")]
        [TestCase("{\"id\":\"test\",\"schemaVersion\":1,\"rulesVersion\":2,\"width\":6,\"height\":6,\"pieces\":[]}", "exit")]
        [TestCase("{\"id\":\"test\",\"schemaVersion\":1,\"rulesVersion\":2,\"width\":6,\"height\":6,\"exit\":{\"x\":5,\"y\":0}}", "pieces")]
        public void DeserializeRejectsMissingRequiredProperties(string json, string missingField)
        {
            var ex = Assert.Throws<FormatException>(() => LevelJsonSerializer.Deserialize(json));
            Assert.That(ex.Message, Does.Contain(missingField));
        }

        [Test]
        public void DeserializeRejectsUnknownPieceKind()
        {
            string json = @"
            {
              ""id"": ""test"",
              ""schemaVersion"": 1,
              ""rulesVersion"": 2,
              ""width"": 6,
              ""height"": 6,
              ""exit"": { ""x"": 5, ""y"": 0 },
              ""pieces"": [
                { ""id"": ""star"", ""kind"": ""UnknownKind"", ""width"": 1, ""height"": 1, ""start"": { ""x"": 0, ""y"": 0 } }
              ]
            }";

            Assert.Throws<FormatException>(() => LevelJsonSerializer.Deserialize(json));
        }

        [Test]
        public void DeserializeRejectsUnsupportedRulesVersionViaValidator()
        {
            string json = @"
            {
              ""id"": ""test"",
              ""schemaVersion"": 1,
              ""rulesVersion"": 999,
              ""width"": 6,
              ""height"": 6,
              ""exit"": { ""x"": 5, ""y"": 0 },
              ""pieces"": [
                { ""id"": ""star"", ""kind"": ""Target"", ""width"": 1, ""height"": 1, ""start"": { ""x"": 0, ""y"": 0 } }
              ]
            }";

            var ex = Assert.Throws<LevelValidationException>(() => LevelJsonSerializer.Deserialize(json));
            Assert.That(ex.ValidationResult.Errors, Does.Contain(BoardError.UnsupportedRulesVersion));
        }

        [Test]
        public void DeserializeRejectsInvalidBoardSizeViaValidator()
        {
            string json = @"
            {
              ""id"": ""test"",
              ""schemaVersion"": 1,
              ""rulesVersion"": 2,
              ""width"": 7,
              ""height"": 7,
              ""exit"": { ""x"": 5, ""y"": 0 },
              ""pieces"": [
                { ""id"": ""star"", ""kind"": ""Target"", ""width"": 1, ""height"": 1, ""start"": { ""x"": 0, ""y"": 0 } }
              ]
            }";

            var ex = Assert.Throws<LevelValidationException>(() => LevelJsonSerializer.Deserialize(json));
            Assert.That(ex.ValidationResult.Errors, Does.Contain(BoardError.InvalidBoardSize));
        }

        [Test]
        public void DeserializeRejectsDisallowed3x3ShapeViaValidator()
        {
            string json = @"
            {
              ""id"": ""test"",
              ""schemaVersion"": 1,
              ""rulesVersion"": 2,
              ""width"": 6,
              ""height"": 6,
              ""exit"": { ""x"": 5, ""y"": 0 },
              ""pieces"": [
                { ""id"": ""star"", ""kind"": ""Target"", ""width"": 1, ""height"": 1, ""start"": { ""x"": 0, ""y"": 0 } },
                { ""id"": ""huge"", ""kind"": ""Obstacle"", ""width"": 3, ""height"": 3, ""start"": { ""x"": 1, ""y"": 1 } }
              ]
            }";

            var ex = Assert.Throws<LevelValidationException>(() => LevelJsonSerializer.Deserialize(json));
            Assert.That(ex.ValidationResult.Errors, Does.Contain(BoardError.InvalidShape));
        }

        [Test]
        public void DeserializeRejectsDuplicatePieceIdViaValidator()
        {
            string json = @"
            {
              ""id"": ""test"",
              ""schemaVersion"": 1,
              ""rulesVersion"": 2,
              ""width"": 6,
              ""height"": 6,
              ""exit"": { ""x"": 5, ""y"": 0 },
              ""pieces"": [
                { ""id"": ""A"", ""kind"": ""Target"", ""width"": 1, ""height"": 1, ""start"": { ""x"": 0, ""y"": 0 } },
                { ""id"": ""A"", ""kind"": ""Obstacle"", ""width"": 1, ""height"": 1, ""start"": { ""x"": 1, ""y"": 1 } }
              ]
            }";

            var ex = Assert.Throws<LevelValidationException>(() => LevelJsonSerializer.Deserialize(json));
            Assert.That(ex.ValidationResult.Errors, Does.Contain(BoardError.DuplicatePieceId));
        }

        [Test]
        public void DeserializeRejectsOverlappingPiecesViaValidator()
        {
            string json = @"
            {
              ""id"": ""test"",
              ""schemaVersion"": 1,
              ""rulesVersion"": 2,
              ""width"": 6,
              ""height"": 6,
              ""exit"": { ""x"": 5, ""y"": 0 },
              ""pieces"": [
                { ""id"": ""star"", ""kind"": ""Target"", ""width"": 1, ""height"": 1, ""start"": { ""x"": 2, ""y"": 2 } },
                { ""id"": ""A"", ""kind"": ""Obstacle"", ""width"": 2, ""height"": 2, ""start"": { ""x"": 1, ""y"": 1 } }
              ]
            }";

            var ex = Assert.Throws<LevelValidationException>(() => LevelJsonSerializer.Deserialize(json));
            Assert.That(ex.ValidationResult.Errors, Does.Contain(BoardError.Overlap));
        }

        [Test]
        public void DeserializeAcceptsOmittedOptionalArrays()
        {
            string json = @"
            {
              ""id"": ""minimal-5x5"",
              ""schemaVersion"": 1,
              ""rulesVersion"": 2,
              ""width"": 5,
              ""height"": 5,
              ""exit"": { ""x"": 4, ""y"": 0 },
              ""pieces"": [
                { ""id"": ""star"", ""kind"": ""Target"", ""width"": 1, ""height"": 1, ""start"": { ""x"": 0, ""y"": 0 } }
              ]
            }";

            var level = LevelJsonSerializer.Deserialize(json);
            Assert.That(level.Id, Is.EqualTo("minimal-5x5"));
            Assert.That(level.Width, Is.EqualTo(5));
            Assert.That(level.FixedCells, Is.Empty);
            Assert.That(level.Triggers, Is.Empty);
        }
    }
}
