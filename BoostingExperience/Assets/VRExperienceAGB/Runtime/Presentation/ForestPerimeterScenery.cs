using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VRExperienceAGB.Presentation
{
    /// <summary>Combined scenery meshes frame the clearing without occupying the main aisle.</summary>
    public sealed class ForestPerimeterScenery : MonoBehaviour
    {
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<Material> materials = new List<Material>();
        public void Build(float length, Material stone, Material foliage, Transform clearing, Mesh rockMesh, Mesh fernMesh)
        {
            var random = new System.Random(82104);
            var mountains = new Geometry();
            for(int side=-1;side<=1;side+=2)
                for(int i=0;i<7;i++)
                    mountains.Mound(new Vector3(side*(78+i%2*15),-3,-35+i*(length+65)/6),
                        new Vector3(24,22+(float)random.NextDouble()*24,22),random);
            for(int i=0;i<7;i++)
                mountains.Mound(new Vector3(-78+i*26,-3,-68-i%2*12),new Vector3(26,22+(float)random.NextDouble()*20,23),random);
            Create("DistantSideAndRearMountains", mountains, stone, new Color(.16f,.22f,.29f),new Color(.018f,.027f,.04f));
            var shrubs = new Geometry();
            for(int side=-1;side<=1;side+=2)
                for(int i=0;i<24;i++) {
                    var center=new Vector3(side*(22+(float)random.NextDouble()*8),0,-9+(length+12)*i/23);
                    Shrub(shrubs,center,random);
                }
            Create("SideShrubs",shrubs,foliage,new Color(.16f,.29f,.23f),new Color(.018f,.035f,.025f));
            ScatterClearingDetails(clearing,"SlateBoulder",rockMesh,24,random);
            ScatterClearingDetails(clearing,"GroundFern",fernMesh,100,random);
        }
        private void ScatterClearingDetails(Transform clearing,string templateName,Mesh sourceMesh,int count,System.Random random)
        {
            var template=clearing!=null?clearing.Find(templateName):null;
            if(template==null)return;
            var renderer=template.GetComponent<MeshRenderer>();
            if(sourceMesh==null || renderer==null)return;
            var instances=new CombineInstance[count];
            for(int i=0;i<count;i++) {
                float x=-24+(float)random.NextDouble()*48,z=-7-(float)random.NextDouble()*15;
                bool rock=templateName=="SlateBoulder";
                float size=.2f+(float)random.NextDouble()*.75f;
                var scale=rock?new Vector3(size*1.5f,size,size):new Vector3(.18f,.18f+(float)random.NextDouble()*.2f,.18f);
                instances[i]=new CombineInstance{mesh=sourceMesh,transform=Matrix4x4.TRS(new Vector3(x,rock?size*.18f:0,z),Quaternion.Euler(0,(float)random.NextDouble()*360,0),scale)};
            }
            var mesh=new Mesh{name="Rear"+templateName};mesh.CombineMeshes(instances);meshes.Add(mesh);
            var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var output=go.GetComponent<MeshRenderer>();output.sharedMaterial=renderer.sharedMaterial;
            output.shadowCastingMode=ShadowCastingMode.Off;output.receiveShadows=false;
        }
        private static void Shrub(Geometry geometry,Vector3 center,System.Random random)
        {
            for(int i=0;i<3;i++)geometry.Bush(center+new Vector3((i-1)*.7f,0,(float)random.NextDouble()*.6f),
                new Vector3(.8f+(float)random.NextDouble()*.7f,.6f+(float)random.NextDouble()*.9f,.7f),random);
        }
        private void Create(string name,Geometry geometry,Material source,Color color,Color emission)
        {
            var mesh=geometry.Mesh(name);meshes.Add(mesh);
            var material=new Material(source){name=name+"Material",enableInstancing=true};materials.Add(material);
            material.mainTexture=Texture2D.whiteTexture;
            material.DisableKeyword("_ALPHATEST_ON");material.SetFloat("_AlphaClip",0);
            material.color=color;material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",emission);
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
        private void OnDestroy()
        {
            foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);
            foreach(var material in materials)if(material!=null)Destroy(material);
        }
        private sealed class Geometry
        {
            private readonly List<Vector3> vertices=new List<Vector3>();
            private readonly List<int> triangles=new List<int>();
            private void Triangle(Vector3 a,Vector3 b,Vector3 c)
            {int index=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);triangles.Add(index);triangles.Add(index+1);triangles.Add(index+2);}
            public void Bush(Vector3 center,Vector3 size,System.Random random)
            {
                const int sides=7;
                var rings=new Vector3[3,sides];
                for(int r=0;r<3;r++)for(int i=0;i<sides;i++) {
                    float angle=i*Mathf.PI*2/sides, radius=(r==0?.65f:r==1?1:.65f)*(.88f+(float)random.NextDouble()*.24f);
                    rings[r,i]=center+new Vector3(Mathf.Cos(angle)*size.x*radius,size.y*(r==0?0:r==1?.38f:.8f),Mathf.Sin(angle)*size.z*radius);
                }
                for(int i=0;i<sides;i++) {
                    int j=(i+1)%sides;
                    for(int r=0;r<2;r++){Triangle(rings[r,i],rings[r+1,i],rings[r,j]);Triangle(rings[r,j],rings[r+1,i],rings[r+1,j]);}
                    Triangle(rings[2,i],center+Vector3.up*size.y,rings[2,j]);
                }
            }
            public void Mound(Vector3 center,Vector3 size,System.Random random)
            {
                const int sides=7;
                var bottom=new Vector3[sides];var shoulder=new Vector3[sides];
                for(int i=0;i<sides;i++) {
                    float angle=i*Mathf.PI*2/sides,variation=.85f+(float)random.NextDouble()*.3f;
                    bottom[i]=center+new Vector3(Mathf.Cos(angle)*size.x,0,Mathf.Sin(angle)*size.z);
                    shoulder[i]=center+new Vector3(Mathf.Cos(angle)*size.x*.48f*variation,size.y*.55f*variation,Mathf.Sin(angle)*size.z*.48f*variation);
                }
                var peak=center+new Vector3(size.x*.12f,size.y,-size.z*.08f);
                for(int i=0;i<sides;i++){int j=(i+1)%sides;Triangle(bottom[i],shoulder[i],bottom[j]);Triangle(bottom[j],shoulder[i],shoulder[j]);Triangle(shoulder[i],peak,shoulder[j]);}
            }
            public Mesh Mesh(string name)
            {var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;}
        }
    }
}
