using System;
using System.Collections.Generic;
using System.Linq;
// Core bizim oyun veri katmanımızdır; NUnit.Framework ise testleri çalıştırıp beklentileri denetleyen kütüphanedir.
using BlockRift.Core;
using NUnit.Framework;

namespace BlockRift.Tests
{
    /// <summary>GDD bölüm tanımı kurallarını küçük ve bağımsız örneklerle doğrular; solver içermez.</summary>
    // Bu sınıf oyunun çalışan mantığının parçası değildir; yalnız Editor test assembly'sinde derlenir.
    // Bağlantı: gerçek Core modellerini üretir, BoardValidator'ı çağırır, beklenen sonuçları Assert ile denetler.
    // Test okuma düzeni: Arrange (veriyi hazırla) -> Act (işlemi yap) -> Assert (beklentiyi doğrula).
    public sealed class BoardValidatorTests
    {
        // Küçük yardımcı metotlar tekrar eden kurulum kodunu azaltır; her çağrıda yeni örnek oluştururlar.
        // private yalnız bu sınıfa açık, static ise BoardValidatorTests nesnesinin durumunu kullanmıyor demektir.
        private static PieceDefinition Target() => new PieceDefinition("target", PieceKind.Target, 1, 1, new Cell(4, 4));
        // = 1 gibi parametre değerleri varsayılandır: Block() çağrısı bunları kullanır.
        // Block(x: 1, id: "second") adlandırılmış argüman kullanımıdır; parametrelerin anlamını görünür kılar.
        private static PieceDefinition Block(int w = 1, int h = 1, int x = 0, int y = 0, string id = "block") =>
            new PieceDefinition(id, PieceKind.Obstacle, w, h, new Cell(x, y));
        // [] dizi türüdür. Cell? ise Nullable<Cell>: normalde değer türü olan Cell'e null seçeneği ekler.
        // ??, özel veri verilmediyse varsayılan örneği seçer; Array.Empty<Cell>() sıfır elemanlı dizidir.
        // new[] { ... } ifadesinde derleyici dizi öğelerinin türünü çıkarır.
        private static LevelDefinition Level(int width = 5, int height = 5, PieceDefinition[] pieces = null,
            Cell[] fixedCells = null, Cell[] triggers = null, Cell? exit = null) =>
            new LevelDefinition("test", 1, 2, width, height, exit ?? new Cell(3, 4),
                pieces ?? new[] { Target(), Block() }, fixedCells ?? Array.Empty<Cell>(), triggers ?? Array.Empty<Cell>());
        // void, metodun değer döndürmediğini belirtir. Assert.That beklenti tutmazsa testi başarısız yapar.
        // Does.Contain, hata koleksiyonunda istenen kodun bulunmasını ister; başka hatalar da olabilir.
        private static void Reject(LevelDefinition level, BoardError error) =>
            Assert.That(BoardValidator.Validate(level).Errors, Does.Contain(error));

        // [...] buradaki kullanımda attribute (öznitelik) yazımıdır; aşağıdaki metot hakkında bilgi taşır.
        // [TestCase] aynı metodu farklı parametrelerle ayrı ayrı çalıştırır: burada iki test vakası vardır.
        // Is.True, Is.EqualTo gibi ifadeler beklenen sonucu tarif eden NUnit kısıtlarıdır.
        [TestCase(5, 5)]
        [TestCase(6, 6)]
        public void ValidBoardSizesAreAccepted(int w, int h) => Assert.That(BoardValidator.Validate(Level(w, h)).IsValid, Is.True);

        [TestCase(5, 6)]
        [TestCase(6, 5)]
        [TestCase(4, 4)]
        [TestCase(0, 0)]
        [TestCase(-1, 5)]
        public void OtherSizesAreRejected(int w, int h) => Reject(Level(w, h), BoardError.InvalidBoardSize);

        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(1, 2)]
        [TestCase(1, 3)]
        [TestCase(3, 1)]
        [TestCase(2, 2)]
        [TestCase(1, 4)]
        [TestCase(4, 1)]
        public void EveryApprovedShapeIsAccepted(int w, int h) =>
            Assert.That(BoardValidator.Validate(Level(pieces: new[] { Target(), Block(w, h) })).IsValid, Is.True);

        [TestCase(3, 3)]
        [TestCase(0, 1)]
        [TestCase(-1, 1)]
        [TestCase(2, 3)]
        [TestCase(3, 2)]
        [TestCase(4, 4)]
        [TestCase(1, 5)]
        [TestCase(int.MaxValue, 1)]
        public void OtherShapesAreRejected(int w, int h) => Reject(Level(pieces: new[] { Target(), Block(w, h) }), BoardError.InvalidShape);

        // [Test], parametresiz tek testtir. Range(1, 6) 1..6 değerlerini üretir.
        // Count koşulu sağlayan boyutları sayar; Sum her genişlikteki sayıları toplar.
        // Lambda'daki w/h değişkenleri, her öğe için hesaplamaya verilen geçici değerlerdir.
        [Test]
        public void CatalogHasExactlyEightShapes() => Assert.That(
            Enumerable.Range(1, 6).Sum(w => Enumerable.Range(1, 6).Count(h => ShapeCatalog.IsAllowed(w, h))), Is.EqualTo(8));
        [Test] public void NullLevelIsRejected() => Reject(null, BoardError.MissingLevel);
        [Test] public void MissingTargetIsRejected() => Reject(Level(pieces: new[] { Block() }), BoardError.InvalidTargetCount);
        [Test] public void TwoTargetsAreRejected() => Reject(Level(pieces: new[] { Target(), new PieceDefinition("t2", PieceKind.Target, 1, 1, new Cell(2, 4)) }), BoardError.InvalidTargetCount);
        [Test] public void LargeTargetIsRejected() => Reject(Level(pieces: new[] { new PieceDefinition("t", PieceKind.Target, 2, 1, new Cell(0, 0)) }), BoardError.InvalidShape);
        [Test] public void NullPieceIsRejected() => Reject(Level(pieces: new[] { Target(), null }), BoardError.InvalidPiece);
        // enum'a sayısal cast yapmak tanımsız bir değer üretebilir; bu yüzden validator ayrıca türü kontrol eder.
        [Test] public void UnknownKindIsRejected() => Reject(Level(pieces: new[] { Target(), new PieceDefinition("b", (PieceKind)99, 1, 1, new Cell(0, 0)) }), BoardError.InvalidPiece);
        [Test] public void EmptyPieceIdIsRejected() => Reject(Level(pieces: new[] { Target(), Block(id: " ") }), BoardError.InvalidPiece);
        [Test] public void DuplicateIdsAreRejected() => Reject(Level(pieces: new[] { Target(), Block(id: "target") }), BoardError.DuplicatePieceId);
        [Test] public void SameShapesWithDifferentIdsAreAccepted() => Assert.That(BoardValidator.Validate(Level(pieces: new[] { Target(), Block(), Block(x: 1, id: "second") })).IsValid, Is.True);
        [TestCase(-1, 0)]
        [TestCase(5, 0)]
        [TestCase(0, 5)]
        [TestCase(0, -1)]
        [TestCase(int.MaxValue, 0)]
        public void OutsidePieceIsRejected(int x, int y) => Reject(Level(pieces: new[] { Target(), Block(x: x, y: y) }), BoardError.PieceOutsideBoard);
        [Test] public void FootprintOverflowIsRejected() => Reject(Level(pieces: new[] { Target(), Block(2, 1, 4) }), BoardError.PieceOutsideBoard);
        [Test] public void PieceOverlapIsRejected() => Reject(Level(pieces: new[] { Target(), Block(), Block(id: "b2") }), BoardError.Overlap);
        [Test] public void FixedOverlapIsRejected() => Reject(Level(fixedCells: new[] { new Cell(0, 0) }), BoardError.Overlap);
        [Test] public void FixedOnExitIsRejected() => Reject(Level(fixedCells: new[] { new Cell(3, 4) }), BoardError.FixedOnExit);
        [Test] public void DuplicateFixedCellIsRejected() => Reject(Level(fixedCells: new[] { new Cell(2, 0), new Cell(2, 0) }), BoardError.InvalidFixedCell);
        [Test] public void OutsideFixedCellIsRejected() => Reject(Level(fixedCells: new[] { new Cell(5, 0) }), BoardError.InvalidFixedCell);
        [Test] public void OutsideExitIsRejected() => Reject(Level(exit: new Cell(5, 0)), BoardError.ExitOutsideBoard);
        [Test] public void ObstacleMayOccupyExit() => Assert.That(BoardValidator.Validate(Level(exit: new Cell(0, 0))).IsValid, Is.True);
        [Test]
        public void TargetStartingOnExitRequiresContentReview() => Assert.That(
            BoardValidator.Validate(Level(exit: new Cell(4, 4))).Warnings, Does.Contain(BoardWarning.TargetStartsOnExit));
        // Assert.Throws<T>, verilen kodun T türünde istisna üretmesini bekler.
        // () => ... parametresiz lambda'dır; kodu hemen çalıştırmak yerine NUnit'in çalıştırmasına veririz.
        [Test]
        public void NullCollectionsFailAtConstruction() => Assert.Throws<ArgumentNullException>(() =>
            new LevelDefinition("test", 1, 2, 5, 5, new Cell(0, 0), null, Array.Empty<Cell>(), Array.Empty<Cell>()));
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void AllowedTriggerCountsAreAccepted(int count) => Assert.That(BoardValidator.Validate(Level(triggers:
            Enumerable.Range(1, count).Select(x => new Cell(x, 2)).ToArray())).IsValid, Is.True);
        [Test] public void ThreeTriggersAreRejected() => Reject(Level(triggers: new[] { new Cell(1, 2), new Cell(2, 2), new Cell(3, 2) }), BoardError.InvalidTriggerCount);
        [Test] public void DuplicateTriggersAreRejected() => Reject(Level(triggers: new[] { new Cell(1, 2), new Cell(1, 2) }), BoardError.InvalidTrigger);
        [Test] public void OutsideTriggerIsRejected() => Reject(Level(triggers: new[] { new Cell(-1, 2) }), BoardError.InvalidTrigger);
        [Test] public void TriggerOnExitIsRejected() => Reject(Level(triggers: new[] { new Cell(3, 4) }), BoardError.TriggerOnExit);
        [TestCase(0, 0)]
        [TestCase(4, 4)]
        public void TriggerUnderPieceIsRejected(int x, int y) => Reject(Level(triggers: new[] { new Cell(x, y) }), BoardError.OccupiedTrigger);
        [Test] public void TriggerUnderFixedCellIsRejected() => Reject(Level(fixedCells: new[] { new Cell(2, 2) }, triggers: new[] { new Cell(2, 2) }), BoardError.OccupiedTrigger);
        [Test] public void FloatingStartIsRejected() => Reject(Level(pieces: new[] { Target(), Block(y: 1) }), BoardError.UnstableStart);
        [Test] public void FloatingStackIsRejected() => Reject(Level(pieces: new[] { Target(), Block(y: 1), Block(y: 2, id: "top") }), BoardError.UnstableStart);
        [Test] public void SupportedStackIsAccepted() => Assert.That(BoardValidator.Validate(Level(pieces: new[] { Target(), Block(), Block(y: 1, id: "top") })).IsValid, Is.True);
        [Test] public void PartialFixedSupportIsEnough() => Assert.That(BoardValidator.Validate(Level(pieces: new[] { Target(), Block(2, 1, 0, 1) }, fixedCells: new[] { new Cell(1, 0) })).IsValid, Is.True);
        [Test] public void TargetSupportsObstacle() => Assert.That(BoardValidator.Validate(Level(pieces: new[] { new PieceDefinition("t", PieceKind.Target, 1, 1, new Cell(0, 0)), Block(y: 1) })).IsValid, Is.True);
        [Test] public void InvalidMetadataIsRejected() => Reject(new LevelDefinition("", 0, 0, 5, 5, new Cell(3, 4), new[] { Target() }, Array.Empty<Cell>(), Array.Empty<Cell>()), BoardError.InvalidMetadata);

        [Test]
        public void InitialStateUsesPieceIdsAndUnusedTriggers()
        {
            // Arrange: örnek bölüm. Act: başlangıç snapshot'ı. Assert: kimlik/konum/tüketim beklentileri.
            var level = Level(triggers: new[] { new Cell(2, 2) });
            var state = BoardState.CreateInitial(level);
            Assert.That(state.Positions.Select(p => p.PieceId), Is.EqualTo(new[] { "target", "block" }));
            Assert.That(state.Positions[0].Origin, Is.EqualTo(new Cell(4, 4)));
            Assert.That(state.ConsumedTriggers, Is.EqualTo(new[] { false }));
        }
        [Test] public void InvalidLevelCannotCreateInitialState() => Assert.Throws<ArgumentException>(() => BoardState.CreateInitial(Level(4, 4)));
        [Test]
        public void DefinitionCopiesInputCollections()
        {
            // Kaynak dizileri bilerek bozuyoruz: model kopya aldıysa kendi verisi bundan etkilenmemelidir.
            var pieces = new[] { Target(), Block() };
            var fixedCells = new[] { new Cell(2, 0) };
            var triggers = new[] { new Cell(2, 2) };
            var level = Level(pieces: pieces, fixedCells: fixedCells, triggers: triggers);
            pieces[0] = null;
            fixedCells[0] = new Cell(99, 99);
            triggers[0] = new Cell(99, 99);
            Assert.That(BoardValidator.Validate(level).IsValid, Is.True);
            // IList<Cell> cast'i ile yazmayı deneriz; alttaki ReadOnlyCollection buna istisnayla karşı çıkar.
            // [0], dizinin/listenin ilk öğesidir. Bu deneme gerçek oyunda yapılacak bir işlem değildir.
            Assert.Throws<NotSupportedException>(() => ((IList<Cell>)level.Triggers)[0] = new Cell(0, 0));
        }
        [Test]
        public void SnapshotCopiesInputCollections()
        {
            // Aynı dış değişiklik korumasını runtime snapshot'ı için de sınarız; testler birbirinden bağımsızdır.
            var positions = new[] { new PiecePosition("t", new Cell(1, 1)) };
            var triggers = new[] { false };
            var state = new BoardState(positions, triggers);
            positions[0] = new PiecePosition("other", new Cell(2, 2));
            triggers[0] = true;
            Assert.That(state.Positions[0].PieceId, Is.EqualTo("t"));
            Assert.That(state.ConsumedTriggers[0], Is.False);
            Assert.Throws<NotSupportedException>(() => ((IList<bool>)state.ConsumedTriggers)[0] = true);
        }
        [Test]
        public void CellEqualityAndHashSupportOccupancyLookup()
        {
            // Aynı koordinatlı iki ayrı Cell, HashSet içinde tek öğe sayılmalıdır.
            // Böylece validator aynı hücrenin iki kez kullanıldığını güvenilir biçimde fark edebilir.
            Assert.That(new Cell(1, 2), Is.EqualTo(new Cell(1, 2)));
            Assert.That(new Cell(1, 2), Is.Not.EqualTo(new Cell(2, 1)));
            Assert.That(new HashSet<Cell> { new Cell(1, 2), new Cell(1, 2) }.Count, Is.EqualTo(1));
        }
    }
}
