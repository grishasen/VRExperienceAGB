using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VRExperienceAGB.Presentation
{
    /// <summary>Original distant wolf silhouettes and occasional positional howls, independent of model evaluation.</summary>
    public sealed class HorizonWolves : MonoBehaviour
    {
        private ForestGardenView garden;
        private Transform root;
        private readonly List<Transform> wolves = new List<Transform>();
        private readonly List<Transform> heads = new List<Transform>();
        private readonly List<AudioSource> voices = new List<AudioSource>();
        private readonly List<Mesh> meshes = new List<Mesh>();
        private Material fur, rock;
        private AudioClip howl;
        private readonly List<Vector3> restingPositions = new List<Vector3>();
        private readonly List<Quaternion> restingRotations = new List<Quaternion>();
        private readonly List<Transform> tails = new List<Transform>();
        private Texture2D furTexture;
        public bool Visible => wolves.Exists(w => w.gameObject.activeSelf);
        private readonly System.Random random = new System.Random(73031);
        private bool soundEnabled = true, paused, unfocused, wasAllowed;
        private float remaining = 9f, callingTime;
        private int calling = -1, lastCaller = -1, treeCount = -1;
        public IReadOnlyList<Transform> Wolves => wolves.AsReadOnly();
        public IReadOnlyList<AudioSource> Voices => voices.AsReadOnly();
        public AudioClip HowlClip => howl;
        public int LastCaller => lastCaller;
        public bool SoundEnabled => soundEnabled;
        public float SecondsUntilHowl => remaining;

        public void Configure(ForestGardenView owner)
        {
            if (root != null) return;
            garden = owner;
            root = new GameObject("HorizonWildlife").transform; root.SetParent(owner.transform, false);
            fur = new Material(owner.stoneMaterial) { name = "OriginalWolfSlateFur", enableInstancing = true };
            fur.color = new Color(.60f, .64f, .68f);
            furTexture = new Texture2D(128,128,TextureFormat.RGB24,false);
            var pixels = new Color[128*128];var textureRandom=new System.Random(735);
            for(int y=0;y<128;y++)for(int x=0;x<128;x++) {
                float strand=(float)textureRandom.NextDouble()*.20f+Mathf.PerlinNoise(x*.15f,y*.035f)*.45f;
                pixels[y*128+x]=Color.Lerp(new Color(.20f,.23f,.25f),new Color(.72f,.70f,.64f),strand);
            }
            furTexture.SetPixels(pixels);furTexture.Apply();fur.mainTexture=furTexture;
            fur.SetFloat("_Smoothness",.05f);
            fur.EnableKeyword("_EMISSION"); fur.SetColor("_EmissionColor", new Color(.035f, .045f, .065f));
            rock = new Material(owner.stoneMaterial) { name = "OriginalWolfRidge", enableInstancing = true };
            rock.color = new Color(.12f, .17f, .23f);
            rock.DisableKeyword("_EMISSION");
            var ridge = new Shape();
            ridge.Ridge();
            var ridgeMesh = Keep(ridge.Mesh("WolfHorizonRidge"));
            MeshObject("WesternRidge", root, ridgeMesh, rock).transform.localPosition = new Vector3(-25, 0, 51);
            MeshObject("EasternRidge", root, ridgeMesh, rock).transform.localPosition = new Vector3(28, .5f, 59);
            var body = Keep(Body()); var head = Keep(Head());
            var points = new[] { new Vector3(-25, 7.7f, 51), new Vector3(-29, 7.7f, 52), new Vector3(28, 8.2f, 59) };
            for (int i = 0; i < points.Length; i++)
            {
                var wolf = MeshObject("HorizonWolf-" + (i + 1), root, body, fur).transform;
                wolf.localPosition = points[i]; wolf.localScale = Vector3.one * (i == 0 ? 1.15f : 1.0f);
                wolf.localRotation = Quaternion.Euler(0, i == 2 ? 155 : -15 + i * 25, 0);
                wolves.Add(wolf);restingPositions.Add(wolf.localPosition);restingRotations.Add(wolf.localRotation);
                var tail=new GameObject("AnimatedTail").transform;tail.SetParent(wolf,false);tail.localPosition=new Vector3(-.69f,1.05f,0);
                var tailShape=new Shape();tailShape.Bone(Vector3.zero,new Vector3(-.42f,-.25f,0),.14f,.18f);tailShape.Bone(new Vector3(-.42f,-.25f,0),new Vector3(-.61f,-.76f,0),.18f,.02f);
                MeshObject("BushyTail",tail,Keep(tailShape.Mesh("WolfTail")),fur);tails.Add(tail);
                var tufts=new Shape();var rng=new System.Random(203+i);
                for(int t=0;t<160;t++){
                    float x=-.70f+(float)rng.NextDouble()*1.35f, angle=(float)rng.NextDouble()*Mathf.PI*2;
                    var surface=new Vector3(x,1.06f+Mathf.Sin(angle)*.29f,Mathf.Cos(angle)*.26f);
                    var normal=new Vector3(-.4f,Mathf.Sin(angle),Mathf.Cos(angle)).normalized;
                    tufts.Tetrahedron(surface+Vector3.right*.04f,surface+Vector3.up*.035f,surface-Vector3.up*.035f,surface+normal*(.09f+(float)rng.NextDouble()*.09f));
                }
                MeshObject("FurTufts",wolf,Keep(tufts.Mesh("WolfFurTufts")),fur);
                wolf.gameObject.SetActive(false);
                var neck = new GameObject("HowlingHead").transform; neck.SetParent(wolf, false);
                neck.localPosition = new Vector3(.60f, 1.18f, 0); heads.Add(neck);
                MeshObject("HeadAndEars", neck, head, fur);
                var mouth = new GameObject("DistantHowl"); mouth.transform.SetParent(neck, false);
                mouth.transform.localPosition = new Vector3(.42f, .73f, 0);
                var source = mouth.AddComponent<AudioSource>(); source.playOnAwake = false; source.loop = false;
                source.spatialBlend = 1; source.dopplerLevel = 0; source.spread = 12;
                source.rolloffMode = AudioRolloffMode.Linear; source.minDistance = 12; source.maxDistance = 150;
                source.volume = .24f; source.pitch = 1f;
                source.priority = 200; voices.Add(source);
            }
            howl = Resources.Load<AudioClip>("WolfHowl");
            if (howl == null) Debug.LogError("The bundled WolfHowl recording is missing.");
            foreach (var voice in voices) voice.clip = howl;
        }
        private Mesh Keep(Mesh mesh) { meshes.Add(mesh); return mesh; }
        private static GameObject MeshObject(string name, Transform parent, Mesh mesh, Material material)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            return go;
        }
        public void SetSoundEnabled(bool value)
        {
            if (soundEnabled == value) return;
            soundEnabled = value;
            foreach (var voice in voices) voice.mute = !value;
            Silence(); remaining = 12f; wasAllowed = false;
        }
        private bool Allowed => howl != null && garden != null && garden.Experience.Ready && soundEnabled && !paused && !unfocused &&
            !garden.M5.HelpOpen && garden.M5.Comparison?.PanelOpen != true && garden.M5.Extensions?.PanelOpen != true && !garden.Experience.Session.State.Paused;
        private void Update() { AdvanceAmbience(Time.unscaledDeltaTime); }
        public void AdvanceAmbience(float seconds)
        {
            if (garden == null || !float.IsFinite(seconds) || seconds < 0) return;
            int count = garden.Experience.Model?.Trees.Count ?? 0;
            if (count != treeCount)
            {
                treeCount = count;
                // Keep the skyline beyond the authored garden even when the model has more beds.
                root.localPosition = new Vector3(0, 0, Mathf.Max(0, ((count + 9) / 10) * 4.8f - 24));
            }
            if (!Allowed)
            {
                if (wasAllowed || calling >= 0) Silence();
                wasAllowed = false; return;
            }
            wasAllowed = true;
            if (calling >= 0)
            {
                callingTime += seconds;
                float length = howl.length + 2.4f;
                var wolf=wolves[calling];
                float enter=Mathf.Clamp01(callingTime/.7f),leave=Mathf.Clamp01((callingTime-howl.length-.5f)/1.9f);
                wolf.localPosition=restingPositions[calling]+new Vector3(-.6f*(1-enter)+leave*.9f,-.3f*(1-enter)-leave*2.7f,leave*1.4f);
                wolf.localRotation=restingRotations[calling]*Quaternion.Euler(Mathf.Sin(callingTime*3)*1.2f,leave*35,Mathf.Sin(callingTime*2)*1.0f);
                tails[calling].localRotation=Quaternion.Euler(0,Mathf.Sin(callingTime*2.1f)*12,0);
                float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(callingTime / length));
                heads[calling].localRotation = Quaternion.Euler(0, 0, -22+envelope*22);
                if (callingTime >= length) { heads[calling].localRotation = Quaternion.identity; wolves[calling].gameObject.SetActive(false); calling = -1; }
            }
            remaining -= seconds;
            if (remaining > 0) return;
            int next = (lastCaller + 1 + random.Next(2)) % voices.Count;
            if (next == lastCaller) next = (next + 1) % voices.Count;
            Silence(); calling = lastCaller = next; callingTime = 0;
            wolves[next].gameObject.SetActive(true);
            voices[next].Play(); remaining = 32 + (float)random.NextDouble() * 22;
        }
        private void Silence()
        {
            foreach (var voice in voices) if (voice != null) voice.Stop();
            foreach (var head in heads) if (head != null) head.localRotation = Quaternion.identity;
            foreach(var wolf in wolves) if(wolf!=null)wolf.gameObject.SetActive(false);
            calling = -1; callingTime = 0;
        }
        private void OnApplicationPause(bool value) { paused = value; if (value) { Silence(); remaining = 12; } }
        private void OnApplicationFocus(bool value) { unfocused = !value; if (!value) { Silence(); remaining = 12; } }
        private void OnDisable() { Silence(); }
        private void OnDestroy()
        {
            if (root != null) Destroy(root.gameObject);
            if (fur != null) Destroy(fur);
            if (furTexture != null) Destroy(furTexture);
            if (rock != null) Destroy(rock);
            foreach (var mesh in meshes) if (mesh != null) Destroy(mesh);
        }

        private static Mesh Body()
        {
            var s = new Shape();
            s.Ellipsoid(new Vector3(-.05f, 1.03f, 0), new Vector3(.79f, .35f, .29f), Quaternion.identity);
            s.Ellipsoid(new Vector3(.43f, 1.20f, 0), new Vector3(.33f, .43f, .32f), Quaternion.Euler(0, 0, -20));
            s.Ellipsoid(new Vector3(-.59f, .95f, 0), new Vector3(.31f, .33f, .28f), Quaternion.identity);
            // Straight forelegs and bent hocks give a canine silhouette from both sides.
            foreach (float z in new[] { -.20f, .20f })
            {
                s.Bone(new Vector3(.47f, 1.06f, z), new Vector3(.49f, .18f, z), .10f, .065f);
                s.Ellipsoid(new Vector3(.56f, .095f, z), new Vector3(.18f, .095f, .105f), Quaternion.identity, 4, 7);
                s.Bone(new Vector3(-.55f, .98f, z), new Vector3(-.37f, .55f, z), .16f, .10f);
                s.Bone(new Vector3(-.37f, .55f, z), new Vector3(-.69f, .27f, z), .10f, .065f);
                s.Bone(new Vector3(-.69f, .27f, z), new Vector3(-.62f, .10f, z), .065f, .055f);
                s.Ellipsoid(new Vector3(-.55f, .075f, z), new Vector3(.16f, .075f, .10f), Quaternion.identity, 4, 7);
            }
            return s.Mesh("OriginalLowPolyWolfBody");
        }
        private static Mesh Head()
        {
            var s = new Shape();
            s.Ellipsoid(new Vector3(.03f, .18f, 0), new Vector3(.28f, .43f, .25f), Quaternion.Euler(0, 0, -25));
            s.Ellipsoid(new Vector3(.20f, .48f, 0), new Vector3(.28f, .25f, .23f), Quaternion.Euler(0, 0, 35));
            s.Bone(new Vector3(.34f, .56f, 0), new Vector3(.53f, .82f, 0), .145f, .073f);
            s.Bone(new Vector3(.31f, .48f, 0), new Vector3(.50f, .69f, 0), .10f, .055f);
            foreach (float z in new[] { -.18f, .18f })
                s.Tetrahedron(new Vector3(-.03f, .52f, z - .08f), new Vector3(.16f, .57f, z),
                    new Vector3(-.02f, .53f, z + .08f), new Vector3(-.08f, .89f, z));
            return s.Mesh("OriginalLowPolyWolfHead");
        }
        private sealed class Shape
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<int> triangles = new List<int>();
            private void Triangle(Vector3 a, Vector3 b, Vector3 c)
            { int i = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c); triangles.Add(i); triangles.Add(i + 1); triangles.Add(i + 2); }
            public void Tetrahedron(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            { Triangle(a, c, b); Triangle(a, b, d); Triangle(b, c, d); Triangle(c, a, d); }
            public void Ridge()
            {
                const int sides = 11;
                Vector3 Point(int ring, int side)
                {
                    float angle = 2 * Mathf.PI * side / sides;
                    float radiusX = ring == 0 ? 7.4f : ring == 1 ? 10.5f : 13.2f;
                    float radiusZ = ring == 0 ? 4.8f : ring == 1 ? 6.5f : 8f;
                    float y = ring == 0 ? 7.7f : ring == 1 ? 3.8f : -2f;
                    float irregular = 1 + .10f * Mathf.Sin(angle * 3 + ring);
                    return new Vector3(Mathf.Cos(angle) * radiusX * irregular, y, Mathf.Sin(angle) * radiusZ * irregular);
                }
                for (int side = 0; side < sides; side++)
                {
                    Triangle(new Vector3(0, 7.7f, 0), Point(0, side + 1), Point(0, side));
                    for (int ring = 0; ring < 2; ring++)
                    {
                        var a = Point(ring, side); var b = Point(ring, side + 1);
                        var c = Point(ring + 1, side); var d = Point(ring + 1, side + 1);
                        Triangle(a, b, c); Triangle(b, d, c);
                    }
                }
            }
            public void Ellipsoid(Vector3 center, Vector3 scale, Quaternion rotation, int rings = 6, int sides = 9)
            {
                Vector3 Point(int r, int side)
                {
                    float latitude = Mathf.PI * r / rings, longitude = 2 * Mathf.PI * side / sides;
                    return center + rotation * Vector3.Scale(new Vector3(Mathf.Sin(latitude) * Mathf.Cos(longitude), Mathf.Cos(latitude), Mathf.Sin(latitude) * Mathf.Sin(longitude)), scale);
                }
                for (int r = 0; r < rings; r++) for (int side = 0; side < sides; side++)
                {
                    var a = Point(r, side); var b = Point(r, side + 1); var c = Point(r + 1, side); var d = Point(r + 1, side + 1);
                    if (r > 0) Triangle(a, b, c);
                    if (r < rings - 1) Triangle(b, d, c);
                }
            }
            public void Bone(Vector3 from, Vector3 to, float start, float end)
            {
                var rotation = Quaternion.FromToRotation(Vector3.up, (to - from).normalized);
                const int sides = 8;
                for (int i = 0; i < sides; i++)
                {
                    Vector3 Direction(int j) => rotation * new Vector3(Mathf.Cos(j * 2 * Mathf.PI / sides), 0, Mathf.Sin(j * 2 * Mathf.PI / sides));
                    var a = from + Direction(i) * start; var b = from + Direction(i + 1) * start;
                    var c = to + Direction(i) * end; var d = to + Direction(i + 1) * end;
                    Triangle(a, c, b); Triangle(b, c, d); Triangle(from, a, b); Triangle(to, d, c);
                }
            }
            public Mesh Mesh(string name)
            {
                var mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
                mesh.uv = vertices.ConvertAll(v => new Vector2(v.x * .8f, v.y * .8f + v.z * .4f)).ToArray();
                mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
            }
        }
    }
}
