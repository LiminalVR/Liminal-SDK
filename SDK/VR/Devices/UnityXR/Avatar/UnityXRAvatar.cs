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
    public class UnityXRAvatar : MonoBehaviour, IVRDeviceAvatar
    {
        private IVRAvatar mAvatar;
        public IVRAvatar Avatar
        {
            get
            {
                if (mAvatar == null)
                { 
                    mAvatar = GetComponentInParent<IVRAvatar>();
                }

                return mAvatar;
            }
        }

        private bool IsHandControllerActive
        {
            get
            {
                if (OVRUtils.IsOculusQuest)
                    return OVRUtils.IsQuestControllerConnected;

                return false;// (OVRInput.GetActiveController() & GearVRController.AllHandControllersMask) != 0;
            }
        }

        private IVRDevice mDevice;

        private IVRTrackedObjectProxy mPrimaryControllerTracker;
        private IVRTrackedObjectProxy mSecondaryControllerTracker;
        
        // OVR
        private OVRManager mManager;
        private OVRCameraRig mCameraRig;

        protected void Awake()
        {
            Debug.Log($"[{GetType().Name}] XRSettings.loadedDeviceName: {XRSettings.loadedDeviceName}");

            mAvatar = GetComponentInParent<IVRAvatar>();
            mAvatar.InitializeExtensions();

            SetupControllers();

            mDevice = VRDevice.Device;
            //mSettings = gameObject.GetOrAddComponent<GearVRAvatarSettings>();
            //mGazeInput = GetComponent<GazeInput>();

            // Setup auxiliary systems
            SetupManager();
            SetupCameraRig();

            // Activate OVRManager once everything is setup
            mManager.gameObject.SetActive(true);

            // Load controller visuals for any VRAvatarController objects attached to the avatar
            {
                var avatarControllers = GetComponentsInChildren<VRAvatarController>(includeInactive: true);
                foreach (var controller in avatarControllers)
                {
                    AttachControllerVisual(controller);
                }
            }

            // Add event listeners
            mDevice.InputDeviceConnected += OnInputDeviceConnected;
            mDevice.InputDeviceDisconnected += OnInputDeviceDisconnected;
            mAvatar.Head.ActiveCameraChanged += OnActiveCameraChanged;
            SetupInitialControllerState();

            UpdateHandedness();
        }

        private void Update()
        {
            VRDevice.Device.Update();
        }

        private void SetupControllers()
        {
            if (XRSettings.loadedDeviceName == "oculus display")
            {
                switch (OVRPlugin.GetSystemHeadsetType())
                {
                    case OVRPlugin.SystemHeadset.None:
                        break;
                    case OVRPlugin.SystemHeadset.GearVR_R320:
                    case OVRPlugin.SystemHeadset.GearVR_R321:
                    case OVRPlugin.SystemHeadset.GearVR_R322:
                    case OVRPlugin.SystemHeadset.GearVR_R323:
                    case OVRPlugin.SystemHeadset.GearVR_R324:
                    case OVRPlugin.SystemHeadset.GearVR_R325:
                        // OVRUtils.IsGearVRHeadset()
                        break;
                    case OVRPlugin.SystemHeadset.Oculus_Go:
                        // OVRUtils.IsOculusGo
                        SetupForOculusGo();
                        break;
                    case OVRPlugin.SystemHeadset.Oculus_Quest:
                        // OVRUtils.IsOculusQuest
                        SetupForOculusQuest();
                        break;
                    case OVRPlugin.SystemHeadset.Rift_DK1:
                    case OVRPlugin.SystemHeadset.Rift_DK2:
                    case OVRPlugin.SystemHeadset.Rift_CV1:
                    case OVRPlugin.SystemHeadset.Rift_CB:
                    case OVRPlugin.SystemHeadset.Rift_S:
                    default:
                        break;
                }
            }
        }

        private void SetupForGearVR()
        {
            throw new NotImplementedException();

            mPrimaryControllerTracker = new GearVRTrackedControllerProxy(mAvatar, VRAvatarLimbType.RightHand);
            mSecondaryControllerTracker = new GearVRTrackedControllerProxy(mAvatar, VRAvatarLimbType.LeftHand);
        }

        private void SetupForOculusGo()
        {
            throw new NotImplementedException();
        }

        private void SetupForOculusQuest()
        {
            throw new NotImplementedException();
        }

        private void SetupManager()
        {
            throw new NotImplementedException();
            //if (OVRManager.instance == null)
            //{
            //    Debug.Log("[GearVR] Adding OVRManager");
            //    var go = new GameObject("OVRManager");
            //    mManager = go.AddComponent<OVRManager>();
            //    DontDestroyOnLoad(go);
            //}
            //else
            //{
            //    mManager = OVRManager.instance;
            //}
        }

        private void SetupCameraRig()
        {
            throw new NotImplementedException();
            //var cameraRigPrefab = VRAvatarHelper.EnsureLoadPrefab<GearVRCameraRig>("GearVRCameraRig");
            //cameraRigPrefab.gameObject.SetActive(false);
            //mCameraRig = Instantiate(cameraRigPrefab);
            //mCameraRig.transform.SetParentAndIdentity(mAvatar.Auxiliaries);

            //OnActiveCameraChanged(mAvatar.Head);
        }

        private void SetupInitialControllerState()
        {
            throw new NotImplementedException();
            //if (mDevice.InputDevices.Any(x => x is GearVRController))
            //{
            //    foreach (var controller in mDevice.InputDevices)
            //    {
            //        EnableController(controller as GearVRController);
            //    }
            //}
            //else
            //{
            //    // Disable controllers and enable gaze controls
            //    DisableAllControllers();
            //}
        }

        private void AttachControllerVisual(VRAvatarController avatarController)
        {
            throw new NotImplementedException();
            //var limb = avatarController.GetComponentInParent<IVRAvatarLimb>();

            //var prefab = VRAvatarHelper.EnsureLoadPrefab<VRControllerVisual>(ControllerVisualPrefabName);
            //prefab.gameObject.SetActive(false);

            //// Create controller instance
            //var instance = Instantiate(prefab);
            //instance.name = prefab.name;
            //instance.transform.SetParentAndIdentity(avatarController.transform);

            //// Make sure the OVRGearVrController component exists...
            //var trackedRemote = instance.gameObject.GetComponent<OVRControllerHelper>();

            //if (trackedRemote == null)
            //    trackedRemote = instance.gameObject.AddComponent<OVRControllerHelper>();

            //avatarController.ControllerVisual = instance;
            //mRemotes.Add(trackedRemote);

            //// Assign the correct controller based on the limb type the controller is attached to
            //OVRInput.Controller controllerType = GetControllerTypeForLimb(limb);
            //trackedRemote.m_controller = controllerType;
            //trackedRemote.m_modelGearVrController.SetActive(true);

            //// Activate the controller
            //// TODO Do we need to set active here? 
            //var active = OVRUtils.IsLimbConnected(limb.LimbType);
            //instance.gameObject.SetActive(active);

            //Debug.Log($"Attached Controller: {limb.LimbType} and SetActive: {active} Controller Type set to: {controllerType}");
        }

        #region Controllers
        // TODO See if this method can be removed, it appears to not be used at all and it can be misleading when debugging.
        /// <summary>
        /// Instantiates a <see cref="VRControllerVisual"/> for a limb.
        /// </summary>
        /// <param name="limb">The limb for the controller.</param>
        /// <returns>The newly instantiated controller visual for the specified limb, or null if no controller visual was able to be created.</returns>
        public VRControllerVisual InstantiateControllerVisual(IVRAvatarLimb limb)
        {
            throw new NotImplementedException();

            //if (limb == null)
            //    throw new ArgumentNullException("limb");

            //if (limb.LimbType == VRAvatarLimbType.Head)
            //    return null;

            //var prefab = VRAvatarHelper.EnsureLoadPrefab<VRControllerVisual>(ControllerVisualPrefabName);
            //var instance = Instantiate(prefab);

            //var ovrController = instance.GetComponent<OVRControllerHelper>();
            //ovrController.m_controller = GetControllerTypeForLimb(limb);
            //ovrController.m_modelGearVrController.SetActive(true);
            //ovrController.enabled = false;

            //instance.gameObject.SetActive(true);
            //return instance;

        }

        //private void EnableController(GearVRController controller)
        //{
        //    throw new NotImplementedException();

        //    //if (controller == null)
        //    //    return;

        //    //// Find the visual for the hand that matches the controller
        //    //var remote = mRemotes.FirstOrDefault(x => (x.m_controller & controller.ControllerMask) != 0);
        //    //if (remote != null)
        //    //    remote.gameObject.SetActive(true);
        //}

        private void DisableAllControllers()
        {
            throw new NotImplementedException();
            //// Disable all controller visuals
            //foreach (var remote in mRemotes)
            //{
            //    remote.gameObject.SetActive(false);
            //}
        }

        #endregion

        private void UpdateHandedness()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Detects and Updates the state of the controllers including the TouchPad on the GearVR headset
        /// </summary>
        public void DetectAndUpdateControllerStates()
        {
            TrySetLimbsActive();
            TrySetGazeInputActive(!IsHandControllerActive);
        }

        /// <summary>
        /// A temporary method to split Oculus Quest changes with the other devices. 
        /// </summary>
        private void TrySetLimbsActive()
        {
            if (OVRUtils.IsOculusQuest)
            {
                TrySetHandActive(VRAvatarLimbType.RightHand);
                TrySetHandActive(VRAvatarLimbType.LeftHand);
            }
            else
            {
                TrySetHandsActive(IsHandControllerActive);
            }
        }

        private void TrySetHandActive(VRAvatarLimbType limbType)
        {
            var isLimbConnected = OVRUtils.IsLimbConnected(limbType);
            var limb = mAvatar.GetLimb(limbType);

            limb.SetActive(isLimbConnected);
        }

        private void TrySetHandsActive(bool active)
        {
            throw new NotImplementedException();
            //if (mAvatar != null)
            //{
            //    if (OVRUtils.IsGearVRHeadset())
            //    {
            //        if (OVRInput.GetActiveController() == OVRInput.Controller.Touchpad)
            //            active = false;
            //    }

            //    mAvatar.SetHandsActive(active);
            //}
        }

        private void TrySetGazeInputActive(bool active)
        {
            throw new NotImplementedException();
            // Ignore Always & Never Policy
            //if (mGazeInput != null && mGazeInput.ActivationPolicy == GazeInputActivationPolicy.NoControllers)
            //{
            //    if (active)
            //        mGazeInput.Activate();
            //    else
            //        mGazeInput.Deactivate();
            //}
        }

        private void RecenterHmdIfRequired()
        {
            throw new NotImplementedException();
            //if (mSettings != null && mSettings.HmdRecenterPolicy != HmdRecenterPolicy.OnControllerRecenter)
            //    return;

            //if (OVRInput.GetControllerWasRecentered())
            //{
            //    // Recenter the camera when the user recenters the controller
            //    UnityEngine.XR.InputTracking.Recenter();
            //}
        }

        private OVRInput.Controller GetControllerTypeForLimb(IVRAvatarLimb limb)
        {
            if (limb.LimbType == VRAvatarLimbType.LeftHand)
                return OVRInput.Controller.LTouch;

            if (limb.LimbType == VRAvatarLimbType.RightHand)
                return OVRInput.Controller.RTouch;

            return OVRInput.Controller.None;
        }

        #region Event Handlers

        //Notes: Device Connecting is difference than controller being active
        private void OnInputDeviceConnected(IVRDevice vrDevice, IVRInputDevice inputDevice)
        {
            throw new NotImplementedException();
            //var gearController = inputDevice as GearVRController;
            //if (gearController != null)
            //{
            //    // A controller was connected
            //    // Disable gaze controls
            //    EnableController(gearController);
            //}
        }

        private void OnInputDeviceDisconnected(IVRDevice vrDevice, IVRInputDevice inputDevice)
        {
            throw new NotImplementedException();
            //if (!vrDevice.InputDevices.Any(x => x is GearVRController))
            //{
            //    // No controllers are connected
            //    // Enable gaze controls
            //    DisableAllControllers();
            //}
        }

        private void OnActiveCameraChanged(IVRAvatarHead head)
        {
            if (mCameraRig != null)
            {
                mCameraRig.usePerEyeCameras = head.UsePerEyeCameras;
            }
        }
        #endregion
    }
}