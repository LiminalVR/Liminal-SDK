using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Devices.GearVR.Avatar;
using Liminal.SDK.VR.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Liminal.SDK.VR.Devices.GearVR
{
    /// <summary>
    /// A concrete implementation of <see cref="IVRDevice"/> for Samsung's GearVR hardware.
    /// </summary>
    public class GearVRDevice : IVRDevice
    {
        private static readonly VRDeviceCapability _capabilities =
            VRDeviceCapability.Controller | VRDeviceCapability.UserPrescenceDetection;

        private OVRInput.Controller mConnectedControllerMask;
        private GearVRController mController;
        private bool mHeadsetInputConnected;
        private OVRInput.Controller mCachedActiveController;
        private IVRInputDevice[] mInputDevices = new IVRInputDevice[0];

        #region Properties

        string IVRDevice.Name { get { return "GearVR"; } }

        int IVRDevice.InputDeviceCount { get { return mInputDevices.Length; } }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public IVRHeadset Headset { get; private set; }
        public IVRInputDevice PrimaryInputDevice { get; private set; }
        public IVRInputDevice SecondaryInputDevice { get; private set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        IEnumerable<IVRInputDevice> IVRDevice.InputDevices { get { return mInputDevices; } }
        int IVRDevice.CpuLevel {  get { return OVRManager.cpuLevel; } set { OVRManager.cpuLevel = value; } }
        int IVRDevice.GpuLevel { get { return OVRManager.gpuLevel; } set { OVRManager.gpuLevel = value; } }

        /// <summary>
        /// Returns true if the device is an Oculus Go, rather than a GearVR device
        /// </summary>
        public static bool IsOculusGo { get { return SystemInfo.deviceModel == "Oculus Pacific"; } }

        #endregion
        
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public event VRInputDeviceEventHandler InputDeviceConnected;
        public event VRInputDeviceEventHandler InputDeviceDisconnected;
        public event VRDeviceEventHandler PrimaryInputDeviceChanged;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        /// Create a GearVR device
        /// </summary>
        public GearVRDevice()
        {
            Headset = OVRUtils.IsGearVRHeadset() ? new GearVRHeadset() : GenericHeadset();
            OVRInput.Update();
            UpdateConnectedControllers();
        }

        private static IVRHeadset GenericHeadset()
        {
            return new SimpleHeadset("GenericHeadset", VRHeadsetCapability.None);
        }

        //Updates once per Tick from VRDeviceMonitor (const 0.5 seconds)
        void IVRDevice.Update()
        {
            if (mConnectedControllerMask != OVRInput.GetConnectedControllers())
            {
                // Connected controller mask has changed
                UpdateConnectedControllers();
            }

            if (mCachedActiveController != OVRInput.GetActiveController())
            {
                // Active controller has changed
                UpdateInputDevices();
            }
        }
        
        bool IVRDevice.HasCapabilities(VRDeviceCapability capabilities)
        {
            return ((_capabilities & capabilities) == capabilities);
        }

        void IVRDevice.SetupAvatar(IVRAvatar avatar)
        {
            if (avatar == null)
                throw new ArgumentNullException("avatar");

            // Attach the GearVR avatar component
            // The component will take care of the rest of the setup
            var deviceAv = avatar.Transform.gameObject.AddComponent<GearVRAvatar>();
            deviceAv.hideFlags = HideFlags.NotEditable;

            UpdateConnectedControllers();
        }

        private void UpdateConnectedControllers()
        {
            var allControllers = new List<IVRInputDevice>();
            var disconnectedList = new List<IVRInputDevice>();
            var connectedList = new List<IVRInputDevice>();

            var ctrlMask = OVRInput.GetConnectedControllers();
            Debug.LogFormat("[GearVRDevice] ConnectedControllers={0}", ctrlMask.ToString());

            // NOTE: Controller tests here are in order of priority. Active hand controllers take priority over headset

            #region Controller

            // GearVR/Touch controller
            var hasController = OVRUtils.IsOculusQuest
                ? OVRInput.IsControllerConnected(OVRInput.Controller.Touch)
                : (ctrlMask & GearVRController.AllHandControllersMask) != 0;

            if (hasController)
            {
                mController = mController ?? new GearVRController();

                // Add to the connected list if the device isn't already in the device list
                if (!mInputDevices.Contains(mController))
                {
                    connectedList.Add(mController);
                }
                allControllers.Add(mController);
            }
            else if (mController != null)
            {
                // Controller disconnected
                disconnectedList.Add(mController);
            }

            #endregion            

            #region Headset (Swipe-pad)

            if (Headset is GearVRHeadset)
            {
                var gearVRHeadset = Headset as GearVRHeadset;

                if ((ctrlMask & OVRInput.Controller.Touchpad) != 0)
                {
                    if (!mHeadsetInputConnected)
                    {
                        connectedList.Add(gearVRHeadset);
                        mHeadsetInputConnected = true;
                    }

                    allControllers.Add(gearVRHeadset);
                }
                else if (Headset != null)
                {
                    disconnectedList.Add(gearVRHeadset);
                    mHeadsetInputConnected = false;
                }
            }
            #endregion

            // Update internal state
            mInputDevices = allControllers.ToArray();
            mConnectedControllerMask = ctrlMask;

            // Dispatch disconnected events
            foreach (var device in disconnectedList)
            {
                if (InputDeviceDisconnected != null)
                    InputDeviceDisconnected(this, device);
            }

            // Dispatch connected events
            foreach (var device in connectedList)
            {
                if (InputDeviceConnected != null)
                    InputDeviceConnected(this, device);
            }

            // Force an update of input devices
            UpdateInputDevices();
        }

        private void UpdateInputDevices()
        {
            mCachedActiveController = OVRInput.GetActiveController();

            var hasController = OVRUtils.IsOculusQuest ? 
                OVRInput.IsControllerConnected(OVRInput.Controller.Touch) :
                (mCachedActiveController & GearVRController.AllHandControllersMask) != 0;

            if (hasController)
            {
                PrimaryInputDevice = mController;
                SecondaryInputDevice = Headset as GearVRInputDevice;
            }
            else
            {
                PrimaryInputDevice = Headset as GearVRInputDevice;
                SecondaryInputDevice = null;
            }
            
            // Raise change event
            if (PrimaryInputDeviceChanged != null)
                PrimaryInputDeviceChanged(this);
        }
    }
}

public static class OVRUtils
{
    public static bool IsOculusQuest => OVRPlugin.GetSystemHeadsetType() == OVRPlugin.SystemHeadset.Oculus_Quest;
    public static bool IsOculusGo => OVRPlugin.GetSystemHeadsetType() == OVRPlugin.SystemHeadset.Oculus_Go;

    public static bool IsGearVRHeadset()
    {
        OVRPlugin.SystemHeadset headsetType = OVRPlugin.GetSystemHeadsetType();
        switch (headsetType)
        {
            case OVRPlugin.SystemHeadset.GearVR_R320:
            case OVRPlugin.SystemHeadset.GearVR_R321:
            case OVRPlugin.SystemHeadset.GearVR_R322:
            case OVRPlugin.SystemHeadset.GearVR_R323:
            case OVRPlugin.SystemHeadset.GearVR_R324:
            case OVRPlugin.SystemHeadset.GearVR_R325:
                return true;
            default:
                return false;
        }
    }
}