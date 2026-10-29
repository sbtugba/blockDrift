namespace BlockRift.Core
{
    /// <summary>Sabit geometri hücre listesinde tutulur; bu tür yalnız oynatılabilir parçaları ayırır.</summary>
    // enum, sayılara anlamlı adlar veren bir türdür: Target ana parça, Obstacle hareketli engeldir.
    // Sabit engeller bu enum'da yoktur; LevelDefinition.FixedCells içinde hücre olarak saklanırlar.
    public enum PieceKind { Target, Obstacle }

    /// <summary>Parçanın değişmez kimliği, yönelimi ve bölüm başlangıç konumudur.</summary>
    // class referans türüdür: aynı nesneyi birden fazla değişken gösterebilir.
    // sealed, bu sınıftan kalıtımla yeni sınıf türetilmesini engeller; burada farklı davranışlı alt tür gerekmiyor.
    // Bağlantı: LevelDefinition bu tanımları saklar; BoardValidator boyut/kimlik/yerleşimlerini denetler.
    public sealed class PieceDefinition
    {
        // string metindir. ID, aynı boyuttaki iki farklı parçayı birbirinden ayırır; görünen isim değildir.
        public string Id { get; }
        public PieceKind Kind { get; }
        // Boyutlar hücre cinsindendir; Width genişlik, Height yüksekliktir. Döndürme işlemi yoktur.
        public int Width { get; }
        public int Height { get; }
        // Start yalnız bölüm başlangıcıdır. Oynarken değişen konum BoardState içindeki PiecePosition'da tutulur.
        // Bu hücre dikdörtgenin sol alt köşesidir; öteki hücreler genişlik/yükseklikten hesaplanır.
        public Cell Start { get; }

        /// <summary>Ham veriyi saklar; geçersiz içerikleri açıklayarak reddetme işi BoardValidator'a aittir.</summary>
        // Burada atama yapıyoruz; örneğin 2x3'ü sessizce başka şekle çevirmiyoruz.
        // Böylece validator hatalı bölüm verisini açıkça raporlayabilir.
        public PieceDefinition(string id, PieceKind kind, int width, int height, Cell start)
        { Id = id; Kind = kind; Width = width; Height = height; Start = start; }
    }

    /// <summary>GDD'deki sekiz sabit yönelimli dikdörtgeni tek yerde tanımlar; rotasyon üretmez.</summary>
    // static sınıf için new ShapeCatalog() yapılmaz; ShapeCatalog.IsAllowed(2, 2) diye çağrılır.
    // Bağlantı: BoardValidator şekil kuralını buradan sorar; katalog tam sekiz şekildir.
    public static class ShapeCatalog
    {
        /// <summary>Boyut çiftinin izinli hareketli engel şekli olup olmadığını söyler.</summary>
        // &&: ve, ||: veya, >=/<=: dahil alt/üst sınır. Parantezler koşulları gruplar.
        // İlk iki satır 1x1'i ortak içerir; bu iki ayrı şekil değildir, sonuç yalnız bir bool'dur.
        public static bool IsAllowed(int width, int height) =>
            (width == 1 && height >= 1 && height <= 4) ||
            (height == 1 && width >= 1 && width <= 4) ||
            (width == 2 && height == 2);
    }
}
