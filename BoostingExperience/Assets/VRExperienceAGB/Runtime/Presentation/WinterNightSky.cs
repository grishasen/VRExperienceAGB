using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using VRExperienceAGB.Application;

namespace VRExperienceAGB.Presentation
{
    /// <summary>Catalog-based winter sky and bounded-rate decorative meteors.</summary>
    public sealed class WinterNightSky : MonoBehaviour
    {
        private const float MaximumVisibleMagnitude = 4.0f;
        private ForestGardenView garden;
        private Transform root;
        private Material previousSky, background, points, moonMaterial;
        private Mesh starMesh, meteorMesh, moonMesh;
        public Vector3 MoonDirection { get; private set; }
        private GameObject meteor;
        private readonly MeteorSchedule schedule = new MeteorSchedule();
        private readonly System.Random random = new System.Random(550105);
        private readonly List<GameObject> hiddenMoons = new List<GameObject>();
        private bool paused, unfocused;
        private float meteorAge = 2;
        private Vector3 start, end;
        public int VisibleStarCount { get; private set; }
        public int MeteorCount => schedule.Count;
        public bool MeteorVisible => meteor != null && meteor.activeSelf;
        public IReadOnlyDictionary<string, Vector3> NamedStars => names;
        private readonly Dictionary<string, Vector3> names = new Dictionary<string, Vector3>();
        public void Configure(ForestGardenView owner)
        {
            if (root != null) return;
            garden = owner;
            root = new GameObject("WinterNightSky").transform; root.SetParent(owner.Experience.transform, false);
            previousSky = RenderSettings.skybox;
            background = new Material(Resources.Load<Shader>("WinterSkyBackground"));
            points = new Material(Resources.Load<Shader>("WinterSkyPoints"));
            RenderSettings.skybox = background;
            // Replace the oversized artistic disc with one camera-relative Moon, clear of the winter landmarks.
            var moon = owner.Experience.transform.Find("MoonlitEnvironment/Moon");
            if (moon != null && moon.gameObject.activeSelf) { hiddenMoons.Add(moon.gameObject); moon.gameObject.SetActive(false); }
            BuildStars(); BuildMoon(); BuildGuides();
            meteorMesh = new Mesh { name = "SingleMeteorRibbon" }; meteorMesh.MarkDynamic();
            meteor = CreateMesh("RareMeteor", meteorMesh); meteor.SetActive(false);
        }
        private void BuildMoon()
        {
            var d = WinterSkyCoordinates.Direction(3, 14);
            MoonDirection = new Vector3(-(float)d.east, (float)d.up, (float)d.south);
            background.SetVector("_MoonDirection", MoonDirection);
            // 0.65-degree illuminated diameter: slightly enlarged for VR. Placement is authored, not an ephemeris.
            float radius = Mathf.Tan(.325f * Mathf.Deg2Rad) / .63f;
            var right = Vector3.Cross(MoonDirection, Vector3.up).normalized * radius;
            var up = Vector3.Cross(right.normalized, MoonDirection) * radius;
            moonMesh = new Mesh { name = "WinterMoonDisc" };
            moonMesh.vertices = new[] { MoonDirection-right-up, MoonDirection+right-up, MoonDirection+right+up, MoonDirection-right+up };
            moonMesh.uv = new[] { Vector2.zero,Vector2.right,Vector2.one,Vector2.up };
            moonMesh.triangles = new[] { 0,1,2,0,2,3 }; moonMesh.bounds = new Bounds(Vector3.zero,Vector3.one*100000);
            moonMaterial = new Material(Resources.Load<Shader>("WinterMoon"));
            CreateMesh("WinterMoon", moonMesh).GetComponent<Renderer>().sharedMaterial = moonMaterial;
        }
        private GameObject CreateMesh(string name, Mesh mesh)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(root, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = points;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            return go;
        }
        private static readonly string[] GuidePaths = {
            "Orion|Betelgeuse,Bellatrix,Mintaka,Alnilam,Alnitak,Saiph,Rigel,Mintaka;Betelgeuse,Alnitak",
            "Canis Major|Mirzam,Sirius,Wezen,Adhara;Wezen,Aludra",
            "Canis Minor|Procyon,Gomeisa",
            "Taurus|Alcyone,Ain,Aldebaran,Elnath",
            "Ursa Major|Dubhe,Merak,Phecda,Megrez,Dubhe;Megrez,Alioth,Mizar,Alkaid",
            "Ursa Minor|Polaris,Yildun,Epsilon UMi,Zeta UMi,Eta UMi,Pherkad,Kochab,Zeta UMi",
            "Cassiopeia|Caph,Schedar,Cih,Ruchbah,Segin",
            "Auriga|Capella,Menkalinan,Elnath,Capella",
            "Gemini|Castor,Pollux"
        };
        private static readonly HashSet<string> LandmarkNames = new HashSet<string>(GuidePaths.SelectMany(p => p.Split('|')[1].Split(';')).SelectMany(p => p.Split(',')));
        private Mesh guideMesh;
        private Transform guideLabels;
        private void BuildGuides()
        {
            guides=new GameObject("ConstellationGuides");guides.transform.SetParent(root,false);
            var vertices=new List<Vector3>();var colors=new List<Color>();var uv=new List<Vector2>();var triangles=new List<int>();
            guideLabels=new GameObject("ConstellationNames").transform;guideLabels.SetParent(guides.transform,false);
            foreach(var constellation in GuidePaths) {
                var parts=constellation.Split('|');Vector3 center=Vector3.zero;int count=0;
                foreach(var chain in parts[1].Split(';')) {
                    var stars=chain.Split(',');
                    for(int n=0;n<stars.Length;n++)if(names.TryGetValue(stars[n],out var d)&&d.y>0){center+=d;count++;}
                    for(int n=1;n<stars.Length;n++) {
                        if(!names.TryGetValue(stars[n-1],out var a)||!names.TryGetValue(stars[n],out var b)||a.y<=0||b.y<=0)continue;
                        for(int j=0;j<8;j++) {
                            var from=Vector3.Slerp(a,b,j/8f);var to=Vector3.Slerp(a,b,(j+1)/8f);var width=Vector3.Cross(from,to).normalized*.00045f;int k=vertices.Count;
                            vertices.AddRange(new[]{from-width,to-width,to+width,from+width});uv.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});
                            colors.AddRange(Enumerable.Repeat(new Color(.20f,.42f,.60f,.5f),4));triangles.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});
                        }
                    }
                }
                if(count>0){
                    var direction=(center/count).normalized;
                    var canvas=garden.CanvasAt(parts[0],direction*80+Vector3.up*1.8f,new Vector2(650,70),.008f,guideLabels);
                    canvas.transform.rotation=Quaternion.LookRotation(direction);
                    garden.Text(canvas.transform,"Name",parts[0],Vector2.zero,new Vector2(650,70),28).color=new Color(.42f,.60f,.75f);
                }
            }
            guideMesh=new Mesh{name="ConstellationLines"};guideMesh.SetVertices(vertices);guideMesh.SetColors(colors);guideMesh.SetUVs(0,uv);guideMesh.SetTriangles(triangles,0);guideMesh.bounds=new Bounds(Vector3.zero,Vector3.one*100000);
            CreateMesh("GuideLines",guideMesh).transform.SetParent(guides.transform,false);guides.SetActive(false);
        }
        private void LateUpdate(){if(guideLabels!=null&&garden?.Locomotion?.Head!=null)guideLabels.position=garden.Locomotion.Head.position;}
        private void BuildStars()
        {
            var vertices = new List<Vector3>(); var colors = new List<Color>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            var lines = Resources.Load<TextAsset>("WinterStars").text.Split('\n');
            foreach (var line in lines.Skip(1).Where(l => l.Length > 0)) {
                var c = line.Split(','); double ra = Parse(c[2]), dec = Parse(c[3]); float magnitude = (float)Parse(c[4]), ci = (float)Parse(c[5]);
                var d = WinterSkyCoordinates.Direction(ra, dec); var direction = new Vector3(-(float)d.east, (float)d.up, (float)d.south);
                string starName=c[1];
                if(c[0]=="81830")starName="Epsilon UMi";
                if(c[0]=="76819")starName="Zeta UMi";
                if(c[0]=="79580")starName="Eta UMi";
                if (starName.Length > 0) names[starName] = direction;
                if (direction.y <= 0 || (magnitude > MaximumVisibleMagnitude && !LandmarkNames.Contains(starName) && !(ra > 3.65 && ra < 3.88 && dec > 23 && dec < 25))) continue;
                float radius = Mathf.Lerp(.075f, .21f, Mathf.InverseLerp(6, -1.5f, magnitude)) * Mathf.Deg2Rad;
                var right = Vector3.Cross(direction, Vector3.up).normalized * radius; var up = Vector3.Cross(right.normalized, direction) * radius;
                var color = Color.Lerp(new Color(.66f,.80f,1), new Color(1,.64f,.36f), Mathf.InverseLerp(-.3f,1.8f,ci));
                float visibility = Mathf.SmoothStep(0,1, direction.y / .12f);
                color.a = Mathf.Lerp(.25f, 3.4f, Mathf.Pow(Mathf.InverseLerp(6, -1.5f, magnitude), 1.7f)) * visibility * 1.8f;
                int index = vertices.Count;
                vertices.Add(direction-right-up); vertices.Add(direction+right-up); vertices.Add(direction+right+up); vertices.Add(direction-right+up);
                uv.AddRange(new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up });
                colors.AddRange(new[] { color,color,color,color });
                triangles.AddRange(new[] { index,index+1,index+2,index,index+2,index+3 }); VisibleStarCount++;
            }
            starMesh = new Mesh { name = "HYGWinterStars" }; starMesh.SetVertices(vertices); starMesh.SetColors(colors); starMesh.SetUVs(0,uv); starMesh.SetTriangles(triangles,0);
            starMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000); CreateMesh("CatalogStars", starMesh);
        }
        private static double Parse(string value) => double.Parse(value, CultureInfo.InvariantCulture);
        public void ToggleGuides() { if (guides != null) guides.SetActive(!guides.activeSelf); }
        private GameObject guides;
        private bool Allowed => !paused && !unfocused && garden.Experience.Ready && !garden.Experience.Session.State.Paused &&
            !garden.M5.HelpOpen && !garden.M5.Comparison.PanelOpen && !garden.M5.Extensions.PanelOpen;
        private void Update() => AdvanceSky(Time.unscaledDeltaTime);
        public void AdvanceSky(float seconds)
        {
            if (garden == null || !float.IsFinite(seconds) || seconds < 0) return;
            if (!Allowed) { meteor.SetActive(false); meteorAge = 2; return; }
            if (schedule.Advance(seconds, true)) {
                // Brief sporadic events high in the sky, without a fictitious meteor-shower radiant.
                float azimuth = (float)random.NextDouble() * Mathf.PI * 2;
                float elevation = Mathf.Lerp(35,75,(float)random.NextDouble()) * Mathf.Deg2Rad;
                start = new Vector3(Mathf.Sin(azimuth)*Mathf.Cos(elevation),Mathf.Sin(elevation),Mathf.Cos(azimuth)*Mathf.Cos(elevation));
                var tangent = Vector3.Cross(start, Vector3.up).normalized;
                end = (start + tangent * .2f - Vector3.up * .1f).normalized; meteorAge = 0;
            } else meteorAge += seconds;
            meteor.SetActive(meteorAge < .85f);
            if (!meteor.activeSelf) return;
            float t = meteorAge / .85f;
            var head = Vector3.Slerp(start,end,t); var tail = Vector3.Slerp(start,end,Mathf.Max(0,t-.45f));
            var width = Vector3.Cross(head, end-start).normalized * .00065f;
            meteorMesh.vertices = new[] { tail-width,head-width,head+width,tail+width };
            meteorMesh.uv = new[] { Vector2.zero,Vector2.right,Vector2.one,Vector2.up };
            var color = new Color(.72f,.85f,1,1.8f*Mathf.Sin(Mathf.PI*t));
            meteorMesh.colors = new[] { new Color(0,0,0,0),color,color,new Color(0,0,0,0) };
            meteorMesh.triangles = new[] { 0,1,2,0,2,3 }; meteorMesh.bounds = new Bounds(Vector3.zero,Vector3.one*100000);
        }
        private void OnApplicationPause(bool value) { paused = value; }
        private void OnApplicationFocus(bool value) { unfocused = !value; }
        private void OnDestroy()
        {
            if (RenderSettings.skybox == background) RenderSettings.skybox = previousSky;
            foreach (var moon in hiddenMoons) if (moon != null) moon.SetActive(true);
            if(root != null) Destroy(root.gameObject);
            if(guideMesh != null) Destroy(guideMesh);
            if(starMesh != null) Destroy(starMesh); if(meteorMesh != null) Destroy(meteorMesh);
            if(moonMesh != null) Destroy(moonMesh); if(moonMaterial != null) Destroy(moonMaterial);
            if(background != null) Destroy(background); if(points != null) Destroy(points);
        }
    }
}
