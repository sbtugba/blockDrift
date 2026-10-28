using System;
using System.Linq;
using System.Collections.Generic;
using BlockRift.Core;
using BlockRift.App;
using NUnit.Framework;

namespace BlockRift.Tests
{
    /// <summary>Ara hücre, terminal ve oturum sınırlarına yönelik davranış regresyonlarıdır.</summary>
    public sealed class MovementTests
    {
        private static LevelDefinition Level(Cell start,int width=1,int height=1,Cell[] fixedCells=null,Cell? exit=null,int size=6) =>
            new LevelDefinition("test",1,1,size,size,exit??new Cell(size-1,size-1),new[]{
                new PieceDefinition("star",PieceKind.Target,1,1,new Cell(size-1,0)),
                new PieceDefinition("block",PieceKind.Obstacle,width,height,start)},fixedCells??Array.Empty<Cell>(),Array.Empty<Cell>());

        [TestCase(MoveAxis.Horizontal,1,3,2)] [TestCase(MoveAxis.Horizontal,-1,1,2)]
        [TestCase(MoveAxis.Vertical,1,2,3)] [TestCase(MoveAxis.Vertical,-1,2,1)]
        public void LongRectangleMovesInAllFourDirections(MoveAxis axis,int distance,int x,int y)
        {
            var level=Level(new Cell(2,2),2,1);
            var state=new BoardState(level.Pieces.Select(p=>new PiecePosition(p.Id,p.Start)),Array.Empty<bool>());
            var result=MovementEngine.Apply(level,state,new MoveCommand("block",axis,distance));
            Assert.That(MovementEngine.Position(result.State,"block"),Is.EqualTo(new Cell(x,y)));
            Assert.That(MovementEngine.Position(state,"block"),Is.EqualTo(new Cell(2,2)),"Girdi snapshot'ı değişmez");
        }
        [TestCase(5)] [TestCase(6)]
        public void MultiCellRequestStopsBeforeIntermediateBlockAndAtBoardEdge(int size)
        {
            var level=Level(new Cell(0,0),fixedCells:new[]{new Cell(2,0)},size:size);
            var state=BoardState.CreateInitial(level);
            Assert.That(MovementEngine.Apply(level,state,new MoveCommand("block",MoveAxis.Horizontal,100)).ActualDistance,Is.EqualTo(1));
            Assert.That(MovementEngine.Apply(level,state,new MoveCommand("block",MoveAxis.Vertical,int.MaxValue)).ActualDistance,Is.EqualTo(size-1));
            Assert.That(MovementEngine.Apply(level,state,new MoveCommand("block",MoveAxis.Horizontal,int.MinValue)).Changed,Is.False);
            Assert.That(MovementEngine.Apply(level,state,new MoveCommand("Fixed",MoveAxis.Horizontal,2)).Changed,Is.False);
        }
        [Test] public void PreviewReturnToOriginNoOpAndRestartDoNotLeakMoves()
        {
            // Motor regresyonu yayınlanan bölümün değişen yerleşimine bağlı değildir.
            var fixture=new LevelDefinition("movement-regression",1,1,6,6,new Cell(5,1),new[]{
                new PieceDefinition("star",PieceKind.Target,1,1,new Cell(0,0)),
                new PieceDefinition("gate",PieceKind.Obstacle,2,2,new Cell(2,0)),
                new PieceDefinition("blue",PieceKind.Obstacle,1,1,new Cell(4,0))},new[]{new Cell(0,1),new Cell(1,1)},Array.Empty<Cell>());
            var session=new GameSession(fixture); var key=BoardSolver.Key(session.State);
            session.Preview(new MoveCommand("gate",MoveAxis.Vertical,3));
            Assert.That(BoardSolver.Key(session.State),Is.EqualTo(key));
            session.Commit(new MoveCommand("gate",MoveAxis.Vertical,0));
            Assert.That(session.MoveCount,Is.Zero);
            session.Commit(new MoveCommand("gate",MoveAxis.Vertical,3));
            Assert.That(session.MoveCount,Is.EqualTo(1));
            Assert.That(MovementEngine.Position(session.State,"blue"),Is.EqualTo(new Cell(4,0)),"Gravity sıradan hamlede çalışmaz");
            session.Restart(); Assert.That(session.MoveCount,Is.Zero); Assert.That(BoardSolver.Key(session.State),Is.EqualTo(key));
        }
        [Test] public void TargetStopsAtExitDuringLongMoveAndCommitsExactlyOnce()
        {
            var level=new LevelDefinition("win",1,1,6,6,new Cell(2,0),new[]{new PieceDefinition("star",PieceKind.Target,1,1,new Cell(0,0))},Array.Empty<Cell>(),Array.Empty<Cell>());
            var session=new GameSession(level); var result=session.Commit(new MoveCommand("star",MoveAxis.Horizontal,5));
            Assert.That(result.ActualDistance,Is.EqualTo(2)); Assert.That(session.Won,Is.True);
            session.Commit(new MoveCommand("star",MoveAxis.Vertical,2)); Assert.That(session.MoveCount,Is.EqualTo(1));
            Assert.That(BoardSolver.Solve(level,BoardState.CreateInitial(level)).Path.Count,Is.EqualTo(1));
        }
        [Test] public void SolverIsDeterministicAndObstacleMovementIsNecessary()
        {
            var level=LevelOne.Create(); var start=BoardState.CreateInitial(level);
            var a=BoardSolver.Solve(level,start); var b=BoardSolver.Solve(level,start);
            Assert.That(a.Path.Select(m=>$"{m.PieceId}:{m.Axis}:{m.Distance}"),Is.EqualTo(b.Path.Select(m=>$"{m.PieceId}:{m.Axis}:{m.Distance}")));
            var queue=new Queue<BoardState>(); var seen=new HashSet<string>(); queue.Enqueue(start); seen.Add(BoardSolver.Key(start));
            while(queue.Count>0)
            {
                var state=queue.Dequeue(); Assert.That(MovementEngine.IsWon(level,state),Is.False);
                foreach(var move in MovementEngine.EnumerateLegalMoves(level,state).Where(m=>m.PieceId=="star"))
                { var next=MovementEngine.Apply(level,state,move).State; if(seen.Add(BoardSolver.Key(next))) queue.Enqueue(next); }
            }
        }
        [Test] public void ExhaustedSearchIsUnsolvableWhileTriggerSupportIsExplicitlyRejected()
        {
            var level=new LevelDefinition("sealed",1,1,5,5,new Cell(4,4),new[]{new PieceDefinition("star",PieceKind.Target,1,1,new Cell(0,0))},new[]{new Cell(1,0),new Cell(0,1)},Array.Empty<Cell>());
            Assert.That(BoardSolver.Solve(level,BoardState.CreateInitial(level)).Status,Is.EqualTo(SearchStatus.Unsolvable));
            var triggerLevel=new LevelDefinition("trigger",1,1,5,5,new Cell(4,4),level.Pieces,level.FixedCells,new[]{new Cell(3,3)});
            Assert.Throws<NotSupportedException>(()=>MovementEngine.Apply(triggerLevel,BoardState.CreateInitial(triggerLevel),new MoveCommand("star",MoveAxis.Horizontal,1)));
        }
    }
}
