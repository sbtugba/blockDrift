using System;
using BlockRift.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlockRift.Editor
{
    /// <summary>Batch doğrulamada mevcut sahneye yalnız boş görsel adaptör referanslarını ekler.</summary>
    public static class GameplayVisualIntegrationSetup
    {
        /// <summary>Mevcut Gameplay sahnesini koruyarak BoardRoot/PiecesRoot adaptörünü bağlar; asset üretmez.</summary>
        public static void ConfigureExistingScene()
        {
            if (!Application.isBatchMode)
                throw new InvalidOperationException("Bu bakım girişi yalnız izole batch doğrulama içindir.");
            var scene = EditorSceneManager.OpenScene(GameplaySceneSetup.ScenePath, OpenSceneMode.Single);
            var root = scene.GetRootGameObjects()[0].transform;
            ConfigureRoots(root);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Block Rift: existing gameplay visual adapter configured; no model imported.");
        }

        // Yeni sahne üreticisi de aynı bağlantı kodunu kullanır; koordinat veya kamera sistemi kopyalanmaz.
        internal static void ConfigureRoots(Transform root)
        {
            var board = root.Find("BoardRoot");
            var framing = root.GetComponentInChildren<GameplayViewportLayout>();
            if (!board || !framing)
                throw new InvalidOperationException("Mevcut Gameplay kökleri eksik.");
            var layout = board.GetComponent<BoardVisualLayout>();
            if (!layout)
                layout = board.gameObject.AddComponent<BoardVisualLayout>();
            var data = new SerializedObject(layout);
            data.FindProperty("boardVisualsRoot").objectReferenceValue = board.Find("BoardVisualsRoot");
            data.FindProperty("piecesRoot").objectReferenceValue = board.Find("PiecesRoot");
            data.FindProperty("viewportLayout").objectReferenceValue = framing;
            data.ApplyModifiedPropertiesWithoutUndo();
            var cameraData = new SerializedObject(framing);
            cameraData.FindProperty("visualLayout").objectReferenceValue = layout;
            cameraData.ApplyModifiedPropertiesWithoutUndo();
            if (!layout.ValidateSetup(out string issue))
                throw new InvalidOperationException(issue);
            framing.ApplyLayout(1080, 1920, new Rect(0, 0, 1080, 1920));
        }
    }
}
