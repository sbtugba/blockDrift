using System;
using System.IO;
using BlockRift.Core;
using UnityEngine;

namespace BlockRift.Presentation
{
    /// <summary>
    /// Unity Resources üzerinden derlenmiş paket veya mobil çalışma ortamında JSON seviyelerini yükler.
    /// Resources/Levels/{levelId} konumundaki TextAsset'i okur ve doğrulanmış LevelDefinition üretir.
    /// </summary>
    public sealed class ResourceLevelLoader : ILevelProvider, ILevelVisualProvider
    {
        public const string LevelsResourceFolder = "Levels/";
        private LevelVisualData loaded;
        /// <summary>Yayın sırasını ayrı sürümlü veri dosyasından okur; bölüm ID'si türetmez.</summary>
        public static string LoadPublishedManifest() => ReadResourceText(LevelsResourceFolder+"level-catalog");

        /// <summary>Resources/Levels altından ID ile JSON okur ve LevelJsonSerializer ile doğrular.</summary>
        public static LevelDefinition LoadLevel(string levelId) => LoadDocument(levelId).Definition;

        private static LevelVisualData LoadDocument(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId))
                throw new ArgumentException("Bölüm ID boş veya null olamaz.", nameof(levelId));

            return LevelVisualData.Deserialize(ReadResourceText(LevelsResourceFolder + levelId));
        }

        private static string ReadResourceText(string resourcePath)
        {
            // Editor ve player aynı kaynağı kullanır; Unity hataları çağırana aynen iletilir.
            var textAsset = Resources.Load<TextAsset>(resourcePath);
            if (textAsset == null)
            {
                throw new FileNotFoundException($"Resources kaynağı bulunamadı: '{resourcePath}'.");
            }

            return textAsset.text;
        }

        LevelDefinition ILevelProvider.GetLevel(string levelId)
        {
            loaded = LoadDocument(levelId);
            return loaded.Definition;
        }

        /// <summary>Aynı yüklemenin görsel görünümünü döndürür; ayrı dosya veya ikinci veri kaynağı yoktur.</summary>
        public LevelVisualData GetVisualData(string levelId)
        {
            if (loaded == null || loaded.Definition.Id != levelId) loaded = LoadDocument(levelId);
            return loaded;
        }
    }
}
