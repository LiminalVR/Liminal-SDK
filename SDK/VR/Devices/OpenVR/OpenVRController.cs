using System.Collections.Generic;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Input;
using Liminal.SDK.VR.Pointers;
using UnityEngine;
using Valve.VR;

namespace Liminal.SDK.OpenVR
{
    public class OpenVRController : IVRInputDevice
    {
        public string Name => "OpenVR Controller";
        public IVRPointer Pointer { get; }

        private static readonly VRInputDeviceCapability _capabilities = VRInputDeviceCapability.DirectionalInput | VRInputDeviceCapability.Touch | VRInputDeviceCapability.TriggerButton;

        public int ButtonCount => 5;
        public VRInputDeviceHand Hand { get; }

        public bool IsTouching { get; }

        public SteamVR_Input_Sources SteamHand => Hand == VRInputDeviceHand.Right ? SteamVR_Input_Sources.RightHand : SteamVR_Input_Sources.LeftHand;

        public Dictionary<string, SteamVR_Action_Boolean_Source> _buttonInputMap => new Dictionary<string, SteamVR_Action_Boolean_Source>()
        {
            {VRButton.Trigger, SteamVR_Input.GetAction<SteamVR_Action_Boolean>("default", "InteractUI")[SteamHand]},
            {VRButton.One, SteamVR_Input.GetAction<SteamVR_Action_Boolean>("default", "InteractUI")[SteamHand]},
            {VRButton.Two, SteamVR_Input.GetAction<SteamVR_Action_Boolean>("default", "Secondary")[SteamHand]},
            {VRButton.Touch, SteamVR_Input.GetAction<SteamVR_Action_Boolean>("default", "Joystick")[SteamHand]},
            {VRButton.Back, SteamVR_Input.GetAction<SteamVR_Action_Boolean>("default", "InteractUI")[SteamHand]},
        };
        
        public OpenVRController(VRInputDeviceHand hand)
        {
            Pointer = new InputDevicePointer(this);
            Hand = hand;
        }

        public bool HasCapabilities(VRInputDeviceCapability capabilities) => ((_capabilities & capabilities) == capabilities);

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
            _buttonInputMap.TryGetValue(button, out var result);
            return result != null && result.state;
        }

        public bool GetButtonDown(string button)
        {
            _buttonInputMap.TryGetValue(button, out var result);
            return result != null && result.stateDown;
        }

        public bool GetButtonUp(string button)
        {
            _buttonInputMap.TryGetValue(button, out var result);
            return result != null && result.stateUp;
        }
    }
}