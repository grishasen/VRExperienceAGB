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
    }
}
