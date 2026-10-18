using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VRExperienceAGB.Presentation
{
    /// <summary>Sparse, static grass with bounded geometry and spatial culling. No colliders or tracking updates.</summary>
    public sealed class ForestGroundCover : MonoBehaviour
    {
        private readonly List<Mesh> meshes=new List<Mesh>();
        public void Build(ForestGardenView garden,int rows,Mesh tuft,Material material)
        {
            if(rows<=0 || tuft==null || material==null)return;
            var random=new System.Random(82107);
            var chunks=new[]{new List<CombineInstance>(),new List<CombineInstance>(),new List<CombineInstance>(),new List<CombineInstance>()};
            var stones=new[]{new List<CombineInstance>(),new List<CombineInstance>(),new List<CombineInstance>(),new List<CombineInstance>()};
            int count=Mathf.Min(240,rows*16);
            for(int i=0;i<count;i++) {
                int row=i%rows;float side=i%2==0?-1:1;
                float x=side*(i%4<2?1.38f:17.5f)+Next(-.16f,.16f);
                float z=5.7f+row*4.8f+(float)random.NextDouble()*2.4f;
                AddGrass(new Vector3(x,-.015f,z),Next(.18f,.32f),Mathf.Min(3,row*4/rows));
            }

            // Small asymmetric groups echo the single-tree clearing without covering labels or walking space.
            int cluster=0;
            foreach(var p in new[]{new Vector3(-2.5f,0,2.7f),new Vector3(3.1f,0,2.5f),new Vector3(-6.8f,0,2),new Vector3(6.7f,0,2.2f)})AddCluster(p,0);
            int samples=Mathf.Min(rows,15);
            for(int sample=0;sample<samples;sample++) {
                int row=sample*rows/samples,chunk=Mathf.Min(3,row*4/rows);
                for(int side=-1;side<=1;side+=2)
                for(int band=0;band<2;band++) {
                    float x=side*(4.45f+((sample+band)%2)*3.1f+band*6.2f)+Next(-.12f,.12f);
                    AddCluster(new Vector3(x,0,7.85f+row*4.8f+Next(-.12f,.12f)),chunk);
                }
            }
            for(int i=0;i<chunks.Length;i++) {
                if(chunks[i].Count>0)CreateChunk("ForestGrass-"+i,chunks[i],material);
                if(stones[i].Count>0)CreateChunk("ForestRocks-"+i,stones[i],garden.carezzaRockMaterial,.009f);
            }

            float Next(float min,float max)=>Mathf.Lerp(min,max,(float)random.NextDouble());
            void AddGrass(Vector3 position,float size,int chunk) {
                if(!Clear(position,size*.55f))return;
                position.y=-.015f;
                chunks[chunk].Add(new CombineInstance {mesh=tuft,
                    transform=Matrix4x4.TRS(position,Quaternion.Euler(0,Next(0,360),0),new Vector3(size*.9f,size,size*.9f))});
            }
            void AddCluster(Vector3 center,int chunk) {
                float size=Next(.6f,.95f);
                AddRock(center,size,chunk,cluster);
                if(cluster%3==0)AddRock(center+new Vector3(.55f,0,-.16f),size*.45f,chunk,cluster+1);
                for(int i=0;i<6;i++) {
                    float angle=(i*60+cluster*137.5f+Next(-18,18))*Mathf.Deg2Rad;
                    var p=center+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*Next(.58f,.95f);
                    AddGrass(p,Next(.25f,.43f),chunk);
                }
                cluster++;
            }
            void AddRock(Vector3 position,float size,int chunk,int variant) {
                var shapes=garden.detailedRockMeshes;
                if(shapes==null || shapes.Length==0 || garden.carezzaRockMaterial==null)return;
                var shape=shapes[variant%shapes.Length];if(shape==null)return;
                var scale=new Vector3(size*1.35f,size*.95f,size);
                var bounds=shape.bounds;
                float radius=new Vector2((Mathf.Abs(bounds.center.x)+bounds.extents.x)*scale.x,
                    (Mathf.Abs(bounds.center.z)+bounds.extents.z)*scale.z).magnitude;
                if(!Clear(position,radius))return;
                // Bury the foot slightly in the same ground plane as the grass.
                position.y=-.02f-bounds.min.y*scale.y-size*.06f;
                stones[chunk].Add(new CombineInstance {mesh=shape,
                    transform=Matrix4x4.TRS(position,Quaternion.Euler(0,Next(0,360),0),scale)});
            }
            bool Clear(Vector3 position,float radius) {
                if(Mathf.Abs(position.x)<1.1f+radius)return false;
                int row=Mathf.Clamp(Mathf.RoundToInt((position.z-4.5f)/4.8f),0,rows-1);
                if(Mathf.Abs(position.z-(4.5f+row*4.8f))<.625f+radius)return false;
                for(int plot=0;plot<garden.PlotCount;plot++) {
                    var delta=garden.PlotPosition(plot)-position;delta.y=0;
                    if(delta.sqrMagnitude<(1.15f+radius)*(1.15f+radius))return false;
                }
                return true;
            }
        }

        public void BuildClearing(Transform clearing,Mesh tuft,Material material)
        {
            var chunks=new List<CombineInstance>();int index=0;
            foreach(Transform stone in clearing) {
                if(stone.name!="SlateBoulder"||stone.localPosition.sqrMagnitude>18*18)continue;
                for(int i=0;i<3;i++) {
                    float angle=(index++*137.5f)*Mathf.Deg2Rad;
                    var p=stone.localPosition+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*stone.localScale.x*.55f;p.y=-.015f;
                    // Keep the teaching route and origin unobstructed.
                    if(Mathf.Abs(p.x)<2.2f||p.sqrMagnitude<9)continue;
                    chunks.Add(new CombineInstance {mesh=tuft,transform=Matrix4x4.TRS(p,Quaternion.Euler(0,index*71,0),Vector3.one*(.20f+(index%4)*.025f))});
                }
            }
            if(chunks.Count>0)CreateChunk("RockGrass",chunks,material);
        }

        private void CreateChunk(string name,List<CombineInstance> instances,Material material,float transition=.018f)
        {
            var mesh=new Mesh {name=name};mesh.CombineMeshes(instances.ToArray());meshes.Add(mesh);
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
            var lod=go.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(transition,new[]{renderer})});
            // Use one metre for distance selection, while the renderer retains its full spatial culling bounds.
            lod.localReferencePoint=mesh.bounds.center;lod.size=1;
        }
        private void OnDestroy(){foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);}
    }
}
