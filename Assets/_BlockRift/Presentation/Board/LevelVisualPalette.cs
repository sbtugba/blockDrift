using UnityEngine;

namespace BlockRift.Presentation
{
    /// <summary>Renk ve mesh kimliği Core'dan ayrı, Inspector üzerinden düzenlenebilir sunum verisidir.</summary>
    [CreateAssetMenu(menuName="Block Rift/Level Visual Palette")]
    public sealed class LevelVisualPalette : ScriptableObject
    {
        [System.Serializable] public sealed class Binding
        {
            public string pieceId, assetId;
            public Material material;
        }
        public Binding[] pieces;
        public Material[] colors;
        public GameObject exitPrefab;
    }
}
