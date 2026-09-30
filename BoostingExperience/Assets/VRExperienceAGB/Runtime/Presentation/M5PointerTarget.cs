using UnityEngine;
using UnityEngine.EventSystems;
using VRExperienceAGB.Application;

namespace VRExperienceAGB.Presentation
{
    public enum M5Action
    {
        Help, HelpNext, HelpPrevious, CloseHelp, Diorama, Forest, Smaller, Larger,
        RotateLeft, RotateRight, ResetView, SelectTree, PreviousTree, NextTree, EnterTree,
        FocusNode, FocusParent, FocusCurrent, Pause, Entrance, ToggleAudio, PreviousTablePage, NextTablePage
    }

    /// <summary>Rejects releases from a retired model, session, view, or node.</summary>
    public sealed class M5PointerTarget : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, IPointerExitHandler
    {
        public M5ForestPresentation presentation;
        public M5Action action;
        public int index;
        public string nodeId;
        private TreeSession pressed;
        private long viewRevision, sessionRevision, navigationRevision, ensembleRevision;
        private int pointer, pressedIndex;
        private string pressedNode;
        public void OnPointerDown(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || !GetComponent<UnityEngine.UI.Button>().IsInteractable()) return;
            pressed = presentation.View.Session;
            pointer = data.pointerId; pressedIndex = index; pressedNode = nodeId;
            viewRevision = presentation.Revision; sessionRevision = pressed.State.Revision;
            navigationRevision = presentation.Garden.Navigation?.Revision ?? 0;
            ensembleRevision = presentation.View.Ensemble.Revision;
        }
        public void OnPointerClick(PointerEventData data)
        {
            var session = pressed; pressed = null;
            if (session == null || !gameObject.activeInHierarchy || !GetComponent<UnityEngine.UI.Button>().IsInteractable() ||
                data.button != PointerEventData.InputButton.Left || pointer != data.pointerId || pressedIndex != index || pressedNode != nodeId ||
                session != presentation.View.Session || sessionRevision != session.State.Revision ||
                viewRevision != presentation.Revision || ensembleRevision != presentation.View.Ensemble.Revision ||
                navigationRevision != (presentation.Garden.Navigation?.Revision ?? 0)) return;
            presentation.Activate(action, index, nodeId); ControllerSelectionFeedback.Pulse(data.pointerId);
        }
        public void OnPointerExit(PointerEventData data) { pressed = null; }
        private void OnDisable() { pressed = null; }
    }
}
