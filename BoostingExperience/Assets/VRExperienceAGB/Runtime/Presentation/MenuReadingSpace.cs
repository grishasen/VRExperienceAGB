using System.Collections.Generic;
using UnityEngine;

namespace VRExperienceAGB.Presentation
{
    /// <summary>Temporarily suppress competing model surfaces without changing navigation or session state.</summary>
    internal sealed class MenuReadingSpace
    {
        private readonly Dictionary<Renderer, bool> renderers = new Dictionary<Renderer, bool>();
        private readonly Dictionary<Behaviour, bool> surfaces = new Dictionary<Behaviour, bool>();
        private readonly Dictionary<Collider, bool> colliders = new Dictionary<Collider, bool>();
        private long scannedRevision=-1;
        public void Hide(Transform scene, Transform menu, long revision)
        {
            if(scannedRevision!=revision) {
            scannedRevision=revision;
            foreach (var r in scene.GetComponentsInChildren<Renderer>(true))
                if (!r.transform.IsChildOf(menu)) { if (!renderers.ContainsKey(r)) renderers.Add(r, r.enabled); r.enabled = false; }
            foreach (var c in scene.GetComponentsInChildren<Canvas>(true))
                if (!c.transform.IsChildOf(menu)) { if (!surfaces.ContainsKey(c)) surfaces.Add(c, c.enabled); c.enabled = false; }
            foreach (var c in scene.GetComponentsInChildren<Collider>(true))
                if (!c.transform.IsChildOf(menu)) { if (!colliders.ContainsKey(c)) colliders.Add(c, c.enabled); c.enabled = false; }
            }
            foreach(var item in renderers)if(item.Key!=null && item.Key.enabled)item.Key.enabled=false;
            foreach(var item in surfaces)if(item.Key!=null && item.Key.enabled)item.Key.enabled=false;
            foreach(var item in colliders)if(item.Key!=null && item.Key.enabled)item.Key.enabled=false;
        }
        public void Restore()
        {
            foreach (var item in renderers) if (item.Key != null) item.Key.enabled = item.Value;
            foreach (var item in surfaces) if (item.Key != null) item.Key.enabled = item.Value;
            foreach (var item in colliders) if (item.Key != null) item.Key.enabled = item.Value;
            renderers.Clear(); surfaces.Clear(); colliders.Clear();scannedRevision=-1;
        }
    }
}
