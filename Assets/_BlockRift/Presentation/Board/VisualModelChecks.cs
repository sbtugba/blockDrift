using UnityEngine;

namespace BlockRift.Presentation
{
    /// <summary>Gelecekte import edilen modellerin wrapper-yerel bounds/pivot sözleşmesini sınar; mesh üretmez.</summary>
    public static class VisualModelChecks
    {
        private const float Tolerance = 0.001f;

        /// <summary>Parçada taban-merkez pivotunu, birim ayakizini ve mevcut kamera yükseklik zarfını doğrular.</summary>
        public static bool ValidatePieceBounds(Bounds bounds, int width, int height, out string issue)
        {
            if (!FiniteBounds(bounds) || width <= 0 || height <= 0 || bounds.size.y <= 0)
            { issue = "Geçerli, sonlu ve hacimli model sınırları gerekli."; return false; }
            if (Mathf.Abs(bounds.center.x) > Tolerance || Mathf.Abs(bounds.center.z) > Tolerance || Mathf.Abs(bounds.min.y) > Tolerance)
            { issue = "Parça pivotu XZ ayakizi merkezinde, tabanı Y=0 olmalı."; return false; }
            if (Mathf.Abs(bounds.size.x - width) > Tolerance || Mathf.Abs(bounds.size.z - height) > Tolerance)
            { issue = "Model ayakizi hücre boyutuyla uyuşmuyor; import ölçeği veya eksenleri kontrol edin."; return false; }
            if (bounds.max.y > 0.7f + Tolerance)
            { issue = "Parça mevcut kamera yükseklik zarfını aşıyor; kadrajı gözden geçirin."; return false; }
            issue = null; return true;
        }

        /// <summary>Tahtada mantıksal yüzey-merkez pivotunu ve çerçevenin mevcut kadraj zarfında kaldığını doğrular.</summary>
        public static bool ValidateBoardBounds(Bounds bounds, int size, out string issue)
        {
            if (!FiniteBounds(bounds) || (size != 5 && size != 6) || bounds.size.y <= 0)
            { issue = "Geçerli model sınırları ve 5/6 tahta boyutu gerekli."; return false; }
            if (Mathf.Abs(bounds.center.x) > Tolerance || Mathf.Abs(bounds.center.z) > Tolerance ||
                bounds.max.y > Tolerance || bounds.min.y < -0.2f - Tolerance)
            { issue = "Tahta pivotu XZ merkezinde/Y=0 mantıksal yüzeyde olmalı; geometri -0.2…0 yüksekliğinde kalmalı."; return false; }
            // Çerçevesiz .97 genişlikli hücreler dış kenarda .015'er birim görsel boşluk bırakır; grid pitch değişmez.
            if (bounds.size.x < size - 0.03f - Tolerance || bounds.size.z < size - 0.03f - Tolerance ||
                bounds.size.x > size + 0.5f + Tolerance || bounds.size.z > size + 0.5f + Tolerance)
            { issue = "Tahta boyutu veya çerçeve mevcut kamera zarfıyla uyuşmuyor."; return false; }
            issue = null; return true;
        }

        private static bool FiniteBounds(Bounds bounds)
        {
            for (int axis = 0; axis < 3; axis++)
                if (float.IsNaN(bounds.center[axis]) || float.IsInfinity(bounds.center[axis]) ||
                    float.IsNaN(bounds.size[axis]) || float.IsInfinity(bounds.size[axis]) || bounds.size[axis] < 0) return false;
            return true;
        }
    }
}
