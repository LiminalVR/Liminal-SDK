using System;
using Liminal.SDK.VR.Input;
using UnityEngine.InputSystem;

namespace Liminal.SDK.XR
{
    [Serializable]
    public class XRInputControllerReferences
    {
        public InputActionReference Trigger;

        public InputAction GetInputAction(string name)
        {
            switch (name)
            {
                case VRButton.Trigger:
                case VRButton.One:
                    return Trigger.action;
            }

            return new InputAction("");
        }

    }
}