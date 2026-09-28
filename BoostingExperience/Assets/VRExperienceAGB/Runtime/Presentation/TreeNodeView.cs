using TMPro;
using UnityEngine;

namespace VRExperienceAGB.Presentation
{
    public sealed class TreeNodeView : MonoBehaviour
    {
        public string nodeId;
        public Renderer platform;
        public TMP_Text title;
        public TMP_Text marker;
        public Transform dropAnchor;
    }
}
