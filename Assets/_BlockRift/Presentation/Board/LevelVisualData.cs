using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BlockRift.Core;

namespace BlockRift.Presentation
{
    /// <summary>Görsel meta verisi domain tanımına eklenmez; aynı JSON'un sunum tarafındaki değişmez görünümüdür.</summary>
    public sealed class LevelVisualData
    {
        public LevelDefinition Definition { get; }
        public IReadOnlyDictionary<string, string> VisualKeys { get; }

        public LevelVisualData(LevelDefinition definition, IReadOnlyDictionary<string, string> keys)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            var copy = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var pair in keys ?? throw new ArgumentNullException(nameof(keys)))
            {
                if (!definition.Pieces.Any(p => p.Id == pair.Key && p.Kind == PieceKind.Obstacle) || string.IsNullOrWhiteSpace(pair.Value))
                    throw new FormatException("Geçersiz visualKey/parça eşlemesi: " + pair.Key);
                copy.Add(pair.Key, pair.Value);
            }
            VisualKeys = new ReadOnlyDictionary<string, string>(copy);
        }

        /// <summary>Eski şema-1 JSON okunur; eksik engel rengi yalnız görselleştirme istendiğinde açık migration hatasıdır.</summary>
        public string RequireKey(string pieceId) => VisualKeys.TryGetValue(pieceId, out var key) ? key :
            throw new InvalidOperationException("Eksik visualKey: " + pieceId + ". Eski JSON'a renk anahtarı eklenmeli; varsayılan renk yok.");

        /// <summary>Mevcut serializer/validator ile domain'i, aynı ayrıştırıcı ile ek görsel alanları okur.</summary>
        public static LevelVisualData Deserialize(string json)
        {
            var definition = LevelJsonSerializer.Deserialize(json);
            var root = LevelJsonSerializer.ParseJsonObject(json);
            var keys = new Dictionary<string, string>();
            foreach (var item in (List<object>)root["pieces"])
            {
                var piece = (Dictionary<string, object>)item;
                if (!piece.TryGetValue("visualKey", out var value)) continue;
                if (!(value is string key) || string.IsNullOrWhiteSpace(key)) throw new FormatException("visualKey boş olmayan metin olmalı.");
                keys.Add((string)piece["id"], key);
            }
            return new LevelVisualData(definition, keys);
        }

        /// <summary>Domain ve görsel alanları birlikte roundtrip eder; domain-only Serialize yerine bunu kullanın.</summary>
        public string Serialize()
        {
            var root = LevelJsonSerializer.ParseJsonObject(LevelJsonSerializer.Serialize(Definition));
            foreach (var item in (List<object>)root["pieces"])
            {
                var piece = (Dictionary<string, object>)item;
                if (VisualKeys.TryGetValue((string)piece["id"], out var key)) piece["visualKey"] = key;
            }
            return LevelJsonSerializer.WriteJsonObject(root);
        }
    }

    /// <summary>Mevcut bölüm sağlayıcısına eşlik eden sunum verisi portu; Core görsel veri taşımaz.</summary>
    public interface ILevelVisualProvider
    {
        LevelVisualData GetVisualData(string levelId);
    }
}
