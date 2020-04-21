using Liminal.SDK.Core;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Devices.Emulator;
using Liminal.SDK.VR.Devices.GearVR;
using Liminal.SDK.XR;
using UnityEngine;

namespace App
{
    public static class DeviceUtils
    {
        public static IVRDevice CreateDevice(ExperienceApp experienceApp = null)
        {
            experienceApp = experienceApp ?? Object.FindObjectOfType<ExperienceApp>();
            var sdkType = experienceApp.SDKType;
            return CreateDevice(sdkType);
        }

        public static IVRDevice CreateDevice(ESDKType sdkType)
        {
            switch (sdkType)
            {
                case ESDKType.Legacy:
                    return new GearVRDevice();

                case ESDKType.UnityXR:
                    return new UnityXRDevice();

                default:
                    return new EmulatorDevice(VREmulatorDevice.Daydream);
            }
        }
    }
}