using System.Linq;
using NUnit.Framework;
using BlockRift.Core;

namespace BlockRift.Tests
{
    /// <summary>Ortak geçişin bölüm kanıtını ve sınır davranışlarını uygulamadan önce sabitler.</summary>
    public sealed class LevelOneTests
    {
        [Test] public void LevelOneRequiresObstacleMovementAndHasShortVerifiedSolution()
        {
            var level = LevelOne.Create();
            Assert.That(BoardValidator.Validate(level).IsValid, Is.True);
            Assert.That(level.Width, Is.EqualTo(6));
            Assert.That(level.Triggers.Count, Is.Zero);
            var proof = BoardSolver.Solve(level, BoardState.CreateInitial(level), 100000);
            Assert.That(proof.Status, Is.EqualTo(SearchStatus.Solved));
            // Hamle kotası değil; offline BFS ile ölçülen sürümün regresyon kanıtıdır.
            Assert.That(proof.Path.Count, Is.EqualTo(8), "Küçük Aktarma rulesVersion 2 minimum kanıtı");
            Assert.That(proof.Path.Any(m => m.PieceId != "star"), Is.True);
            var state = BoardState.CreateInitial(level);
            foreach (var move in proof.Path) state = MovementEngine.Apply(level,state,move).State;
            Assert.That(MovementEngine.IsWon(level,state), Is.True);
            Assert.That(BoardSolver.Solve(level,BoardState.CreateInitial(level),1).Status, Is.EqualTo(SearchStatus.Incomplete));
        }
    }
}
