using Liminal.SDK.Core;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Devices.Emulator;
using Liminal.SDK.VR.Devices.GearVR;
using Liminal.SDK.XR;
using UnityEngine;
using UnityEngine.XR;

namespace App
{
    public static class DeviceUtils
    {
        public static IVRDevice CreateDevice(ExperienceApp experienceApp = null)
        {
            experienceApp = experienceApp ?? Object.FindObjectOfType<ExperienceApp>();
            return CreateDevice(ESDKType.UnityXR);
        }

        public static IVRDevice CreateDevice(ESDKType sdkType)
        {
            switch (sdkType)
            {
#if UNITY_XR
                case ESDKType.UnityXR:
                    return new UnityXRDevice();
#endif
                case ESDKType.OVR:
                    XRSettings.enabled = true;
                    Debug.Log(XRSettings.enabled);
                    XRSettings.LoadDeviceByName("Oculus");
                    return new GearVRDevice();

                default:
                    return new EmulatorDevice(VREmulatorDevice.Daydream);
            }
        }
    }
}