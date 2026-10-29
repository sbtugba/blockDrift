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
        [TestCase("B")] [TestCase("C")]
        public void RequiredGatewayBlocksMustMoveInAnyWinningSolution(string id)
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
        [Test] public void VisibleExitNeedsLinkedHorizontalAndVerticalClearances()
        {
            var level=LevelOne.Create(); var state=BoardState.CreateInitial(level);
            Assert.That(level.Pieces.Any(p=>level.Exit.X>=p.Start.X&&level.Exit.X<p.Start.X+p.Width&&
                level.Exit.Y>=p.Start.Y&&level.Exit.Y<p.Start.Y+p.Height),Is.False,"Hedef başlangıçta görünür");
            Assert.That(MovementEngine.EnumerateLegalMoves(level,state).Any(m=>m.PieceId=="B"),Is.False);
            Assert.That(MovementEngine.Apply(level,state,new MoveCommand("A",MoveAxis.Vertical,-2)).Changed,Is.False);
            state=MovementEngine.Apply(level,state,new MoveCommand("C",MoveAxis.Vertical,2)).State;
            var b=MovementEngine.Apply(level,state,new MoveCommand("B",MoveAxis.Horizontal,-3));
            Assert.That(b.ActualDistance,Is.EqualTo(-3),"C, B'nin sol park alanını boşaltır");state=b.State;
            var a=MovementEngine.Apply(level,state,new MoveCommand("A",MoveAxis.Vertical,-2));
            Assert.That(a.ActualDistance,Is.EqualTo(-2),"B, A'nın alt alanını açar");state=a.State;
            state=MovementEngine.Apply(level,state,new MoveCommand("E",MoveAxis.Horizontal,-1)).State;
            state=MovementEngine.Apply(level,state,new MoveCommand("star",MoveAxis.Horizontal,3)).State;
            Assert.That(MovementEngine.Apply(level,state,new MoveCommand("star",MoveAxis.Vertical,-5)).Won,Is.False,
                "E açıldıktan sonra D hâlâ iniş koridorunu keser");
        }
        [Test] public void SingleCellCornerParkingAndSquareManoeuvresAreValid()
        {
            var level=LevelOne.Create(); var state=BoardState.CreateInitial(level);
            Assert.That(MovementEngine.Apply(level,state,new MoveCommand("F",MoveAxis.Horizontal,1)).Changed,Is.True);
            Assert.That(MovementEngine.Apply(level,state,new MoveCommand("F",MoveAxis.Vertical,1)).Changed,Is.True);
            var path=new[]{new MoveCommand("star",MoveAxis.Horizontal,3),new MoveCommand("C",MoveAxis.Vertical,2),
                new MoveCommand("B",MoveAxis.Horizontal,-3),new MoveCommand("A",MoveAxis.Vertical,-2),
                new MoveCommand("D",MoveAxis.Vertical,-1),new MoveCommand("D",MoveAxis.Horizontal,-1),
                new MoveCommand("E",MoveAxis.Horizontal,-1),new MoveCommand("star",MoveAxis.Vertical,-5)};
            foreach(var move in path)
            { var result=MovementEngine.Apply(level,state,move); Assert.That(result.ActualDistance,Is.EqualTo(move.Distance)); state=result.State; }
            Assert.That(MovementEngine.IsWon(level,state),Is.True);
            Assert.That(MovementEngine.Position(state,"D"),Is.EqualTo(new Cell(4,1)),"D iki ayrı sürüklemeyle köşeye park eder");
            Assert.That(path.Length,Is.EqualTo(BoardSolver.Solve(level,BoardState.CreateInitial(level)).Path.Count));
        }
    }
}
