#if UNITY_XR
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
using Liminal.SDK.VR.Devices.GearVR.Avatar;

namespace Liminal.SDK.XR
{
    /// <summary>
    /// Base input device for UnityXR devices
    /// </summary>
    public abstract class UnityXRInputDevice : IVRInputDevice
    {
        #region Constants

        #endregion

        #region Statics

        #endregion

        #region Fields
        #region Public

        #endregion

        #region Private

        #endregion
        #endregion

        #region Properties
        #region Public
        public virtual string Name { get => InputDevice.name; }
        public IVRPointer Pointer { get; }
        public abstract int ButtonCount { get; }
        public abstract VRInputDeviceHand Hand { get; }
        public abstract bool IsTouching { get; }

        public InputDevice InputDevice { get; private set; }
        #endregion

        #region Private

        #endregion
        #endregion

        public UnityXRInputDevice(InputDevice inputDevice) 
        {
            InputDevice = inputDevice;

            Pointer = CreatePointer();
        }

        #region IVRHeadset
        protected abstract IVRPointer CreatePointer();

        public abstract float GetAxis1D(string axis);
        public abstract Vector2 GetAxis2D(string axis);
        public abstract bool GetButton(string button);
        public abstract bool GetButtonDown(string button);
        public abstract bool GetButtonUp(string button);
        public abstract bool HasAxis1D(string axis);
        public abstract bool HasAxis2D(string axis);
        public abstract bool HasButton(string button);

        /// <summary>
        /// TODO: Potentially determine and store the input device capabilities at this level of 
        /// code using the assigned InputDevice capabilities and inputFeatures
        /// </summary>
        /// <param name="capability"></param>
        /// <returns></returns>
        public abstract bool HasCapabilities(VRInputDeviceCapability capability);

        public abstract void Update(); 
        public abstract void LateUpdate();
        #endregion
    }
}
#endif