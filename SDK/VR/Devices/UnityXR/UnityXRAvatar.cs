using System.Collections.Generic;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Avatars.Controllers;
using Liminal.SDK.VR.Input;
using Liminal.SDK.VR.Pointers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SpatialTracking;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

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
        public string Name => "UnityController";
        public IVRPointer Pointer { get; }

        public int ButtonCount => 3;
        public VRInputDeviceHand Hand { get; }
        public bool IsTouching { get; }

        private static readonly VRInputDeviceCapability _capabilities =
            VRInputDeviceCapability.DirectionalInput |
            VRInputDeviceCapability.Touch |
            VRInputDeviceCapability.TriggerButton;

        public UnityXRInputDevice(VRInputDeviceHand hand)
        {
            Hand = hand;
            Pointer = new InputDevicePointer(this);
            Pointer.Activate();
        }

        public InputDevice InputDevice => InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        public bool HasCapabilities(VRInputDeviceCapability capabilities)
        {
            return ((_capabilities & capabilities) == capabilities);
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
            return _inputsMap[CommonUsages.triggerButton] == EPressState.Pressing;
        }

        private InputFeatureUsage<bool> _using;

        public bool GetButtonDown(string button)
        {
            return _inputsMap[CommonUsages.triggerButton] == EPressState.Down;
        }

        public bool GetButtonUp(string button)
        {
            return _inputsMap[CommonUsages.triggerButton] == EPressState.Up;
        }

        public Dictionary<InputFeatureUsage<bool>, EPressState> _inputsMap = new Dictionary<InputFeatureUsage<bool>, EPressState>();
        public List<InputFeatureUsage<bool>> _inputs = new List<InputFeatureUsage<bool>>();

        private void Update()
        {
            foreach (var input in _inputs)
            {
                if (!_inputsMap.ContainsKey(input))
                    _inputsMap.Add(input, EPressState.None);

                InputDevice.TryGetFeatureValue(input, out var pressed);

                var currentState = _inputsMap[input];
                if (pressed)
                {
                    switch (currentState)
                    {
                        case EPressState.None:
                            _inputsMap[input] = EPressState.Down;
                            break;
                        case EPressState.Down:
                            _inputsMap[input] = EPressState.Pressing;
                            break;
                    }
                }
                else
                {
                    switch (currentState)
                    {
                        case EPressState.Up:
                            _inputsMap[input] = EPressState.None;
                            break;
                        default:
                            _inputsMap[input] = EPressState.Up;
                            break;
                    }
                }
            }
        }

        public enum EPressState
        {
            None,
            Down,
            Pressing,
            Up
        }
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

        public UnityXRDevice()
        {
            PrimaryInputDevice = new UnityXRInputDevice(VRInputDeviceHand.Right);
            SecondaryInputDevice = new UnityXRInputDevice(VRInputDeviceHand.Left);

            InputDevices = new List<IVRInputDevice>
            {
                PrimaryInputDevice,
                SecondaryInputDevice,
            };
        }

        public bool HasCapabilities(VRDeviceCapability capabilities)
        {
            return (_capabilities & capabilities) == capabilities;
        }

        public void SetupAvatar(IVRAvatar avatar)
        {
            avatar.Transform.gameObject.AddComponent<UnityXRAvatar>();
            var manager = new GameObject().AddComponent<XRInteractionManager>();
            
            var avatarGo = avatar.Transform.gameObject;
            
            var xrRig = avatarGo.AddComponent<XRRig>();
            var centerEye = avatar.Head.CenterEyeCamera.gameObject;
            var eyeDriver = centerEye.AddComponent<TrackedPoseDriver>();
            eyeDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            eyeDriver.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRDevice, TrackedPoseDriver.TrackedPose.Center);
            xrRig.cameraGameObject = centerEye.gameObject;
            xrRig.TrackingOriginMode = TrackingOriginModeFlags.Floor;

            var primaryHandPrefab = Resources.Load("RightHand Controller");
            var primaryHand = Object.Instantiate(primaryHandPrefab, avatar.Transform) as GameObject;

            var secondaryHandPrefab = Resources.Load("LeftHand Controller");
            var secondaryHand = Object.Instantiate(secondaryHandPrefab, avatar.Transform) as GameObject;

            avatar.PrimaryHand.Transform.SetParent(primaryHand.transform);
            var primaryPointer = primaryHand.GetComponentInChildren<LaserPointerVisual>();
            PrimaryInputDevice.Pointer.Transform = primaryPointer.transform;

            primaryPointer?.Bind(PrimaryInputDevice.Pointer);
            avatar.SecondaryHand.Transform.SetParent(secondaryHand.transform);

            var secondaryPointer = secondaryHand.GetComponentInChildren<LaserPointerVisual>();
            secondaryPointer?.Bind(SecondaryInputDevice.Pointer);

            //avatar/head need to be at 0
            //UpdateConnectedControllers();
            //SetDefaultPointerActivation();
        }

        public void Update()
        {
        }
    }
}