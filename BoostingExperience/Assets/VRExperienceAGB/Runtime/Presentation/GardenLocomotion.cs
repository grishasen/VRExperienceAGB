using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

namespace VRExperienceAGB.Presentation
{
    /// <summary>Deliberate origin teleports and snap turns. Never writes tracked eye/controller transforms.</summary>
    public sealed class GardenLocomotion : MonoBehaviour
    {
        public Transform Origin { get; private set; }
        public Transform Head { get; private set; }
        public bool InGarden { get; private set; }
        private Vector3 gardenPosition;
        private Quaternion gardenRotation;
        private Vector3 entrancePosition;
        private Transform presentation;
        private Quaternion presentationRotation;
        private Vector3 presentationPosition;
        private UnityEngine.UI.Image fade;
        private Coroutine fading;
        private bool stickReleased = true;
        private OneTreeExperience experience;
        public bool WalkEnabled { get; set; } = true;
        public bool ComfortFadeEnabled { get; set; } = true;
        public bool InputBlocked => experience == null || experience.ApplicationSuspended || experience.Director?.MenuVisible == true ||
            experience.NameTooltip?.Pinned == true || experience.Garden?.M5?.HelpOpen == true ||
            experience.Garden?.M5?.Comparison.PanelOpen == true || experience.Garden?.M5?.Extensions.PanelOpen == true || experience.Garden?.M6?.StoryOpen == true;
        public float WalkSpeed { get; set; } = 1.35f;
        public void Configure(OneTreeExperience view)
        {
            if (Origin != null) return;
            experience = view;
            var preview = view.GetComponent<DesktopTreePreview>();
            Origin = new GameObject("GardenObserverOrigin").transform;
            if (preview.trackedRig != null) preview.trackedRig.transform.SetParent(Origin, true);
            preview.previewCamera.transform.SetParent(Origin, true);
            Head = preview.previewCamera.gameObject.activeInHierarchy ? preview.previewCamera.transform :
                (preview.trackedRig.GetComponentsInChildren<Camera>(true).FirstOrDefault(c=>c.name=="CenterEyeAnchor") ?? preview.trackedRig.GetComponentsInChildren<Camera>(true)[0]).transform;
            entrancePosition = Origin.position; gardenPosition = Origin.position;
            gardenRotation = Origin.rotation;
            presentation = view.presentationRoot; presentationRotation = presentation.rotation; presentationPosition=presentation.position;
            var canvasObject = new GameObject("ComfortFade", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(Head, false); canvasObject.transform.localPosition = new Vector3(0, 0, .25f);
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.sortingOrder = 32760;
            ((RectTransform)canvasObject.transform).sizeDelta = new Vector2(4, 4);
            var imageObject = new GameObject("Fade", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            imageObject.transform.SetParent(canvasObject.transform, false);
            var rect = (RectTransform)imageObject.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
            fade = imageObject.GetComponent<UnityEngine.UI.Image>(); fade.raycastTarget = false; fade.color = Color.clear;
        }
        public void SetGarden(bool garden)
        {
            if (InGarden == garden) return;
            InGarden = garden;
            if (garden)
            {
                Origin.SetPositionAndRotation(gardenPosition, gardenRotation);
                presentation.rotation = presentationRotation;
                presentation.position=new Vector3(presentationPosition.x,presentation.position.y,presentationPosition.z);
            }
            else
            {
                gardenPosition = Origin.position;
                gardenRotation = Origin.rotation;
                // Center the entire clearing once, like B, while retaining the forest return pose.
                CenterScene();
            }
            FadeFromBlack();
        }
        public void Recenter()
        {
            CenterScene();
            if (InGarden)
            {
                entrancePosition = gardenPosition = Origin.position;
                gardenRotation = Origin.rotation;
            }
            FadeFromBlack();
        }
        private void CenterScene()
        {
            // Move only the tracking origin; tracked local poses and observer height stay untouched.
            var yaw = Head.eulerAngles.y;
            Origin.RotateAround(Head.position, Vector3.up, -yaw);
            var shift = new Vector3(-Head.position.x, 0, -Head.position.z);
            Origin.position += shift;
            presentation.rotation = presentationRotation;
            presentation.position = new Vector3(presentationPosition.x,presentation.position.y,presentationPosition.z);
        }
        public void AdjustPlacement(float horizontal, float distance, float yaw)
        {
            // Explicit placement input transforms the tracking origin, never the tracked local pose.
            if (yaw != 0) Origin.RotateAround(Head.position, Vector3.up, -yaw);
            Origin.position -= Vector3.right * horizontal + Vector3.forward * distance;
            if (InGarden)
            {
                entrancePosition = gardenPosition = Origin.position;
                gardenRotation = Origin.rotation;
            }
            // Keep content anchored while explicit origin adjustments move the scene relative to the observer.
        }
        public void Teleport(Vector3 standingPoint)
        {
            if (!InGarden || InputBlocked) return;
            var offset = standingPoint - Head.position; offset.y = 0;
            Origin.position += offset; gardenPosition = Origin.position; FadeFromBlack();
        }
        public void ReturnToEntrance()
        {
            if (!InGarden) return;
            Origin.position = entrancePosition; gardenPosition = Origin.position; FadeFromBlack();
        }
        public void SnapTurn(float degrees)
        {
            if (!InGarden || InputBlocked || (degrees != -30 && degrees != 30)) return;
            Origin.RotateAround(Head.position, Vector3.up, degrees);
            gardenPosition = Origin.position; FadeFromBlack();
        }
        private void Update()
        {
            if (!InGarden || InputBlocked) return;
            var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if (left.TryGetFeatureValue(CommonUsages.primary2DAxis, out var walkAxis)) HandleLeftStick(walkAxis, Mathf.Min(Time.unscaledDeltaTime, .05f));
            var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (!right.TryGetFeatureValue(CommonUsages.primary2DAxis, out var axis)) return;
            if (Mathf.Abs(axis.x) < .25f) stickReleased = true;
            if (stickReleased && Mathf.Abs(axis.x) > .75f) { stickReleased = false; SnapTurn(Mathf.Sign(axis.x) * 30); }
        }
        public void HandleLeftStick(Vector2 axis, float seconds)
        {
            if (!InGarden || InputBlocked || !float.IsFinite(seconds) || seconds<=0 ||
                !float.IsFinite(axis.x) || !float.IsFinite(axis.y)) return;
            var garden=experience.Garden;
            if (garden.Navigation?.Page!=NavigationPage.Diorama) { Move(axis,seconds);return; }
            if (!garden.M5.Diorama.Visible) return;
            float turn=StickAxis(axis.x), distance=StickAxis(axis.y);
            if (turn==0 && distance==0) return;
            garden.Navigation.InvalidatePointerPresses();
            float dt=Mathf.Min(seconds,.1f);
            if (turn!=0) garden.M5.Diorama.Rotate(turn*45*dt);
            if (distance!=0) garden.M5.Diorama.MoveDistance(-distance*.65f*dt);
        }
        private static float StickAxis(float value) => Mathf.Abs(value)<=.2f ? 0 : Mathf.Sign(value)*(Mathf.Clamp01(Mathf.Abs(value))-.2f)/.8f;
        public void Move(Vector2 axis, float seconds)
        {
            var garden = experience.Garden;
            if (!WalkEnabled || !InGarden || InputBlocked || garden == null || garden.M5.HelpOpen || garden.M5.Comparison.PanelOpen || garden.M5.Extensions.PanelOpen ||
                (garden.simplifiedNavigation && garden.Navigation?.Page != NavigationPage.Forest) || seconds <= 0 ||
                !float.IsFinite(seconds) || !float.IsFinite(axis.x) || !float.IsFinite(axis.y) || !float.IsFinite(WalkSpeed)) return;
            float magnitude = Mathf.Clamp01(axis.magnitude);
            if (magnitude < .2f) return;
            var forward = Head.forward; forward.y = 0;
            if (forward.sqrMagnitude < .01f) forward = Origin.forward;
            forward.Normalize(); var right = Vector3.Cross(Vector3.up, forward);
            var direction = axis.normalized;
            var delta = (forward * direction.y + right * direction.x) * ((magnitude - .2f) / .8f) * Mathf.Clamp(WalkSpeed, 0, 2) * Mathf.Min(seconds, .1f);
            var desired = Head.position + delta;
            // Match the authored garden ground. Keep physical tracking and vertical head pose untouched.
            desired.x = Mathf.Clamp(desired.x, -20, 20);
            desired.z = Mathf.Clamp(desired.z, -4, ((garden.PlotCount + 9) / 10) * 4.8f + 6);
            delta = desired - Head.position; delta.y = 0;
            Origin.position += delta; gardenPosition = Origin.position;
        }
        private void FadeFromBlack()
        {
            if (fade == null || !ComfortFadeEnabled) return;
            if (fading != null) StopCoroutine(fading);
            fading = StartCoroutine(FadeOut());
        }
        private IEnumerator FadeOut()
        {
            fade.color = Color.black;
            for (float elapsed = 0; elapsed < .18f; elapsed += Time.unscaledDeltaTime)
            { fade.color = new Color(0,0,0,1 - elapsed/.18f); yield return null; }
            fade.color = Color.clear; fading = null;
        }
        private void OnDestroy()
        {
            if (Origin != null) Destroy(Origin.gameObject);
        }
    }
}
