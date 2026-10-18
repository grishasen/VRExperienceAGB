using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRExperienceAGB.Presentation;

namespace VRExperienceAGB.Tests
{
    public class NearSceneryTests
    {
        private OneTreeExperience view;
        [UnitySetUp] public IEnumerator Open()
        {
            yield return SceneManager.LoadSceneAsync("OneTreeLearning"); yield return null;
            view=Object.FindAnyObjectByType<OneTreeExperience>();
            view.enabled=false;view.Director.enabled=false;view.GetComponent<DesktopTreePreview>().enabled=false;
            Load(100);yield return null;
        }

        private void Load(int count)
        {
            const string tree="{\"score\":0,\"gain\":1,\"sampleCount\":10,\"split\":\"Example < 5\",\"left\":{\"score\":-0.25,\"gain\":0,\"sampleCount\":5},\"right\":{\"score\":0.75,\"gain\":0,\"sampleCount\":5}}";
            string export="{\"type\":\"AdaptiveBoostScoringModel\",\"algorithm\":\"GRADIENT_BOOST\",\"model\":{\"booster\":{\"trees\":["+string.Join(",",Enumerable.Repeat(tree,count))+"]}}}";
            Assert.That(view.LoadAgbStructure(export,"Synthetic scenery regression").IsSuccess,Is.True);
            view.Director.Activate("forest");
        }

        [UnityTest] public IEnumerator DetailIsBoundedSharedAndIndependentOfTrackingAndModelTrees()
        {
            view.Garden.SetFullSceneryDetail(false);
            var root=view.transform.Find("ModelPineGarden");
            var head=view.Garden.Locomotion.Head;var position=head.localPosition;var rotation=head.localRotation;
            var trees=root.Cast<Transform>().Where(t=>t.name=="GardenInteriorPine").OrderBy(t=>t.localPosition.sqrMagnitude).ToArray();
            var groups=trees.Select(t=>t.GetComponent<LODGroup>()).ToArray();
            Assert.That(groups.Length,Is.EqualTo(6));
            Assert.That(groups.All(g=>g!=null),Is.True);
            foreach(var group in groups) {
                var levels=group.GetLODs();Assert.That(levels.Length,Is.EqualTo(3));
                Assert.That(levels[0].renderers.Sum(r=>r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3),Is.LessThan(650));
                Assert.That(levels[2].renderers.Sum(r=>r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3),Is.LessThan(200));
                Assert.That(group.transform.Find("InteriorBranches").gameObject.activeSelf,Is.False);
                Assert.That(group.transform.Find("InteriorWoodyBranches").gameObject.activeSelf,Is.False);
                Assert.That(group.GetComponentsInChildren<Collider>(),Is.Empty);
                for(int level=0;level<3;level++) {
                    group.ForceLOD(level);yield return null;
                    Assert.That(levels[level].renderers.All(r=>r!=null && r.gameObject.activeInHierarchy),Is.True);
                }
                group.ForceLOD(-1);
            }
            Assert.That(root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith("DetailedFirLOD"))
                .Select(r=>r.sharedMaterial).Distinct().Count(),Is.EqualTo(1));
            Assert.That(view.Model.Trees.Count,Is.EqualTo(100));Assert.That(view.Garden.PlotCount,Is.EqualTo(100));
            Assert.That(view.Ensemble.CompletedCount,Is.Zero);
            Assert.That(head.localPosition,Is.EqualTo(position));Assert.That(head.localRotation,Is.EqualTo(rotation));
        }

        [UnityTest] public IEnumerator RebuildingAndSmallModelsDoNotRetainOldDetailOrDestroySharedAssets()
        {
            var oldRoot=view.transform.Find("ModelPineGarden");
            var shared=view.Garden.detailedFirHighMesh;
            Load(3);yield return null;yield return null;
            Assert.That(oldRoot==null,Is.True);
            Assert.That(view.transform.Find("ModelPineGarden").Cast<Transform>().Count(t=>t.name=="GardenInteriorPine"),Is.Zero);
            Assert.That(shared!=null,Is.True);
            Load(100);yield return null;yield return null;
            Assert.That(view.transform.Find("ModelPineGarden").Cast<Transform>().Count(t=>t.name=="GardenInteriorPine" && t.GetComponent<LODGroup>()!=null),Is.EqualTo(6));
            Assert.That(view.Garden.detailedFirHighMesh,Is.SameAs(shared));
            view.Director.Activate("single");yield return null;
            Assert.That(view.transform.Find("ModelPineGarden").gameObject.activeSelf,Is.False);
            Assert.That(view.transform.Find("MoonlitEnvironment").GetComponentsInChildren<LODGroup>().Count(g=>g.name=="PineTree"),Is.EqualTo(42));
        }

        [UnityTest] public IEnumerator ModelLodsPreserveMetricBoundsAndNeverCullTheEnsemble()
        {
            view.Garden.SetFullSceneryDetail(false);
            var root=view.transform.Find("ModelPineGarden");
            var crowns=root.GetComponentsInChildren<LODGroup>().Where(g=>g.name=="Pine").ToArray();
            Assert.That(crowns.Length,Is.EqualTo(100));
            foreach(var crown in crowns) {
                var levels=crown.GetLODs();Assert.That(levels.Length,Is.EqualTo(2));
                Assert.That(levels[1].screenRelativeTransitionHeight,Is.Zero);
                var high=levels[0].renderers[0].bounds;var low=levels[1].renderers[0].bounds;
                Assert.That(Vector3.Distance(high.center,low.center),Is.LessThan(.001f));
                Assert.That(Vector3.Distance(high.size,low.size),Is.LessThan(.001f));
                Assert.That(levels[0].renderers[0].GetComponent<MeshFilter>().sharedMesh.triangles.Length/3,Is.InRange(320,384));
                crown.ForceLOD(1);
            }
            yield return null;
            Assert.That(view.Garden.PlotCount,Is.EqualTo(view.Model.Trees.Count));
            Assert.That(view.Garden.VisiblePlotCount,Is.EqualTo(100));
            Assert.That(root.GetComponentsInChildren<Canvas>(true).Count(c=>c.name.StartsWith("PineCrownTarget-")),Is.EqualTo(100));
        }

        [UnityTest] public IEnumerator CompleteBranchesRemainVisibleAcrossTheMiddleDistance()
        {
            var root=view.transform.Find("ModelPineGarden");
            foreach(var group in root.GetComponentsInChildren<LODGroup>().Where(g=>g.name=="Pine"||g.name=="GardenInteriorPine")) {
                // Conservative 110-degree vertical field of view, independent of global LOD bias.
                float distance=group.name=="Pine"?20:40;
                var scale=group.transform.lossyScale;
                float projectedSize=group.size*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z))/(2*distance*Mathf.Tan(55*Mathf.Deg2Rad));
                Assert.That(projectedSize,Is.GreaterThan(group.GetLODs()[0].screenRelativeTransitionHeight),group.name+" loses complete branches too close to the viewer.");
            }
            Assert.That(view.Garden.modelPineLowMesh.triangles.Length/3,Is.InRange(128,160),"Keep a fuller far silhouette within its geometry budget.");
            Assert.That(view.Garden.detailedFirLowMesh,Is.SameAs(view.Garden.modelPineLowMesh));
            yield return null;
        }

        [UnityTest] public IEnumerator GrassAndRocksKeepWalkingSpaceClearAndReleaseTheirMeshes()
        {
            var root=view.transform.Find("ModelPineGarden");
            var chunks=root.Cast<Transform>().Where(t=>t.name.StartsWith("ForestGrass-")).ToArray();
            Assert.That(chunks.Length,Is.InRange(1,4));
            Assert.That(chunks.Sum(t=>t.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3),Is.InRange(180*6,640*6));
            var rocks=root.Cast<Transform>().Where(t=>t.name.StartsWith("ForestRocks-")).ToArray();
            Assert.That(rocks.Length,Is.InRange(1,4));
            Assert.That(rocks.Sum(t=>t.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3),Is.LessThanOrEqualTo(90*60));
            Assert.That(rocks.All(t=>t.GetComponent<MeshRenderer>().sharedMaterial==view.Garden.carezzaRockMaterial),Is.True);
            var owned=chunks.Concat(rocks).Select(t=>t.GetComponent<MeshFilter>().sharedMesh).ToArray();
            foreach(var chunk in chunks.Concat(rocks)) {
                Assert.That(chunk.GetComponentsInChildren<Collider>(),Is.Empty);
                foreach(var p in chunk.GetComponent<MeshFilter>().sharedMesh.vertices) {
                    Assert.That(Mathf.Abs(p.x),Is.GreaterThan(1.1f));
                    for(int row=0;row<10;row++)Assert.That(Mathf.Abs(p.z-(4.5f+row*4.8f)),Is.GreaterThan(.625f));
                    for(int tree=0;tree<100;tree++)Assert.That(Vector2.Distance(new Vector2(p.x,p.z),new Vector2(view.Garden.PlotPosition(tree).x,view.Garden.PlotPosition(tree).z)),Is.GreaterThan(1.1f));
                }
            }
            var banks=root.Cast<Transform>().Where(t=>t.name.StartsWith("CarezzaShorePines")).ToArray();
            Assert.That(banks.Length,Is.EqualTo(4));
            Assert.That(banks.Sum(t=>t.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3),Is.EqualTo(72*6));
            Load(3);yield return null;yield return null;
            Assert.That(owned.All(m=>m==null),Is.True);
        }

        [UnityTest] public IEnumerator FullDetailKeepsGrassRocksAndBranchesRenderedAcrossViewsAndRebuilds()
        {
            Assert.That(view.Garden.FullSceneryDetail,Is.True,"The comparison build must start with full scenery detail.");
            var head=view.Garden.Locomotion.Head;
            var position=head.localPosition;var rotation=head.localRotation;
            var root=view.transform.Find("ModelPineGarden");
            foreach(var name in new[]{"ForestGrass-0","ForestRocks-0","Pine","GardenInteriorPine","GardenBackdrop-PineTree"})
                yield return Compare(root.GetComponentsInChildren<LODGroup>().First(g=>g.name==name));
            Assert.That(view.Garden.VisiblePlotCount,Is.EqualTo(100));
            Assert.That(head.localPosition,Is.EqualTo(position));Assert.That(head.localRotation,Is.EqualTo(rotation));

            view.Director.Activate("single");yield return null;
            var clearing=view.transform.Find("MoonlitEnvironment");
            foreach(var name in new[]{"PineTree","RockGrass"})
                yield return Compare(clearing.GetComponentsInChildren<LODGroup>().First(g=>g.name==name));

            Load(3);yield return null;yield return null;
            Assert.That(view.Garden.FullSceneryDetail,Is.True);
            yield return Compare(view.transform.Find("ModelPineGarden").GetComponentsInChildren<LODGroup>().First(g=>g.name=="ForestRocks-0"));
        }

        private IEnumerator Compare(LODGroup group)
        {
            var renderers=group.GetLODs().SelectMany(l=>l.renderers).Distinct().ToArray();
            var layers=renderers.Select(r=>r.gameObject.layer).ToArray();
            var cameraObject=new GameObject("SceneryDistanceProbe",typeof(Camera));
            var target=new RenderTexture(256,256,24);
            var pixels=new Texture2D(256,256,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;float bias=QualitySettings.lodBias;
            try {
                foreach(var renderer in renderers)renderer.gameObject.layer=30;
                var bounds=renderers[0].bounds;
                foreach(var renderer in group.GetLODs()[0].renderers)bounds.Encapsulate(renderer.bounds);
                if(group.name.StartsWith("Forest")||group.name=="RockGrass") {
                    // Frame a local tuft/rock, not the entire wide batch where grass becomes subpixel.
                    var filter=renderers[0].GetComponent<MeshFilter>();
                    var points=filter.sharedMesh.vertices.Take(12).Select(filter.transform.TransformPoint).ToArray();
                    bounds=new Bounds(points[0],Vector3.zero);foreach(var point in points)bounds.Encapsulate(point);
                }
                var camera=cameraObject.GetComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<30;
                camera.fieldOfView=60;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
                camera.nearClipPlane=.01f;camera.farClipPlane=2000;camera.targetTexture=target;
                camera.transform.position=bounds.center+new Vector3(0,.65f,-1).normalized*Mathf.Max(2,bounds.size.magnitude*1.2f);
                camera.transform.LookAt(bounds.center);
                // Simulate a far-distance LOD decision while retaining enough pixels to inspect branch geometry.
                QualitySettings.lodBias=.0001f;
                yield return null;
                var full=Render();
                Assert.That(full.Count(c=>c.maxColorComponent>.01f),Is.GreaterThan(10),group.name+" disappeared in full-detail mode.");
                view.Garden.SetFullSceneryDetail(false);yield return null;
                var automatic=Render();
                int changed=full.Zip(automatic,(a,b)=>Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b)).Count(d=>d>.04f);
                Assert.That(changed,Is.GreaterThan(10),group.name+" did not retain additional visible detail over automatic LOD.");
                view.Garden.SetFullSceneryDetail(true);yield return null;
                var restored=Render();
                Assert.That(restored.Zip(full,(a,b)=>Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b)).Max(),Is.LessThan(.01f),group.name+" did not restore full detail.");

                Color[] Render() {
                    camera.Render();RenderTexture.active=target;
                    pixels.ReadPixels(new Rect(0,0,256,256),0,0);pixels.Apply();return pixels.GetPixels();
                }
            } finally {
                QualitySettings.lodBias=bias;view.Garden.SetFullSceneryDetail(true);
                for(int i=0;i<renderers.Length;i++)renderers[i].gameObject.layer=layers[i];
                RenderTexture.active=previous;cameraObject.GetComponent<Camera>().targetTexture=null;target.Release();
                Object.Destroy(cameraObject);Object.Destroy(target);Object.Destroy(pixels);
            }
        }
    }
}
