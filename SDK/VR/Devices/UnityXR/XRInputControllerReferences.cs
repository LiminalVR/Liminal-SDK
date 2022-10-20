using System;
using System.Collections.Generic;
using Liminal.SDK.VR.Input;
using UnityEngine.InputSystem;

namespace Liminal.SDK.XR
{
    [Serializable]
    public class XRInputControllerReferences
    {
        public InputActionReference Trigger;
        public InputActionReference Joystick;

        public InputActionReference Two;
        public InputActionReference Three;
        public InputActionReference Four;
        public InputActionReference Touch;
        public InputActionReference Back;

        public InputAction GetInputAction(string actionName)
        {
            switch (actionName)
            {
                case VRButton.Trigger:
                case VRButton.One:
                    return Trigger.action;

                case VRButton.Two:
                    return Two.action;

                case VRButton.Three:
                    return Three.action;

                case VRButton.Four:
                    return Four.action;

                case VRButton.Touch:
                    return Touch.action;

                case VRButton.Back:
                    return Back.action;

                case VRAxis.One:
                case VRAxis.OneRaw:
                    return Joystick;
            }

            return new InputAction("");
        }

        public Dictionary<string, OVRInput.Button> QuestButtonMapping()
        {
            return new Dictionary<string, OVRInput.Button>()
            {
                { VRButton.One, OVRInput.Button.PrimaryIndexTrigger},
                { VRButton.Trigger, OVRInput.Button.PrimaryIndexTrigger},
                { VRButton.Two, OVRInput.Button.PrimaryTouchpad | OVRInput.Button.PrimaryThumbstick },
                { VRButton.Three, OVRInput.Button.PrimaryHandTrigger },
                { VRButton.Four, OVRInput.Button.One}, // A / X on Quest controllers
                { VRButton.Touch, OVRInput.Button.PrimaryTouchpad },
                { VRButton.Back, OVRInput.Button.Back | OVRInput.Button.Two}
            };
        }
    }
}