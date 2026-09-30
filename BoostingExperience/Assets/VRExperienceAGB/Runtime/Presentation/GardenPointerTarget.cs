using UnityEngine;
using UnityEngine.EventSystems;
using VRExperienceAGB.Application;

namespace VRExperienceAGB.Presentation
{
    public enum GardenCommand { SelectTree, Enter, Visit, PreviousTree, NextTree, PreviousBed, NextBed, Entrance, TurnLeft, TurnRight, Profile, Manual, Waypoint, FieldGuide }
    public sealed class GardenPointerTarget : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public ForestGardenView garden;
        public GardenCommand command;
        public int index;
        private EnsembleSession pressed;
        private int pointer, pressedIndex;
        private long pressedRevision;
        public void OnPointerDown(PointerEventData data)
        { if (garden.Visible && GetComponent<UnityEngine.UI.Button>().IsInteractable() && data.button == PointerEventData.InputButton.Left) { pressed=garden.Experience.Ensemble; pointer=data.pointerId; pressedIndex=index; pressedRevision=pressed.Revision; } }
        public void OnPointerClick(PointerEventData data)
        {
            if (pressed == null || pressed != garden.Experience.Ensemble || pointer != data.pointerId || pressedIndex != index || pressedRevision != pressed.Revision || data.button != PointerEventData.InputButton.Left) return;
            pressed=null;
            if (garden.Visible) { garden.Activate(command,index); ControllerSelectionFeedback.Pulse(data.pointerId); }
        }
        public void OnPointerEnter(PointerEventData data) { if (command == GardenCommand.SelectTree && garden.Visible) garden.Hover(index); }
        public void OnPointerExit(PointerEventData data) { pressed=null; if(command==GardenCommand.SelectTree) garden.ClearHover(index); }
        private void OnDisable() { pressed=null; if(garden!=null&&command==GardenCommand.SelectTree)garden.ClearHover(index); }
    }
}
