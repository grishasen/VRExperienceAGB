using System.Collections.Generic;
using TMPro;
using VRExperienceAGB.Application;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VRExperienceAGB.Presentation
{
    public enum TreeAction { TrueBranch, FalseBranch, Step, Play, Pause, Back, Restart, Overview, Manual, Profile, NextProfile, Seated, DeepExample, TreeMap, PreviousTree, NextTree, EditProfile, NextFeature, DecreaseValue, IncreaseValue, RestoreProfile, CloseEdit, Menu, InspectNodes, NextNode, CloseInspect, CancelEdit, SetMissing, TourDetail, GroupRemaining, Result, NextLedgerPage, CloseResult, ReviseChoice, ContinueFree, LargerEnsemble }

    /// <summary>Captures the decision at press time so delayed releases cannot act on a different node.</summary>
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public sealed class TreeActionButton : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public OneTreeExperience experience;
        public TreeAction action;
        public TMP_Text label;
        private readonly Dictionary<int, (TreeSession session, long revision, string node, long ensembleRevision)> presses = new Dictionary<int, (TreeSession, long, string, long)>();
        private UnityEngine.UI.Button button;
        private Vector3 originalScale;
        private void Awake() { button = GetComponent<UnityEngine.UI.Button>(); originalScale = transform.localScale; }
        public void OnPointerDown(PointerEventData data)
        {
            if (button.IsInteractable() && data.button == PointerEventData.InputButton.Left && experience.Session != null)
            { var state = experience.Session.State; presses[data.pointerId] = (experience.Session, state.Revision, state.NodeId, experience.Ensemble.Revision); }
        }
        public void OnPointerClick(PointerEventData data)
        {
            if (!presses.TryGetValue(data.pointerId, out var press)) return;
            presses.Remove(data.pointerId);
            if (ReferenceEquals(press.session, experience.Session) && experience.Session.State.Revision == press.revision && experience.Ensemble.Revision == press.ensembleRevision && button.IsInteractable() && data.button == PointerEventData.InputButton.Left)
                {
                experience.Execute(action, press.revision, press.node);
                ControllerSelectionFeedback.Pulse(data.pointerId);
            }
        }
        public void OnPointerEnter(PointerEventData data) { if (button.IsInteractable()) transform.localScale = originalScale * 1.035f; }
        public void OnPointerExit(PointerEventData data) { transform.localScale = originalScale; presses.Remove(data.pointerId); }
        private void OnDisable() { presses.Clear(); if (originalScale != Vector3.zero) transform.localScale = originalScale; }
    }
}
