using Liminal.SDK.OpenVR;
using Liminal.SDK.VR.Input;
using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine;
using Liminal.SDK.VR.Avatars;

namespace Liminal.SDK.VR.Utils
{
    public static class ControllerUtils
    {
        public static Dictionary<OVRInput.Controller, DateTime> HapticLastUsedTable = new Dictionary<OVRInput.Controller, DateTime>();

        /// <summary>
        /// Send haptics to a device input / controller that supports haptic. Currently only support Meta Quest controllers. 
        /// </summary>
        /// <param name="device">The controller device such as the PrimaryHand</param>
        /// <param name="frequency">The speed of a vibration cycle.</param>
        /// <param name="amplitude">The strength of the vibration, this controls how strong it feels.</param>
        /// <param name="duration">The duration for how long this vibration go on for.</param>
        /// <returns></returns>
        public static Coroutine SendInputHaptics(this IVRInputDevice device, float frequency = 0.5f, float amplitude = 0.5F, float duration = 0.005F)
        {
            if (device == null)
                return null;

            var mask = device.Hand == VRInputDeviceHand.Left ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;

            if (!HapticLastUsedTable.ContainsKey(mask))
                HapticLastUsedTable.Add(mask, DateTime.Now + TimeSpan.FromSeconds(0.2f));

            var timeElapsed = DateTime.Now - HapticLastUsedTable[mask];
            if (timeElapsed.TotalSeconds < 0.1F)
                return null;
            
            return CoroutineService.Instance.StartCoroutine(Routine());
            
            IEnumerator Routine()
            {
                HapticLastUsedTable[mask] = DateTime.Now;

                OVRInput.SetControllerVibration(frequency, amplitude, mask);
                yield return new WaitForSecondsRealtime(duration);
                OVRInput.SetControllerVibration(0, 0, mask);
            }
        }
        public static void SetControllerVisibility(this IVRAvatarHand hand, bool state)
        {
            var renderers = hand.Transform.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in renderers)
                r.enabled = state;
        }
    }
}
