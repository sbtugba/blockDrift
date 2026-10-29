using System;
using System.IO;
using BlockRift.App;
using BlockRift.Presentation;
using UnityEngine.SceneManagement;

namespace BlockRift.Tests
{
    /// <summary>Scene.Start'tan önce test kaydını bağlar; gerçek oyuncu kaydına hiçbir test erişmez.</summary>
    internal sealed class ProgressionSceneIsolation : IDisposable
    {
        public string Folder
        {
            get;
        }
        public string SavePath => Path.Combine(Folder, "progress.json");
        public ProgressionSceneIsolation()
        {
            Folder = Path.Combine(Path.GetTempPath(), "BlockRiftPlayTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Folder);
            SceneManager.sceneLoaded += Configure;
        }
        private void Configure(Scene scene, LoadSceneMode mode)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var game in root.GetComponentsInChildren<LevelOneGameplay>(true))
                    game.ConfigureProgression(ResourceLevelLoader.LoadPublishedManifest(), new JsonProgressStore(SavePath));
        }
        public void Dispose()
        {
            SceneManager.sceneLoaded -= Configure;
            if (!Path.GetFullPath(Folder).StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase))
                throw new IOException("Unsafe test directory");
            if (Directory.Exists(Folder))
                Directory.Delete(Folder, true);
        }
    }
}
