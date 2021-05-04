using System.Collections.Generic;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Input;
using Liminal.SDK.VR.Pointers;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace Liminal.SDK.PicoVR
{
    public class PicoVRDevice : IVRDevice
    {
        public string Name => "Pico Device";
        public int InputDeviceCount => 3;
        public IVRHeadset Headset => new SimpleHeadset("Pico Headset", VRHeadsetCapability.PositionalTracking);
        
        public IEnumerable<IVRInputDevice> InputDevices { get; }
        public IVRInputDevice PrimaryInputDevice { get; }
        public IVRInputDevice SecondaryInputDevice { get; }

        public int CpuLevel { get; set; }
        public int GpuLevel { get; set; }

        public bool HasCapabilities(VRDeviceCapability capabilities) => VRDeviceCommonUtils.HasCapabilities(capabilities, VRDeviceCommonUtils.GenericDeviceCapability);

        public PicoVRDevice()
        {
            PrimaryInputDevice = new PicoVRController(VRInputDeviceHand.Right);
            SecondaryInputDevice = new PicoVRController(VRInputDeviceHand.Left);

            InputDevices = new List<IVRInputDevice>
            {
                PrimaryInputDevice,
                SecondaryInputDevice,
            };
        }

        private Transform _aux;
        private Pvr_UnitySDKHeadTrack _head;
        private IVRAvatar _avatar;

        public static GameObject Rig;

        public void SetupAvatar(IVRAvatar avatar)
        {
            var rigPrefab = Resources.Load("PicoVRRig");
            
            if (Rig == null)
            {
                var pvrRig = Object.FindObjectOfType<Pvr_UnitySDKManager>();

                if (pvrRig == null)
                    Rig = GameObject.Instantiate(rigPrefab) as GameObject;
                else
                    Rig = pvrRig.gameObject;

                Rig.transform.parent = null;
                Object.DontDestroyOnLoad(Rig);
            }

            if (Rig == null)
            {
                Debug.LogError("Rig not setup for PicoVRRIg or could not be found. Check VRDevices/PicoVR/Resources/PicoVRRig.prefab");
                return;
            }

            _avatar = avatar;
            _aux = avatar.Auxiliaries;
            _head = Rig.GetComponentInChildren<Pvr_UnitySDKHeadTrack>();

            Rig.transform.position = avatar.Transform.position;
            Rig.transform.rotation = avatar.Transform.rotation;

            _head.transform.position = avatar.Head.Transform.position;
            _head.transform.rotation = avatar.Head.Transform.rotation;

            var pvrControllers = Rig.GetComponentInChildren<Pvr_Controller>();
            var leftController = pvrControllers.controller0.GetComponent<Pvr_ControllerModuleInit>();
            var rightController = pvrControllers.controller1.GetComponent<Pvr_ControllerModuleInit>();

            avatar.PrimaryHand.TrackedObject = new PicoVRTrackedControllerProxy(rightController, avatar.Head.Transform, avatar.Transform);
            avatar.SecondaryHand.TrackedObject = new PicoVRTrackedControllerProxy(leftController, avatar.Head.Transform, avatar.Transform);
            avatar.Head.TrackedObject = new PicoVRTrackedHeadsetProxy(_head.transform);

            BindPointer(leftController, avatar.SecondaryHand);
            BindPointer(rightController, avatar.PrimaryHand);

            avatar.InitializeExtensions();
        }

        public void BindPointer(MonoBehaviour controller, IVRAvatarHand hand)
        {
            // If a laser pointer visual is not found, this means the prefab for each controller don't have a laser in it.
            var pointerVisual = controller.GetComponentInChildren<LaserPointerVisual>(includeInactive: true);
            pointerVisual.Bind(hand.InputDevice.Pointer);
            hand.InputDevice.Pointer.Transform = pointerVisual.transform;
        }

        // This is called once every 0.5 seconds.
        public void Update()
        {
            if (Rig == null)
                return;

            Rig.transform.position = _aux.position;
            Rig.transform.rotation = _aux.rotation;
        }

        #region Unused

        public event VRInputDeviceEventHandler InputDeviceConnected;
        public event VRInputDeviceEventHandler InputDeviceDisconnected;
        public event VRDeviceEventHandler PrimaryInputDeviceChanged;

        #endregion
    }
}