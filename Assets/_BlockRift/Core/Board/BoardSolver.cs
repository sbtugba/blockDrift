using System.Collections.Generic;
using System.Linq;

namespace BlockRift.Core
{
    public enum SearchStatus
    {
        Solved, Unsolvable, Incomplete
    }
    /// <summary>Kesik arama kanıtlanmış çözümsüzlükten ayrıdır.</summary>
    public sealed class SolutionProof
    {
        public SearchStatus Status
        {
            get;
        }
        public IReadOnlyList<MoveCommand> Path
        {
            get;
        }
        public int VisitedStates
        {
            get;
        }
        public int RulesVersion
        {
            get;
        }
        internal SolutionProof(SearchStatus status, List<MoveCommand> path, int visited, int rulesVersion)
        {
            Status = status;
            Path = path.AsReadOnly();
            VisitedStates = visited;
            RulesVersion = rulesVersion;
        }
    }
    /// <summary>Bütün kenarları ortak Core geçişinden gelen, deterministik minimum hamle BFS'i.</summary>
    public static class BoardSolver
    {
        private sealed class Node
        {
            public BoardState State; public Node Parent; public MoveCommand Move;
        }
        /// <summary>FIFO katmanları tam tarar; limitte INCOMPLETE döndürür. RNG içermez.</summary>
        public static SolutionProof Solve(LevelDefinition level, BoardState initial, int maxStates = 100000)
        {
            MovementEngine.RequireSupportedRules(level);
            var queue = new Queue<Node>();
            var seen = new HashSet<string>();
            var root = new Node { State = initial };
            queue.Enqueue(root);
            seen.Add(Key(initial));
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                if (MovementEngine.IsWon(level, node.State))
                {
                    var path = new List<MoveCommand>();
                    for (var n = node; n.Parent != null; n = n.Parent)
                        path.Add(n.Move);
                    path.Reverse();
                    return new SolutionProof(SearchStatus.Solved, path, seen.Count, level.RulesVersion);
                }
                foreach (var move in MovementEngine.EnumerateLegalMoves(level, node.State))
                {
                    var next = MovementEngine.Apply(level, node.State, move).State;
                    string key = Key(next);
                    if (seen.Contains(key))
                        continue;
                    if (seen.Count >= maxStates)
                        return new SolutionProof(SearchStatus.Incomplete, new List<MoveCommand>(), seen.Count, level.RulesVersion);
                    seen.Add(key);
                    queue.Enqueue(new Node { State = next, Parent = node, Move = move });
                }
            }
            return new SolutionProof(SearchStatus.Unsolvable, new List<MoveCommand>(), seen.Count, level.RulesVersion);
        }
        /// <summary>ID sırası ve trigger bitleri state anahtarının parçasıdır.</summary>
        public static string Key(BoardState state) => string.Join(";", state.Positions.OrderBy(p => p.PieceId, System.StringComparer.Ordinal)
            .Select(p => $"{p.PieceId.Length}:{p.PieceId}:{p.Origin.X},{p.Origin.Y}")) + "/" + string.Join("", state.ConsumedTriggers.Select(b => b ? "1" : "0"));
    }
}
