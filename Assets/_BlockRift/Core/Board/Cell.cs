// using, başka bir isim alanındaki türleri kısa adlarıyla kullanmamızı sağlar.
// System içindeki IEquatable gibi temel C#/.NET türlerini aşağıda kullanıyoruz.
using System;

// namespace, türleri mantıksal bir ad altında toplar; klasörle aynı şey değildir.
// Bağlantı: LevelDefinition, PieceDefinition, PiecePosition ve BoardValidator hücreleri Cell ile ifade eder.
namespace BlockRift.Core
{
    /// <summary>Tamsayı hücre koordinatı; başlangıç sol alttadır, aşağı yön y-1'dir.</summary>
    // public: diğer assembly'ler de bu türü kullanabilir.
    // struct: değer türüdür; atandığında koordinat değeri kopyalanır, ortak bir nesne referansı paylaşılmaz.
    // readonly: bu struct'ın örnek durumu oluşturulduktan sonra değiştirilemez.
    // IEquatable<Cell>: iki Cell'in nasıl karşılaştırılacağını belirten generic (tür parametreli) arayüz.
    public readonly struct Cell : IEquatable<Cell>
    {
        // int tam sayıdır. Property (özellik), nesnenin sunduğu veriye erişim noktasıdır.
        // { get; } değeri okumaya izin verir; dışarıdan cell.X = 3 şeklinde değiştirmeye izin vermez.
        public int X { get; }
        public int Y { get; }

        /// <summary>Görsel dünya koordinatlarından bağımsız bir hücre oluşturur.</summary>
        // Constructor (kurucu), türle aynı adı taşır ve new Cell(2, 3) çağrısında çalışır.
        // Küçük harfli x/y gelen parametreler, büyük harfli X/Y saklanan özelliklerdir.
        public Cell(int x, int y) { X = x; Y = y; }
        // bool yalnız true/false değeridir. => tek ifadeli metot gövdesidir; burada sonucu döndürür.
        // == eşitlik, && ise "iki koşul da doğru olmalı" anlamındadır.
        public bool Equals(Cell other) => X == other.X && Y == other.Y;
        // override, temel türden gelen metodu bize uygun biçimde yeniden tanımlar.
        // object herhangi bir türü taşıyabilir. "is Cell other" hem türü kontrol eder hem other değişkenini oluşturur.
        public override bool Equals(object obj) => obj is Cell other && Equals(other);
        // Dictionary/HashSet, hücreyi hızlı bulabilmek için bu hash (özet) sayısını kullanır.
        // Eşit hücrelerin hash'i eşit olmalıdır; farklı hücrelerin hash'i çakışabilir, son kararı Equals verir.
        // * çarpma, ^ bit düzeyinde XOR'dur. unchecked, bu hesapta sayı taşmasının istisna üretmesini engeller.
        public override int GetHashCode() => unchecked((X * 397) ^ Y);
        // $ ile başlayan metin, {X} ve {Y} değerlerini metne yerleştirir (string interpolation).
        public override string ToString() => $"({X},{Y})";
    }
}
