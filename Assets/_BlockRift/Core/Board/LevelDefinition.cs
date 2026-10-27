using System;
using System.Collections.Generic;

namespace BlockRift.Core
{
    /// <summary>Oyuncu ekonomisinden ve geçici oturumdan ayrı, sürümlü bölüm içeriğidir.</summary>
    // Definition (tanım), bölümün başlangıç tarifidir; State (durum) ise oynanırken değişen bilgidir.
    // Bağlantı: PieceDefinition ve Cell içerir; BoardValidator bunu kontrol eder;
    // BoardState.CreateInitial doğrulanmış bu tariften ilk tahta snapshot'ını oluşturur.
    public sealed class LevelDefinition
    {
        public string Id { get; }
        // SchemaVersion veri düzeninin, RulesVersion kural sürümünün kimliğidir.
        // Bunlar GDD belgesinin sürüm numarasıyla aynı olmak zorunda değildir; henüz dosya yükleyici yoktur.
        public int SchemaVersion { get; }
        public int RulesVersion { get; }
        public int Width { get; }
        public int Height { get; }
        public Cell Exit { get; }
        // IReadOnlyList<T>: T türünde öğeleri indeksle okuyabildiğimiz, bu arayüzden değiştiremediğimiz liste.
        // <PieceDefinition> ve <Cell>, generic tür parametresinin somut karşılıklarıdır.
        // Bu arayüz tek başına dışarıdaki asıl listeyi dondurmaz; aşağıda kopyalama da bu yüzden yapılır.
        public IReadOnlyList<PieceDefinition> Pieces { get; }
        public IReadOnlyList<Cell> FixedCells { get; }
        // Tetikleyicinin kalıcı kimliği bu sürümde listedeki indeksidir.
        public IReadOnlyList<Cell> Triggers { get; }

        /// <summary>Girdi listelerini kopyalayarak dışarıdan sonradan değiştirilmelerini engeller.</summary>
        // IEnumerable<T> üzerinde foreach ile dolaşılabilen veri demektir; dizi veya List<T> kabul edilebilir.
        public LevelDefinition(string id, int schemaVersion, int rulesVersion, int width, int height,
            Cell exit, IEnumerable<PieceDefinition> pieces, IEnumerable<Cell> fixedCells, IEnumerable<Cell> triggers)
        {
            Id = id; SchemaVersion = schemaVersion; RulesVersion = rulesVersion;
            Width = width; Height = height; Exit = exit;
            Pieces = Copy(pieces); FixedCells = Copy(fixedCells); Triggers = Copy(triggers);
        }

        // private: yalnız bu sınıf kullanabilir. static: çağırmak için nesne durumu gerekmez.
        // Copy<T>, hem Cell hem PieceDefinition listelerinde kullanılan generic yardımcı metottur.
        // new List<T>(source), koleksiyonun kopyasını çıkarır; sınıf öğelerini derin kopyalamaz.
        // PieceDefinition zaten değişmez olduğu için referanslarının kopyalanması burada güvenlidir.
        // ?? soldaki null ise sağdakini kullanır. throw, işlemi bir exception (istisna) ile durdurur.
        // nameof(source), parametre adını yazım hatasına açık bir metin yerine derleyiciden alır.
        // AsReadOnly(), yeni listenin değişiklik metotlarını dışarıya açmayan görünümünü verir.
        private static IReadOnlyList<T> Copy<T>(IEnumerable<T> source) =>
            new List<T>(source ?? throw new ArgumentNullException(nameof(source))).AsReadOnly();
    }
}
