using System.Collections.Generic;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Input;

namespace Liminal.SDK.OpenVR
{
    public class OpenVRDevice : IVRDevice
    {
        private static readonly VRDeviceCapability _capabilities =
            VRDeviceCapability.Controller | VRDeviceCapability.DualController |
            VRDeviceCapability.UserPrescenceDetection;

        public string Name => "OpenVR";
        public int InputDeviceCount => 3;

        public IVRHeadset Headset => new SimpleHeadset("", VRHeadsetCapability.PositionalTracking);

        public IEnumerable<IVRInputDevice> InputDevices { get; }
        public IVRInputDevice PrimaryInputDevice { get; }
        public IVRInputDevice SecondaryInputDevice { get; }

        public event VRInputDeviceEventHandler InputDeviceConnected;
        public event VRInputDeviceEventHandler InputDeviceDisconnected;
        public event VRDeviceEventHandler PrimaryInputDeviceChanged;

        public int CpuLevel { get; set; }
        public int GpuLevel { get; set; }

        public OpenVRDevice()
        {
            PrimaryInputDevice = new OpenVRController(VRInputDeviceHand.Right);
            SecondaryInputDevice = new OpenVRController(VRInputDeviceHand.Left);
        }

        public bool HasCapabilities(VRDeviceCapability capabilities) => ((_capabilities & capabilities) == capabilities);

        public void SetupAvatar(IVRAvatar avatar)
        {

        }

        public void Update()
        {
        }
    }
}