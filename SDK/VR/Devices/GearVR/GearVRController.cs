using Liminal.SDK.VR.Input;
using UnityEngine;
using Liminal.SDK.VR.Pointers;
using System.Collections.Generic;

namespace Liminal.SDK.VR.Devices.GearVR
{
    /// <summary>
    /// Represents a GearVR controller.
    /// <seealso href="https://developer.oculus.com/documentation/unity/latest/concepts/unity-ovrinput/"/>
    /// An Oculus Touch controller can be used to emulate a GearVR controller in the Editor. At the time of
    /// writing, button presses don't seem to be detected from Touch though.
    /// </summary>
    internal class GearVRController : GearVRInputDevice
    {
        public override string Name => "GearVRController";
        public override int ButtonCount { get { return 3; } }

        public static readonly OVRInput.Controller RightHandControllerMask = OVRInput.Controller.RTouch | OVRInput.Controller.RTrackedRemote;
        public static readonly OVRInput.Controller LeftHandControllerMask = OVRInput.Controller.LTouch | OVRInput.Controller.LTrackedRemote;
        public static readonly OVRInput.Controller AllHandControllersMask = RightHandControllerMask | LeftHandControllerMask;

        private static readonly VRInputDeviceCapability _capabilities =
            VRInputDeviceCapability.DirectionalInput |
            VRInputDeviceCapability.Touch |
            VRInputDeviceCapability.TriggerButton;

        public override VRInputDeviceHand Hand => _hand;
        private VRInputDeviceHand _hand;

        private OVRInput.Controller Controller => _hand == VRInputDeviceHand.Right
            ? OVRInput.Controller.RTouch
            : OVRInput.Controller.LTouch;

        public GearVRController(VRInputDeviceHand hand) : base(OVRUtils.GetControllerType(hand))
        {
            _hand = hand;
        }

        protected override IVRPointer CreatePointer()
        {
            return new InputDevicePointer(this);
        }

        // TODO: Add HandTrigger support and IndexTrigger support since Oculus Quest can provide an Axis.
        // Note, on the Touch Controllers (Oculus Quest controllers), there are two triggers that provide 1Axis.
        // The HandTrigger is the Grip on the side of the controller.
        // The IndexTrigger is the common trigger, like firing a gun.
        public override float GetAxis1D(string axis)
        {
            // No 1D axes on the GearVR controller.
            return 0;
        }

        // TODO: Add Controller masks to detect between left or right hand.
        public override Vector2 GetAxis2D(string axis)
        {
            switch (axis)
            {
                case VRAxis.OneRaw:
                    var allRawAxis =
                        OVRInput.RawAxis2D.LTouchpad |
                        OVRInput.RawAxis2D.LThumbstick |
                        OVRInput.RawAxis2D.RTouchpad |
                        OVRInput.RawAxis2D.RThumbstick;

                    return OVRInput.Get(allRawAxis, base.ControllerMask);
                case VRAxis.One:
                    var allAxis = OVRInput.Axis2D.PrimaryTouchpad |
                                 OVRInput.Axis2D.SecondaryTouchpad |
                                 OVRInput.Axis2D.PrimaryThumbstick |
                                 OVRInput.Axis2D.SecondaryThumbstick;

                    return OVRInput.Get(allAxis);
                default:
                    return Vector2.zero;
            }
        }

        // TODO: Add Controller masks to detect between left or right hand.
        public override bool GetButton(string button)
        {
            GetControllerButtonMapping(Controller).TryGetValue(button, out var ovrButton);
            return (ovrButton != OVRInput.Button.None) && OVRInput.Get(ovrButton, Controller);
        }

        public override bool GetButtonDown(string button)
        {
            GetControllerButtonMapping(Controller).TryGetValue(button, out var ovrButton);
            return (ovrButton != OVRInput.Button.None) && OVRInput.GetDown(ovrButton, Controller);
        }

        public override bool GetButtonUp(string button)
        {
            GetControllerButtonMapping(Controller).TryGetValue(button, out var ovrButton);
            return (ovrButton != OVRInput.Button.None) && OVRInput.GetUp(ovrButton, Controller);            
        }

        public override bool HasAxis1D(string axis)
        {
            return false;
        }

        public override bool HasAxis2D(string axis)
        {
            switch (axis)
            {
                case VRAxis.OneRaw:
                case VRAxis.One:
                    return true;

                default:
                    return false;
            }
        }

        public override bool HasButton(string button)
        {
            return GetControllerButtonMapping(Controller).ContainsKey(button);
        }

        public override bool HasCapabilities(VRInputDeviceCapability capabilities)
        {
            return ((_capabilities & capabilities) == capabilities);
        }

        public Dictionary<string, OVRInput.Button> GetControllerButtonMapping(OVRInput.Controller controller)
        {
            switch (controller)
            {
                case OVRInput.Controller.RTouch:
                    return new Dictionary<string, OVRInput.Button>()
                    {
                        { VRButton.One, OVRInput.Button.PrimaryIndexTrigger},
                        { VRButton.Trigger, OVRInput.Button.PrimaryIndexTrigger},
                        { VRButton.Two, OVRInput.Button.PrimaryTouchpad },
                        { VRButton.Touch, OVRInput.Button.PrimaryTouchpad },
                        { VRButton.Back, OVRInput.Button.Back }
                    };

                case OVRInput.Controller.LTouch:
                    return new Dictionary<string, OVRInput.Button>()
                    {
                        { VRButton.One, OVRInput.Button.SecondaryIndexTrigger},
                        { VRButton.Trigger, OVRInput.Button.SecondaryIndexTrigger},
                        { VRButton.Two, OVRInput.Button.SecondaryTouchpad },
                        { VRButton.Touch, OVRInput.Button.SecondaryTouchpad },
                        { VRButton.Back, OVRInput.Button.Back }
                    };

                default:
                    return null;
            }
        }
    }
}
