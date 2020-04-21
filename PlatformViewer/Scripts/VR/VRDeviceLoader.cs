using System;
using Liminal.SDK.Core;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Devices.Emulator;
using Liminal.SDK.VR.Devices.GearVR;
using Liminal.SDK.XR;
using UnityEngine;
using UnityEngine.XR;
using Object = UnityEngine.Object;
using App;

namespace Liminal.Platform.Experimental.VR
{
    public class VRDeviceLoader : IVRDeviceInitializer
    {
        [Tooltip("The device to emulate during development. NOTE: You may need to change Player Settings to correctly emulate some devices.")]
        [SerializeField] private VREmulatorDevice m_EmulatorDevice = VREmulatorDevice.Daydream;

        #region Properties

        /// <summary>
        /// Gets the device type the emulator will attempt emulate.
        /// </summary>
        public VREmulatorDevice EmulatorDevice
        {
            get { return m_EmulatorDevice; }
        }

        #endregion

        public VRDeviceLoader()
        {
            if (VRDevice.Device == null)
            {
                var device = CreateDevice();
                VRDevice.Initialize(device);
            }
        }

        public IVRDevice CreateDevice()
        {
            XRSettings.enabled = true;
            return DeviceUtils.CreateDevice();
        }

        private static IVRDevice FindConnectedDeviceByModel(string modelName)
        {
            if (string.IsNullOrEmpty(modelName))
                return null;

            throw new NotImplementedException("Device support is not implemented yet");
        }
    }
}

namespace App
{
    public static class DeviceUtils
    {
        public static IVRDevice CreateDevice(ExperienceApp experienceApp = null)
        {
            experienceApp = experienceApp ?? Object.FindObjectOfType<ExperienceApp>();
            var sdkType = experienceApp.SDKType;

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