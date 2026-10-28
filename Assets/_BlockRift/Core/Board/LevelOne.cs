using System;

namespace BlockRift.Core
{
    /// <summary>Sürüm 1 sabit yayın içeriği; runtime üretim veya RNG değildir.</summary>
    public static class LevelOne
    {
        /// <summary>Her yüklemede aynı başlangıç tanımını üretir. Renk domain verisi değildir.</summary>
        public static LevelDefinition Create() => new LevelDefinition("level-001",1,1,6,6,new Cell(2,0),
            new[]{new PieceDefinition("star",PieceKind.Target,1,1,new Cell(5,5)),
                new PieceDefinition("gate",PieceKind.Obstacle,2,2,new Cell(2,1)),
                new PieceDefinition("blue",PieceKind.Obstacle,3,1,new Cell(1,3))},
            // Alt cepten yana kaçışı kapatır; hedefi boşaltmak ve hedefin girişini açmak ayrı kararlardır.
            new[]{new Cell(1,0),new Cell(3,0),new Cell(1,1),new Cell(4,1)},Array.Empty<Cell>());
    }
}
