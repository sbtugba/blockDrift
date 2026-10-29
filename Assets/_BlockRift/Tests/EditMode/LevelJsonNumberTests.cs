using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BlockRift.Core;
using NUnit.Framework;

namespace BlockRift.Tests
{
    /// <summary>Bütün sayısal alanlarda kayıplı dönüşümü ve taşmayı engelleyen regresyon testleri.</summary>
    public sealed class LevelJsonNumberTests
    {
        public static IEnumerable<TestCaseData> InvalidNumbers()
        {
            foreach (string field in new[] { "schemaVersion", "rulesVersion", "width", "height", "x", "y" })
                foreach (string value in new[] { "4294967298", "-4294967294", "2147483648", "-2147483649", "1e100", "-1e100", "2.0000001", "9223372036854775808" })
                    yield return new TestCaseData(field, value);
        }

        [TestCaseSource(nameof(InvalidNumbers))]
        public void EveryNumericOccurrenceRejectsLossyConversion(string field, string value)
        {
            string json = LevelJsonSerializer.Serialize(LevelOne.Create());
            var pattern = new Regex("(\"" + field + "\"\\s*:\\s*)(-?[0-9]+)");
            int count = pattern.Matches(json).Count;
            Assert.That(count, Is.GreaterThan(0));
            for (int target = 0; target < count; target++)
            {
                int index = 0;
                string invalid = pattern.Replace(json, match => index++ == target ? match.Groups[1].Value + value : match.Value);
                Assert.Throws<FormatException>(() => LevelJsonSerializer.Deserialize(invalid), field + " occurrence " + target + " = " + value);
            }
        }

        [TestCase("2.0")]
        [TestCase("2e0")]
        public void ExactIntegralNotationKeepsRulesVersion(string value)
        {
            string json = LevelJsonSerializer.Serialize(LevelOne.Create());
            json = Regex.Replace(json, "\"rulesVersion\"\\s*:\\s*2", "\"rulesVersion\": " + value);
            Assert.That(LevelJsonSerializer.Deserialize(json).RulesVersion, Is.EqualTo(2));
        }
    }
}
