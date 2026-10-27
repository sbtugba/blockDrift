using System;
using System.Collections.Generic;
using System.Linq;

namespace BlockRift.Core
{
    /// <summary>Kararlı hata kodları UI metninden bağımsız test ve araç raporlaması sağlar.</summary>
    // enum değerleri hata kategorileridir; oyuncuya gösterilecek çevrilmiş mesajların kendisi değildir.
    // Örneğin Overlap hücre çakışması, UnstableStart başlangıçta düşebilen engel bulunmasıdır.
    public enum BoardError
    {
        MissingLevel, InvalidMetadata, InvalidBoardSize, ExitOutsideBoard, InvalidPiece,
        DuplicatePieceId, InvalidTargetCount, InvalidShape, PieceOutsideBoard, Overlap,
        InvalidFixedCell, FixedOnExit, InvalidTriggerCount, InvalidTrigger, TriggerOnExit,
        OccupiedTrigger, UnstableStart
    }

    /// <summary>Kesin kural ihlali olmayan, içerik incelemesi gerektiren durumları ayırır.</summary>
    // Uyarı ile hata ayrıdır: ana parçanın baştan kapıda olması henüz yasaklanmış bir kural değildir.
    public enum BoardWarning { TargetStartsOnExit }

    /// <summary>Yapısal doğrulama sonucudur; IsValid çözülebilirlik veya yayın kabulü anlamına gelmez.</summary>
    // Bağlantı: BoardValidator bu sonucu üretir; BoardState.CreateInitial ve testler sonucu okur.
    public sealed class BoardValidationResult
    {
        public IReadOnlyList<BoardError> Errors { get; }
        public IReadOnlyList<BoardWarning> Warnings { get; }
        // Hesaplanan property: ayrı bir bool saklamaz, her okumada hata sayısından sonucu çıkarır.
        // Count öğe sayısıdır. Uyarılar IsValid'i false yapmaz; içerik incelemesi ayrıca gerekir.
        public bool IsValid => Errors.Count == 0;
        // internal erişim, bu kurucuyu yalnız aynı assembly'deki (BlockRift.Core) kodlara açar.
        internal BoardValidationResult(List<BoardError> errors, List<BoardWarning> warnings)
        { Errors = errors.AsReadOnly(); Warnings = warnings.AsReadOnly(); }
    }

    /// <summary>Bölüm başlangıcının GDD invariantlarını kontrol eder; hamle veya gravity çalıştırmaz.</summary>
    // Validator (doğrulayıcı), tarifin kurallara uyup uymadığını inceler; hataları kendiliğinden düzeltmez.
    // Bağlantılar: LevelDefinition'dan okur, ShapeCatalog'dan izinli boyutları sorar,
    // Cell eşitliğiyle dolu hücreleri izler ve BoardValidationResult döndürür. Unity bağımlılığı yoktur.
    public static class BoardValidator
    {
        /// <summary>Geçersiz tanımı hata kodlarıyla reddeder; solver kanıtları sonraki aşamanın sorumluluğudur.</summary>
        public static BoardValidationResult Validate(LevelDefinition level)
        {
            // var, derleyicinin sağdaki ifadeden türü çıkarmasıdır; dinamik veya türsüz değişken değildir.
            // List<T>, gerektiğinde büyüyen listedir. Add bir öğe ekler; birden çok ihlali birlikte raporlarız.
            var errors = new List<BoardError>();
            var warnings = new List<BoardWarning>();
            // null "nesne yok" demektir. Önce bunu kontrol ederek level.Id erişiminde hata oluşmasını önleriz.
            // Erken return, üzerinde işlem yapamayacağımız veride metodun geri kalanını atlar.
            if (level == null) { errors.Add(BoardError.MissingLevel); return new BoardValidationResult(errors, warnings); }
            // IsNullOrWhiteSpace boş veya yalnız boşluk içeren kimlikleri de yakalar.
            if (string.IsNullOrWhiteSpace(level.Id) || level.SchemaVersion < 1 || level.RulesVersion < 1)
                errors.Add(BoardError.InvalidMetadata);
            if (!((level.Width == 5 && level.Height == 5) || (level.Width == 6 && level.Height == 6)))
                errors.Add(BoardError.InvalidBoardSize);
            if (!Inside(level.Exit, level)) errors.Add(BoardError.ExitOutsideBoard);

            // Dictionary<TKey,TValue>, bir anahtarı değere bağlar: "bu hücreyi hangi parça kaplıyor?"
            // HashSet<T> yalnız benzersiz öğeleri tutar: "bu hücre sabit engel mi?"
            // İkisi de Cell.GetHashCode ve Cell.Equals metotlarını kullanır.
            var occupied = new Dictionary<Cell, PieceDefinition>();
            var fixedCells = new HashSet<Cell>();
            // foreach, listedeki her öğe için gövdeyi çalıştırır; burada her sabit hücre denetlenir.
            foreach (var cell in level.FixedCells)
            {
                // HashSet.Add, öğe zaten varsa false döndürür; !Add tekrarları yakalar.
                // || kısa devre yapar: sol koşul doğruysa sağ taraf çalışmaz.
                if (!Inside(cell, level) || !fixedCells.Add(cell)) errors.Add(BoardError.InvalidFixedCell);
                if (cell.Equals(level.Exit)) errors.Add(BoardError.FixedOnExit);
            }
            int targetCount = 0;
            // Ordinal, ID karşılaştırmasını kültür/dil ayarından bağımsız ve büyük-küçük harfe duyarlı yapar.
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var piece in level.Pieces)
            {
                // continue, yalnız bu döngünün sıradaki öğesine geçer; tüm metodu bitirmez.
                if (piece == null) { errors.Add(BoardError.InvalidPiece); continue; }
                if (string.IsNullOrWhiteSpace(piece.Id)) errors.Add(BoardError.InvalidPiece);
                else if (!ids.Add(piece.Id)) errors.Add(BoardError.DuplicatePieceId);
                if (piece.Kind == PieceKind.Target)
                {
                    // ++ sayacı bir artırır. Sonunda tam bir hedef olup olmadığını kontrol edeceğiz.
                    targetCount++;
                    if (piece.Start.Equals(level.Exit)) warnings.Add(BoardWarning.TargetStartsOnExit);
                }
                else if (piece.Kind != PieceKind.Obstacle) errors.Add(BoardError.InvalidPiece);
                if (!ShapeCatalog.IsAllowed(piece.Width, piece.Height) ||
                    (piece.Kind == PieceKind.Target && (piece.Width != 1 || piece.Height != 1)))
                { errors.Add(BoardError.InvalidShape); continue; }
                // long aritmetiği, bozuk içerikte int taşmasının geçerli konum gibi görünmesini engeller.
                // (long) bir cast'tir: toplama yapılmadan önce 64 bit tam sayıya dönüştürür.
                if (piece.Start.X < 0 || piece.Start.Y < 0 ||
                    (long)piece.Start.X + piece.Width > level.Width ||
                    (long)piece.Start.Y + piece.Height > level.Height)
                { errors.Add(BoardError.PieceOutsideBoard); continue; }
                // İç içe for döngüsü dikdörtgenin bütün hücrelerini gezer (ayakizi / footprint).
                // x/y parça içindeki ofsettir; Start eklenince tahtadaki gerçek hücre bulunur.
                // for sırası: başlangıç; devam koşulu; her tur sonrası artırım.
                for (int x = 0; x < piece.Width; x++)
                for (int y = 0; y < piece.Height; y++)
                {
                    var cell = new Cell(piece.Start.X + x, piece.Start.Y + y);
                    // Contains / ContainsKey, listelerde rastgele ilerlemek yerine hücrenin kayıtlı olup olmadığını sorar.
                    // Aynı hücre ikinci kez işgal edilirse Overlap üretiriz; ilk kaydı ezmeyiz.
                    if (fixedCells.Contains(cell) || occupied.ContainsKey(cell)) errors.Add(BoardError.Overlap);
                    else occupied.Add(cell, piece);
                }
            }
            if (targetCount != 1) errors.Add(BoardError.InvalidTargetCount);
            // Tetikleyiciler parça değildir; zemin üzerindeki özel hücrelerdir.
            // Ancak başlangıçta boş olmaları gerektiğinden hem sabit hem hareketli işgali kontrol edilir.
            if (level.Triggers.Count > 2) errors.Add(BoardError.InvalidTriggerCount);
            var triggers = new HashSet<Cell>();
            foreach (var cell in level.Triggers)
            {
                if (!Inside(cell, level) || !triggers.Add(cell)) errors.Add(BoardError.InvalidTrigger);
                if (cell.Equals(level.Exit)) errors.Add(BoardError.TriggerOnExit);
                if (fixedCells.Contains(cell) || occupied.ContainsKey(cell)) errors.Add(BoardError.OccupiedTrigger);
            }

            // Bu yalnız statik kararlılık testi: düşebilen bir küme varsa en alttaki üyesinin
            // alt kenarı şu an da boştur. Dolayısıyla kararsızlığı saptamak için cascade gerekmez.
            // GDD §13.1'deki geçici başlangıç koşulunu doğrular; oyun sırasında her hamlede gravity demek değildir.
            // Önceki veriler hatalıysa bu kontrolü atlarız; örneğin bozuk ayakizinden destek hesabı çıkarmayız.
            if (errors.Count == 0)
            // LINQ Where filtreler: p => ... yalnız Obstacle türündekileri seçen lambda'dır.
            foreach (var piece in level.Pieces.Where(p => p.Kind == PieceKind.Obstacle))
            {
                bool supported = piece.Start.Y == 0;
                // Tahtanın altı zaten destektir. Değilse alt kenar boyunca en az bir destek aranır.
                // && !supported sayesinde destek bulunduğunda kalan hücreler gereksiz yere taranmaz.
                for (int x = 0; x < piece.Width && !supported; x++)
                {
                    var below = new Cell(piece.Start.X + x, piece.Start.Y - 1);
                    supported = fixedCells.Contains(below) || occupied.ContainsKey(below);
                }
                if (!supported) errors.Add(BoardError.UnstableStart);
            }
            // Sonuç üretmek tahta verisini değiştirmez ve bölümün çözülebilirliğini kanıtlamaz.
            return new BoardValidationResult(errors, warnings);
        }

        // Koordinatlar 0'dan başladığı için 5 genişlikte geçerli X değerleri 0..4'tür; 5 dışarıdadır.
        private static bool Inside(Cell cell, LevelDefinition level) =>
            cell.X >= 0 && cell.Y >= 0 && cell.X < level.Width && cell.Y < level.Height;
    }
}
