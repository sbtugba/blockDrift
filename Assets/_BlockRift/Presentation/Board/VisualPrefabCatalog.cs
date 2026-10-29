using System;
using System.Collections.Generic;
using UnityEngine;
using BlockRift.Core;

namespace BlockRift.Presentation
{
    /// <summary>Onaylı görsel kimliklerini prefab'lara eşler; level/state veya runtime envanteri tutmaz.</summary>
    [CreateAssetMenu(menuName = "Block Rift/Visual Prefab Catalog")]
    public sealed class VisualPrefabCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private string assetId;
            [SerializeField] private int width, height;
            [SerializeField] private bool board;
            [SerializeField] private GameObject prefab;
            public string AssetId => assetId;
            public int Width => width;
            public int Height => height;
            public bool IsBoard => board;
            public GameObject Prefab => prefab;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();
        [Serializable]
        public sealed class ColorBinding
        {
            public string key;
            public Material material;
        }
        [SerializeField] private ColorBinding[] colors = Array.Empty<ColorBinding>();
        [SerializeField] private GameObject exitPrefab;
        public GameObject ExitPrefab => exitPrefab ? exitPrefab : throw new InvalidOperationException("Exit prefab referansı eksik.");
        public IReadOnlyList<Entry> Entries => Array.AsReadOnly(entries);

        /// <summary>Parça ID'sini kullanmadan onaylı tür ve boyutu mevcut asset kimliğine çözer.</summary>
        public Entry ResolvePiece(PieceDefinition piece)
        {
            if (piece == null)
                throw new ArgumentNullException(nameof(piece));
            if (!ShapeCatalog.IsAllowed(piece.Width, piece.Height) ||
                (piece.Kind == PieceKind.Target && (piece.Width != 1 || piece.Height != 1)) ||
                (piece.Kind != PieceKind.Target && piece.Kind != PieceKind.Obstacle))
                throw new ArgumentException("Desteklenmeyen parça türü/boyutu.", nameof(piece));
            return RequireEntry(piece.Kind == PieceKind.Target ? "Special_1x1_Star" : "Block_" + piece.Width + "x" + piece.Height, piece.Width, piece.Height);
        }

        /// <summary>Sabit geometri özel taş prefabını kullanır.</summary>
        public Entry ResolveFixedStone() => RequireEntry("Obstacle_1x1_Stone", 1, 1);

        private Entry RequireEntry(string id, int width, int height)
        {
            Entry found = null;
            foreach (var entry in entries)
                if (entry != null && !entry.IsBoard && entry.AssetId == id)
                {
                    if (found != null)
                        throw new InvalidOperationException("Çift prefab eşlemesi: " + id);
                    found = entry;
                }
            if (found == null || !found.Prefab || found.Width != width || found.Height != height)
                throw new InvalidOperationException("Eksik veya boyutu yanlış prefab: " + id);
            return found;
        }

        /// <summary>Anahtar tam eşleşmelidir; paylaşılan materyal döner, kopya veya sessiz renk alternatifi üretilmez.</summary>
        public Material ResolveColor(string key)
        {
            Material found = null;
            if (!string.IsNullOrWhiteSpace(key))
                foreach (var binding in colors)
                    if (binding != null && binding.key == key)
                    {
                        if (found || !binding.material)
                            throw new InvalidOperationException("Çift/boş renk eşlemesi: " + key);
                        found = binding.material;
                    }
            if (!found)
                throw new InvalidOperationException("Eksik veya bilinmeyen visualKey: '" + key + "'. JSON'a açık renk anahtarı ekleyin.");
            return found;
        }

        /// <summary>Yıldızın krem/altın materyalleri prefabında kalır; engelin rengi yalnız JSON anahtarından gelir.</summary>
        public Material ResolveMaterial(PieceDefinition piece, LevelVisualData data) =>
            piece.Kind == PieceKind.Target ? null : ResolveColor(data.RequireKey(piece.Id));

        /// <summary>Kimlikle tam eşleşme arar; eksik asset için alternatif model üretmez.</summary>
        public bool TryGetPiece(string assetId, out Entry entry)
        {
            foreach (var candidate in entries)
                if (!candidate.IsBoard && candidate.AssetId == assetId && candidate.Prefab)
                {
                    entry = candidate;
                    return true;
                }
            entry = null;
            return false;
        }

        /// <summary>İstenen boyutun gerçek tahta prefab'ını döndürür; 6×6'yı 5×5'e ölçeklemez.</summary>
        public bool TryGetBoard(int size, out Entry entry)
        {
            foreach (var candidate in entries)
                if (candidate.IsBoard && candidate.Width == size && candidate.Height == size && candidate.Prefab)
                {
                    entry = candidate;
                    return true;
                }
            entry = null;
            return false;
        }
    }
}
