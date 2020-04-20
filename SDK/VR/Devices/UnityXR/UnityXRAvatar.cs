using System.Collections.Generic;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Avatars.Controllers;
using Liminal.SDK.VR.Input;
using Liminal.SDK.VR.Pointers;
using UnityEngine;
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

        private void Update()
        {
            VRDevice.Device.Update();
        }
    }

    public class UnityXRInputDevice : IVRInputDevice
    {
        public string Name => "UnityController";
        public IVRPointer Pointer { get; }

        public int ButtonCount => 3;
        public VRInputDeviceHand Hand { get; }
        public bool IsTouching { get; }

        private static readonly VRInputDeviceCapability _capabilities = VRInputDeviceCapability.DirectionalInput |
                                                                        VRInputDeviceCapability.Touch |
                                                                        VRInputDeviceCapability.TriggerButton;

        public UnityXRInputDevice(VRInputDeviceHand hand)
        {
            Hand = hand;
            Pointer = new InputDevicePointer(this);
            Pointer.Activate();

            foreach (var button in _XrButtonMap)
                AddInput(button.Key);
        }

        public Dictionary<string, string> _XrButtonMap = new Dictionary<string, string>
        {
            { VRButton.Trigger, "TriggerButton"},
        };

        public Dictionary<string, EPressState> _inputsMap = new Dictionary<string, EPressState>();
        public List<string> _inputs = new List<string>();

        public InputDevice InputDevice => InputDevices.GetDeviceAtXRNode(Hand == VRInputDeviceHand.Right ? XRNode.RightHand : XRNode.LeftHand);

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
            return _inputsMap[_XrButtonMap[button]] == EPressState.Pressing;
        }

        public bool GetButtonDown(string button)
        {
            return _inputsMap[_XrButtonMap[button]] == EPressState.Down;
        }

        public bool GetButtonUp(string button)
        {
            return _inputsMap[_XrButtonMap[button]] == EPressState.Up;
        }

        public void AddInput(string input)
        {
            if (!_inputsMap.ContainsKey(input))
            {
                _inputs.Add(input);
                _inputsMap.Add(input, EPressState.None);
            }
        }

        public void Update()
        {
            foreach (var input in _inputs)
            {
                if (!_inputsMap.ContainsKey(input))
                    _inputsMap.Add(input, EPressState.None);

                InputDevice.TryGetFeatureValue(new InputFeatureUsage<bool>(input), out var pressed);

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
                        case EPressState.None:
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

        public List<UnityXRInputDevice> XRInputs = new List<UnityXRInputDevice>();

        public int CpuLevel { get; set; }
        public int GpuLevel { get; set; }

        public event VRInputDeviceEventHandler InputDeviceConnected;
        public event VRInputDeviceEventHandler InputDeviceDisconnected;
        public event VRDeviceEventHandler PrimaryInputDeviceChanged;

        public UnityXRDevice()
        {
            var primary = new UnityXRInputDevice(VRInputDeviceHand.Right);
            var secondary = new UnityXRInputDevice(VRInputDeviceHand.Left);

            PrimaryInputDevice = primary;
            SecondaryInputDevice = secondary;

            XRInputs.Add(primary);
            XRInputs.Add(secondary);

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

            avatar.Head.Transform.localPosition = Vector3.zero;
            //UpdateConnectedControllers();
            //SetDefaultPointerActivation();
        }

        public void Update()
        {
            foreach (var input in XRInputs)
            {
                input.Update();
            }
        }
    }
}