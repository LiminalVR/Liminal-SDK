using System.Collections.Generic;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Avatars.Controllers;
using Liminal.SDK.VR.Input;
using Liminal.SDK.VR.Pointers;
using UnityEngine;

namespace Liminal.SDK.XR
{
    public class UnityXRAvatar : MonoBehaviour, IVRDeviceAvatar
    {
        public IVRAvatar Avatar { get; }

        public VRControllerVisual InstantiateControllerVisual(IVRAvatarLimb limb)
        {
            throw new System.NotImplementedException();
        }
    }

    public class UnityXRInputDevice : IVRInputDevice
    {
        public string Name { get; }
        public IVRPointer Pointer { get; }
        public bool HasCapabilities(VRInputDeviceCapability capabilities)
        {
            throw new System.NotImplementedException();
        }

        public bool HasAxis1D(string axis)
        {
            throw new System.NotImplementedException();
        }

        public bool HasAxis2D(string axis)
        {
            throw new System.NotImplementedException();
        }

        public bool HasButton(string button)
        {
            throw new System.NotImplementedException();
        }

        public float GetAxis1D(string axis)
        {
            throw new System.NotImplementedException();
        }

        public Vector2 GetAxis2D(string axis)
        {
            throw new System.NotImplementedException();
        }

        public bool GetButton(string button)
        {
            throw new System.NotImplementedException();
        }

        public bool GetButtonDown(string button)
        {
            throw new System.NotImplementedException();
        }

        public bool GetButtonUp(string button)
        {
            throw new System.NotImplementedException();
        }

        public int ButtonCount { get; }
        public VRInputDeviceHand Hand { get; }
        public bool IsTouching { get; }
    }

    public class UnityXRHeadset : IVRHeadset
    {
        private static readonly VRHeadsetCapability _capabilities = VRHeadsetCapability.PositionalTracking | VRHeadsetCapability.PositionalTracking;

        public string Name => "UnityXRHeadset";
        public IVRPointer Pointer { get; }

        public bool HasCapabilities(VRHeadsetCapability capabilities)
        {
            return (_capabilities & capabilities) == capabilities;
        }
    }

    public class UnityXRDevice : IVRDevice
    {
        private static readonly VRDeviceCapability _capabilities = VRDeviceCapability.Controller | VRDeviceCapability.UserPrescenceDetection;

        public UnityXRDevice()
        {
            InputDevices = new List<IVRInputDevice>();
        }

        public bool HasCapabilities(VRDeviceCapability capabilities)
        {
            return (_capabilities & capabilities) == capabilities;
        }

        public void SetupAvatar(IVRAvatar avatar)
        {
            var deviceAv = avatar.Transform.gameObject.AddComponent<UnityXRAvatar>();
            

            //UpdateConnectedControllers();
            //SetDefaultPointerActivation();
        }

        public void Update()
        {
            //UpdateConnectedControllers
        }

        public string Name => "UnityXR";
        public int InputDeviceCount => 2;

        public IVRHeadset Headset => new UnityXRHeadset();
        public IEnumerable<IVRInputDevice> InputDevices { get; }
        public IVRInputDevice PrimaryInputDevice { get; }
        public IVRInputDevice SecondaryInputDevice { get; }
        public int CpuLevel { get; set; }
        public int GpuLevel { get; set; }
        public event VRInputDeviceEventHandler InputDeviceConnected;
        public event VRInputDeviceEventHandler InputDeviceDisconnected;
        public event VRDeviceEventHandler PrimaryInputDeviceChanged;
    }

}