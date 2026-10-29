using System;
using System.Collections.Generic;
using System.Linq;

namespace BlockRift.Core
{
    public enum MoveAxis { Horizontal, Vertical }
    /// <summary>Tek eksenli manuel istek; mesafe hücre cinsindendir.</summary>
    public readonly struct MoveCommand
    {
        public string PieceId { get; }
        public MoveAxis Axis { get; }
        public int Distance { get; }
        public MoveCommand(string pieceId, MoveAxis axis, int distance)
        { PieceId=pieceId; Axis=axis; Distance=distance; }
    }
    /// <summary>Önizleme ve commit aynı saf sonuçtan yararlanır.</summary>
    public sealed class MoveResult
    {
        public BoardState State { get; }
        public bool Changed { get; }
        public bool Won { get; }
        public int ActualDistance { get; }
        internal MoveResult(BoardState state, bool changed, bool won, int distance)
        { State=state; Changed=changed; Won=won; ActualDistance=distance; }
    }
    /// <summary>Tetikleyicisiz kuralların tek geçiş kaynağıdır; fizik veya Unity kullanmaz.</summary>
    public static class MovementEngine
    {
        public const int RulesVersion = 2;
        /// <summary>Eski/farklı sürümdeki içerik yeni kurallarla sessizce oynatılamaz.</summary>
        internal static void RequireSupportedRules(LevelDefinition level)
        {
            if(level==null) throw new ArgumentNullException(nameof(level));
            if(level.RulesVersion!=RulesVersion) throw new NotSupportedException("Beklenen rulesVersion=2; içerik yeniden doğrulanmalı.");
        }
        /// <summary>Yalnız manuel sürükleme iznidir; gelecekteki otomatik gravity bu filtreden geçmez.</summary>
        public static bool AllowsManualAxis(PieceDefinition piece,MoveAxis axis) => piece!=null &&
            ShapeCatalog.IsAllowed(piece.Width,piece.Height) &&
            (axis==MoveAxis.Horizontal || axis==MoveAxis.Vertical) &&
            (piece.Width==piece.Height || (piece.Width>piece.Height?axis==MoveAxis.Horizontal:axis==MoveAxis.Vertical));
        /// <summary>Tüm ara hücreleri tarar; kapıda veya ilk engelden önce durur.</summary>
        public static MoveResult Apply(LevelDefinition level, BoardState state, MoveCommand command)
        {
            RequireSupportedRules(level);
            if (level.Triggers.Count != 0) throw new NotSupportedException("Trigger/cascade henüz uygulanmadı; sessizce atlanamaz.");
            if (command.Axis != MoveAxis.Horizontal && command.Axis != MoveAxis.Vertical) throw new ArgumentOutOfRangeException(nameof(command));
            bool won=IsWon(level,state);
            var piece=level.Pieces.FirstOrDefault(p=>p.Id==command.PieceId);
            if (won || piece==null || command.Distance==0 || !AllowsManualAxis(piece,command.Axis)) return new MoveResult(state,false,won,0);
            Cell start=Position(state,piece.Id), end=start;
            int sign=Math.Sign(command.Distance), distance=0;
            // Tahta çapından uzun istekler de sabit zamanda sınırda kesilir; int.MinValue taşmaz.
            long count=Math.Min(Math.Abs((long)command.Distance), Math.Max(level.Width,level.Height));
            for(int step=0;step<count;step++)
            {
                var next=new Cell(end.X+(command.Axis==MoveAxis.Horizontal?sign:0),end.Y+(command.Axis==MoveAxis.Vertical?sign:0));
                if (!Fits(level,state,piece,next)) break;
                end=next; distance+=sign;
                if(piece.Kind==PieceKind.Target && end.Equals(level.Exit)) { won=true; break; }
            }
            if(distance==0) return new MoveResult(state,false,false,0);
            var result=new BoardState(state.Positions.Select(p=>p.PieceId==piece.Id?new PiecePosition(p.PieceId,end):p),state.ConsumedTriggers);
            return new MoveResult(result,true,won,distance);
        }
        /// <summary>Güncel snapshot içindeki hücreyi kararlı ID ile bulur.</summary>
        public static Cell Position(BoardState state,string id) => state.Positions.First(p=>p.PieceId==id).Origin;
        /// <summary>Ana parça kapı hücresindeyse terminal durumdur.</summary>
        public static bool IsWon(LevelDefinition level,BoardState state) =>
            Position(state,level.Pieces.First(p=>p.Kind==PieceKind.Target).Id).Equals(level.Exit);
        /// <summary>Solver yalnız gerçek yer değiştiren ortak geçişleri kullanır.</summary>
        public static IEnumerable<MoveCommand> EnumerateLegalMoves(LevelDefinition level,BoardState state)
        {
            RequireSupportedRules(level);
            foreach(var piece in level.Pieces)
            foreach(MoveAxis axis in new[]{MoveAxis.Horizontal,MoveAxis.Vertical})
            foreach(int sign in new[]{-1,1})
            for(int n=1;n<Math.Max(level.Width,level.Height);n++)
            {
                var command=new MoveCommand(piece.Id,axis,n*sign);
                var result=Apply(level,state,command);
                if(!result.Changed || result.ActualDistance!=n*sign) break;
                yield return command;
                if(result.Won) break;
            }
        }
        private static bool Fits(LevelDefinition level,BoardState state,PieceDefinition piece,Cell origin)
        {
            if(origin.X<0||origin.Y<0||origin.X+piece.Width>level.Width||origin.Y+piece.Height>level.Height) return false;
            foreach(var cell in level.FixedCells)
                if(cell.X>=origin.X&&cell.X<origin.X+piece.Width&&cell.Y>=origin.Y&&cell.Y<origin.Y+piece.Height) return false;
            foreach(var other in level.Pieces)
            {
                if(other.Id==piece.Id) continue;
                var p=Position(state,other.Id);
                if(origin.X<p.X+other.Width&&origin.X+piece.Width>p.X&&origin.Y<p.Y+other.Height&&origin.Y+piece.Height>p.Y) return false;
            }
            return true;
        }
    }
}
