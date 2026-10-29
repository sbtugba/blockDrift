using System.Linq;
using System;
using System.IO;
using System.Reflection;
using BlockRift.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlockRift.Tests
{
    /// <summary>Gerçek ithal mesh üzerinde boşluklarda kum, oyuk merkezleri ve boyut değişimini denetler.</summary>
    public sealed class ApprovedAssetMigrationTests
    {
        private const string Prefabs="Assets/_BlockRift/Art/Prefabs/Gameplay/";

        /// <summary>Gerçek kurulum ve yenileme mevcut materyal byte'larını, değerlerini ve prefab GUID'lerini korur.</summary>
        [TestCase("Build")] [TestCase("RefreshPuzzle")]
        public void LevelOneSetupPreservesExistingMaterials(string command)
        {
            if (!UnityEngine.Application.isBatchMode) Assert.Ignore("Kurulum komutu yalnız izole batch doğrulamasında çalışır.");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            var files = Directory.GetFiles("Assets", "*", SearchOption.AllDirectories)
                .Concat(new[]{"ProjectSettings/EditorBuildSettings.asset"})
                .ToDictionary(p => p, File.ReadAllBytes);
            var materialValues = files.Keys.Where(p => p.EndsWith(".mat"))
                .ToDictionary(p => p, p => EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<Material>(p)));
            try
            {
                if (command == "Build") BlockRift.Editor.LevelOneSetup.Build();
                else BlockRift.Editor.LevelOneSetup.RefreshPuzzle();
                foreach (var pair in files.Where(p => p.Key.EndsWith(".mat") || p.Key.EndsWith(".prefab.meta")))
                    CollectionAssert.AreEqual(pair.Value, File.ReadAllBytes(pair.Key), pair.Key);
                foreach (var pair in materialValues)
                    Assert.That(EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<Material>(pair.Key)), Is.EqualTo(pair.Value), pair.Key);
                var palette = AssetDatabase.LoadAssetAtPath<LevelVisualPalette>("Assets/_BlockRift/Content/LevelOnePalette.asset");
                var catalog = AssetDatabase.LoadAssetAtPath<VisualPrefabCatalog>(BlockRift.Editor.GameplayArtAssets.CatalogPath);
                string[] keys = {"blue","green","red","yellow","purple","orange"};
                for (int i=0;i<keys.Length;i++) Assert.That(palette.colors[i], Is.SameAs(catalog.ResolveColor(keys[i])));
                foreach (int length in new[]{2,3,4})
                    Assert.That(catalog.TryGetPiece("Block_1x"+length, out _), Is.True);
            }
            finally
            {
                // Kurulumun sahne/palette/prefab çıktıları test projesinde bile diğer testlere sızmaz.
                foreach (var pair in files)
                    if (!File.ReadAllBytes(pair.Key).SequenceEqual(pair.Value)) File.WriteAllBytes(pair.Key, pair.Value);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                if (setup.Any(s => s.isLoaded) && setup.Any(s => s.isActive))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        /// <summary>Eksik materyal sessizce üretilmez ve mevcut materyaller değiştirilmez.</summary>
        [Test] public void MissingSetupMaterialFailsWithoutCreatingAnAsset()
        {
            const string name = "__BlockRiftMissingMaterial_Phase2B";
            string path = BlockRift.Editor.GameplayArtAssets.Materials + name + ".mat";
            Assert.That(File.Exists(path), Is.False);
            var method = typeof(BlockRift.Editor.LevelOneSetup).GetMethod("RequireMaterial", BindingFlags.NonPublic | BindingFlags.Static);
            var error = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, new object[]{name}));
            Assert.That(error.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(File.Exists(path), Is.False);
        }

        [TestCase(5)] [TestCase(6)]
        public void SandIsDirectlyVisibleInTileGapsAndBelowPiecePlacement(int size)
        {
            var board=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+(size==6?"Board_6x6_Frameless":"Board_5x5_Mobile")+".prefab");
            Assert.That(board.GetComponentsInChildren<Collider>(true),Is.Empty);
            Assert.That(board.transform.childCount,Is.EqualTo(2));
            Vector2 gap=new Vector2(-size*.5f+1,-(size-1)*.5f);
            Assert.That(HeightAt(board.transform.Find("Cells"),gap),Is.EqualTo(float.NegativeInfinity));
            Assert.That(HeightAt(board.transform.Find("Sand"),gap),Is.EqualTo(-.062f).Within(.00001f));
            var cells=BlockRift.Editor.GameplayArtAssets.MeasureBounds(board.transform.Find("Cells"));
            Assert.That(cells.max.y,Is.EqualTo(-.001f).Within(.00001f));
            Assert.That(cells.min.y,Is.EqualTo(-.061f).Within(.00001f));
            var sand=BlockRift.Editor.GameplayArtAssets.MeasureBounds(board.transform.Find("Sand"));
            Assert.That(sand.max.y,Is.LessThan(-.001f));
        }

        [TestCase("2x1",2,1)] [TestCase("3x1",3,1)] [TestCase("4x1",4,1)] [TestCase("2x2",2,2)]
        public void UnifiedDeckHasNoInteriorPanelChannelAndRetainsEveryDiamond(string shape,int width,int height)
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"Block_"+shape+".prefab").transform;
            Assert.That(HeightAt(root,Vector2.zero),Is.EqualTo(width%2==1?.26f:.3f).Within(.0001f));
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                Vector2 center=new Vector2(x-(width-1)*.5f,y-(height-1)*.5f);
                Assert.That(HeightAt(root,center),Is.EqualTo(.26f).Within(.0001f));
            }
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var m=filter.sharedMesh;
                Assert.That(m.normals.Length,Is.EqualTo(m.vertexCount));
                Assert.That(m.tangents.Length,Is.EqualTo(m.vertexCount));
                Assert.That(m.uv.Length,Is.EqualTo(m.vertexCount));
                Assert.That(m.normals.All(n=>!float.IsNaN(n.x)&&Mathf.Abs(n.magnitude-1)<.001f),Is.True);
            }
        }

        [Test] public void RuntimeBoardSelectionUsesActualFiveAndSixPrefabsAtUnitScale()
        {
            var scene=EditorSceneManager.OpenPreviewScene("Assets/Scenes/BlockRift_Gameplay.unity");
            try
            {
                var layout=scene.GetRootGameObjects()[0].GetComponentInChildren<BoardVisualLayout>();
                foreach(int size in new[]{5,6,5})
                {
                    layout.SetBoardSize(size);var views=layout.transform.Find("BoardVisualsRoot");
                    Assert.That(views.childCount,Is.EqualTo(1));
                    var board=views.GetChild(0);
                    Assert.That(board.localScale,Is.EqualTo(Vector3.one));
                    Assert.That(BlockRift.Editor.GameplayArtAssets.MeasureBounds(board.Find("Cells")).size.x,Is.EqualTo(size-.03f).Within(.0001f));
                }
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }

        // Dikey ışının XZ üçgen kesişimi: collider eklemeden gerçek mesh yüzeyini ölçer.
        private static float HeightAt(Transform root,Vector2 point)
        {
            float top=float.NegativeInfinity;
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var m=filter.sharedMesh;var verts=m.vertices;var tris=m.triangles;
                Matrix4x4 matrix=root.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                for(int i=0;i<tris.Length;i+=3)
                {
                    Vector3 a=matrix.MultiplyPoint3x4(verts[tris[i]]),b=matrix.MultiplyPoint3x4(verts[tris[i+1]]),c=matrix.MultiplyPoint3x4(verts[tris[i+2]]);
                    float det=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);
                    if(Mathf.Abs(det)<1e-10f)continue;
                    float u=((b.z-c.z)*(point.x-c.x)+(c.x-b.x)*(point.y-c.z))/det;
                    float v=((c.z-a.z)*(point.x-c.x)+(a.x-c.x)*(point.y-c.z))/det;
                    if(u>=-1e-6f&&v>=-1e-6f&&u+v<=1.000001f)top=Mathf.Max(top,u*a.y+v*b.y+(1-u-v)*c.y);
                }
            }
            return top;
        }
    }
}
