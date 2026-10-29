using System;
using BlockRift.Core;

namespace BlockRift.Editor
{
    /// <summary>Referanstan kopyalanmayan üç elle tasarlanmış strateji; arama/üretim algoritması içermez.</summary>
    internal static class IndependentLevelOneCandidates
    {
        public static LevelDefinition[] All() => new[] { Relay(), GoalPocket(), TargetParking() };
        // Dört basamaklı alan açma zinciri; öğretici yükü karşılaştırmak için aday.
        private static LevelDefinition Relay() => new LevelDefinition("A-relay", 1, 1, 6, 6, new Cell(1, 4), new[]{
            new PieceDefinition("star",PieceKind.Target,1,1,new Cell(4,0)),
            new PieceDefinition("gate",PieceKind.Obstacle,2,2,new Cell(0,4)),
            new PieceDefinition("blue",PieceKind.Obstacle,3,1,new Cell(2,4)),
            new PieceDefinition("bridge",PieceKind.Obstacle,2,1,new Cell(2,3)),
            new PieceDefinition("support",PieceKind.Obstacle,1,1,new Cell(3,2))},
            new[] { new Cell(0, 3), new Cell(3, 1) }, Array.Empty<Cell>());
        // Hedefin üstündeki büyük bloğa iki hücrelik park alanı aç; sonra hedefe giriş yüzünü bul.
        private static LevelDefinition GoalPocket() => new LevelDefinition("B-goal-pocket", 1, 1, 6, 6, new Cell(2, 0), new[]{
            new PieceDefinition("star",PieceKind.Target,1,1,new Cell(5,5)),
            new PieceDefinition("gate",PieceKind.Obstacle,2,2,new Cell(2,1)),
            new PieceDefinition("blue",PieceKind.Obstacle,3,1,new Cell(1,3))},
            new[] { new Cell(1, 0), new Cell(3, 0), new Cell(1, 1), new Cell(4, 1) }, Array.Empty<Cell>());
        // Yıldız da engelin manevrasını kapatır: önce geçici park, sonra karşılıklı yol açma.
        private static LevelDefinition TargetParking() => new LevelDefinition("C-target-parking", 1, 1, 6, 6, new Cell(0, 3), new[]{
            new PieceDefinition("star",PieceKind.Target,1,1,new Cell(3,3)),
            new PieceDefinition("gate",PieceKind.Obstacle,3,1,new Cell(0,3)),
            new PieceDefinition("blue",PieceKind.Obstacle,2,1,new Cell(4,3))},
            new[] { new Cell(0, 2), new Cell(0, 4), new Cell(2, 2), new Cell(2, 4), new Cell(5, 2) }, Array.Empty<Cell>());
    }
}
