using System.Collections.Generic;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Input;
using Liminal.SDK.VR.Pointers;
using Pvr_UnitySDKAPI;
using UnityEngine;

namespace Liminal.SDK.PicoVR
{
    public class PicoVRController : IVRInputDevice
    {
        public string Name => "Pico Controller";
        public int ButtonCount => 7;
        
        public VRInputDeviceHand Hand { get; }
        public IVRPointer Pointer { get; }
        
        public int PicoHandId => Hand == VRInputDeviceHand.Right ? 1 : 0;
        public bool HasCapabilities(VRInputDeviceCapability capabilities) => VRDeviceCommonUtils.HasCapabilities(capabilities, VRDeviceCommonUtils.GenericInputDeviceCapability);

        public PicoVRController(VRInputDeviceHand hand)
        {
            Pointer = new InputDevicePointer(this);
            Pointer.Activate();
            Hand = hand;

            ControllerMap = GenerateControllerMap(hand);
        }

        public Dictionary<string, Pvr_KeyCode> ControllerMap { get; set; }

        public Dictionary<string, Pvr_KeyCode> GenerateControllerMap(VRInputDeviceHand hand)
        {
            var map = new Dictionary<string, Pvr_KeyCode>()
            {
                {VRButton.One, Pvr_KeyCode.TRIGGER},
                {VRButton.Trigger, Pvr_KeyCode.TRIGGER},
                {VRButton.Two, Pvr_KeyCode.TOUCHPAD | Pvr_KeyCode.Thumbrest},
                {VRButton.Three, Pvr_KeyCode.Right | Pvr_KeyCode.Left},
                {VRButton.Touch, Pvr_KeyCode.TOUCHPAD },
            };

            switch (hand)
            {
                case VRInputDeviceHand.Left:
                    map.Add(VRButton.Four, Pvr_KeyCode.X);
                    map.Add(VRButton.Back, Pvr_KeyCode.Y);
                    break;

                case VRInputDeviceHand.Right:
                    map.Add(VRButton.Four, Pvr_KeyCode.A);
                    map.Add(VRButton.Back, Pvr_KeyCode.B);
                    break;
            }

            return map;
        }

        public bool IsTouching => Controller.UPvr_IsTouching(PicoHandId);

        public bool HasAxis1D(string axis) => ControllerMap.ContainsKey(axis);
        public bool HasAxis2D(string axis) => ControllerMap.ContainsKey(axis);
        public bool HasButton(string button) => ControllerMap.ContainsKey(button);

        public float GetAxis1D(string axis) => Controller.UPvr_GetAxis1D(PicoHandId, ControllerMap[axis]);

        // The only axis 2D available is the joystick/touchpad. Pico's GetAxis2D seems to be something different?
        public Vector2 GetAxis2D(string axis) => Controller.UPvr_GetTouchPadPosition(PicoHandId);

        public bool GetButton(string button) => Controller.UPvr_GetKey(PicoHandId, ControllerMap[button]);
        public bool GetButtonDown(string button) => Controller.UPvr_GetKeyDown(PicoHandId, ControllerMap[button]);
        public bool GetButtonUp(string button) => Controller.UPvr_GetKeyUp(PicoHandId, ControllerMap[button]);
    }
}