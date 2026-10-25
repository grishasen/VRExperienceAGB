using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace VRExperienceAGB.Presentation
{
    /// <summary>Mouse observer for Editor previews and the standalone macOS player.</summary>
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
        public bool Active => preview;
        private void Awake()
        {
#if UNITY_EDITOR
            preview = editorPreview;
#elif UNITY_STANDALONE_OSX
            preview = true;
#endif
            if (trackedRig != null) trackedRig.SetActive(!preview);
            if (previewCamera != null) previewCamera.gameObject.SetActive(preview);
            if (preview && canvases != null) foreach (var canvas in canvases) canvas.worldCamera = previewCamera;
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            if (preview) gameObject.AddComponent<DesktopExperienceInput>();
#endif
        }
        private void Update()
        {
            if (!preview || !UnityEngine.Application.isFocused || Mouse.current == null || EventSystem.current == null) return;
            if (Mouse.current.rightButton.isPressed)
            {
                // Explicit mouse look only; the Quest player never enters this path.
                var delta=Mouse.current.delta.ReadValue();var angles=previewCamera.transform.localEulerAngles;
                float pitch=angles.x>180?angles.x-360:angles.x;
                previewCamera.transform.localRotation=Quaternion.Euler(Mathf.Clamp(pitch-delta.y*.12f,-65,65),angles.y+delta.x*.12f,0);
            }
            var pointer = new PointerEventData(EventSystem.current) { pointerId = -1, position = Mouse.current.position.ReadValue(), button = PointerEventData.InputButton.Left };
            if (Mouse.current.rightButton.isPressed) { CancelPointer(pointer); return; }
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
        private void CancelPointer(PointerEventData pointer)
        {
            if (pressed != null) ExecuteEvents.Execute(pressed, pointer, ExecuteEvents.pointerUpHandler);
            if (hovered != null) ExecuteEvents.Execute(hovered, pointer, ExecuteEvents.pointerExitHandler);
            pressed = hovered = null;
        }
        private void OnApplicationFocus(bool focused)
        {
            if (!focused && EventSystem.current != null) CancelPointer(new PointerEventData(EventSystem.current) { pointerId = -1 });
        }
    }
}
