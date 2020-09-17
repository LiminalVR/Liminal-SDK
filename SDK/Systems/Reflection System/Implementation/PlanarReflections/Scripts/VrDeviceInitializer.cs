using System;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Devices.DaydreamView;
using Liminal.SDK.VR.Devices.Emulator;
using Liminal.SDK.VR.Devices.GearVR;

namespace App
{
    /// <summary>
    /// Enables the user to use platform specific avatar and device
    /// </summary>
    public class VrDeviceInitializer
        : IVRDeviceInitializer
    {
        /// <summary>
        /// Setup platform specific device and avatar such as GearVR or Google Daydream
        /// </summary>
        /// <returns></returns>
        public IVRDevice Initialize(IVRAvatar avatar)
        {
            var device = CreateDevice();

            VRDevice.Initialize(device);
            device.SetupAvatar(avatar);

            return device;
        }

        /// <summary>
        /// Create and return a device for the specific Platform
        /// </summary>
        /// <returns></returns>
        public IVRDevice CreateDevice()
        {
            var deviceType = GetDeviceType();
            switch (deviceType)
            {
                case EVrDeviceType.Emulator:
                    return new EmulatorDevice(VREmulatorDevice.Daydream);

                case EVrDeviceType.GearVr:
                case EVrDeviceType.OculusGo:
                    return new GearVRDevice();

                case EVrDeviceType.Daydream:
                    return new DaydreamViewDevice();

                default:
                    throw new Exception($"No device found for {deviceType}");
            }
        }

        private EVrDeviceType GetDeviceType()
        {
#if UNITY_EDITOR
            return EVrDeviceType.Emulator;
#else
            return EVrDeviceType.OculusGo;
#endif
        }
    }
}