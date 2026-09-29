using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.XR;

namespace VRExperienceAGB.Presentation
{
    /// <summary>Resolves the actual canvas pointer to its controller; desktop/hand input stays silent.</summary>
    public static class ControllerSelectionFeedback
    {
        public static void Pulse(int pointerId)
        {
            foreach (var ray in Object.FindObjectsByType<RayInteractor>())
            {
                if (!ray.isActiveAndEnabled || ray.Identifier != pointerId) continue;
                var controller = ray.GetComponentInParent<ControllerRef>();
                if (controller == null || !controller.IsConnected) return;
                var device = InputDevices.GetDeviceAtXRNode(controller.Handedness == Handedness.Left ? XRNode.LeftHand : XRNode.RightHand);
                if (device.isValid && device.TryGetHapticCapabilities(out var capabilities) && capabilities.supportsImpulse)
                    device.SendHapticImpulse(0, .25f, .04f);
                return;
            }
        }
    }
}
