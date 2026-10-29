using System;
using System.IO;
using System.Linq;
using BlockRift.Core;
using BlockRift.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace BlockRift.Editor
{
    /// <summary>Onaylı sahneye yalnız Level 1 bileşenleri ekler; kamera veya sanat geometrisine dokunmaz.</summary>
    public static class LevelOneSetup
    {
        /// <summary>İzole editör doğrulamasında çalıştırılan tekrar kullanılabilir kurulum.</summary>
        public static void Build()
        {
            if(!UnityEngine.Application.isBatchMode) throw new InvalidOperationException("Batch bakım girişi");
            var level=LevelOne.Create(); var state=BoardState.CreateInitial(level);
            var proof=BoardSolver.Solve(level,state);
            if(proof.Status!=SearchStatus.Solved) throw new InvalidOperationException("Level 1 kanıtlanmadı: "+proof.Status);
            foreach(var move in proof.Path) state=MovementEngine.Apply(level,state,move).State;
            if(!MovementEngine.IsWon(level,state)) throw new InvalidOperationException("Replay başarısız");
            Directory.CreateDirectory("TestResults/Level1");
            File.WriteAllText("TestResults/Level1/solver.txt",$"Status={proof.Status}\nMinimum={proof.Path.Count}\nVisited={proof.VisitedStates}\nReplay=WIN\n"+
                string.Join("\n",proof.Path.Select(m=>$"{m.PieceId} {m.Axis} {m.Distance}")));
            var palette=AssetDatabase.LoadAssetAtPath<LevelVisualPalette>("Assets/_BlockRift/Content/LevelOnePalette.asset");
            if(!palette) { palette=ScriptableObject.CreateInstance<LevelVisualPalette>(); AssetDatabase.CreateAsset(palette,"Assets/_BlockRift/Content/LevelOnePalette.asset"); }
            string[] names={"Blue","Green","Red","Yellow","Purple","Orange"};
            palette.colors=new Material[6];
            for(int i=0;i<6;i++) palette.colors[i]=RequireMaterial("BR_Plastic_"+names[i]);
            EnsureVerticalPrefabs(AssetDatabase.LoadAssetAtPath<VisualPrefabCatalog>(GameplayArtAssets.CatalogPath));
            BindApprovedPalette(palette);
            var cyan=RequireMaterial("BR_Exit_Cyan");
            string exitPath=GameplayArtAssets.Prefabs+"Exit_Cell.prefab";
            var exit=GameObject.CreatePrimitive(PrimitiveType.Quad); exit.name="Exit_Cell";
            UnityEngine.Object.DestroyImmediate(exit.GetComponent<Collider>());
            exit.transform.rotation=Quaternion.Euler(90,0,0); exit.transform.localScale=Vector3.one*.88f;
            exit.GetComponent<MeshRenderer>().sharedMaterial=cyan;
            palette.exitPrefab=PrefabUtility.SaveAsPrefabAsset(exit,exitPath); UnityEngine.Object.DestroyImmediate(exit);
            EditorUtility.SetDirty(palette);
            var scene=EditorSceneManager.OpenScene(GameplaySceneSetup.ScenePath,OpenSceneMode.Single);
            var root=scene.GetRootGameObjects()[0];
            var layout=root.GetComponentInChildren<BoardVisualLayout>();
            var piecesRoot=layout.transform.Find("PiecesRoot");
            while(piecesRoot.childCount>0) UnityEngine.Object.DestroyImmediate(piecesRoot.GetChild(0).gameObject);
            foreach(var piece in level.Pieces)
            {
                var binding=palette.pieces.First(p=>p.pieceId==piece.Id);
                layout.VisualCatalog.TryGetPiece(binding.assetId,out var entry);
                var view=(GameObject)PrefabUtility.InstantiatePrefab(entry.Prefab,scene); view.name=binding.assetId;
                layout.PlacePieceView(view.transform,piece,piece.Start);
                if(binding.material) foreach(var renderer in view.GetComponentsInChildren<MeshRenderer>())
                { var mats=renderer.sharedMaterials; for(int i=0;i<mats.Length;i++) mats[i]=binding.material; renderer.sharedMaterials=mats; }
            }
            layout.VisualCatalog.TryGetPiece("Obstacle_1x1_Stone",out var stoneEntry);
            foreach(var cell in level.FixedCells)
            {
                var view=(GameObject)PrefabUtility.InstantiatePrefab(stoneEntry.Prefab,scene); view.name="Obstacle_1x1_Stone";
                layout.PlacePieceView(view.transform,new PieceDefinition("fixed",PieceKind.Obstacle,1,1,cell),cell);
            }
            var safe=root.transform.Find("UIRoot/GameplayCanvas/SafeArea");
            var canvas=safe.parent.gameObject;
            if(!canvas.GetComponent<GraphicRaycaster>()) canvas.AddComponent<GraphicRaycaster>();
            var hud=safe.Find("TopHUD"); var bottom=safe.Find("BottomControls");
            // Var olan alanlara tek minimal HUD eklenir; dekoratif tam ekran UI tasarımı yapılmaz.
            var moves=Label(hud,"LevelOneMoves",new Vector2(.03f,.3f),new Vector2(.97f,.85f),46);
            var win=Label(bottom,"LevelOneStatus",new Vector2(.02f,.65f),new Vector2(.98f,1),30);
            var restartObject=bottom.Find("LevelOneRestart");
            if(!restartObject) { var go=new GameObject("LevelOneRestart",typeof(RectTransform),typeof(Image),typeof(Button)); go.transform.SetParent(bottom,false); restartObject=go.transform; }
            var rect=restartObject.GetComponent<RectTransform>(); rect.anchorMin=new Vector2(.25f,.15f); rect.anchorMax=new Vector2(.75f,.55f); rect.offsetMin=rect.offsetMax=Vector2.zero;
            restartObject.GetComponent<Image>().color=new Color(1,.9f,.72f,.98f);
            var restartText=Label(restartObject,"Text",Vector2.zero,Vector2.one,34); restartText.text="Yeniden Başlat";
            if(!UnityEngine.Object.FindAnyObjectByType<EventSystem>())
            { var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule)); events.transform.SetParent(root.transform,false); }
            var controller=root.GetComponent<LevelOneGameplay>(); if(!controller) controller=root.AddComponent<LevelOneGameplay>();
            var data=new SerializedObject(controller);
            data.FindProperty("levelId").stringValue=level.Id;
            data.FindProperty("layout").objectReferenceValue=layout;
            data.FindProperty("gameplayCamera").objectReferenceValue=root.GetComponentInChildren<Camera>();
            data.FindProperty("movesLabel").objectReferenceValue=moves;
            data.FindProperty("winLabel").objectReferenceValue=win;
            data.FindProperty("restartButton").objectReferenceValue=restartObject.GetComponent<Button>();
            data.ApplyModifiedPropertiesWithoutUndo();
            moves.text="Bölüm 1    Hamle: 0"; win.text="Yıldızı turkuaz hedefe ulaştır";
            AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(GameplaySceneSetup.ScenePath,true)}
                .Concat(EditorBuildSettings.scenes.Where(s=>s.path!=GameplaySceneSetup.ScenePath)).ToArray();
            Debug.Log($"LEVEL_ONE_READY minimum={proof.Path.Count}, visited={proof.VisitedStates}, replay=WIN");
        }
        /// <summary>Yalnız bölüm yerleşimi ve şekil eşlemesini yeniler; materyal/prefab/UI/kamera üretmez veya düzenlemez.</summary>
        public static void RefreshPuzzle()
        {
            if(!UnityEngine.Application.isBatchMode) throw new InvalidOperationException("Batch bakım girişi");
            var level=LevelOne.Create();
            if(!BoardValidator.Validate(level).IsValid) throw new InvalidOperationException("Geçersiz bölüm");
            var initial=BoardState.CreateInitial(level);var proof=BoardSolver.Solve(level,initial);
            if(proof.Status!=SearchStatus.Solved||proof.RulesVersion!=MovementEngine.RulesVersion)
                throw new InvalidOperationException("Bölüm kanıtı tamamlanmadı: "+proof.Status);
            var replay=initial;
            foreach(var move in proof.Path)
            { var result=MovementEngine.Apply(level,replay,move);if(result.ActualDistance!=move.Distance)throw new InvalidOperationException("Replay mesafe hatası");replay=result.State; }
            if(!MovementEngine.IsWon(level,replay))throw new InvalidOperationException("Replay kazanmadı");
            var palette=AssetDatabase.LoadAssetAtPath<LevelVisualPalette>("Assets/_BlockRift/Content/LevelOnePalette.asset");
            var catalog=AssetDatabase.LoadAssetAtPath<VisualPrefabCatalog>(GameplayArtAssets.CatalogPath);
            EnsureVerticalPrefabs(catalog);
            // Mevcut URP materyalleri paylaşılır; kaynak materyaller yeniden yazılmaz.
            BindApprovedPalette(palette);
            EditorUtility.SetDirty(palette);
            var scene=EditorSceneManager.OpenScene(GameplaySceneSetup.ScenePath,OpenSceneMode.Single);
            var layout=scene.GetRootGameObjects()[0].GetComponentInChildren<BoardVisualLayout>();
            var piecesRoot=layout.transform.Find("PiecesRoot");
            while(piecesRoot.childCount>0) UnityEngine.Object.DestroyImmediate(piecesRoot.GetChild(0).gameObject);
            foreach(var piece in level.Pieces)
            {
                var binding=palette.pieces.Single(p=>p.pieceId==piece.Id);
                if(!layout.VisualCatalog.TryGetPiece(binding.assetId,out var entry)||entry.Width!=piece.Width||entry.Height!=piece.Height)
                    throw new InvalidOperationException("Görsel eşleme eksik/yanlış: "+piece.Id);
                var view=(GameObject)PrefabUtility.InstantiatePrefab(entry.Prefab,scene); view.name=binding.assetId;
                layout.PlacePieceView(view.transform,piece,piece.Start);
                if(binding.material) foreach(var renderer in view.GetComponentsInChildren<MeshRenderer>())
                { var mats=renderer.sharedMaterials; for(int i=0;i<mats.Length;i++) mats[i]=binding.material; renderer.sharedMaterials=mats; }
            }
            layout.VisualCatalog.TryGetPiece("Obstacle_1x1_Stone",out var stone);
            foreach(var cell in level.FixedCells)
            {
                var view=(GameObject)PrefabUtility.InstantiatePrefab(stone.Prefab,scene); view.name="Obstacle_1x1_Stone";
                layout.PlacePieceView(view.transform,new PieceDefinition("fixed",PieceKind.Obstacle,1,1,cell),cell);
            }
            var markers=layout.transform.Find("MarkersRoot");
            var exit=markers.Find("ExitVisual");
            if(!exit) { exit=((GameObject)PrefabUtility.InstantiatePrefab(palette.exitPrefab,scene)).transform;exit.name="ExitVisual";exit.SetParent(markers,false); }
            exit.position=layout.CellToWorld(level.Exit)+Vector3.up*.004f;
            AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("TestResults/MovementV2");
            File.WriteAllText("TestResults/MovementV2/solver.txt",$"RulesVersion={proof.RulesVersion}\nStatus={proof.Status}\nMinimum={proof.Path.Count}\nVisited={proof.VisitedStates}\nReplay=WIN\n"+
                string.Join("\n",proof.Path.Select(m=>$"{m.PieceId} {m.Axis} {m.Distance}")));
            Debug.Log("LEVEL_ONE_LAYOUT_REFRESHED: existing assets and camera preserved");
        }
        private static void BindApprovedPalette(LevelVisualPalette palette)
        {
            palette.pieces=new[]{
                new LevelVisualPalette.Binding{pieceId="star",assetId="Special_1x1_Star"},
                new LevelVisualPalette.Binding{pieceId="A",assetId="Block_1x3",material=palette.colors[0]},
                new LevelVisualPalette.Binding{pieceId="B",assetId="Block_3x1",material=palette.colors[1]},
                new LevelVisualPalette.Binding{pieceId="C",assetId="Block_1x2",material=palette.colors[3]},
                new LevelVisualPalette.Binding{pieceId="D",assetId="Block_1x1",material=palette.colors[2]},
                new LevelVisualPalette.Binding{pieceId="E",assetId="Block_2x1",material=palette.colors[4]},
                new LevelVisualPalette.Binding{pieceId="F",assetId="Block_2x2",material=palette.colors[5]}};
        }
        // Oyuncu rotasyonu değildir: onaylı FBX'in aynı mesh'ini sabit yönelimli yeni prefab'da kullanır.
        // Kaynak mesh/FBX değişmez, ölçek her katmanda 1 kalır; yalnız model çocuğu Y'de 90° yönelir.
        private static void EnsureVerticalPrefabs(VisualPrefabCatalog catalog)
        {
            foreach(int length in new[]{2,3,4})
            {
                string id="Block_1x"+length;
                if(catalog.TryGetPiece(id,out var existing))
                {
                    if(!VisualModelChecks.ValidatePieceBounds(GameplayArtAssets.MeasureBounds(existing.Prefab.transform),1,length,out var error))
                        throw new InvalidOperationException(id+": "+error);
                    continue;
                }
                if(!catalog.TryGetPiece("Block_"+length+"x1",out var source))throw new InvalidOperationException("Kaynak FBX prefab yok: "+id);
                var wrapper=new GameObject(id);
                try
                {
                    var model=(GameObject)PrefabUtility.InstantiatePrefab(source.Prefab);
                    model.transform.SetParent(wrapper.transform,false);model.transform.localRotation=Quaternion.Euler(0,90,0);
                    if(!VisualModelChecks.ValidatePieceBounds(GameplayArtAssets.MeasureBounds(wrapper.transform),1,length,out var error))
                        throw new InvalidOperationException(id+": "+error);
                    var prefab=PrefabUtility.SaveAsPrefabAsset(wrapper,GameplayArtAssets.Prefabs+id+".prefab");
                    var data=new SerializedObject(catalog);var entries=data.FindProperty("entries");int index=entries.arraySize;entries.InsertArrayElementAtIndex(index);
                    var entry=entries.GetArrayElementAtIndex(index);
                    entry.FindPropertyRelative("assetId").stringValue=id;
                    entry.FindPropertyRelative("width").intValue=1;entry.FindPropertyRelative("height").intValue=length;
                    entry.FindPropertyRelative("board").boolValue=false;entry.FindPropertyRelative("prefab").objectReferenceValue=prefab;
                    data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(catalog);
                }
                finally { UnityEngine.Object.DestroyImmediate(wrapper); }
            }
        }
        /// <summary>Mevcut sanat materyalini yalnız okur; oluşturma ve düzenleme ApprovedAssetMigration sorumluluğudur.</summary>
        private static Material RequireMaterial(string name)
        {
            string path=GameplayArtAssets.Materials+name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat) throw new InvalidOperationException("Mevcut materyal eksik: "+path+". Güncel sanat entegrasyonunu doğrulayın.");
            return mat;
        }
        private static Text Label(Transform parent,string name,Vector2 min,Vector2 max,int size)
        {
            var child=parent.Find(name);
            if(!child) { var go=new GameObject(name,typeof(RectTransform),typeof(Text)); go.transform.SetParent(parent,false); child=go.transform; }
            var rect=child.GetComponent<RectTransform>(); rect.anchorMin=min; rect.anchorMax=max; rect.offsetMin=rect.offsetMax=Vector2.zero;
            var text=child.GetComponent<Text>(); text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize=size;
            text.alignment=TextAnchor.MiddleCenter; text.color=new Color(.05f,.2f,.32f); text.raycastTarget=false;
            return text;
        }
    }
}
