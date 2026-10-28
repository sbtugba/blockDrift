using BlockRift.Core;

namespace BlockRift.App
{
    /// <summary>Tek authoritative state sahibi; ekonomi, input ve Unity bağımlılığı yoktur.</summary>
    public sealed class GameSession
    {
        public LevelDefinition Level { get; }
        public BoardState State { get; private set; }
        public int MoveCount { get; private set; }
        public bool Won => MovementEngine.IsWon(Level,State);
        public GameSession(LevelDefinition level) { Level=level; Restart(); }
        /// <summary>Gerçek yer değiştirmeyi bir kez sayar; NO_OP ve terminal input sayılmaz.</summary>
        public MoveResult Commit(MoveCommand command)
        {
            var result=Preview(command);
            if(result.Changed) { State=result.State; MoveCount++; }
            return result;
        }
        /// <summary>Sürükleme önizlemesi oturum durumuna yazmaz.</summary>
        public MoveResult Preview(MoveCommand command) => MovementEngine.Apply(Level,State,command);
        /// <summary>Ücretsiz restart her zaman yayınlanmış başlangıcı geri yükler.</summary>
        public void Restart() { State=BoardState.CreateInitial(Level); MoveCount=0; }
    }
}
