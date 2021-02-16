using System.Collections.Generic;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Input;

public class PicoVRDevice : SimpleVRDevice
{
}

public class SimpleVRDevice : IVRDevice
{
    private static readonly VRDeviceCapability _capabilities = VRDeviceCapability.Controller | VRDeviceCapability.DualController | VRDeviceCapability.UserPrescenceDetection;

    public string Name => "Simple";
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

    public SimpleVRDevice()
    {
        //PrimaryInputDevice = new OpenVRController(VRInputDeviceHand.Right);
        //SecondaryInputDevice = new OpenVRController(VRInputDeviceHand.Left);

        InputDevices = new List<IVRInputDevice>
        {
            PrimaryInputDevice,
            SecondaryInputDevice,
        };
    }

    public bool HasCapabilities(VRDeviceCapability capabilities) => ((_capabilities & capabilities) == capabilities);

    public void Update()
    {
    }

    public void SetupAvatar(IVRAvatar avatar)
    {
    }
}