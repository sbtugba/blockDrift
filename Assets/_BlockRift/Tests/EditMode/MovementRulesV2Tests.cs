using System;
using System.Collections.Generic;
using System.Linq;
using BlockRift.Core;
using BlockRift.App;
using NUnit.Framework;

namespace BlockRift.Tests
{
    /// <summary>GDD 3.3/4: sekiz şekil, izinli eksenler, sürüm sınırı ve onaylı bölüm kanıtı.</summary>
    public sealed class MovementRulesV2Tests
    {
        private static LevelDefinition Fixture(int w=2,int h=1,int version=2) => new LevelDefinition(
            "v2-fixture",1,version,6,6,new Cell(5,4),new[]{
                new PieceDefinition("star",PieceKind.Target,1,1,new Cell(0,5)),
                new PieceDefinition("block",PieceKind.Obstacle,w,h,new Cell(1,0))},Array.Empty<Cell>(),Array.Empty<Cell>());

        public static IEnumerable<object[]> Directions()
        {
            int[,] sizes={{1,1},{2,1},{3,1},{4,1},{1,2},{1,3},{1,4},{2,2}};
            for(int i=0;i<8;i++) foreach(MoveAxis axis in new[]{MoveAxis.Horizontal,MoveAxis.Vertical})
            foreach(int sign in new[]{-1,1})
                yield return new object[]{sizes[i,0],sizes[i,1],axis,sign,
                    sizes[i,0]==sizes[i,1] || (sizes[i,0]>sizes[i,1]?axis==MoveAxis.Horizontal:axis==MoveAxis.Vertical)};
        }
        [TestCaseSource(nameof(Directions))]
        public void ManualDirectionsFollowShapeAndForbiddenMoveKeepsSnapshot(int w,int h,MoveAxis axis,int sign,bool allowed)
        {
            var level=Fixture(w,h);
            // Oyun ortasında yükseltilmiş blok kendiliğinden düşmez; başlangıç validator'ı çalıştırılmaz.
            var state=new BoardState(new[]{new PiecePosition("star",new Cell(0,5)),new PiecePosition("block",new Cell(1,1))},Array.Empty<bool>());
            var move=new MoveCommand("block",axis,sign);
            var result=MovementEngine.Apply(level,state,move);
            Assert.That(result.Changed,Is.EqualTo(allowed));
            Assert.That(result.ActualDistance,Is.EqualTo(allowed?sign:0));
            if(!allowed) Assert.That(result.State,Is.SameAs(state));
            var legal=MovementEngine.EnumerateLegalMoves(level,state).Where(m=>m.PieceId=="block").ToArray();
            Assert.That(legal.Any(m=>m.Axis==axis&&m.Distance==sign),Is.EqualTo(allowed));
        }
        [TestCase(3,3)] [TestCase(2,3)]
        public void RemovedOrUnknownShapesAreRejected(int w,int h)
        {
            Assert.That(ShapeCatalog.IsAllowed(w,h),Is.False);
            Assert.That(BoardValidator.Validate(Fixture(w,h)).Errors,Does.Contain(BoardError.InvalidShape));
        }
        [TestCase(MoveAxis.Horizontal,1)] [TestCase(MoveAxis.Horizontal,-1)]
        [TestCase(MoveAxis.Vertical,1)] [TestCase(MoveAxis.Vertical,-1)]
        public void TargetRetainsAllFourDirections(MoveAxis axis,int distance)
        {
            var level=new LevelDefinition("target-v2",1,2,6,6,new Cell(5,5),new[]{
                new PieceDefinition("star",PieceKind.Target,1,1,new Cell(2,2))},Array.Empty<Cell>(),Array.Empty<Cell>());
            var session=new GameSession(level);var result=session.Commit(new MoveCommand("star",axis,distance));
            Assert.That(result.Changed,Is.True);Assert.That(result.ActualDistance,Is.EqualTo(distance));Assert.That(session.MoveCount,Is.EqualTo(1));
        }
        [TestCase("A",1,3,3,2)] [TestCase("B",3,1,3,1)] [TestCase("C",1,2,2,0)]
        [TestCase("D",1,1,5,2)] [TestCase("E",2,1,4,3)] [TestCase("F",2,2,0,3)]
        public void ApprovedCoordinatesAndFootprintsAreExact(string id,int width,int height,int x,int y)
        {
            var piece=LevelOne.Create().Pieces.Single(p=>p.Id==id);
            Assert.That(piece.Width,Is.EqualTo(width));Assert.That(piece.Height,Is.EqualTo(height));Assert.That(piece.Start,Is.EqualTo(new Cell(x,y)));
        }
        [Test] public void ExactlyEightShapesRemain() => Assert.That(
            Enumerable.Range(1,4).Sum(w=>Enumerable.Range(1,4).Count(h=>ShapeCatalog.IsAllowed(w,h))),Is.EqualTo(8));
        [Test] public void ForbiddenAxisCannotSpendAMoveOrMutateState()
        {
            var session=new GameSession(Fixture()); var initial=session.State;
            var preview=session.Preview(new MoveCommand("block",MoveAxis.Vertical,2));
            Assert.That(preview.Changed,Is.False); Assert.That(preview.State,Is.SameAs(initial));
            session.Commit(new MoveCommand("block",MoveAxis.Vertical,2));
            Assert.That(session.State,Is.SameAs(initial)); Assert.That(session.MoveCount,Is.Zero);
        }
        [TestCase(1)] [TestCase(3)]
        public void OtherRuleVersionsCannotBeLoadedMovedOrSolved(int version)
        {
            var level=Fixture(version:version);
            Assert.That(BoardValidator.Validate(level).IsValid,Is.False);
            Assert.Throws<ArgumentException>(()=>BoardState.CreateInitial(level));
            var raw=new BoardState(level.Pieces.Select(p=>new PiecePosition(p.Id,p.Start)),Array.Empty<bool>());
            Assert.Throws<NotSupportedException>(()=>MovementEngine.Apply(level,raw,new MoveCommand("block",MoveAxis.Horizontal,1)));
            Assert.Throws<NotSupportedException>(()=>BoardSolver.Solve(level,raw));
        }
        [Test] public void TriggerLevelsRemainExplicitlyUnsupportedRatherThanSimulatingGravityAsManualMove()
        {
            var l=Fixture();var triggered=new LevelDefinition(l.Id,1,2,6,6,l.Exit,l.Pieces,l.FixedCells,new[]{new Cell(4,3)});
            Assert.Throws<NotSupportedException>(()=>MovementEngine.Apply(triggered,BoardState.CreateInitial(triggered),new MoveCommand("block",MoveAxis.Horizontal,1)));
        }
        [Test] public void ApprovedLevelUsesV2AndCoreProvesEightMovesWithExactReplay()
        {
            var l=LevelOne.Create(); Assert.That(l.RulesVersion,Is.EqualTo(2));
            Assert.That(l.Exit,Is.EqualTo(new Cell(5,0)));
            Assert.That(l.Pieces.Single(p=>p.Kind==PieceKind.Target).Start,Is.EqualTo(new Cell(2,5)));
            Assert.That(l.Pieces.Count,Is.EqualTo(7)); Assert.That(l.FixedCells,Is.EquivalentTo(new[]{new Cell(0,2),new Cell(4,2),new Cell(4,0)}));
            var state=BoardState.CreateInitial(l);var proof=BoardSolver.Solve(l,state);
            Assert.That(proof.Status,Is.EqualTo(SearchStatus.Solved));Assert.That(proof.Path.Count,Is.EqualTo(8));
            var version=proof.GetType().GetProperty("RulesVersion");Assert.That(version,Is.Not.Null,"Kanıt kural sürümü taşımalı");
            Assert.That(version.GetValue(proof),Is.EqualTo(2));
            foreach(var m in proof.Path){var result=MovementEngine.Apply(l,state,m);Assert.That(result.ActualDistance,Is.EqualTo(m.Distance));state=result.State;}
            Assert.That(MovementEngine.IsWon(l,state),Is.True);
            Assert.That(BoardSolver.Solve(l,BoardState.CreateInitial(l),1).Status,Is.EqualTo(SearchStatus.Incomplete));
        }
    }
}
