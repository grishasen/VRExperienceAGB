using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace VRExperienceAGB.Presentation
{
    /// <summary>Bounded decorative detail. Shared asset meshes need no runtime allocation or disposal.</summary>
    public static class NearSceneryDetail
    {
        public static void Apply(Transform root, Mesh high, Mesh medium, Mesh low, Material needles,
            Mesh trunkHigh=null, Mesh trunkLow=null, Material bark=null)
        {
            if (high == null || medium == null || low == null || needles == null) return;
            foreach (var tree in root.Cast<Transform>().Where(t => t.name == "GardenInteriorPine"))
            {
                if (tree.GetComponent<LODGroup>() != null) continue;
                var original = tree.Find("InteriorBranches");
                if (original == null) continue;
                var trunk=tree.Find("InteriorTrunk").GetComponent<Renderer>();
                BuildTree(tree,trunk.transform.localScale.x,high,medium,low,needles,
                    trunkHigh??trunk.GetComponent<MeshFilter>().sharedMesh,trunkLow??trunk.GetComponent<MeshFilter>().sharedMesh,bark??trunk.sharedMaterial);
            }
        }

        public static void ApplyClearing(Transform clearing, ForestGardenView garden)
        {
            if(clearing==null || garden.detailedTrunkMesh==null || garden.detailedBarkMaterial==null)return;
            foreach(var tree in clearing.Cast<Transform>().Where(t=>t.name=="PineTree")) {
                var crown=tree.Find("RadialBranches");
                if(crown==null || tree.GetComponent<LODGroup>()!=null)continue;
                BuildTree(tree,crown.localScale.y,garden.detailedFirHighMesh,garden.detailedFirMediumMesh,garden.detailedFirLowMesh,
                    garden.detailedFirMaterial,garden.detailedTrunkMesh,garden.detailedTrunkLowMesh,garden.detailedBarkMaterial);
            }
        }

        private static void BuildTree(Transform tree,float height,Mesh high,Mesh medium,Mesh low,Material needles,Mesh trunk,Mesh trunkLow,Material bark)
        {
            foreach(var renderer in tree.GetComponentsInChildren<MeshRenderer>())renderer.gameObject.SetActive(false);
            // Keep complete branches across the walkable garden; simplify only in the distant scenery.
            var crownMeshes=new[]{high,medium,low};var levels=new LOD[3];var thresholds=new[]{.055f,.021f,.004f};
            for(int i=0;i<3;i++) {
                var crown=Renderer(tree,"DetailedFirLOD"+i,crownMeshes[i],needles,Vector3.one*height);
                var wood=Renderer(tree,"DetailedTrunkLOD"+i,i==0?trunk:trunkLow,bark,Vector3.one*height);
                levels[i]=new LOD(thresholds[i],new[]{crown,wood});
            }
            var lod=tree.gameObject.AddComponent<LODGroup>();lod.fadeMode=LODFadeMode.None;lod.SetLODs(levels);lod.RecalculateBounds();
        }

        public static void AddModelLod(GameObject pine,Mesh low)
        {
            if(low==null)return;
            var high=pine.GetComponent<MeshRenderer>();var bounds=pine.GetComponent<MeshFilter>().sharedMesh.bounds;
            var scale=new Vector3(bounds.size.x/low.bounds.size.x,bounds.size.y/low.bounds.size.y,bounds.size.z/low.bounds.size.z);
            var far=Renderer(pine.transform,"PineFar",low,high.sharedMaterial,scale);
            far.transform.localPosition=bounds.center-Vector3.Scale(low.bounds.center,scale);
            high.shadowCastingMode=ShadowCastingMode.Off;high.receiveShadows=false;
            var lod=pine.AddComponent<LODGroup>();lod.fadeMode=LODFadeMode.None;
            // The last level is never distance-culled: every model tree remains represented.
            lod.SetLODs(new[]{new LOD(.018f,new[]{high}),new LOD(0,new[]{far})});lod.RecalculateBounds();
        }

        public static GameObject BackdropCopy(Transform source,Transform parent)
        {
            var copy=new GameObject("GardenBackdrop-PineTree");copy.transform.SetParent(parent,false);
            copy.transform.localPosition=source.localPosition;copy.transform.localRotation=source.localRotation;copy.transform.localScale=source.localScale;
            var original=source.Find("RadialBranches");
            if(original!=null) {
                var crown=Renderer(copy.transform,"RadialBranches",original.GetComponent<MeshFilter>().sharedMesh,
                    original.GetComponent<MeshRenderer>().sharedMaterial,original.localScale);
                crown.transform.localPosition=original.localPosition;crown.transform.localRotation=original.localRotation;
            }
            return copy;
        }

        private static MeshRenderer Renderer(Transform parent,string name,Mesh mesh,Material material,Vector3 scale)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localScale=scale;
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;return renderer;
        }
    }
}
