using System;
using Liminal.SDK.VR.Input;
using UnityEngine.InputSystem;

namespace Liminal.SDK.XR
{
    [Serializable]
    public class XRInputControllerReferences
    {
        public InputActionReference Trigger;
        public InputActionReference Joystick;

        public InputAction GetInputAction(string name)
        {
            switch (name)
            {
                case VRButton.Trigger:
                case VRButton.One:
                    return Trigger.action;
                case VRAxis.One:
                case VRAxis.OneRaw:
                    return Joystick;
            }

            return new InputAction("");
        }

    }
}