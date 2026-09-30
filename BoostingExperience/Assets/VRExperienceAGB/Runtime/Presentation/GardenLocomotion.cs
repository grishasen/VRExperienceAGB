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
        private Vector3 entrancePosition;
        private Transform presentation;
        private Quaternion presentationRotation;
        private UnityEngine.UI.Image fade;
        private Coroutine fading;
        private bool stickReleased = true;
        private OneTreeExperience experience;
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
            presentation = view.presentationRoot; presentationRotation = presentation.rotation;
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
                Origin.position = gardenPosition;
                presentation.rotation = presentationRotation;
            }
            else
            {
                gardenPosition = Origin.position;
                Origin.position = entrancePosition;
                // Orient the teaching content to the observer's chosen reference direction, not their head.
                presentation.rotation = Origin.rotation * presentationRotation;
            }
            FadeFromBlack();
        }
        public void Teleport(Vector3 standingPoint)
        {
            if (!InGarden) return;
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
            if (!InGarden || (degrees != -30 && degrees != 30)) return;
            Origin.RotateAround(Head.position, Vector3.up, degrees);
            gardenPosition = Origin.position; FadeFromBlack();
        }
        private void Update()
        {
            if (!InGarden) return;
            var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if (left.TryGetFeatureValue(CommonUsages.primary2DAxis, out var walkAxis)) Move(walkAxis, Mathf.Min(Time.unscaledDeltaTime, .05f));
            var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (!right.TryGetFeatureValue(CommonUsages.primary2DAxis, out var axis)) return;
            if (Mathf.Abs(axis.x) < .25f) stickReleased = true;
            if (stickReleased && Mathf.Abs(axis.x) > .75f) { stickReleased = false; SnapTurn(Mathf.Sign(axis.x) * 30); }
        }
        public void Move(Vector2 axis, float seconds)
        {
            var garden = experience.Garden;
            if (!InGarden || garden == null || garden.M5.HelpOpen ||
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
            if (fade == null) return;
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
