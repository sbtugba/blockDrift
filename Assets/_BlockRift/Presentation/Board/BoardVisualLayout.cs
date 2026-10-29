using System;
using BlockRift.Core;
using UnityEngine;

namespace BlockRift.Presentation
{
    /// <summary>BoardRoot üzerindeki görsel yerleşim adaptörüdür; nesne üretmez, Core'a geri yazmaz.</summary>
    [DisallowMultipleComponent]
    public sealed class BoardVisualLayout : MonoBehaviour
    {
        [SerializeField] private int boardSize = 6;
        [SerializeField] private float surfaceHeight;
        [SerializeField] private Transform boardVisualsRoot;
        [SerializeField] private Transform piecesRoot;
        [SerializeField] private GameplayViewportLayout viewportLayout;
        [SerializeField] private VisualPrefabCatalog visualCatalog;
        [SerializeField] private GameObject activeBoardPrefab;
        public VisualPrefabCatalog VisualCatalog => visualCatalog;

        public BoardGridLayout Grid => new BoardGridLayout(boardSize, surfaceHeight);
        public Bounds PresentationBounds => Grid.PresentationBounds;

        /// <summary>Geçersiz Inspector girdisinde kameranın hata döngüsüne girmeden önceki kadrajı kullanmasını sağlar.</summary>
        public bool TryGetPresentationBounds(out Bounds bounds)
        {
            bounds = default;
            if ((boardSize != 5 && boardSize != 6) || float.IsNaN(surfaceHeight) || float.IsInfinity(surfaceHeight)) return false;
            bounds = PresentationBounds;
            return true;
        }

        /// <summary>Yalnız sunum boyutunu seçer; bölüm verisini veya oyun durumunu değiştirmez.</summary>
        public void SetBoardSize(int size)
        {
            _ = new BoardGridLayout(size, surfaceHeight);
            boardSize = size;
            RefreshBoardView();
            if (viewportLayout) viewportLayout.RefreshLayout();
        }

        /// <summary>Gerçek 5×5/6×6 hücre ve kum prefabını seçer; grid veya parça geometrisini ölçeklemez.</summary>
        private void RefreshBoardView()
        {
            if (!boardVisualsRoot || !visualCatalog || !visualCatalog.TryGetBoard(boardSize, out var entry)) return;
            if (activeBoardPrefab == entry.Prefab && boardVisualsRoot.childCount == 1) return;
            for (int i = boardVisualsRoot.childCount - 1; i >= 0; i--)
            {
                var old = boardVisualsRoot.GetChild(i).gameObject;
                old.SetActive(false);
                // Eski runtime görseli aynı karede input/render kapsamından çıkar; asset dosyası silinmez.
                if (Application.isPlaying) Destroy(old); else DestroyImmediate(old);
            }
            var view = Instantiate(entry.Prefab, boardVisualsRoot);
            view.name = entry.Prefab.name;
            view.transform.localPosition = new Vector3(0, surfaceHeight, 0);
            activeBoardPrefab = entry.Prefab;
        }

        /// <summary>Hücre merkezini birim ölçekli BoardRoot üzerinden dünya koordinatına dönüştürür.</summary>
        public Vector3 CellToWorld(Cell cell)
        {
            RequireValidSetup();
            return transform.TransformPoint(Grid.CellCenter(cell));
        }

        /// <summary>Core parça tanımını ve verilen hücreyi yalnız okuyarak taban merkezi hesaplar.</summary>
        public Vector3 PieceToWorld(PieceDefinition definition, Cell origin)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            RequireValidSetup();
            return transform.TransformPoint(Grid.PieceBaseCenter(origin, definition.Width, definition.Height));
        }

        /// <summary>Hazır görseli PiecesRoot'a yerleştirir; üretim, animasyon ve state sahipliği çağırandadır.</summary>
        public void PlacePieceView(Transform view, PieceDefinition definition, Cell origin)
        {
            Vector3 position = PieceToWorld(definition, origin);
            RequireNeutralView(view);
            view.SetParent(piecesRoot, false);
            view.position = position;
        }

        /// <summary>Yüzey merkezi pivotlu hazır tahtayı mevcut BoardVisualsRoot'a yerleştirir.</summary>
        public void PlaceBoardView(Transform view)
        {
            RequireValidSetup(); RequireNeutralView(view);
            view.SetParent(boardVisualsRoot, false);
            view.localPosition = new Vector3(0, surfaceHeight, 0);
        }

        /// <summary>Referansları, merkez pivotlarını ve 1 Blender birimi = 1 Unity birimi sözleşmesini denetler.</summary>
        public bool ValidateSetup(out string issue)
        {
            if (boardSize != 5 && boardSize != 6) { issue = "Tahta boyutu 5 veya 6 olmalı."; return false; }
            if (float.IsNaN(surfaceHeight) || float.IsInfinity(surfaceHeight)) { issue = "Yüzey yüksekliği sonlu olmalı."; return false; }
            for (Transform current = transform; current; current = current.parent)
                if ((current.localScale - Vector3.one).sqrMagnitude > 0.00000001f)
                { issue = "BoardRoot ve tüm üst köklerin ölçeği (1,1,1) olmalı."; return false; }
            if (Vector3.Dot(transform.up, Vector3.up) < 0.99999f)
            { issue = "BoardRoot XZ düzleminde olmalı; yalnız Y ekseninde sahne yönelimi desteklenir."; return false; }
            if (!NeutralChild(boardVisualsRoot) || !NeutralChild(piecesRoot) || boardVisualsRoot == piecesRoot)
            { issue = "BoardVisualsRoot/PiecesRoot ayrı doğrudan çocuklar; konum/dönüş sıfır, ölçek bir olmalı."; return false; }
            if (!viewportLayout || !viewportLayout.UsesBoardLayout(this))
            { issue = "Mevcut kamera kadrajı aynı BoardRoot ve BoardVisualLayout'a bağlı olmalı."; return false; }
            issue = null; return true;
        }

        private bool NeutralChild(Transform child) => child && child.parent == transform &&
            child.localPosition.sqrMagnitude < 0.00000001f && Quaternion.Angle(child.localRotation, Quaternion.identity) < 0.001f &&
            (child.localScale - Vector3.one).sqrMagnitude < 0.00000001f;

        private void RequireValidSetup()
        {
            if (!ValidateSetup(out string issue)) throw new InvalidOperationException(issue);
        }

        private void RequireNeutralView(Transform view)
        {
            if (!view) throw new ArgumentNullException(nameof(view));
            // Sahne altyapısı yanlışlıkla kendisinin altına taşınamaz; hatada hiçbir transform değiştirilmez.
            if (view == transform || transform.IsChildOf(view) || boardVisualsRoot.IsChildOf(view) ||
                piecesRoot.IsChildOf(view) || view.IsChildOf(viewportLayout.transform))
                throw new ArgumentException("Model wrapper yerine sahne altyapısı verilemez.", nameof(view));
            if ((view.localScale - Vector3.one).sqrMagnitude > 0.00000001f ||
                Quaternion.Angle(view.localRotation, Quaternion.identity) > 0.001f)
                throw new ArgumentException("Görsel wrapper ölçeği bir, dönüşü sıfır olmalı; import düzeltmesi model çocuğunda yapılmalı.", nameof(view));
        }

        private void OnDrawGizmosSelected()
        {
            if (!TryGetPresentationBounds(out _)) return;
            Matrix4x4 old = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.35f, 0.8f, 1, 0.7f);
            float half = boardSize * 0.5f;
            for (int i = 0; i <= boardSize; i++)
            {
                float edge = i - half;
                Gizmos.DrawLine(new Vector3(edge, surfaceHeight, -half), new Vector3(edge, surfaceHeight, half));
                Gizmos.DrawLine(new Vector3(-half, surfaceHeight, edge), new Vector3(half, surfaceHeight, edge));
            }
            Gizmos.color = Color.red; Gizmos.DrawLine(Vector3.zero, Vector3.right);
            Gizmos.color = Color.green; Gizmos.DrawLine(Vector3.zero, Vector3.up);
            Gizmos.color = Color.blue; Gizmos.DrawLine(Vector3.zero, Vector3.forward);
            Gizmos.matrix = old;
        }
    }
}
