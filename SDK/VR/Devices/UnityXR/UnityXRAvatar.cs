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
using System;
using Object = UnityEngine.Object;
using System.Linq;

namespace Liminal.SDK.XR
{
    public class UnityXRAvatar : MonoBehaviour, IVRDeviceAvatar
    {
        public IVRAvatar Avatar { get; }

        public VRControllerVisual InstantiateControllerVisual(IVRAvatarLimb limb)
        {
            throw new NotImplementedException();
        }

        private void Update()
        {
            VRDevice.Device.Update();
        }
    }

    /// <summary>
    /// Mappings and further manual information available here: https://docs.unity3d.com/Manual/xr_input.html
    /// All of the below are on a per-controller basis and may or may not exist depending on the platform that it currently running
    /// 
    /// Buttons:
    /// - primaryButton
    /// - secondaryButton
    /// - secondaryTouch
    /// - gripButton
    /// - triggerButton
    /// - menuButton
    /// - primary2DAxisClick
    /// - primary2DAxisTouch
    /// - userPresence (WMR, Oculus)
    /// 
    /// Axis:
    /// - trigger
    /// - grip
    /// - batteryLevel (only WMR)
    /// 
    /// 2D Axis:
    /// - primary2DAxis
    /// - secondary2DAxis
    /// </summary>
    public class UnityXRInputDevice : IVRInputDevice
    {
        #region Inner enums
        public enum EPressState
        {
            None,
            Down,
            Pressing,
            Up
        } 
        #endregion

        #region InputFeature inner classes
        public abstract class InputFeature
        {
            protected InputDevice? Device { get; private set; }
            public EPressState PressState { get; protected set; }
            public abstract string Name { get; }

            public InputFeature()
            {
                PressState = EPressState.None;
            }

            public void AssignDevice(InputDevice aDevice)
            {
                if (Device.HasValue) return;

                Device = aDevice;
            }

            public abstract void UpdateState();
        }

        public abstract class InputFeature<T> : InputFeature where T : IEquatable<T>
        {
            // RawValue is assigned, also assign the 'normalised' Value
            public virtual T RawValue 
            {
                get; protected set;
            }
            public T Value { get; protected set; }

            public InputFeatureUsage<T> BaseFeature { get; }

            public override string Name => BaseFeature.name;

            public InputFeature(InputFeatureUsage<T> aBaseFeature) : base()
            {
                BaseFeature = aBaseFeature;
            }
        }

        public class ButtonInputFeature : InputFeature<bool>
        {
            public override bool RawValue
            { 
                get => base.RawValue;
                protected set
                {
                    base.RawValue = value;
                    Value = RawValue;
                }
            }

            public ButtonInputFeature(InputFeatureUsage<bool> aBaseFeature) : base(aBaseFeature)
            {
            }

            public override void UpdateState()
            {
                if (!Device.HasValue) return;

                if (!Device.Value.TryGetFeatureValue(BaseFeature, out bool isPressed))
                {
                    // couldn't get input for the feature, so mark press state as none
                    PressState = EPressState.None;
                    RawValue = false;
                }

                // received a value, so update accordingly
                EPressState currentState = PressState;
                RawValue = isPressed;

                if (isPressed)
                {
                    switch (currentState)
                    {
                        case EPressState.None:
                            PressState = EPressState.Down;
                            break;
                        case EPressState.Down:
                            PressState = EPressState.Pressing;
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    switch (currentState)
                    {
                        case EPressState.Pressing:
                            PressState = EPressState.Up;
                            break;
                        case EPressState.Up:
                            PressState = EPressState.None;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public interface AxisInputFeature { }

        public class Axis1DInputFeature : InputFeature<float>, AxisInputFeature
        {
            private const float THRESHOLD = 0.1f;

            public override float RawValue
            { 
                get => base.RawValue;
                protected set
                {
                    base.RawValue = value;
                    Value = value >= THRESHOLD ? 1f : 0f;
                }
            }

            public Axis1DInputFeature(InputFeatureUsage<float> aBaseFeature) : base(aBaseFeature)
            {
            }

            public override void UpdateState()
            {
                if (!Device.HasValue) return;

                if (!Device.Value.TryGetFeatureValue(BaseFeature, out float rawActuated))
                {
                    // couldn't get input for the feature, so mark press state as none
                    PressState = EPressState.None;
                    RawValue = 0.0f;
                }

                // received a value, so update accordingly
                EPressState currentState = PressState;
                RawValue = rawActuated;

                // if above or equal to the threshold the axis is considered 'pressed'
                if (rawActuated >= THRESHOLD)
                {
                    switch (currentState)
                    {
                        case EPressState.None:
                            PressState = EPressState.Down;
                            break;
                        case EPressState.Down:
                            PressState = EPressState.Pressing;
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    switch (currentState)
                    {
                        case EPressState.Pressing:
                            PressState = EPressState.Up;
                            break;
                        case EPressState.Up:
                            PressState = EPressState.None;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public class Axis2DInputFeature : InputFeature<Vector2>, AxisInputFeature
        {
            private const float THRESHOLD = 0.1f;

            public override Vector2 RawValue
            {
                get => base.RawValue;
                protected set
                {
                    base.RawValue = value;

                    Value = new Vector2(
                        Mathf.Abs(base.RawValue.x) >= THRESHOLD ? 1f * Mathf.Sign(base.RawValue.x) : 0f,
                        Mathf.Abs(base.RawValue.y) >= THRESHOLD ? 1f * Mathf.Sign(base.RawValue.y) : 0f
                    );
                }
            }

            public Axis2DInputFeature(InputFeatureUsage<Vector2> aBaseFeature) : base(aBaseFeature)
            {
            }

            public override void UpdateState()
            {
                if (!Device.HasValue) return;

                if (!Device.Value.TryGetFeatureValue(BaseFeature, out Vector2 rawActuated))
                {
                    // couldn't get input for the feature, so mark press state as none
                    PressState = EPressState.None;
                    RawValue = Vector2.zero;
                }

                // received a value, so update accordingly
                EPressState currentState = PressState;
                RawValue = rawActuated;

                // if either axis exceeds the threshold, considered pressed
                if (Mathf.Abs(rawActuated.x) >= THRESHOLD ||
                    Mathf.Abs(rawActuated.y) >= THRESHOLD)
                {
                    switch (currentState)
                    {
                        case EPressState.None:
                            PressState = EPressState.Down;
                            break;
                        case EPressState.Down:
                            PressState = EPressState.Pressing;
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    switch (currentState)
                    {
                        case EPressState.Pressing:
                            PressState = EPressState.Up;
                            break;
                        case EPressState.Up:
                            PressState = EPressState.None;
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        #endregion

        public string Name => "UnityController";
        public IVRPointer Pointer { get; }

        // TODO: Confirm this?
        public int ButtonCount => 3;
        public VRInputDeviceHand Hand { get; }

        // this is mapped to 'primaryTouch' inputFeature
        public bool IsTouching { get => GetButton(VRButton.Touch); }

        private static readonly VRInputDeviceCapability _capabilities = VRInputDeviceCapability.DirectionalInput |
                                                                        VRInputDeviceCapability.Touch |
                                                                        VRInputDeviceCapability.TriggerButton;

        private Dictionary<string, InputFeature> _inputFeatures = new Dictionary<string, InputFeature>
        {
            // buttons
            { VRButton.Back, new ButtonInputFeature(CommonUsages.secondaryButton) },
            { VRButton.Touch, new ButtonInputFeature(CommonUsages.primaryTouch) },
            { VRButton.Trigger, new ButtonInputFeature(CommonUsages.triggerButton) },
            { VRButton.Primary, new ButtonInputFeature(CommonUsages.triggerButton) },
            { VRButton.Seconday, new ButtonInputFeature(CommonUsages.gripButton) },
            { VRButton.Three, new ButtonInputFeature(CommonUsages.primary2DAxisTouch) },
            { VRButton.Four, new ButtonInputFeature(CommonUsages.primary2DAxisClick) },

            // axis 2D
            { VRAxis.One, new Axis2DInputFeature(CommonUsages.primary2DAxis) },

            // axis 1D
            { VRAxis.Two, new Axis1DInputFeature(CommonUsages.trigger) },
            { VRAxis.Three, new Axis1DInputFeature(CommonUsages.grip) },
        };

        public UnityXRInputDevice(VRInputDeviceHand hand)
        {
            Hand = hand;
            Pointer = new InputDevicePointer(this);
            Pointer.Activate();

            foreach (var pairs in _inputFeatures.ToArray())
            {
                InputFeature inputFeature = pairs.Value;

                // also register axes with their raw counterpart
                if (inputFeature is AxisInputFeature && !pairs.Key.EndsWith("Raw"))
                {
                    string rawKey = $"{pairs.Key}Raw";

                    if (!_inputFeatures.ContainsKey(rawKey))
                    {
                        _inputFeatures.Add(rawKey, inputFeature);
                    }
                }

                inputFeature.AssignDevice(InputDevice);
            }
        }

        public UnityXRInputDevice()
        {
        }

        public InputDevice InputDevice => InputDevices.GetDeviceAtXRNode(Hand == VRInputDeviceHand.Right ? XRNode.RightHand : XRNode.LeftHand);

        public bool HasCapabilities(VRInputDeviceCapability capabilities)
        {
            return ((_capabilities & capabilities) == capabilities);
        }

        public bool HasAxis1D(string axis)
        {
            return _inputFeatures.TryGetValue(axis, out var feature) && feature is Axis1DInputFeature;
        }

        public bool HasAxis2D(string axis)
        {
            return _inputFeatures.TryGetValue(axis, out var feature) && feature is Axis2DInputFeature;
        }

        public bool HasButton(string button)
        {
            return _inputFeatures.TryGetValue(button, out var feature) && feature is ButtonInputFeature;
        }

        public float GetAxis1D(string axis)
        {
            if (!HasAxis1D(axis)) return 0f;

            var axis1DFeature = _inputFeatures[axis] as Axis1DInputFeature;
            return axis.Contains("Raw") ? axis1DFeature.RawValue : axis1DFeature.Value;
        }

        public Vector2 GetAxis2D(string axis)
        {
            if (!HasAxis2D(axis)) return Vector2.zero;

            var axis2DFeature = _inputFeatures[axis] as Axis2DInputFeature;
            return axis.Contains("Raw") ? axis2DFeature.RawValue : axis2DFeature.Value;
        }

        public bool GetButton(string button)
        {
            return GetButtonState(button) == EPressState.Pressing;
        }

        public bool GetButtonDown(string button)
        {
            return GetButtonState(button) == EPressState.Down;
        }

        public bool GetButtonUp(string button)
        {
            return GetButtonState(button) == EPressState.Up;
        }

        public EPressState GetButtonState(string button)
        {
            if (!HasButton(button)) return EPressState.None;

            var buttonFeature = _inputFeatures[button] as ButtonInputFeature;
            return buttonFeature.PressState;
        }

        public void Update()
        {
            // foreach input registered
            foreach (var feature in _inputFeatures.Values)
            {
                // update it
                try
                {
                    feature.UpdateState();
                }
                catch (Exception)
                {
                    Debug.LogError($"Problems occuring within {feature.Name}");
                }
            }
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
            var manager = new GameObject("XRInteractionManager").AddComponent<XRInteractionManager>();
            
            var avatarGo = avatar.Transform.gameObject;
            
            var xrRig = avatarGo.AddComponent<XRRig>();
            var centerEye = avatar.Head.CenterEyeCamera.gameObject;
            var eyeDriver = centerEye.AddComponent<TrackedPoseDriver>();
            eyeDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            eyeDriver.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRDevice, TrackedPoseDriver.TrackedPose.Center);
            xrRig.cameraGameObject = centerEye.gameObject;
            xrRig.TrackingOriginMode = TrackingOriginModeFlags.Floor;

            var primaryHandPrefab = Resources.Load("RightHand Controller");
            var primaryHand = Object.Instantiate(primaryHandPrefab, avatar.Head.Transform) as GameObject;

            var secondaryHandPrefab = Resources.Load("LeftHand Controller");
            var secondaryHand = Object.Instantiate(secondaryHandPrefab, avatar.Head.Transform) as GameObject;

            SetupControllerPointer(PrimaryInputDevice, avatar.PrimaryHand, primaryHand.transform);
            SetupControllerPointer(SecondaryInputDevice, avatar.SecondaryHand, secondaryHand.transform);
        }

        public void SetupControllerPointer(IVRInputDevice inputDevice, IVRAvatarHand hand, Transform xrHand)
        {
            hand.Transform.SetParent(xrHand);
            var pointer = xrHand.GetComponentInChildren<LaserPointerVisual>(includeInactive: true);
            var controllerVisual = hand.Transform.GetComponentInChildren<VRAvatarController>(includeInactive: true);

            if (pointer != null)
            {
                if (controllerVisual != null)
                {
                    pointer.Bind(inputDevice.Pointer);
                    inputDevice.Pointer.Transform = pointer.transform;
                    pointer.transform.SetParent(controllerVisual.transform);
                }
                else
                {
                    Object.Destroy(pointer.gameObject);       
                }
            }
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