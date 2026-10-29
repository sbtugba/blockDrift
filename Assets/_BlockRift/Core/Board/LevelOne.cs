using System;

namespace BlockRift.Core
{
    /// <summary>Onaylı Küçük Aktarma; rulesVersion 2 sabit içeriği, runtime üretim değildir.</summary>
    public static class LevelOne
    {
        /// <summary>Her yüklemede aynı başlangıç tanımını üretir. Renk domain verisi değildir.</summary>
        public static LevelDefinition Create() => new LevelDefinition("level-001", 1, MovementEngine.RulesVersion, 6, 6, new Cell(5, 0),
            new[]{new PieceDefinition("star",PieceKind.Target,1,1,new Cell(2,5)),
                new PieceDefinition("A",PieceKind.Obstacle,1,3,new Cell(3,2)),
                new PieceDefinition("B",PieceKind.Obstacle,3,1,new Cell(3,1)),
                new PieceDefinition("C",PieceKind.Obstacle,1,2,new Cell(2,0)),
                new PieceDefinition("D",PieceKind.Obstacle,1,1,new Cell(5,2)),
                new PieceDefinition("E",PieceKind.Obstacle,2,1,new Cell(4,3)),
                new PieceDefinition("F",PieceKind.Obstacle,2,2,new Cell(0,3))},
            new[] { new Cell(0, 2), new Cell(4, 2), new Cell(4, 0) }, Array.Empty<Cell>());
    }
}
