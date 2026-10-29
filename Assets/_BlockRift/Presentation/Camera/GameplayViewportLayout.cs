using UnityEngine;

namespace BlockRift.Presentation
{
    /// <summary>Safe area içinde sabit orthographic veya perspective tahta kadrajını uyarlar; Core'a dokunmaz.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class GameplayViewportLayout : MonoBehaviour
    {
        [SerializeField] private Camera boardCamera;
        [SerializeField] private Transform boardRoot;
        [SerializeField] private RectTransform safeAreaRoot;
        [SerializeField] private RectTransform boardViewport;
        [SerializeField] private BoardVisualLayout visualLayout;
        [SerializeField] private Transform focusRoot;
        // En büyük tahta + çerçeve + yükseklik zarfıdır; level veya Core state değildir.
        [SerializeField] private Bounds localPresentationBounds = new Bounds(new Vector3(0, 0.25f, 0), new Vector3(6.5f, 0.9f, 6.5f));
        [SerializeField, Min(1f)] private float framingPadding = 1.08f;
        [SerializeField] private bool usePerspective;
        [SerializeField, Range(15f, 40f)] private float verticalFieldOfView = 22f;
        private Transform cachedFocusRoot;
        private Renderer[] focusRenderers;
        private int lastWidth, lastHeight;
        private Rect lastSafeArea;

        private void OnEnable() => RefreshLayout();

        private void LateUpdate()
        {
            // Oyun durumu okunmaz. Yalnız ekran/safe-area değişirse yeniden kadrajlanır.
            if (!Application.isPlaying || Screen.width != lastWidth || Screen.height != lastHeight || Screen.safeArea != lastSafeArea)
                RefreshLayout();
        }

        /// <summary>Ekranın güncel güvenli alanını kullanarak UI ve kamera sunumunu yeniler.</summary>
        public void RefreshLayout() => ApplyLayout(Screen.width, Screen.height, Screen.safeArea);

        /// <summary>Tahta görselinin dünya bounds merkezidir; parçalar kamera hedefini kaydırmaz.</summary>
        public Vector3 FocusPoint
        {
            get
            {
                // Kum oynanış alanından geniştir; kamera hedefini onun asimetrik bounds'u kaydırmamalı.
                if (visualLayout && visualLayout.TryGetPresentationBounds(out _))
                    return visualLayout.transform.TransformPoint(new Vector3(0, visualLayout.Grid.SurfaceHeight, 0));
                if (cachedFocusRoot != focusRoot)
                {
                    cachedFocusRoot = focusRoot;
                    focusRenderers = focusRoot ? focusRoot.GetComponentsInChildren<Renderer>(true) : null;
                }
                bool found = false;
                Bounds combined = default;
                if (focusRenderers != null)
                    foreach (var renderer in focusRenderers)
                    {
                        if (!renderer)
                            continue;
                        if (!found)
                        {
                            combined = renderer.bounds;
                            found = true;
                        }
                        else
                            combined.Encapsulate(renderer.bounds);
                    }
                if (found)
                    return combined.center;
                return boardRoot ? boardRoot.position : transform.position;
            }
        }

        /// <summary>Görsel adaptörün aynı tahta referansını kullandığını doğrulamak için bağlantıyı bildirir.</summary>
        public bool UsesBoardLayout(BoardVisualLayout layout) => visualLayout == layout && layout && boardRoot == layout.transform &&
            boardCamera && boardCamera.orthographic != usePerspective && safeAreaRoot && boardViewport && boardViewport.parent == safeAreaRoot;

        /// <summary>Piksel cinsinden ekran/güvenli alanla düzenler; editör doğrulaması da aynı yolu kullanır.</summary>
        public void ApplyLayout(int width, int height, Rect safeArea)
        {
            if (!boardCamera || !boardRoot || !safeAreaRoot || !boardViewport || width <= 0 || height <= 0)
                return;
            safeArea = Rect.MinMaxRect(Mathf.Clamp(safeArea.xMin, 0, width), Mathf.Clamp(safeArea.yMin, 0, height),
                Mathf.Clamp(safeArea.xMax, 0, width), Mathf.Clamp(safeArea.yMax, 0, height));
            if (safeArea.width <= 0 || safeArea.height <= 0)
                return;
            lastWidth = width;
            lastHeight = height;
            lastSafeArea = safeArea;
            safeAreaRoot.anchorMin = new Vector2(safeArea.xMin / width, safeArea.yMin / height);
            safeAreaRoot.anchorMax = new Vector2(safeArea.xMax / width, safeArea.yMax / height);
            safeAreaRoot.offsetMin = safeAreaRoot.offsetMax = Vector2.zero;

            // BoardViewport doğrudan SafeArea çocuğudur; ofsetsiz anchor alanını kameraya eşleriz.
            Vector2 min = safeAreaRoot.anchorMin + Vector2.Scale(boardViewport.anchorMin, safeAreaRoot.anchorMax - safeAreaRoot.anchorMin);
            Vector2 max = safeAreaRoot.anchorMin + Vector2.Scale(boardViewport.anchorMax, safeAreaRoot.anchorMax - safeAreaRoot.anchorMin);
            if (max.x <= min.x || max.y <= min.y)
                return;
            boardCamera.orthographic = !usePerspective;
            boardCamera.ResetProjectionMatrix();
            boardCamera.rect = new Rect(0, 0, 1, 1);
            boardCamera.aspect = (float)width / height;
            Vector3 right = boardCamera.transform.right, up = boardCamera.transform.up;
            Bounds bounds = visualLayout && visualLayout.TryGetPresentationBounds(out Bounds provided) ? provided : localPresentationBounds;
            Vector3 center = boardRoot.TransformPoint(bounds.center);
            if (usePerspective)
            {
                FitPerspective(bounds, center, min, max);
                return;
            }
            float halfWidth = 0, halfHeight = 0;
            for (int i = 0; i < 8; i++)
            {
                Vector3 offset = Vector3.Scale(bounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 worldOffset = boardRoot.TransformVector(offset);
                halfWidth = Mathf.Max(halfWidth, Mathf.Abs(Vector3.Dot(worldOffset, right)));
                halfHeight = Mathf.Max(halfHeight, Mathf.Abs(Vector3.Dot(worldOffset, up)));
            }
            float size = Mathf.Max(halfWidth / (boardCamera.aspect * (max.x - min.x)), halfHeight / (max.y - min.y)) * Mathf.Max(1, framingPadding);
            boardCamera.orthographicSize = Mathf.Max(0.01f, size);
            Vector2 viewportCenter = (min + max) * 0.5f;
            // Kamerayı kaydırarak tahta merkezini UI için ayrılmış alanın merkezinde tutarız.
            boardCamera.transform.position = center - boardCamera.transform.forward * 20f
                - right * ((viewportCenter.x - 0.5f) * 2f * size * boardCamera.aspect)
                - up * ((viewportCenter.y - 0.5f) * 2f * size);
        }

        private void FitPerspective(Bounds bounds, Vector3 center, Vector2 min, Vector2 max)
        {
            boardCamera.fieldOfView = float.IsNaN(verticalFieldOfView) ? 22f : Mathf.Clamp(verticalFieldOfView, 15f, 40f);
            float tanY = Mathf.Tan(boardCamera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            float tanX = tanY * boardCamera.aspect;
            Vector2 desired = min + (max - min) * 0.5f;
            float cx = desired.x * 2 - 1, cy = desired.y * 2 - 1;
            float left = min.x * 2 - 1, right = max.x * 2 - 1;
            float bottom = min.y * 2 - 1, top = max.y * 2 - 1;
            Transform cameraTransform = boardCamera.transform;
            Vector3 target = FocusPoint;
            // Kamera daima pozitif Y tarafından bakar; hedef için parça/tahta Transform'u değiştirilmez.
            if (cameraTransform.forward.y >= -0.001f)
                cameraTransform.rotation = Quaternion.Euler(78, 0, 0);
            float distance = boardCamera.nearClipPlane;
            // Her köşenin perspective bölmesindeki kendi derinliği kullanılır. Tek bir orthographic zarf yetmez.
            for (int i = 0; i < 8; i++)
            {
                Vector3 offset = center + boardRoot.TransformVector(Vector3.Scale(bounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))) - target;
                float x = Vector3.Dot(offset, cameraTransform.right);
                float y = Vector3.Dot(offset, cameraTransform.up);
                float z = Vector3.Dot(offset, cameraTransform.forward);
                distance = Mathf.Max(distance, -x / ((cx - left) * tanX) - z);
                distance = Mathf.Max(distance, x / ((right - cx) * tanX) - z);
                distance = Mathf.Max(distance, -y / ((cy - bottom) * tanY) - z);
                distance = Mathf.Max(distance, y / ((top - cy) * tanY) - z);
                distance = Mathf.Max(distance, boardCamera.nearClipPlane + 0.1f - z);
            }
            distance *= Mathf.Max(1, framingPadding);
            cameraTransform.position = target - cameraTransform.forward * distance;
            cameraTransform.LookAt(target, Vector3.up);
            // Off-axis görüntü penceresi, hedefe bakan kamera eksenini bozmadan UI için kadrajı taşır.
            Matrix4x4 projection = Matrix4x4.Perspective(boardCamera.fieldOfView, boardCamera.aspect,
                boardCamera.nearClipPlane, boardCamera.farClipPlane);
            projection.m02 = -cx;
            projection.m12 = -cy;
            boardCamera.projectionMatrix = projection;
        }

        private void OnDrawGizmosSelected()
        {
            if (!boardRoot)
                return;
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.7f);
            Gizmos.matrix = boardRoot.localToWorldMatrix;
            Bounds bounds = visualLayout && visualLayout.TryGetPresentationBounds(out Bounds provided) ? provided : localPresentationBounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}
