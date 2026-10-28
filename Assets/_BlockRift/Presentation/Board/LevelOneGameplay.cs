using System.Collections.Generic;
using System.Linq;
using BlockRift.Core;
using BlockRift.App;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BlockRift.Presentation
{
    /// <summary>Mevcut grid ve katalogla gerçek bölümü gösterir; kuralları yalnız GameSession hesaplar.</summary>
    public sealed class LevelOneGameplay : MonoBehaviour
    {
        [SerializeField] private BoardVisualLayout layout;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private LevelVisualPalette palette;
        [SerializeField] private Text movesLabel, winLabel;
        [SerializeField] private Button restartButton;
        private readonly Dictionary<string,Transform> views=new Dictionary<string,Transform>();
        private readonly List<RaycastResult> uiHits=new List<RaycastResult>();
        private string selected;
        private MoveAxis? axis;
        private Vector3 dragStart;
        private MoveCommand previewCommand;
        private int pointerId=-1;
        public GameSession Session { get; private set; }
        public IReadOnlyDictionary<string,Transform> Views => views;
        public BoardVisualLayout Layout => layout;
        public Camera GameplayCamera => gameplayCamera;

        private void Start()
        {
            Session=new GameSession(LevelOne.Create());
            var piecesRoot=layout.transform.Find("PiecesRoot");
            // Sanat denetim dizilimi editörde korunur; runtime authoritative bölümün yerini tutmaz.
            foreach(Transform child in piecesRoot) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            foreach(var piece in Session.Level.Pieces)
            {
                var binding=palette.pieces.First(p=>p.pieceId==piece.Id);
                if(!layout.VisualCatalog.TryGetPiece(binding.assetId,out var entry)) throw new System.InvalidOperationException("Eksik asset: "+binding.assetId);
                if(entry.Width!=piece.Width||entry.Height!=piece.Height) throw new System.InvalidOperationException("Mesh/grid boyutu uyuşmuyor: "+piece.Id);
                var view=Instantiate(entry.Prefab); view.name=piece.Id;
                if(binding.material) foreach(var renderer in view.GetComponentsInChildren<MeshRenderer>())
                {
                    var mats=renderer.sharedMaterials;
                    for(int i=0;i<mats.Length;i++) mats[i]=binding.material;
                    renderer.sharedMaterials=mats;
                }
                layout.PlacePieceView(view.transform,piece,piece.Start); views.Add(piece.Id,view.transform);
            }
            foreach(var cell in Session.Level.FixedCells)
            {
                if(!layout.VisualCatalog.TryGetPiece("Obstacle_1x1_Stone",out var stone)) throw new System.InvalidOperationException("Stone eksik");
                var view=Instantiate(stone.Prefab,piecesRoot); view.name="Fixed_"+cell;
                view.transform.position=layout.CellToWorld(cell);
            }
            var exit=Instantiate(palette.exitPrefab,layout.transform); exit.name="ExitVisual";
            exit.transform.position=layout.CellToWorld(Session.Level.Exit)+Vector3.up*0.004f;
            restartButton.onClick.AddListener(Restart);
            Render(Session.State);
        }
        private void OnDestroy() { if(restartButton) restartButton.onClick.RemoveListener(Restart); }
        private void OnDisable() { selected=null; axis=null; if(Session!=null) Render(Session.State); }
        /// <summary>Restart sürükleme durumunu da iptal eder, oturumu başlangıca döndürür.</summary>
        public void Restart() { selected=null; axis=null; Session.Restart(); Render(Session.State); }

        private void Update()
        {
            if(Session==null||Session.Won) return;
            var touch=Touchscreen.current;
            if(touch!=null && (touch.primaryTouch.press.isPressed||touch.primaryTouch.press.wasReleasedThisFrame))
            {
                var t=touch.primaryTouch;
                if(t.press.wasPressedThisFrame) { pointerId=t.touchId.ReadValue(); BeginPointer(t.position.ReadValue()); }
                if(pointerId==t.touchId.ReadValue() && t.press.isPressed) DragPointer(t.position.ReadValue());
                if(t.press.wasReleasedThisFrame) { EndPointer(t.position.ReadValue()); pointerId=-1; }
            }
            else if(Mouse.current!=null)
            {
                var mouse=Mouse.current;
                if(mouse.leftButton.wasPressedThisFrame) BeginPointer(mouse.position.ReadValue());
                if(mouse.leftButton.isPressed) DragPointer(mouse.position.ReadValue());
                if(mouse.leftButton.wasReleasedThisFrame) EndPointer(mouse.position.ReadValue());
            }
        }
        private bool OnUI(Vector2 screen)
        {
            if(!EventSystem.current) return false;
            var data=new PointerEventData(EventSystem.current){position=screen};
            uiHits.Clear(); EventSystem.current.RaycastAll(data,uiHits); return uiHits.Count>0;
        }
        private bool Ground(Vector2 screen,out Vector3 world)
        {
            var plane=new Plane(layout.transform.up,layout.CellToWorld(new Cell(0,0)));
            var ray=gameplayCamera.ScreenPointToRay(screen);
            if(plane.Raycast(ray,out float d)) { world=ray.GetPoint(d); return true; }
            world=default; return false;
        }
        /// <summary>Ekran ışını grid düzlemine düşürülür; seçim yalnız Core ayakizini kullanır.</summary>
        public bool BeginPointer(Vector2 screen)
        {
            if(Session==null||Session.Won||OnUI(screen)||!Ground(screen,out var world)) return false;
            Vector3 local=layout.transform.InverseTransformPoint(world);
            foreach(var piece in Session.Level.Pieces)
            {
                var p=layout.transform.InverseTransformPoint(layout.PieceToWorld(piece,MovementEngine.Position(Session.State,piece.Id)));
                if(Mathf.Abs(local.x-p.x)>piece.Width*.5f||Mathf.Abs(local.z-p.z)>piece.Height*.5f) continue;
                selected=piece.Id; dragStart=local; axis=null;
                previewCommand=new MoveCommand(selected,MoveAxis.Horizontal,0); return true;
            }
            return false;
        }
        /// <summary>Eksen bir kez kilitlenir. Kazanma önizlemesi kapıya ulaştığı karede commit edilir.</summary>
        public void DragPointer(Vector2 screen)
        {
            if(selected==null||Session.Won||!Ground(screen,out var world)) return;
            Vector3 delta=layout.transform.InverseTransformPoint(world)-dragStart;
            if(!axis.HasValue)
            {
                if(Mathf.Max(Mathf.Abs(delta.x),Mathf.Abs(delta.z))<.15f) return;
                axis=Mathf.Abs(delta.x)>=Mathf.Abs(delta.z)?MoveAxis.Horizontal:MoveAxis.Vertical;
            }
            previewCommand=new MoveCommand(selected,axis.Value,Mathf.RoundToInt(axis==MoveAxis.Horizontal?delta.x:delta.z));
            var preview=Session.Preview(previewCommand);
            Render(preview.State);
            if(preview.Won) { Session.Commit(previewCommand); selected=null; Render(Session.State); }
        }
        /// <summary>Son konum commit edilir; başlangıca dönüş NO_OP olarak Core'da kalır.</summary>
        public void EndPointer(Vector2 screen)
        {
            if(selected==null) return;
            DragPointer(screen);
            if(selected==null) return;
            Session.Commit(previewCommand); selected=null; axis=null; Render(Session.State);
        }
        private void Render(BoardState state)
        {
            foreach(var piece in Session.Level.Pieces) views[piece.Id].position=layout.PieceToWorld(piece,MovementEngine.Position(state,piece.Id));
            movesLabel.text="Bölüm 1    Hamle: "+Session.MoveCount;
            winLabel.text=Session.Won?"Bölüm tamamlandı!":"Yıldızı turkuaz hedefe ulaştır";
        }
    }
}
