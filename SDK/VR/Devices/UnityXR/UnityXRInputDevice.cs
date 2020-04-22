﻿using System.Collections.Generic;
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
using Liminal.SDK.VR.Devices.GearVR.Avatar;

namespace Liminal.SDK.XR
{
    /// <summary>
    /// Base input device for UnityXR devices
    /// </summary>
    public abstract class UnityXRInputDevice : IVRInputDevice
    {
        public abstract string Name { get; }
        public IVRPointer Pointer { get; }
        public abstract int ButtonCount { get; }
        public VRInputDeviceHand Hand { get; }
        public abstract bool IsTouching { get; }

        public UnityXRInputDevice() { }
        public UnityXRInputDevice(VRInputDeviceHand hand) 
        {
            Hand = hand;
            Pointer = CreatePointer();
        }

        protected abstract IVRPointer CreatePointer();

        public abstract float GetAxis1D(string axis);
        public abstract Vector2 GetAxis2D(string axis);
        public abstract bool GetButton(string button);
        public abstract bool GetButtonDown(string button);
        public abstract bool GetButtonUp(string button);
        public abstract bool HasAxis1D(string axis);
        public abstract bool HasAxis2D(string axis);
        public abstract bool HasButton(string button);
        public abstract bool HasCapabilities(VRInputDeviceCapability capability);

        public abstract void Update();
    }
}