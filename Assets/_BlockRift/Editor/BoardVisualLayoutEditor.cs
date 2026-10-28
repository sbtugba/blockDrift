using BlockRift.Presentation;
using UnityEditor;
using UnityEngine;

namespace BlockRift.Editor
{
    /// <summary>Görsel adaptörün referanslarını ve ileride seçilecek model wrapper'ının pivotunu incelemeyi sağlar.</summary>
    [CustomEditor(typeof(BoardVisualLayout))]
    public sealed class BoardVisualLayoutEditor : UnityEditor.Editor
    {
        private Transform modelWrapper;
        private bool checkBoard;
        private int pieceWidth = 1, pieceHeight = 1;
        /// <summary>Eksik model denetimini tamamlanmış gibi göstermeden kurulum durumunu sunar.</summary>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var layout = (BoardVisualLayout)target;
            bool valid = layout.ValidateSetup(out string issue);
            EditorGUILayout.HelpBox(valid ? "Yerleşim referansları hazır. 1 hücre = 1 Unity birimi. Model/pivot doğrulaması: INCOMPLETE (assetler henüz bağlanmadı)." : issue,
                valid ? MessageType.Info : MessageType.Error);
            EditorGUILayout.HelpBox("Scene/Gizmos açıkken BoardRoot'u seçin: çizgiler görsel grid'i gösterir; Game görünümüne mesh eklenmez.", MessageType.Info);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Import sonrası model/pivot kontrolü", EditorStyles.boldLabel);
            modelWrapper = (Transform)EditorGUILayout.ObjectField("Model wrapper", modelWrapper, typeof(Transform), true);
            checkBoard = EditorGUILayout.Toggle("Tahta modeli", checkBoard);
            if (!checkBoard)
            {
                pieceWidth = EditorGUILayout.IntField("Parça genişliği", pieceWidth);
                pieceHeight = EditorGUILayout.IntField("Parça yüksekliği", pieceHeight);
            }
            EditorGUI.BeginDisabledGroup(!valid);
            if (GUILayout.Button("Model ölçüsü ve pivotunu doğrula"))
            {
                if (!modelWrapper || !TryMeasureBounds(modelWrapper, out Bounds bounds))
                    Debug.LogWarning("INCOMPLETE: kontrol için gerçek statik mesh içeren model wrapper gerekli.");
                else if ((modelWrapper.localScale - Vector3.one).sqrMagnitude > 0.00000001f ||
                    Quaternion.Angle(modelWrapper.localRotation, Quaternion.identity) > 0.001f)
                    Debug.LogError("Wrapper dönüşü sıfır ve ölçeği bir olmalı; import eksen düzeltmesini model çocuğunda yapın.", modelWrapper);
                else
                {
                    string modelIssue;
                    bool passed = checkBoard
                        ? VisualModelChecks.ValidateBoardBounds(bounds, layout.Grid.Size, out modelIssue)
                        : VisualModelChecks.ValidatePieceBounds(bounds, pieceWidth, pieceHeight, out modelIssue);
                    if (passed) Debug.Log("Model bounds/pivot kontrolü PASS. Gerçek panel merkezleri, materyal ve hücre fit kontrolü ayrıca yapılmalı.", modelWrapper);
                    else Debug.LogError(modelIssue, modelWrapper);
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        /// <summary>Seçili model wrapper'ının altında gerçek mesh varsa onun yerel sınırlarını raporlar.</summary>
        [MenuItem("Block Rift/Visuals/Inspect Selected Model Bounds")]
        public static void InspectSelectedModelBounds()
        {
            Transform wrapper = Selection.activeTransform;
            if (!wrapper) { Debug.LogWarning("Önce gelecekte import edilecek model wrapper'ını seçin. INCOMPLETE."); return; }
            if (!TryMeasureBounds(wrapper, out Bounds bounds))
            { Debug.LogWarning("INCOMPLETE: seçili wrapper altında statik model mesh'i yok.", wrapper); return; }
            Debug.Log($"Wrapper yerel bounds: center={bounds.center:F4}, size={bounds.size:F4}, min={bounds.min:F4}, max={bounds.max:F4}. " +
                "Ölçüler wrapper ölçeğinden bağımsızdır; birim ölçek ayrıca doğrulanmalıdır. Bu ölçüm tek başına pivot/asset kabulü değildir.", wrapper);
        }

        private static bool TryMeasureBounds(Transform wrapper, out Bounds bounds)
        {
            bool found = false;
            bounds = default;
            foreach (var mesh in wrapper.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!mesh.sharedMesh) continue;
                Matrix4x4 matrix = wrapper.worldToLocalMatrix * mesh.transform.localToWorldMatrix;
                Bounds source = mesh.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = source.center + Vector3.Scale(source.extents,
                        new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 point = matrix.MultiplyPoint3x4(corner);
                    if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; }
                    else bounds.Encapsulate(point);
                }
            }
            return found;
        }
    }
}
