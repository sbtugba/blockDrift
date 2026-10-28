using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BlockRift.Core;
using UnityEngine;

namespace BlockRift.Editor
{
    /// <summary>Elle hazırlanmış Level 1 adaylarının offline kalite denetimi; generator veya kural motoru değildir.</summary>
    public static class LevelOneQualityAudit
    {
        private sealed class Node
        {
            public BoardState State; public int Depth;
            public readonly List<Link> Parents=new List<Link>();
        }
        private sealed class Link { public Node Parent; public MoveCommand Move; }
        private sealed class RouteStats
        {
            public int Count, MinStarRows=6, MaxStarRows, MinStarColumns=6, MaxStarColumns;
            public int MinMovedObstacles=int.MaxValue, MinSwept=36, MaxSwept;
            public bool Complete=true;
        }
        /// <summary>Mevcut BFS ve Core ile aday, dondurma, çıkarma, optimum yol ve reversibility kanıtlarını kaydeder.</summary>
        public static void Run()
        {
            if(!UnityEngine.Application.isBatchMode) throw new InvalidOperationException("Offline batch denetimi");
            var output=new StringBuilder("# Level 1 — algoritmik kalite denetimi\n\n");
            foreach(var chosen in IndependentLevelOneCandidates.All())
            {
            Candidate(output,chosen.Id,chosen);
            foreach(var obstacle in chosen.Pieces.Where(p=>p.Kind==PieceKind.Obstacle))
                Freeze(output,chosen,obstacle.Id);
            if(chosen.Id.StartsWith("A-")) { output.AppendLine("A adayı: derin zincir; tam grafik/ablasyon çalıştırılmadı, zorunluluk iddiası yok."); continue; }
            output.AppendLine("## "+chosen.Id+": engel karşılaştırmaları");
            foreach(var piece in chosen.Pieces.Where(p=>p.Kind==PieceKind.Obstacle))
            {
                var removed=new LevelDefinition("remove-"+piece.Id,1,1,6,6,chosen.Exit,chosen.Pieces.Where(p=>p.Id!=piece.Id),chosen.FixedCells,Array.Empty<Cell>());
                Counterfactual(output,"Çıkar: "+piece.Id,removed);
            }
            foreach(var cell in chosen.FixedCells)
                Counterfactual(output,"Sabit hücreyi çıkar: "+cell,new LevelDefinition("remove-fixed",1,1,6,6,chosen.Exit,chosen.Pieces,chosen.FixedCells.Where(c=>!c.Equals(cell)),Array.Empty<Cell>()));
            AuditWholeGraph(output,chosen);
            }
            Directory.CreateDirectory("TestResults/Level1Independent");
            File.WriteAllText("TestResults/Level1Independent/quality.md",output.ToString());
            Debug.Log("LEVEL_ONE_QUALITY_AUDIT_COMPLETE");
        }
        private static void Freeze(StringBuilder output,LevelDefinition level,string id)
        {
            // Solver'ın filtre API'si yoktur. Bu ek kalite taraması özgün geçerli level'ı korur;
            // yalnız dondurulan ID'nin kenarlarını kaldırır. Bütün hareketler yine aynı Core'dur.
            var start=BoardState.CreateInitial(level); var queue=new Queue<BoardState>(); var seen=new HashSet<string>();
            queue.Enqueue(start); seen.Add(BoardSolver.Key(start));
            SearchStatus status=SearchStatus.Unsolvable;
            while(queue.Count>0)
            {
                var state=queue.Dequeue(); if(MovementEngine.IsWon(level,state)) { status=SearchStatus.Solved; break; }
                foreach(var move in MovementEngine.EnumerateLegalMoves(level,state).Where(m=>m.PieceId!=id))
                {
                    var next=MovementEngine.Apply(level,state,move).State; var key=BoardSolver.Key(next);
                    if(seen.Contains(key)) continue;
                    if(seen.Count>=100000) { status=SearchStatus.Incomplete; queue.Clear(); break; }
                    seen.Add(key); queue.Enqueue(next);
                }
            }
            output.AppendLine($"Dondurma grafiği {id}: {status}, visited={seen.Count}; level/exit kuralları değiştirilmedi.");
        }
        private static void Candidate(StringBuilder output,string label,LevelDefinition level)
        {
            var validation=BoardValidator.Validate(level);
            output.AppendLine($"\n### {label}\nBaşlangıç geçerli: {validation.IsValid}; hatalar: {string.Join(",",validation.Errors)}");
            if(!validation.IsValid) return;
            DescribeProof(output,level,BoardState.CreateInitial(level));
        }
        private static void Counterfactual(StringBuilder output,string label,LevelDefinition level)
        {
            // Destek silinince başlangıç kararsız olabilir. Bu yayın adayı değil, kısıt kaldırma deneyidir.
            output.AppendLine($"\n### Karşı-olgusal: {label}\nBaşlangıç validator: {string.Join(",",BoardValidator.Validate(level).Errors)}");
            var state=new BoardState(level.Pieces.Select(p=>new PiecePosition(p.Id,p.Start)),Array.Empty<bool>());
            DescribeProof(output,level,state);
        }
        private static void DescribeProof(StringBuilder output,LevelDefinition level,BoardState state)
        {
            var proof=BoardSolver.Solve(level,state,100000);
            output.AppendLine($"Durum: {proof.Status}; minimum: {(proof.Status==SearchStatus.Solved?proof.Path.Count.ToString():"kanıt yok")}; visited: {proof.VisitedStates}");
            if(proof.Status!=SearchStatus.Solved) return;
            var swept=new HashSet<Cell>(); var rows=new HashSet<int>(); var columns=new HashSet<int>();
            foreach(var move in proof.Path)
            {
                var from=MovementEngine.Position(state,move.PieceId); var result=MovementEngine.Apply(level,state,move);
                var to=MovementEngine.Position(result.State,move.PieceId);
                output.AppendLine($"- {move.PieceId}: {move.Axis} {result.ActualDistance}, {from} → {to}");
                AddSwept(level,move,from,to,swept,rows,columns); state=result.State;
            }
            output.AppendLine($"Replay WIN: {MovementEngine.IsWon(level,state)}; farklı hareket eden parçalar: {string.Join(",",proof.Path.Select(m=>m.PieceId).Distinct())}");
            output.AppendLine($"Yıldız satırları: {string.Join(",",rows.OrderBy(x=>x))}; sütunları: {string.Join(",",columns.OrderBy(x=>x))}; hareket ayakizlerinin süpürdüğü hücre: {swept.Count}");
            output.AppendLine("Süpürülen hücreler: "+string.Join(" ",swept.OrderBy(c=>c.Y).ThenBy(c=>c.X)));
        }
        private static void AddSwept(LevelDefinition level,MoveCommand move,Cell from,Cell to,HashSet<Cell> swept,HashSet<int> rows,HashSet<int> columns)
        {
            var piece=level.Pieces.Single(p=>p.Id==move.PieceId);
            int steps=Math.Abs(from.X-to.X)+Math.Abs(from.Y-to.Y);
            for(int n=0;n<=steps;n++)
            {
                int x=from.X+Math.Sign(to.X-from.X)*n,y=from.Y+Math.Sign(to.Y-from.Y)*n;
                for(int py=0;py<piece.Height;py++) for(int px=0;px<piece.Width;px++) swept.Add(new Cell(x+px,y+py));
                if(piece.Kind==PieceKind.Target) { rows.Add(y); columns.Add(x); }
            }
        }
        private static void AuditWholeGraph(StringBuilder output,LevelDefinition level)
        {
            output.AppendLine("\n## "+level.Id+": tam erişilebilir durum grafiği ve tüm minimum yollar");
            var initial=BoardState.CreateInitial(level); var root=new Node{State=initial};
            var nodes=new Dictionary<string,Node>{{BoardSolver.Key(initial),root}}; var queue=new Queue<Node>(); queue.Enqueue(root);
            var winners=new List<Node>(); int min=int.MaxValue,arcs=0; bool reversible=true,complete=true;
            while(queue.Count>0)
            {
                var node=queue.Dequeue();
                if(MovementEngine.IsWon(level,node.State)) { if(node.Depth<min) { min=node.Depth; winners.Clear(); } if(node.Depth==min) winners.Add(node); continue; }
                foreach(var move in MovementEngine.EnumerateLegalMoves(level,node.State))
                {
                    var result=MovementEngine.Apply(level,node.State,move); arcs++;
                    if(!result.Won)
                    {
                        var back=MovementEngine.Apply(level,result.State,new MoveCommand(move.PieceId,move.Axis,-result.ActualDistance));
                        reversible &= BoardSolver.Key(back.State)==BoardSolver.Key(node.State);
                    }
                    string key=BoardSolver.Key(result.State);
                    if(!nodes.TryGetValue(key,out var next))
                    {
                        if(nodes.Count>=100000) { complete=false; queue.Clear(); break; }
                        next=new Node{State=result.State,Depth=node.Depth+1}; nodes.Add(key,next); queue.Enqueue(next);
                    }
                    if(next.Depth==node.Depth+1) next.Parents.Add(new Link{Parent=node,Move=move});
                }
            }
            output.AppendLine($"Grafik: {(complete?"COMPLETE":"INCOMPLETE")}; durum={nodes.Count}; yönlü geçiş={arcs}; tüm terminal olmayan geçişlerin Core ile tersi doğrulandı={reversible}");
            output.AppendLine(complete&&reversible&&winners.Count>0?"Çıkmaz: 0 (her erişilen terminal olmayan durum başlangıca geri dönebilir; başlangıcın kazanma yolu kanıtlandı).":"Çıkmaz sayısı: INCOMPLETE");
            if(!complete) return;
            var stats=new RouteStats(); var reverse=new List<MoveCommand>();
            foreach(var win in winners) VisitPaths(win,level,initial,reverse,stats);
            output.AppendLine($"Optimum yol incelemesi: {(stats.Complete?"COMPLETE":"INCOMPLETE")}; minimum={min}; incelenen farklı komut dizisi={stats.Count}");
            output.AppendLine($"Tüm optimumlarda yıldızın kullandığı farklı satır min/max={stats.MinStarRows}/{stats.MaxStarRows}, sütun min/max={stats.MinStarColumns}/{stats.MaxStarColumns}; farklı hareketli engel alt sınırı={stats.MinMovedObstacles}; süpürülen hücre min/max={stats.MinSwept}/{stats.MaxSwept}");
        }
        private static void VisitPaths(Node node,LevelDefinition level,BoardState initial,List<MoveCommand> reverse,RouteStats stats)
        {
            if(stats.Count>=100000) { stats.Complete=false; return; }
            if(node.Parents.Count==0)
            {
                var state=initial; var rows=new HashSet<int>(); var columns=new HashSet<int>(); var swept=new HashSet<Cell>(); var obstacles=new HashSet<string>();
                for(int i=reverse.Count-1;i>=0;i--)
                {
                    var move=reverse[i]; var result=MovementEngine.Apply(level,state,move);
                    AddSwept(level,move,MovementEngine.Position(state,move.PieceId),MovementEngine.Position(result.State,move.PieceId),swept,rows,columns);
                    if(level.Pieces.Single(p=>p.Id==move.PieceId).Kind==PieceKind.Obstacle) obstacles.Add(move.PieceId);
                    state=result.State;
                }
                if(!MovementEngine.IsWon(level,state)) throw new InvalidOperationException("Optimum yol replay hatası");
                stats.Count++; stats.MinStarRows=Math.Min(stats.MinStarRows,rows.Count); stats.MaxStarRows=Math.Max(stats.MaxStarRows,rows.Count);
                stats.MinStarColumns=Math.Min(stats.MinStarColumns,columns.Count); stats.MaxStarColumns=Math.Max(stats.MaxStarColumns,columns.Count);
                stats.MinMovedObstacles=Math.Min(stats.MinMovedObstacles,obstacles.Count); stats.MinSwept=Math.Min(stats.MinSwept,swept.Count); stats.MaxSwept=Math.Max(stats.MaxSwept,swept.Count);
                return;
            }
            foreach(var link in node.Parents) { reverse.Add(link.Move); VisitPaths(link.Parent,level,initial,reverse,stats); reverse.RemoveAt(reverse.Count-1); }
        }
    }
}
