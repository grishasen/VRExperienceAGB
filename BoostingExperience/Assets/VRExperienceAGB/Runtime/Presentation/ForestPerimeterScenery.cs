using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VRExperienceAGB.Presentation
{
    /// <summary>World-anchored Carezza scenery, independent of model evaluation and the tracked camera.</summary>
    public sealed class ForestPerimeterScenery : MonoBehaviour
    {
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<Material> materials = new List<Material>();
        private static readonly Vector3 LakeCenter = new Vector3(0, .025f, -55);
        private static readonly Vector2 LakeRadius = new Vector2(65, 45);

        public void Build(float length, Material stone, Material foliage, Transform clearing, Mesh rockMesh, Mesh fernMesh,
            Material backdrop = null, Material spruce = null, Material water = null, Material ground = null, Material rock = null)
        {
            var random = new System.Random(82104);
            if (backdrop != null && spruce != null && water != null)
            {
                // Draw the fading ridge after the sky without hiding it behind transparent depth.
                var ridge = new Material(backdrop) { name = "CarezzaFadingRidge", renderQueue = (int)RenderQueue.Transparent };
                ridge.SetFloat("_ZWrite", 0);
                ridge.SetFloat("_SideAtmosphere", 1);
                materials.Add(ridge);
                Create("CarezzaMountainPanorama", Panorama(), ridge);
                Create("CarezzaLake", Lake(), water);
                var shore = ground != null ? ground : stone;
                Create("CarezzaShore", Shore(), shore);
                Create("CarezzaSurroundingGround", Ground(length), shore);
                var treeMesh = SpruceCards();
                RestyleGardenTrees(treeMesh, spruce);
                ScatterTrees(treeMesh, spruce, length, random);
            }
            ScatterDetails(clearing, "SlateBoulder", rockMesh, 24, length, random, rock);
            ScatterDetails(clearing, "GroundFern", fernMesh, 100, length, random, null);
        }

        private void RestyleGardenTrees(Mesh treeMesh, Material material)
        {
            foreach (Transform tree in transform)
            {
                if (tree.name != "GardenBackdrop-PineTree") continue;
                tree.localPosition = GroundedTreePosition(tree.localPosition);
                var branches = tree.Find("RadialBranches");
                if (branches == null) continue;
                // Cloned statically batched renderers retain the source batch offsets. Replace the renderer,
                // not just sharedMesh, so the new local-space cards cannot use stale batched geometry.
                var crown = new GameObject("RadialBranches",typeof(MeshFilter),typeof(MeshRenderer));
                crown.transform.SetParent(tree,false);
                crown.transform.localPosition=Vector3.zero;
                crown.transform.localRotation=branches.localRotation;
                crown.transform.localScale=branches.localScale;
                crown.GetComponent<MeshFilter>().sharedMesh=treeMesh;
                crown.GetComponent<MeshRenderer>().sharedMaterial=material;
                branches.gameObject.SetActive(false);
                Destroy(branches.gameObject);
                foreach (var renderer in tree.GetComponentsInChildren<MeshRenderer>())
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    if (renderer.name == "WoodyBranch" || renderer.name == "Trunk")
                    {
                        renderer.enabled = false;
                        Destroy(renderer);
                    }
                }
            }
        }

        private void ScatterTrees(Mesh treeMesh, Material material, float length, System.Random random)
        {
            var instances = new List<CombineInstance>();
            foreach(var position in new[]{new Vector3(-12,0,-9),new Vector3(-15,0,-10),new Vector3(14,0,-10),new Vector3(18,0,-12)})
                instances.Add(Instance(treeMesh,GroundedTreePosition(position),Vector3.one*Next(random,7,11),random));
            // Asymmetric groups along both sides, outside the locomotion area and the teaching beds.
            for (int side = -1; side <= 1; side += 2)
                for (int group = 0; group < 7; group++)
                {
                    float z = -19 + group * (length + 15) / 6;
                    float x = side * (27 + Next(random, 0, 12));
                    int count = group % 3 == 0 ? 4 : 2;
                    for (int i = 0; i < count; i++)
                    {
                        var position = new Vector3(x + Next(random, -3, 3), 0, z + Next(random, -4, 4));
                        float height = Next(random, 6, 13);
                        instances.Add(Instance(treeMesh, GroundedTreePosition(position), new Vector3(height * Next(random,.8f,1.05f),height,height), random));
                    }
                }
            // Broken tree line on the far bank gives the lake a distinct middle distance.
            for (int i = 0; i < 28; i++)
            {
                float angle = (Mathf.Lerp(65, 295, i / 27f)+Next(random,-4,4)) * Mathf.Deg2Rad;
                var position = LakeCenter + new Vector3(Mathf.Sin(angle) * 73, 0, Mathf.Cos(angle) * 57);
                position.y = 0;
                float height = Next(random, 10, 20);
                instances.Add(Instance(treeMesh, GroundedTreePosition(position), Vector3.one * height, random));
            }
            Combine("CarezzaShorePines", instances, material);
        }

        private static Vector3 GroundedTreePosition(Vector3 position)
        {
            var offset = new Vector2(position.x - LakeCenter.x, position.z - LakeCenter.z);
            float radius = new Vector2(offset.x / LakeRadius.x, offset.y / LakeRadius.y).magnitude;
            // Include the irregular bank's maximum variation and keep every trunk on dry land.
            if (radius < 1.12f)
            {
                offset *= 1.12f / Mathf.Max(radius, .001f);
                position.x = LakeCenter.x + offset.x;
                position.z = LakeCenter.z + offset.y;
            }
            position.y = -.055f;
            return position;
        }

        public Mesh OpenInteriorCrown(Mesh source)
        {
            // The authored Moonlit crown contains seven layers of ten independent branch quads.
            if (source.vertexCount != 280) return source;
            var sourceVertices = source.vertices;
            var sourceUv = source.uv;
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var indices = new List<int>();
            for (int layer = 1; layer < 7; layer++)
                for (int branch = 0; branch < 10; branch++)
                {
                    if ((branch + layer) % 3 == 0) continue;
                    int sourceIndex = (layer * 10 + branch) * 4, n = vertices.Count;
                    for (int i = 0; i < 4; i++) { vertices.Add(sourceVertices[sourceIndex + i]); uv.Add(sourceUv[sourceIndex + i]); }
                    indices.AddRange(new[] { n, n + 2, n + 1, n, n + 3, n + 2 });
                }
            var mesh = new Mesh { name = "OpenInteriorFirCrown" };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); meshes.Add(mesh); return mesh;
        }

        public Mesh InteriorLimbs(Mesh trunk)
        {
            var instances = new CombineInstance[14];
            for (int branch = 0; branch < instances.Length; branch++)
            {
                float y = .26f + branch * .038f, angle = branch * 137.5f * Mathf.Deg2Rad;
                var direction = new Vector3(Mathf.Cos(angle), .22f, Mathf.Sin(angle)).normalized;
                instances[branch] = new CombineInstance { mesh = trunk, transform = Matrix4x4.TRS(
                    Vector3.up * y, Quaternion.FromToRotation(Vector3.up, direction), new Vector3(.17f, .28f - y * .20f, .17f)) };
            }
            var mesh = new Mesh { name = "InteriorFirWoodyBranches" };
            mesh.CombineMeshes(instances); meshes.Add(mesh); return mesh;
        }

        private void ScatterDetails(Transform clearing, string templateName, Mesh sourceMesh, int count, float length, System.Random random, Material rockMaterial)
        {
            var template = clearing != null ? clearing.Find(templateName) : null;
            var renderer = template != null ? template.GetComponent<MeshRenderer>() : null;
            if (sourceMesh == null || renderer == null) return;
            bool rock = templateName == "SlateBoulder";
            var instances = new List<CombineInstance>();
            for (int i = 0; i < count; i++)
            {
                Vector3 position;
                if (i % 3 == 0)
                {
                    // Natural shoreline rather than a uniform row of props.
                    float angle = Next(random, -Mathf.PI, Mathf.PI);
                    float radius = Next(random, 1.015f, 1.13f);
                    position = LakeCenter + new Vector3(Mathf.Sin(angle)*LakeRadius.x*radius,0,Mathf.Cos(angle)*LakeRadius.y*radius);
                }
                else
                {
                    int side = i % 2 == 0 ? 1 : -1;
                    position = new Vector3(side*Next(random,23,40),0,Next(random,-13,length+12));
                    if (i % 7 == 0) position = new Vector3(Next(random,-18,18),0,Next(random,-13,-8));
                }
                float size = rock ? Next(random,.45f,2.3f) : Next(random,.28f,.65f);
                if(rock && Mathf.Abs(position.x)<20 && position.z>-18)size*=.45f;
                position.y = rock ? size * .16f : 0;
                var scale = rock ? new Vector3(size*1.4f,size*.8f,size) : new Vector3(size,size*1.35f,size);
                instances.Add(Instance(sourceMesh, position, scale, random));
            }
            if (rock && rockMaterial != null) { Combine("Rear"+templateName,instances,rockMaterial); return; }
            var detail = new Material(renderer.sharedMaterial) { name = "Carezza" + templateName };
            detail.color = rock ? new Color(.38f,.45f,.52f) : new Color(.12f,.25f,.21f);
            detail.EnableKeyword("_EMISSION");
            detail.SetColor("_EmissionColor", rock ? new Color(.045f,.058f,.075f) : new Color(.012f,.03f,.025f));
            materials.Add(detail);
            Combine("Rear" + templateName, instances, detail);
        }

        private static float Next(System.Random random, float min, float max) => Mathf.Lerp(min,max,(float)random.NextDouble());
        private static CombineInstance Instance(Mesh mesh, Vector3 position, Vector3 scale, System.Random random) =>
            new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(position,Quaternion.Euler(0,Next(random,0,360),0),scale) };

        private void Combine(string name, List<CombineInstance> instances, Material material)
        {
            var mesh = new Mesh { name = name };
            long vertices = 0;
            foreach (var instance in instances) vertices += instance.mesh.vertexCount;
            if (vertices > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.CombineMeshes(instances.ToArray()); meshes.Add(mesh);
            Create(name,mesh,material);
        }

        private void Create(string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));
            go.transform.SetParent(transform,false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private Mesh Panorama()
        {
            var geometry = new Geometry();
            const int segments = 96;
            for (int i = 0; i < segments; i++)
            {
                float fraction0 = i/(float)segments, fraction1 = (i+1)/(float)segments;
                float degreesA=Mathf.Lerp(-145,145,fraction0),degreesB=Mathf.Lerp(-145,145,fraction1);
                float u0=PanoramaU(degreesA),u1=PanoramaU(degreesB);
                float a = degreesA*Mathf.Deg2Rad, b = degreesB*Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Sin(a)*220,-8,-Mathf.Cos(a)*220);
                var q = new Vector3(Mathf.Sin(b)*220,-8,-Mathf.Cos(b)*220);
                float alphaA=Mathf.SmoothStep(0,1,Mathf.InverseLerp(145,115,Mathf.Abs(degreesA)));
                float alphaB=Mathf.SmoothStep(0,1,Mathf.InverseLerp(145,115,Mathf.Abs(degreesB)));
                geometry.Quad(p,q,q+Vector3.up*148,p+Vector3.up*148,new Vector2(u0,0),new Vector2(u1,1),alphaA,alphaB);
            }
            return Finish(geometry,"CarezzaPanoramaMesh");
        }

        private static float PanoramaU(float degrees)
        {
            if (degrees < -60) return Mathf.Lerp(.28f,0,Mathf.InverseLerp(-145,-60,degrees));
            if (degrees > 60) return Mathf.Lerp(1,.72f,Mathf.InverseLerp(60,145,degrees));
            return Mathf.InverseLerp(-60,60,degrees);
        }

        private Mesh SpruceCards()
        {
            var geometry = new Geometry();
            // Three fixed intersecting planes retain volume as the observer turns. No camera-facing updates.
            for (int i = 0; i < 3; i++)
            {
                var rotation = Quaternion.Euler(0,i*60,0);
                // Alpha-tested artwork occupies pixels x=124..923, y=29..1507 in 1024x1536.
                // Map the visible trunk base to the ground instead of lifting it by the transparent margin.
                geometry.Quad(rotation*new Vector3(-.27f,-.003f,0),rotation*new Vector3(.27f,-.003f,0),
                    rotation*new Vector3(.27f,1,0),rotation*new Vector3(-.27f,1,0),
                    new Vector2(124f/1024,28f/1536),new Vector2(924f/1024,1507f/1536));
            }
            return Finish(geometry,"CarezzaSpruceCards");
        }

        private Mesh Lake()
        {
            var geometry = new Geometry();
            for (int i = 0; i < 128; i++)
            {
                var a = ShorePoint(i/128f,1);
                var b = ShorePoint((i+1)/128f,1);
                a.y = b.y = LakeCenter.y;
                geometry.Triangle(LakeCenter,a,b);
            }
            return Finish(geometry,"CarezzaLakeMesh");
        }

        private static Vector3 ShorePoint(float fraction, float radius)
        {
            float a = fraction*Mathf.PI*2;
            float variation = 1+.025f*Mathf.Sin(a*5)+.015f*Mathf.Sin(a*11);
            return LakeCenter+new Vector3(Mathf.Sin(a)*LakeRadius.x*radius*variation,0,Mathf.Cos(a)*LakeRadius.y*radius*variation);
        }

        private Mesh Shore()
        {
            var geometry = new Geometry();
            for (int i = 0; i < 128; i++)
            {
                var a=ShorePoint(i/128f,.995f);var b=ShorePoint((i+1)/128f,.995f);
                var c=ShorePoint((i+1)/128f,1.10f);var d=ShorePoint(i/128f,1.10f);
                a.y=b.y=.028f; c.y=d.y=-.035f;
                geometry.GroundQuad(a,b,c,d);
            }
            return Finish(geometry,"CarezzaShoreMesh");
        }

        private Mesh Ground(float length)
        {
            var geometry = new Geometry();
            geometry.GroundQuad(new Vector3(-170,-.045f,-170),new Vector3(-170,-.045f,length+80),
                new Vector3(170,-.045f,length+80),new Vector3(170,-.045f,-170));
            geometry.GroundQuad(new Vector3(-170,-.012f,-170),new Vector3(-170,-.012f,-4),
                new Vector3(170,-.012f,-4),new Vector3(170,-.012f,-170));
            for(int side=-1;side<=1;side+=2) {
                float min=side<0?-170:20,max=side<0?-20:170;
                geometry.GroundQuad(new Vector3(min,-.012f,-4),new Vector3(min,-.012f,length+80),
                    new Vector3(max,-.012f,length+80),new Vector3(max,-.012f,-4));
            }
            return Finish(geometry,"CarezzaGroundMesh");
        }

        private Mesh Finish(Geometry geometry,string name)
        {
            var mesh=geometry.ToMesh(name);meshes.Add(mesh);return mesh;
        }

        private void OnDestroy()
        {
            foreach (var mesh in meshes) if (mesh != null) Destroy(mesh);
            foreach (var material in materials) if (material != null) Destroy(material);
        }

        private sealed class Geometry
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<Vector2> uv = new List<Vector2>();
            private readonly List<int> triangles = new List<int>();
            private readonly List<Color> colors = new List<Color>();
            public void Triangle(Vector3 a,Vector3 b,Vector3 c)
            {
                int n=vertices.Count;vertices.AddRange(new[]{a,b,c});
                uv.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.up});triangles.AddRange(new[]{n,n+1,n+2});
                colors.AddRange(new[]{Color.white,Color.white,Color.white});
            }
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector2 min,Vector2 max,float alphaA=1,float alphaB=1)
            {
                int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});
                uv.AddRange(new[]{min,new Vector2(max.x,min.y),max,new Vector2(min.x,max.y)});
                triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
                colors.AddRange(new[]{new Color(1,1,1,alphaA),new Color(1,1,1,alphaB),new Color(1,1,1,alphaB),new Color(1,1,1,alphaA)});
            }
            public void GroundQuad(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
            {
                int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});
                foreach(var p in new[]{a,b,c,d})uv.Add(new Vector2(p.x,p.z)*.18f);
                triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
                colors.AddRange(new[]{Color.white,Color.white,Color.white,Color.white});
            }
            public Mesh ToMesh(string name)
            {
                var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);
                mesh.SetColors(colors);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
            }
        }
    }
}
