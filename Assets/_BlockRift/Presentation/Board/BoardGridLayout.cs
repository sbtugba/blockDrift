using System;
using BlockRift.Core;
using UnityEngine;

namespace BlockRift.Presentation
{
    /// <summary>Core hücrelerini merkezlenmiş XZ sunum düzlemine eşler; kural veya oyun durumu değiştirmez.</summary>
    public readonly struct BoardGridLayout
    {
        public int Size { get; }
        public float SurfaceHeight { get; }
        // Blender'daki hücre aralığı sabittir; kamera uyarlaması model ölçeğini değiştirmez.
        public const float CellPitch = 1f;

        /// <summary>Yalnız desteklenen kare tahtalar için görsel koordinat çerçevesi oluşturur.</summary>
        public BoardGridLayout(int size, float surfaceHeight = 0)
        {
            if (size != 5 && size != 6) throw new ArgumentOutOfRangeException(nameof(size));
            if (float.IsNaN(surfaceHeight) || float.IsInfinity(surfaceHeight))
                throw new ArgumentOutOfRangeException(nameof(surfaceHeight));
            Size = size; SurfaceHeight = surfaceHeight;
        }

        /// <summary>Sol-alt başlangıçlı hücrenin merkezini BoardRoot yerel uzayında verir.</summary>
        public Vector3 CellCenter(Cell cell) => PieceBaseCenter(cell, 1, 1);

        /// <summary>Çok hücreli dikdörtgenin taban merkezini verir; çarpışma veya hamle geçerliliği hesaplamaz.</summary>
        public Vector3 PieceBaseCenter(Cell origin, int width, int height)
        {
            if (Size != 5 && Size != 6) throw new InvalidOperationException("Başlatılmamış görsel grid.");
            // Bu sınır kontrolü hatalı görsel koordinatları yakalar; Core geçiş motorunun yerine geçmez.
            if (width <= 0 || height <= 0 || origin.X < 0 || origin.Y < 0 ||
                (long)origin.X + width > Size || (long)origin.Y + height > Size)
                throw new ArgumentOutOfRangeException(nameof(origin), "Görsel ayakizi tahta dışında.");
            return new Vector3(origin.X + width * 0.5f - Size * 0.5f,
                SurfaceHeight, origin.Y + height * 0.5f - Size * 0.5f);
        }

        /// <summary>Mevcut kameranın çerçeve/yükseklik payını 5×5 ve 6×6 için aynı ölçekte sağlar.</summary>
        public Bounds PresentationBounds => new Bounds(new Vector3(0, SurfaceHeight + 0.25f, 0),
            new Vector3(Size + 0.5f, 0.9f, Size + 0.5f));
    }
}
