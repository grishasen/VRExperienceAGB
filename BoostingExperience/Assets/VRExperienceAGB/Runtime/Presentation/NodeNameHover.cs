using UnityEngine;
using UnityEngine.EventSystems;

namespace VRExperienceAGB.Presentation
{
    /// <summary>Uses the same pointer events as mouse and Interaction SDK canvas rays.</summary>
    public sealed class NodeNameHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public OneTreeExperience experience;
        public TreeNodeView node;
        public void OnPointerClick(PointerEventData data) { if(data.button==PointerEventData.InputButton.Left) { OnPointerEnter(data); experience.NameTooltip.TogglePin(); } }
        public void OnPointerEnter(PointerEventData data)
        {
            if (experience.Ready && !experience.Session.State.Overview)
                {
                string id=node == null ? experience.Session.State.NodeId : node.nodeId;
                experience.Garden?.M6?.Point(this,id);
                experience.NameTooltip.ShowNode(this,id);
            }
        }
        public void OnPointerExit(PointerEventData data) { if (experience != null) { experience.NameTooltip?.Leave(this); experience.Garden?.M6?.Leave(this); } }
        private void OnDisable() { if (experience != null) { experience.NameTooltip?.Leave(this); experience.Garden?.M6?.Leave(this); } }
    }
}
