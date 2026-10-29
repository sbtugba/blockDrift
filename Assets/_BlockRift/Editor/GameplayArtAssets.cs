using System;
using UnityEngine;

namespace BlockRift.Editor
{
    /// <summary>Editor sanat araçları ve kabul testleri için ortak asset yollarını ve gerçek mesh ölçümünü sağlar.</summary>
    public static class GameplayArtAssets
    {
        public const string Models = "Assets/_BlockRift/Art/Models/Gameplay/";
        public const string Prefabs = "Assets/_BlockRift/Art/Prefabs/Gameplay/";
        public const string Materials = "Assets/_BlockRift/Art/Materials/Gameplay/";
        public const string CatalogPath = "Assets/_BlockRift/Content/GameplayVisualCatalog.asset";

        /// <summary>Gerçek mesh köşelerini wrapper yerel uzayında ölçer; imported rotation/scale'i hesaba katar.</summary>
        public static Bounds MeasureBounds(Transform wrapper)
        {
            bool found = false; Bounds bounds = default;
            foreach (var filter in wrapper.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!filter.sharedMesh) throw new InvalidOperationException("Eksik mesh.");
                Matrix4x4 matrix = wrapper.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                foreach (Vector3 vertex in filter.sharedMesh.vertices)
                {
                    Vector3 point = matrix.MultiplyPoint3x4(vertex);
                    if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; } else bounds.Encapsulate(point);
                }
            }
            if (!found) throw new InvalidOperationException("Gerçek mesh bulunamadı.");
            return bounds;
        }
    }
}
