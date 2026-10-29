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
    /// <summary>JSON kimliğiyle herhangi bir bölümü oynatır; eski ad sahne/GUID bağlarını korumak için geçicidir.</summary>
    [DisallowMultipleComponent]
    public sealed class LevelOneGameplay : MonoBehaviour
    {
        [SerializeField] private string levelId;
        [SerializeField] private BoardVisualLayout layout;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private Text movesLabel, winLabel;
        [SerializeField] private Button restartButton;
        private readonly Dictionary<string, Transform> views = new Dictionary<string, Transform>();
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        private string selected;
        private MoveAxis? axis;
        private Vector3 dragStart;
        private MoveCommand previewCommand;
        private int pointerId = -1;
        private ILevelProvider levelProvider;
        private LevelVisualData visualData;
        private IProgressStore progressStore;
        public LevelProgression Progression
        {
            get; private set;
        }
        public string ProgressError
        {
            get; private set;
        }
        public string ProgressNotice
        {
            get; private set;
        }

        /// <summary>Test veya composition için manifest ve kayıt portunu dışarıdan bağlar; sahne oluşturmaz.</summary>
        public void ConfigureProgression(string manifest, IProgressStore store)
        {
            var next = new LevelProgression(manifest, (store ?? throw new System.ArgumentNullException(nameof(store))).Load());
            Progression = next;
            progressStore = store;
            ProgressNotice = next.ReconciliationNotice ?? (store as JsonProgressStore)?.RecoveryNotice;
        }

        private void EnsureProgression()
        {
            if (Progression != null)
                return;
            string manifest = ResourceLevelLoader.LoadPublishedManifest();
            // Player'da manifest yalnız gerçekten paketlenmiş, doğru kimlikli JSON'ları yayınlayabilir.
            var catalog = new LevelProgression(manifest);
            foreach (string id in catalog.Levels)
                if (ResourceLevelLoader.LoadLevel(id).Id != id)
                    throw new System.InvalidOperationException("Manifest/Resources ID uyuşmazlığı: " + id);
            ConfigureProgression(manifest, new JsonProgressStore(System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "BlockRift", "progress.json")));
        }

        public GameSession Session
        {
            get; private set;
        }
        public IReadOnlyDictionary<string, Transform> Views => views;
        public BoardVisualLayout Layout => layout;
        public Camera GameplayCamera => gameplayCamera;
        public string LevelId => levelId;

        /// <summary>Test veya özel sağlayıcı enjeksiyonu için kullanılır; atanmazsa varsayılan ResourceLevelLoader kullanılır.</summary>
        public void SetLevelProvider(ILevelProvider provider)
        {
            levelProvider = provider;
        }

        private void Start()
        {
            InitializeLevel();
        }

        /// <summary>Inspector'da seçilen bölümü ortak yükleme yolundan başlatır.</summary>
        // Inspector ID legacy sahne bilgisidir; kayıt erişilebilirse kayıt, aksi halde manifestin ilk bölümü seçilir.
        public void InitializeLevel()
        {
            EnsureProgression();
            LoadLevel(Progression.State.Selected);
        }

        /// <summary>İstenen JSON'u doğrular; yeni oturumu ve mevcut sunumu aynı controller üzerinde yeniler.</summary>
        public void LoadLevel(string requestedLevelId)
        {
            if (string.IsNullOrWhiteSpace(requestedLevelId))
                throw new System.ArgumentException("Bölüm ID Inspector üzerinden veya LoadLevel ile belirtilmeli.", nameof(requestedLevelId));
            EnsureProgression();
            var selection = Progression.Selection(requestedLevelId);
            levelProvider ??= new ResourceLevelLoader();
            var level = levelProvider.GetLevel(requestedLevelId);
            if (level == null)
                throw new System.InvalidOperationException($"Bölüm verisi yüklenemedi: {requestedLevelId}.");
            var validation = BoardValidator.Validate(level);
            if (!validation.IsValid)
                throw new LevelValidationException("Bölüm verisi geçersiz: " + requestedLevelId, validation);
            if (level.Id != requestedLevelId)
                throw new System.InvalidOperationException("İstenen bölüm ID ile sağlayıcının döndürdüğü ID uyuşmuyor: " + requestedLevelId);
            // Veri veya mevcut görsel eşleme hatalıysa çalışan oturumun yerini alma.
            LevelVisualData nextVisuals = null;
            if (layout)
            {
                if (!(levelProvider is ILevelVisualProvider visuals))
                    throw new System.InvalidOperationException("Sağlayıcı bölümün JSON görsel verisini de sağlamalı (ILevelVisualProvider).");
                nextVisuals = visuals.GetVisualData(requestedLevelId);
                if (nextVisuals == null || LevelJsonSerializer.Serialize(nextVisuals.Definition) != LevelJsonSerializer.Serialize(level))
                    throw new System.InvalidOperationException("Görsel veri ile bölüm tanımı uyuşmuyor.");
            }
            ValidateVisuals(level, nextVisuals);
            var nextSession = new GameSession(level);
            // Kayıt hatasında önceki aktif oturum/seçim değişmeden hata çağırana yayılır.
            progressStore.Save(selection);
            if (Session != null)
                Session.WonCommitted -= OnSessionWon;
            ResetPointer();
            levelId = requestedLevelId;
            Session = nextSession;
            Progression.Activate(Session, selection);
            Session.WonCommitted += OnSessionWon;
            ProgressError = null;
            visualData = nextVisuals;
            if (layout)
            {
                layout.SetBoardSize(level.Width);
                SpawnVisuals();
            }
            else
                Render(Session.State);
        }

        private void ValidateVisuals(LevelDefinition level, LevelVisualData data)
        {
            // Saf oturum testlerinde sunum bağları bulunmayabilir; gerçek sahnede mevcut adaptör kullanılır.
            if (!layout)
                return;
            if (!layout.ValidateSetup(out string issue))
                throw new System.InvalidOperationException(issue);
            if (!layout.VisualCatalog || !layout.VisualCatalog.ExitPrefab || !layout.transform.Find("MarkersRoot"))
                throw new System.InvalidOperationException("Gameplay sunum referansları eksik.");
            foreach (var piece in level.Pieces)
            {
                layout.VisualCatalog.ResolvePiece(piece);
                layout.VisualCatalog.ResolveMaterial(piece, data);
            }
            if (level.FixedCells.Count > 0)
                layout.VisualCatalog.ResolveFixedStone();
        }

        /// <summary>Mevcut palette/katalog eşlemelerini kullanır; yeniden çağrıda eski görselleri çoğaltmaz.</summary>
        public void SpawnVisuals()
        {
            if (!layout)
                return;
            ValidateVisuals(Session.Level, visualData);
            var piecesRoot = layout.transform.Find("PiecesRoot");
            // Sanat denetim dizilimi editörde korunur; runtime authoritative bölümün yerini tutmaz.
            foreach (Transform child in piecesRoot)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            views.Clear();
            foreach (var piece in Session.Level.Pieces)
            {
                var entry = layout.VisualCatalog.ResolvePiece(piece);
                var material = layout.VisualCatalog.ResolveMaterial(piece, visualData);
                var view = Instantiate(entry.Prefab);
                view.name = piece.Id;
                if (material)
                    foreach (var renderer in view.GetComponentsInChildren<MeshRenderer>())
                    {
                        var mats = renderer.sharedMaterials;
                        for (int i = 0; i < mats.Length; i++)
                            mats[i] = material;
                        renderer.sharedMaterials = mats;
                    }
                layout.PlacePieceView(view.transform, piece, piece.Start);
                views.Add(piece.Id, view.transform);
            }
            foreach (var cell in Session.Level.FixedCells)
            {
                var stone = layout.VisualCatalog.ResolveFixedStone();
                var view = Instantiate(stone.Prefab, piecesRoot);
                view.name = "Fixed_" + cell;
                view.transform.position = layout.CellToWorld(cell);
            }
            var markers = layout.transform.Find("MarkersRoot");
            var exit = markers.Find("ExitVisual");
            if (!exit)
            {
                exit = Instantiate(layout.VisualCatalog.ExitPrefab, markers).transform;
                exit.name = "ExitVisual";
            }
            exit.position = layout.CellToWorld(Session.Level.Exit) + Vector3.up * 0.004f;
            if (restartButton)
            {
                restartButton.onClick.RemoveListener(Restart);
                restartButton.onClick.AddListener(Restart);
            }
            Render(Session.State);
        }
        private void OnDestroy()
        {
            if (Session != null)
                Session.WonCommitted -= OnSessionWon;
            if (restartButton)
                restartButton.onClick.RemoveListener(Restart);
        }

        private void OnSessionWon(GameSession session)
        {
            if (!ReferenceEquals(session, Session))
                return;
            var completed = Progression.Completion(session);
            if (completed == null)
                return;
            try
            {
                progressStore.Save(completed);
                Progression.AcceptCompletion(session, completed);
                ProgressError = null;
            }
            catch (System.Exception error) { ProgressError = "İlerleme kaydedilemedi: " + error.Message; Render(Session.State); throw; }
        }

        /// <summary>Başarısız kayıt sonrası kazanma kanıtını yeniden yazmayı dener; çift tamamlama oluşturmaz.</summary>
        public void PersistCompletion()
        {
            if (Session != null)
                OnSessionWon(Session);
        }

        /// <summary>Son bölümde false döner; yalnız açık sonraki bölüm ortak LoadLevel yolundan açılır.</summary>
        public bool LoadNextLevel()
        {
            EnsureProgression();
            string next = Progression.NextLevel();
            if (next == null)
                return false;
            LoadLevel(next);
            return true;
        }
        private void OnDisable()
        {
            ResetPointer();
            if (Session != null)
                Render(Session.State);
        }
        /// <summary>Restart sürükleme durumunu da iptal eder, oturumu başlangıca döndürür.</summary>
        public void Restart()
        {
            ResetPointer();
            Session?.Restart();
            if (Session != null)
                Render(Session.State);
        }

        // Yükleme, Restart ve devre dışı kalma aynı yarım sürüklemeyi tamamen iptal eder.
        private void ResetPointer()
        {
            selected = null;
            axis = null;
            pointerId = -1;
            previewCommand = default;
        }

        private void Update()
        {
            if (Session == null || Session.Won)
                return;
            var touch = Touchscreen.current;
            if (touch != null && (touch.primaryTouch.press.isPressed || touch.primaryTouch.press.wasReleasedThisFrame))
            {
                var t = touch.primaryTouch;
                if (t.press.wasPressedThisFrame)
                {
                    pointerId = t.touchId.ReadValue();
                    BeginPointer(t.position.ReadValue());
                }
                if (pointerId == t.touchId.ReadValue() && t.press.isPressed)
                    DragPointer(t.position.ReadValue());
                if (t.press.wasReleasedThisFrame)
                {
                    EndPointer(t.position.ReadValue());
                    pointerId = -1;
                }
            }
            else if (Mouse.current != null)
            {
                var mouse = Mouse.current;
                if (mouse.leftButton.wasPressedThisFrame)
                    BeginPointer(mouse.position.ReadValue());
                if (mouse.leftButton.isPressed)
                    DragPointer(mouse.position.ReadValue());
                if (mouse.leftButton.wasReleasedThisFrame)
                    EndPointer(mouse.position.ReadValue());
            }
        }
        private bool OnUI(Vector2 screen)
        {
            if (!EventSystem.current)
                return false;
            var data = new PointerEventData(EventSystem.current) { position = screen };
            uiHits.Clear();
            EventSystem.current.RaycastAll(data, uiHits);
            return uiHits.Count > 0;
        }
        private bool Ground(Vector2 screen, out Vector3 world)
        {
            var plane = new Plane(layout.transform.up, layout.CellToWorld(new Cell(0, 0)));
            var ray = gameplayCamera.ScreenPointToRay(screen);
            if (plane.Raycast(ray, out float d))
            {
                world = ray.GetPoint(d);
                return true;
            }
            world = default;
            return false;
        }
        /// <summary>Ekran ışını grid düzlemine düşürülür; seçim yalnız Core ayakizini kullanır.</summary>
        public bool BeginPointer(Vector2 screen)
        {
            if (Session == null || Session.Won || OnUI(screen) || !Ground(screen, out var world))
                return false;
            Vector3 local = layout.transform.InverseTransformPoint(world);
            foreach (var piece in Session.Level.Pieces)
            {
                var p = layout.transform.InverseTransformPoint(layout.PieceToWorld(piece, MovementEngine.Position(Session.State, piece.Id)));
                if (Mathf.Abs(local.x - p.x) > piece.Width * .5f || Mathf.Abs(local.z - p.z) > piece.Height * .5f)
                    continue;
                selected = piece.Id;
                dragStart = local;
                axis = null;
                previewCommand = new MoveCommand(selected, MoveAxis.Horizontal, 0);
                return true;
            }
            return false;
        }
        /// <summary>Eksen bir kez kilitlenir. Kazanma önizlemesi kapıya ulaştığı karede commit edilir.</summary>
        public void DragPointer(Vector2 screen)
        {
            if (selected == null || Session.Won || !Ground(screen, out var world))
                return;
            Vector3 delta = layout.transform.InverseTransformPoint(world) - dragStart;
            if (!axis.HasValue)
            {
                if (Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.z)) < .15f)
                    return;
                var candidate = Mathf.Abs(delta.x) >= Mathf.Abs(delta.z) ? MoveAxis.Horizontal : MoveAxis.Vertical;
                // İzin yalnız Core'dan gelir; yasak ilk yön ne görseli taşır ne ekseni kilitler.
                if (!MovementEngine.AllowsManualAxis(Session.Level.Pieces.First(p => p.Id == selected), candidate))
                    return;
                axis = candidate;
            }
            previewCommand = new MoveCommand(selected, axis.Value, Mathf.RoundToInt(axis == MoveAxis.Horizontal ? delta.x : delta.z));
            var preview = Session.Preview(previewCommand);
            Render(preview.State);
            if (preview.Won)
            {
                Session.Commit(previewCommand);
                selected = null;
                Render(Session.State);
            }
        }
        /// <summary>Son konum commit edilir; başlangıca dönüş NO_OP olarak Core'da kalır.</summary>
        public void EndPointer(Vector2 screen)
        {
            if (selected == null)
                return;
            DragPointer(screen);
            if (selected == null)
                return;
            Session.Commit(previewCommand);
            selected = null;
            axis = null;
            Render(Session.State);
        }
        private void Render(BoardState state)
        {
            if (views.Count > 0 && layout)
                foreach (var piece in Session.Level.Pieces)
                    views[piece.Id].position = layout.PieceToWorld(piece, MovementEngine.Position(state, piece.Id));
            if (movesLabel)
            {
                string id = Session.Level.Id;
                string caption = int.TryParse(id.Substring(id.LastIndexOf('-') + 1), out int number) ? "Bölüm " + number : id;
                movesLabel.text = caption + "    Hamle: " + Session.MoveCount;
            }
            if (winLabel)
                winLabel.text = ProgressError ?? (Session.Won ? "Bölüm tamamlandı!" : "Yıldızı turkuaz hedefe ulaştır");
        }
    }
}
