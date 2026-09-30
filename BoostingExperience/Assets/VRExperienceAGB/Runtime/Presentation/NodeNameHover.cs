using UnityEngine;
using UnityEngine.EventSystems;

namespace VRExperienceAGB.Presentation
{
    /// <summary>Uses the same pointer events as mouse and Interaction SDK canvas rays.</summary>
    public sealed class NodeNameHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public OneTreeExperience experience;
        public TreeNodeView node;
        public void OnPointerEnter(PointerEventData data)
        {
            if (experience.Ready && !experience.Session.State.Overview)
                experience.NameTooltip.Show(this, experience.FullNodeName(node == null ? experience.Session.State.NodeId : node.nodeId));
        }
        public void OnPointerExit(PointerEventData data) { if (experience != null) experience.NameTooltip?.Leave(this); }
        private void OnDisable() { if (experience != null) experience.NameTooltip?.Leave(this); }
    }
}
