using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BlockRift.App;
using BlockRift.Core;
using BlockRift.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
namespace BlockRift.Tests
{
    /// <summary>Dosya kaydı, yeniden açılış ve aynı sahnede geçişi Editor ve Windows player'da sınar.</summary>
    public sealed class ProgressionPlayTests
    {
        private ProgressionSceneIsolation isolation;
        [UnitySetUp]public IEnumerator Setup(){isolation=new ProgressionSceneIsolation();yield return Open();}
        [UnityTearDown]public IEnumerator Cleanup(){isolation.Dispose();yield return null;}
        private IEnumerator Open()
        {
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/BlockRift_Gameplay.unity",new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return SceneManager.LoadSceneAsync("BlockRift_Gameplay");
#endif
            yield return null;yield return null;
        }
        private LevelOneGameplay Game=>UnityEngine.Object.FindAnyObjectByType<LevelOneGameplay>();
        private sealed class Provider:ILevelProvider,ILevelVisualProvider
        {
            public LevelVisualData GetVisualData(string id)=>new LevelVisualData(new LevelDefinition(id,1,2,6,6,new Cell(5,0),new[]{new PieceDefinition("comet",PieceKind.Target,1,1,new Cell(0,0))},Array.Empty<Cell>(),Array.Empty<Cell>()),new Dictionary<string,string>());
            public LevelDefinition GetLevel(string id)=>GetVisualData(id).Definition;
        }
        private static void Solve(LevelOneGameplay game)
        {
            var proof=BoardSolver.Solve(game.Session.Level,game.Session.State);Assert.That(proof.Status,Is.EqualTo(SearchStatus.Solved));
            foreach(var move in proof.Path)game.Session.Commit(move);
            game.PersistCompletion();
        }
        [UnityTest]public IEnumerator RealLevelJsonWinRestartAndReopenPreserveProgress()
        {
            var game=Game;Assert.That(game.LevelId,Is.EqualTo("level-001"));Assert.Throws<ArgumentException>(()=>game.LoadLevel("level-002"));
            Solve(game);var store=new JsonProgressStore(isolation.SavePath);Assert.That(store.Load().Completed,Is.EqualTo(new[]{"level-001"}));
            string saved=File.ReadAllText(isolation.SavePath);game.Restart();Solve(game);Assert.That(File.ReadAllText(isolation.SavePath),Is.EqualTo(saved));Assert.That(game.LoadNextLevel(),Is.False);
            yield return Open();game=Game;Assert.That(game.Progression.State.Completed,Is.EqualTo(new[]{"level-001"}));Assert.That(game.Session.MoveCount,Is.Zero);Assert.That(game.Session.Won,Is.False);
        }
        [UnityTest]public IEnumerator FixtureTransitionUsesSameSceneAndSavesSelectedId()
        {
            var game=Game;var scene=SceneManager.GetActiveScene().handle;var root=game.Layout.transform;
            game.SetLevelProvider(new Provider());game.ConfigureProgression("{\"schemaVersion\":1,\"levels\":[\"test-z\",\"test-a\"]}",new JsonProgressStore(System.IO.Path.Combine(isolation.Folder,"fixture.json")));
            game.InitializeLevel();Assert.Throws<InvalidOperationException>(()=>game.LoadLevel("test-a"));Solve(game);Assert.That(game.LoadNextLevel(),Is.True);yield return null;
            Assert.That(game.LevelId,Is.EqualTo("test-a"));Assert.That(game.Layout.transform,Is.SameAs(root));Assert.That(SceneManager.GetActiveScene().handle,Is.EqualTo(scene));
            Assert.That(game.Views.Count,Is.EqualTo(1));Assert.That(UnityEngine.Object.FindObjectsByType<LevelOneGameplay>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            var saved=new JsonProgressStore(System.IO.Path.Combine(isolation.Folder,"fixture.json")).Load();Assert.That(saved.Selected,Is.EqualTo("test-a"));
            game.ConfigureProgression("{\"schemaVersion\":1,\"levels\":[\"test-z\",\"test-a\"]}",new JsonProgressStore(System.IO.Path.Combine(isolation.Folder,"fixture.json")));
            game.InitializeLevel();Assert.That(game.LevelId,Is.EqualTo("test-a"));Solve(game);Assert.That(game.LoadNextLevel(),Is.False);
        }
        [UnityTest]public IEnumerator CrossProcessProgressUsesRealControllerAndIsolatedFile()
        {
            // CLI iki ayrı Windows player'ı aynı test profiliyle başlatır; normal testte geçici klasör kullanılır.
            string path=Environment.GetEnvironmentVariable("BLOCKRIFT_TEST_PROGRESS_PATH")??System.IO.Path.Combine(isolation.Folder,"cross-process-progress.json");
            bool exists=File.Exists(path);var game=Game;
            game.ConfigureProgression(ResourceLevelLoader.LoadPublishedManifest(),new JsonProgressStore(path));game.InitializeLevel();
            if(exists)Assert.That(game.Progression.State.Completed,Is.EqualTo(new[]{"level-001"}));
            else Solve(game);
            Assert.That(new JsonProgressStore(path).Load().Completed,Is.EqualTo(new[]{"level-001"}));
            File.AppendAllText(path+".processes",(exists?"READ":"WRITE")+" "+System.Diagnostics.Process.GetCurrentProcess().Id+Environment.NewLine);
            Assert.That(game.LevelId,Is.EqualTo("level-001"));yield return null;
        }
    }
}
