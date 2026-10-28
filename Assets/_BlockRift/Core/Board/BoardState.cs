using System;
using System.Collections.Generic;
// LINQ, koleksiyonlar üzerinde seçme/dönüştürme işlemleri sağlar; Select aşağıda kullanılır.
using System.Linq;

namespace BlockRift.Core
{
    /// <summary>Kararlı parça kimliğini güncel hücresine bağlayan değişmez değerdir.</summary>
    // Bağlantı: PieceId, PieceDefinition.Id ile eşleşir; Origin ise o parçanın güncel sol alt hücresidir.
    public readonly struct PiecePosition
    {
        public string PieceId { get; }
        public Cell Origin { get; }
        /// <summary>Aynı şekilli parçalar arasında kimlik kaybı olmadan konum saklar.</summary>
        public PiecePosition(string pieceId, Cell origin) { PieceId = pieceId; Origin = origin; }
    }

    /// <summary>Yalnız tahta snapshot'ıdır; coin, yardım envanteri ve Undo geçmişini içermez.</summary>
    // Snapshot, belirli bir andaki verinin bağımsız görüntüsüdür; Unity ekran görüntüsü değildir.
    // LevelDefinition başlangıç tarifini, BoardState o andaki tahta verisini taşır.
    // MovementEngine yeni snapshot üretir; GameSession bunu sahiplenir. Undo henüz uygulanmadı.
    public sealed class BoardState
    {
        public IReadOnlyList<PiecePosition> Positions { get; }
        // bool tüketim bayrağıdır: false kullanılmamış, true tüketilmiş.
        // ConsumedTriggers[i], LevelDefinition.Triggers[i] hücresine aittir; indeks 0'dan başlar.
        public IReadOnlyList<bool> ConsumedTriggers { get; }

        /// <summary>Snapshot listelerinin sahipliğini almaz, bağımsız kopyalarını saklar.</summary>
        // Kaynak listeler daha sonra değişse bile bu snapshot değişmez.
        // Bu kurucu kopyalar; keyfi runtime verisinin bölüme uygunluğunu denetleyen restore sistemi henüz yoktur.
        public BoardState(IEnumerable<PiecePosition> positions, IEnumerable<bool> consumedTriggers)
        {
            Positions = new List<PiecePosition>(positions ?? throw new ArgumentNullException(nameof(positions))).AsReadOnly();
            ConsumedTriggers = new List<bool>(consumedTriggers ?? throw new ArgumentNullException(nameof(consumedTriggers))).AsReadOnly();
        }

        /// <summary>Yalnız doğrulanmış tanımdan, tüm tetikleyicileri kullanılmamış başlangıç snapshot'ı üretir.</summary>
        // static factory (üretici metot): kurucuya göre daha anlamlı bir işlem adıyla nesne üretir.
        // Çağrı zinciri: LevelDefinition -> BoardValidator.Validate -> BoardState.
        public static BoardState CreateInitial(LevelDefinition level)
        {
            if (!BoardValidator.Validate(level).IsValid)
                // !, bool değerini tersine çevirir: geçerli DEĞİLSE başlangıç üretmeyi reddederiz.
                throw new ArgumentException("Geçersiz bölümden başlangıç durumu oluşturulamaz.", nameof(level));
            // Select her tanımı bir PiecePosition'a dönüştürür; p => ... bir lambda (kısa fonksiyon) ifadesidir.
            // new bool[n], n elemanlı dizi oluşturur; bool'un varsayılan değeri false olduğundan hepsi kullanılmamıştır.
            // return üretilen nesneyi çağıran koda verir; bu metot hiçbir parçayı hareket ettirmez.
            return new BoardState(level.Pieces.Select(p => new PiecePosition(p.Id, p.Start)), new bool[level.Triggers.Count]);
        }
    }
}
