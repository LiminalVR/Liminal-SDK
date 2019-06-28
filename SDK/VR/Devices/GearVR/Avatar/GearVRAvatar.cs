using Liminal.SDK.Extensions;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Avatars.Controllers;
using Liminal.SDK.VR.Avatars.Extensions;
using Liminal.SDK.VR.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Liminal.SDK.VR.Devices.GearVR.Avatar
{
    /// <summary>
    /// A device-specific implementation of <see cref="IVRDeviceAvatar"/> to prepare an <see cref="IVRAvatar"/> for Samsung's GearVR hardware.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public class GearVRAvatar : MonoBehaviour, IVRDeviceAvatar
    {
        private GazeInput mGazeInput = null;

        private const string ControllerVisualPrefabName = "GearVRController";
        private const int TargetFramerate = 72;
        private readonly List<OVRControllerHelper> mRemotes = new List<OVRControllerHelper>();

        private IVRAvatar mAvatar;
        private IVRDevice mDevice;
        private GearVRAvatarSettings mSettings;
        private GearVRTrackedControllerProxy mControllerTracker;

        // OVR
        private OVRManager mManager;
        private OVRCameraRig mCameraRig;

        // Cached state values
        private OVRInput.Controller mCachedActiveController;

        #region Properties

        /// <summary>
        /// Gets the <see cref="IVRAvatar"/> for this device avatar.
        /// </summary>
        public IVRAvatar Avatar
        {
            get
            {
                if (mAvatar == null)
                    mAvatar = GetComponentInParent<IVRAvatar>();

                return mAvatar;
            }
        }

        #endregion

        #region MonoBehaviour

        private void Awake()
        {
            mAvatar = GetComponentInParent<IVRAvatar>();
            mAvatar.InitializeExtensions();
            mControllerTracker = new GearVRTrackedControllerProxy(mAvatar);

            mDevice = VRDevice.Device;
            mSettings = gameObject.GetOrAddComponent<GearVRAvatarSettings>();
            mGazeInput = GetComponent<GazeInput>();

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

        private void OnEnable()
        {
            TrySetHandsActive(IsHandControllerActive);
        }

        private void OnDestroy()
        {
            // Clean up event handlers
            if (mAvatar != null)
            {
                if (mAvatar.Head != null)
                    mAvatar.Head.ActiveCameraChanged -= OnActiveCameraChanged;
            }

            if (mDevice != null)
            {
                mDevice.InputDeviceConnected -= OnInputDeviceConnected;
                mDevice.InputDeviceDisconnected -= OnInputDeviceDisconnected;
            }
        }

        private void OnTransformParentChanged()
        {
            mAvatar = GetComponentInParent<IVRAvatar>();
        }

        private void Update()
        {
            if (mCachedActiveController != OVRInput.GetActiveController())
            {
                UpdateHandedness();
            }

            TrySetHandsActive(IsHandControllerActive);
            RecenterHmdIfRequired();
        }

        #endregion

        #region Setup

        private void SetupManager()
        {
            if (OVRManager.instance == null)
            {
                Debug.Log("[GearVR] Adding OVRManager");
                var go = new GameObject("OVRManager");
                mManager = go.AddComponent<OVRManager>();
                DontDestroyOnLoad(go);
            }
            else
            {
                mManager = OVRManager.instance;
            }
        }

        private void SetupCameraRig()
        {
            var cameraRigPrefab = VRAvatarHelper.EnsureLoadPrefab<GearVRCameraRig>("GearVRCameraRig");
            cameraRigPrefab.gameObject.SetActive(false);
            mCameraRig = Instantiate(cameraRigPrefab);
            mCameraRig.transform.SetParentAndIdentity(mAvatar.Auxiliaries);

            OnActiveCameraChanged(mAvatar.Head);
        }
        
        private void SetupInitialControllerState()
        {
            if (mDevice.InputDevices.Any(x => x is GearVRController))
            {
                foreach (var controller in mDevice.InputDevices)
                {
                    EnableController(controller as GearVRController);
                }
            }
            else
            {
                // Disable controllers and enable gaze controls
                DisableAllControllers();
            }
        }
        
        private void AttachControllerVisual(VRAvatarController avatarController)
        {
            Debug.Log("attaching controller " + avatarController.name);

            var prefab = VRAvatarHelper.EnsureLoadPrefab<VRControllerVisual>(ControllerVisualPrefabName);
            prefab.gameObject.SetActive(false);

            // Create controller instance
            var instance = Instantiate(prefab);
            instance.name = prefab.name;
            instance.transform.SetParentAndIdentity(avatarController.transform);

            // Make sure the OVRGearVrController component exists...
            var trackedRemote = instance.gameObject.GetComponent<OVRControllerHelper>();

            if (trackedRemote == null)
                trackedRemote = instance.gameObject.AddComponent<OVRControllerHelper>();

            avatarController.ControllerVisual = instance;
            mRemotes.Add(trackedRemote);

            // Assign the correct controller based on the limb type the controller is attached to
            var limb = avatarController.GetComponentInParent<IVRAvatarLimb>();
            OVRInput.Controller controllerType = GetControllerTypeForLimb(limb);
            trackedRemote.m_controller = controllerType;
            trackedRemote.m_modelGearVrController.SetActive(true);

            // Activate the controller
            var active = IsControllerConnected(controllerType);
            instance.gameObject.SetActive(active);
        }

        /// <summary>
        /// Replacement for OVRInput.IsControllerConnected() that handles None properly
        /// and works with masks as well as single enum values
        /// </summary>
        private bool IsControllerConnected(OVRInput.Controller controllerMask)
        {
            return (controllerMask != OVRInput.Controller.None)
                && ((controllerMask & OVRInput.GetConnectedControllers()) != 0);
        }

        #endregion

        #region Controllers

        /// <summary>
        /// Instantiates a <see cref="VRControllerVisual"/> for a limb.
        /// </summary>
        /// <param name="limb">The limb for the controller.</param>
        /// <returns>The newly instantiated controller visual for the specified limb, or null if no controller visual was able to be created.</returns>
        public VRControllerVisual InstantiateControllerVisual(IVRAvatarLimb limb)
        {
            if (limb == null)
                throw new ArgumentNullException("limb");

            if (limb.LimbType == VRAvatarLimbType.Head)
                return null;

            Debug.LogFormat("GearVRAvatar.InstantiateControllerVisual() {0}", limb.LimbType);

            var prefab = VRAvatarHelper.EnsureLoadPrefab<VRControllerVisual>(ControllerVisualPrefabName);
            var instance = Instantiate(prefab);

            var ovrController = instance.GetComponent<OVRControllerHelper>();
            ovrController.m_controller = GetControllerTypeForLimb(limb);
            ovrController.m_modelGearVrController.SetActive(true);
            ovrController.enabled = false;

            instance.gameObject.SetActive(true);
            return instance;
        }

        private void EnableController(GearVRController controller)
        {
            if (controller == null)
                return;

            Debug.LogFormat("GearVRAvatar.EnableController() {0}", controller.ControllerMask);

            // Find the visual for the hand that matches the controller
            var remote = mRemotes.FirstOrDefault(x => (x.m_controller & controller.ControllerMask) != 0);
            if (remote != null)
            {
                remote.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning("No controller visual found");
            }
        }

        private void DisableAllControllers()
        {
            // Disable all controller visuals
            foreach (var remote in mRemotes)
            {
                remote.gameObject.SetActive(false);
            }
        }
        
        #endregion

        private void UpdateHandedness()
        {
            Debug.Log("[GearVR] UpdateHandedness()");

            // GetActiveController seems to always return Touch and not TouchL or TouchR will be a problem when we support 2 hands.
            mCachedActiveController = OVRInput.GetActiveController();

            // GearVR only supports a single controller, so the tracker for the currently active.
            // controller is always assigned to the avatar's primary hand - the secondary hand is not tracked.
            // and is always deactivated.
            bool isHandController = ((mCachedActiveController & GearVRController.AllHandControllersMask) != 0);
            mAvatar.PrimaryHand.TrackedObject = isHandController ? mControllerTracker : null;

            if (isHandController)
            {
                mAvatar.PrimaryHand.TrackedObject = mControllerTracker;
                foreach (var remote in mRemotes)
                {
                    remote.m_controller = GetControllerTypeForLimb(mAvatar.PrimaryHand);
                }
            }

            var secondary = mAvatar.SecondaryHand;
            secondary.TrackedObject = null;
            secondary.SetActive(false);
        }

        private bool IsHandControllerActive
        {
            get
            {
                return (OVRInput.GetActiveController() & GearVRController.AllHandControllersMask) != 0;
            }
        }

        private void TrySetHandsActive(bool active)
        {
            if (mAvatar != null)
            {
                mAvatar.SetHandsActive(active);
            }
        }

        private void RecenterHmdIfRequired()
        {
            if (mSettings != null && mSettings.HmdRecenterPolicy != HmdRecenterPolicy.OnControllerRecenter)
                return;

            if (OVRInput.GetControllerWasRecentered())
            {
                // Recenter the camera when the user recenters the controller
                UnityEngine.XR.InputTracking.Recenter();
            }
        }

        private OVRInput.Controller GetControllerTypeForLimb(IVRAvatarLimb limb)
        {
            if (limb.LimbType == VRAvatarLimbType.LeftHand)
            {
                // if you're on the quest return Touch
                return OVRInput.Controller.LTouch;
            }
            if (limb.LimbType == VRAvatarLimbType.RightHand)
            {
                return OVRInput.Controller.RTouch;
            }

            return OVRInput.Controller.None;
        }

        #region Event Handlers

        //Notes: Device Connecting is difference than controller being active
        private void OnInputDeviceConnected(IVRDevice vrDevice, IVRInputDevice inputDevice)
        {
            var gearController = inputDevice as GearVRController;
            if (gearController != null)
            {
                // A controller was connected
                // Disable gaze controls
                EnableController(gearController);
            }
        }

        private void OnInputDeviceDisconnected(IVRDevice vrDevice, IVRInputDevice inputDevice)
        {
            if (!vrDevice.InputDevices.Any(x => x is GearVRController))
            {
                // No controllers are connected
                // Enable gaze controls
                DisableAllControllers();
            }
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
