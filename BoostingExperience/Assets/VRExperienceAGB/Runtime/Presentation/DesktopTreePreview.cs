using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace VRExperienceAGB.Presentation
{
    /// <summary>Explicit Editor-only preview. Player builds always retain the tracked rig.</summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class DesktopTreePreview : MonoBehaviour
    {
        public bool editorPreview = true;
        public GameObject trackedRig;
        public Camera previewCamera;
        public Canvas[] canvases;
        private GameObject hovered;
        private GameObject pressed;
        private readonly List<RaycastResult> hits = new List<RaycastResult>();
        private bool preview;
        private void Awake()
        {
#if UNITY_EDITOR
            preview = editorPreview;
#endif
            if (trackedRig != null) trackedRig.SetActive(!preview);
            if (previewCamera != null) previewCamera.gameObject.SetActive(preview);
            if (preview && canvases != null) foreach (var canvas in canvases) canvas.worldCamera = previewCamera;
        }
        private void Update()
        {
            if (!preview || Mouse.current == null || EventSystem.current == null) return;
            var pointer = new PointerEventData(EventSystem.current) { pointerId = -1, position = Mouse.current.position.ReadValue(), button = PointerEventData.InputButton.Left };
            hits.Clear(); EventSystem.current.RaycastAll(pointer, hits);
            var target = hits.Count == 0 ? null : ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
            var hoverTarget = hits.Count == 0 ? null : ExecuteEvents.GetEventHandler<IPointerEnterHandler>(hits[0].gameObject);
            if (hovered != hoverTarget)
            {
                if (hovered != null) ExecuteEvents.Execute(hovered, pointer, ExecuteEvents.pointerExitHandler);
                hovered = hoverTarget;
                if (hovered != null) ExecuteEvents.Execute(hovered, pointer, ExecuteEvents.pointerEnterHandler);
            }
            var wheel = Mouse.current.scroll.ReadValue();
            if (wheel != Vector2.zero && hits.Count > 0)
            {
                pointer.scrollDelta = wheel / 120f;
                ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.scrollHandler);
            }
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                pressed = target;
                if (pressed != null) ExecuteEvents.Execute(pressed, pointer, ExecuteEvents.pointerDownHandler);
            }
            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                if (pressed != null)
                {
                    ExecuteEvents.Execute(pressed, pointer, ExecuteEvents.pointerUpHandler);
                    if (pressed == target) ExecuteEvents.Execute(pressed, pointer, ExecuteEvents.pointerClickHandler);
                }
                pressed = null;
            }
        }
    }
}
