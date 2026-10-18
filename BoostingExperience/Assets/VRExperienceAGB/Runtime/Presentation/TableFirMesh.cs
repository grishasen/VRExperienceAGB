using System.Collections.Generic;
using UnityEngine;

namespace VRExperienceAGB.Presentation
{
    /// <summary>Dense miniature foliage with solid boughs beneath textured needle edges.</summary>
    internal static class TableFirMesh
    {
        public static Mesh Build()
        {
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();

            int Vertex(Vector3 p, Vector2 texcoord, Color color, bool needles = false)
            {
                int index = vertices.Count;
                color.a = needles ? 1 : 0;
                vertices.Add(p); uv.Add(texcoord); colors.Add(color);
                return index;
            }
            void Triangle(int a, int b, int c) { triangles.Add(a); triangles.Add(b); triangles.Add(c); }

            // The central foliage closes the gaps between whorls without defining the outer silhouette.
            int trunk = vertices.Count;
            for (int ring = 0; ring < 9; ring++)
            for (int side = 0; side < 12; side++)
            {
                float y = ring == 0 ? 0 : .10f + (ring - 1) * .128f;
                float radius = ring < 2 ? .025f : Mathf.Max(.008f, (1-y) * .24f);
                float angle = side * Mathf.PI / 6;
                radius *= 1 + .12f * Mathf.Sin(side * 2.7f + ring);
                Color color = ring < 2 ? new Color(.12f,.065f,.028f) : new Color(.055f,.135f,.045f);
                Vertex(new Vector3(Mathf.Cos(angle)*radius,y,Mathf.Sin(angle)*radius),
                    new Vector2(side / 12f, ring / 8f), color);
            }
            for (int ring = 0; ring < 8; ring++)
            for (int side = 0; side < 12; side++)
            {
                int a=trunk+ring*12+side, b=trunk+ring*12+(side+1)%12;
                Triangle(a,a+12,b); Triangle(b,a+12,b+12);
            }

            for (int layer = 0; layer < 11; layer++)
            for (int branch = 0; branch < 8; branch++)
            {
                float phase = layer * 2.39996f + branch * Mathf.PI / 4;
                float variation = Mathf.Sin(branch * 7.13f + layer * 3.71f);
                float y = .31f + layer * .062f + variation * .012f;
                float length = (.50f - layer * .043f) * (1 + variation * .09f);
                var direction = new Vector3(Mathf.Cos(phase),0,Mathf.Sin(phase));
                var side = new Vector3(-direction.z,0,direction.x);
                var origin = Vector3.up * y;
                float drop = length * (.26f + .08f * Mathf.Sin(layer + branch));
                Color shade = Color.Lerp(new Color(.055f,.15f,.047f),new Color(.13f,.29f,.085f),
                    .45f + variation * .22f + layer * .025f);

                Vector3 Center(float t) => origin + direction * (length*t)
                    + Vector3.up * (length*.06f*Mathf.Sin(t*Mathf.PI) - drop*t*t);

                // Two oval rings form a tapered, curved foliage volume. Its coverage survives mip loss.
                int start = Vertex(origin,new Vector2(.5f,0),shade*.42f);
                int rings = vertices.Count;
                for (int ring = 0; ring < 2; ring++)
                for (int edge = 0; edge < 6; edge++)
                {
                    float t = ring == 0 ? .34f : .74f;
                    float angle = edge * Mathf.PI / 3;
                    float width = length * (ring == 0 ? .18f : .12f);
                    var p = Center(t) + side * (Mathf.Cos(angle)*width)
                        + Vector3.up * (Mathf.Sin(angle)*length*.105f);
                    Vertex(p,new Vector2(.5f+Mathf.Cos(angle)*.30f,t),shade*(ring==0?.62f:1.02f));
                }
                int tip = Vertex(Center(1),new Vector2(.5f,1),shade*1.3f);
                for (int edge = 0; edge < 6; edge++)
                {
                    int next=(edge+1)%6;
                    Triangle(start,rings+edge,rings+next);
                    Triangle(rings+edge,rings+6+edge,rings+next);
                    Triangle(rings+next,rings+6+edge,rings+6+next);
                    Triangle(rings+6+edge,tip,rings+6+next);
                }

                // Crossed bent sprigs supply fine, irregular edges, backed by the solid bough above.
                for (int plane = 0; plane < 3; plane++)
                {
                    var across = plane == 0 ? side : (side*.45f+Vector3.up*(plane==1?.89f:-.89f));
                    int n = vertices.Count;
                    for (int segment = 0; segment < 3; segment++)
                    {
                        float t=segment*.5f;
                        float width=length*(segment==0?.12f:segment==1?.50f:.32f);
                        Color sprig=shade*Mathf.Lerp(.48f,1.3f,t);
                        Vertex(Center(t)-across*width,new Vector2(0,t),sprig, true);
                        Vertex(Center(t)+across*width,new Vector2(1,t),sprig, true);
                    }
                    Triangle(n,n+2,n+1); Triangle(n+1,n+2,n+3);
                    Triangle(n+2,n+4,n+3); Triangle(n+3,n+4,n+5);
                }
            }

            var mesh = new Mesh { name = "TableNaturalFir" };
            mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetUVs(0,uv); mesh.SetTriangles(triangles,0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }
    }
}
