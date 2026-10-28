using System;
using System.Collections.Generic;
using UnityEngine;

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
        public IReadOnlyList<Entry> Entries => Array.AsReadOnly(entries);

        /// <summary>Kimlikle tam eşleşme arar; eksik asset için alternatif model üretmez.</summary>
        public bool TryGetPiece(string assetId, out Entry entry)
        {
            foreach (var candidate in entries)
                if (!candidate.IsBoard && candidate.AssetId == assetId && candidate.Prefab)
                { entry = candidate; return true; }
            entry = null; return false;
        }

        /// <summary>İstenen boyutun gerçek tahta prefab'ını döndürür; 6×6'yı 5×5'e ölçeklemez.</summary>
        public bool TryGetBoard(int size, out Entry entry)
        {
            foreach (var candidate in entries)
                if (candidate.IsBoard && candidate.Width == size && candidate.Height == size && candidate.Prefab)
                { entry = candidate; return true; }
            entry = null; return false;
        }
    }
}
