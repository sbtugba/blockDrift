using System;
using System.Linq;
using System.Collections.Generic;
using BlockRift.Core;
using NUnit.Framework;

namespace BlockRift.Tests
{
    /// <summary>Doluluk/hamle kotası yerine alan kullanımı ve gerekli blok bağımlılıklarını denetler.</summary>
    public sealed class LevelOneQualityTests
    {
        [Test] public void WinningStarRouteUsesLowerMiddleAndUpperRegions()
        {
            var level=LevelOne.Create(); var state=BoardState.CreateInitial(level);
            var proof=BoardSolver.Solve(level,state);
            Assert.That(proof.Status,Is.EqualTo(SearchStatus.Solved));
            var rows=new HashSet<int>{MovementEngine.Position(state,"star").Y};
            var columns=new HashSet<int>{MovementEngine.Position(state,"star").X};
            foreach(var move in proof.Path)
            {
                var next=MovementEngine.Apply(level,state,move);
                if(move.PieceId=="star")
                {
                    var from=MovementEngine.Position(state,"star"); var to=MovementEngine.Position(next.State,"star");
                    for(int x=Math.Min(from.X,to.X);x<=Math.Max(from.X,to.X);x++) columns.Add(x);
                    for(int y=Math.Min(from.Y,to.Y);y<=Math.Max(from.Y,to.Y);y++) rows.Add(y);
                }
                state=next.State;
            }
            Assert.That(rows.Any(y=>y<=1)&&rows.Any(y=>y>=2&&y<=3)&&rows.Any(y=>y>=4),Is.True,"Yıldız rotası üç bölgeyi de kullanmalı");
            Assert.That(columns.Max()-columns.Min(),Is.GreaterThanOrEqualTo(3),"Rota tek sütunluk çözüm olmamalı");
            Assert.That(MovementEngine.IsWon(level,state),Is.True);
        }
        [TestCase("gate")] [TestCase("blue")]
        public void EveryDesignedObstacleMustMoveInAnyWinningSolution(string id)
        {
            var level=LevelOne.Create(); var frozen=level.Pieces.Single(p=>p.Id==id);
            // Hedefi örten bir parçayı FixedCell yapmak GDD'yi ihlal eder. Özgün geçerli level korunur;
            // yalnız analiz grafiğinde ilgili parçanın manuel kenarları filtrelenir.
            var initial=BoardState.CreateInitial(level); var queue=new Queue<BoardState>(); var seen=new HashSet<string>();
            queue.Enqueue(initial); seen.Add(BoardSolver.Key(initial));
            while(queue.Count>0)
            {
                var state=queue.Dequeue(); Assert.That(MovementEngine.IsWon(level,state),Is.False,"Dondurulan engeli atlayan kazanma yolu var");
                foreach(var move in MovementEngine.EnumerateLegalMoves(level,state).Where(m=>m.PieceId!=id))
                { var next=MovementEngine.Apply(level,state,move).State; if(seen.Add(BoardSolver.Key(next))) queue.Enqueue(next); }
                Assert.That(seen.Count,Is.LessThan(100000),"INCOMPLETE: dondurma kanıtı tamamlanmadı");
            }
        }
        [Test] public void VisibleExitPocketNeedsItsApproachOpened()
        {
            var level=LevelOne.Create(); var state=BoardState.CreateInitial(level);
            var gate=level.Pieces.Single(p=>p.Id=="gate");
            Assert.That(level.Exit.X>=gate.Start.X&&level.Exit.X<gate.Start.X+gate.Width&&
                level.Exit.Y>=gate.Start.Y&&level.Exit.Y<gate.Start.Y+gate.Height,Is.False,"Öğreticide hedef görünür kalmalı");
            Assert.That(gate.Start.Y,Is.EqualTo(level.Exit.Y+1),"Büyük blok hedefi değil, hedef cebinin üst girişini kapatır");
            Assert.That(MovementEngine.EnumerateLegalMoves(level,state).Any(m=>m.PieceId=="gate"),Is.False,"Geçit başlangıçta manevra yapamamalı");
            var cleared=MovementEngine.Apply(level,state,new MoveCommand("blue",MoveAxis.Vertical,2));
            Assert.That(cleared.Changed,Is.True);
            var opened=MovementEngine.Apply(level,cleared.State,new MoveCommand("gate",MoveAxis.Vertical,2));
            Assert.That(opened.ActualDistance,Is.EqualTo(2),"Büyük bloğun iki satırlık park alanı açılmalı");
            var partial=MovementEngine.Apply(level,cleared.State,new MoveCommand("gate",MoveAxis.Vertical,1));
            var p=MovementEngine.Position(partial.State,"gate");
            Assert.That(p.Y,Is.EqualTo(level.Exit.Y+2));
            // Bir satır yükseltince cebin hemen üstü boşalır, fakat cebin üst ağzı hâlâ kapalıdır.
            var fixedCells=new List<Cell>(level.FixedCells);
            foreach(var obstacle in level.Pieces.Where(q=>q.Kind==PieceKind.Obstacle))
            {
                var origin=MovementEngine.Position(partial.State,obstacle.Id);
                for(int y=0;y<obstacle.Height;y++) for(int x=0;x<obstacle.Width;x++) fixedCells.Add(new Cell(origin.X+x,origin.Y+y));
            }
            var pocket=new LevelDefinition("partial-pocket",1,1,6,6,level.Exit,level.Pieces.Where(q=>q.Kind==PieceKind.Target),fixedCells,Array.Empty<Cell>());
            Assert.That(BoardSolver.Solve(pocket,BoardState.CreateInitial(pocket)).Status,Is.EqualTo(SearchStatus.Unsolvable));
        }
        [Test] public void LeftParkingIsARealAlternativeRatherThanDecorativeEmptySpace()
        {
            var level=LevelOne.Create(); var state=BoardState.CreateInitial(level);
            var path=new[]{new MoveCommand("blue",MoveAxis.Vertical,1),new MoveCommand("blue",MoveAxis.Horizontal,2),
                new MoveCommand("gate",MoveAxis.Vertical,1),new MoveCommand("gate",MoveAxis.Horizontal,-2),
                new MoveCommand("star",MoveAxis.Horizontal,-3),new MoveCommand("star",MoveAxis.Vertical,-5)};
            foreach(var move in path)
            { var result=MovementEngine.Apply(level,state,move); Assert.That(result.ActualDistance,Is.EqualTo(move.Distance)); state=result.State; }
            Assert.That(MovementEngine.IsWon(level,state),Is.True);
            Assert.That(path.Length,Is.GreaterThan(BoardSolver.Solve(level,BoardState.CreateInitial(level)).Path.Count),"Alternatif park stratejisi optimum diye sunulmaz");
        }
    }
}
