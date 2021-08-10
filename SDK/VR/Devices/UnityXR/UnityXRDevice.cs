#if UNITY_XR
using System.Collections.Generic;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Input;
using UnityEngine;
using UnityEngine.SpatialTracking;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Object = UnityEngine.Object;
using UnityEngine.Assertions;
using System.Linq;
using Liminal.SDK.VR.Pointers;

namespace Liminal.SDK.XR
{
	public enum UnityXRControllerMask
	{
		None = 0,
		Left = 1 << 0,
		Right = 1 << 1
	}

	/// <summary>
	/// IVRDevice implementation for the UnityXR system
	/// 
	/// UnityXR supports many systems, so individual UnityXR-prefixed scripts will handle internal wrapping or feature-specific restrictions for now.
	/// </summary>
	public class UnityXRDevice : IVRDevice
	{
		private static readonly VRDeviceCapability _capabilities = 
			VRDeviceCapability.Controller |
			// Is this VRDeviceCapability needed? Will having it in break things? ... only time will tell
			VRDeviceCapability.DualController |
			VRDeviceCapability.UserPrescenceDetection;

#region Variables
		public string Name => "UnityXR";
		public int InputDeviceCount => mInputDevicesList.Count;

		public IVRHeadset Headset { get; }
		public IEnumerable<IVRInputDevice> InputDevices { get; }
		private readonly List<IVRInputDevice> mInputDevicesList = new List<IVRInputDevice>();

		public IVRInputDevice PrimaryInputDevice { get; private set;  }
		public IVRInputDevice SecondaryInputDevice { get; private set; }

		private UnityXRController mRightController;
		private UnityXRController mLeftController;
		public List<UnityXRInputDevice> XRInputs { get; } = new List<UnityXRInputDevice>();
		private UnityXRControllerMask mControllerMask = UnityXRControllerMask.None;

		// XRNode/UnityXRController pairs to check for presence of valid controllers
		private KeyValuePair<XRNode, UnityXRControllerMask>[] mNodes =
		{
			new KeyValuePair<XRNode, UnityXRControllerMask>(XRNode.LeftHand, UnityXRControllerMask.Left),
			new KeyValuePair<XRNode, UnityXRControllerMask>(XRNode.RightHand, UnityXRControllerMask.Right)
			// head, maybe?
		};

		public int CpuLevel { get; set; }
		public int GpuLevel { get; set; }
#endregion

#region Events
		public event VRInputDeviceEventHandler InputDeviceConnected;
		public event VRInputDeviceEventHandler InputDeviceDisconnected;
		public event VRDeviceEventHandler PrimaryInputDeviceChanged;
#endregion

#region Constructors
		public UnityXRDevice()
		{
			Headset = new UnityXRHeadset();
			PrimaryInputDevice = mRightController = new UnityXRController(VRInputDeviceHand.Right, XRNode.RightHand);
			SecondaryInputDevice = mLeftController = new UnityXRController(VRInputDeviceHand.Left, XRNode.LeftHand);

			InputDevices = new List<IVRInputDevice>
			{
				PrimaryInputDevice,
				SecondaryInputDevice,
			};

			XRInputs.Add(mRightController);
			XRInputs.Add(mLeftController);
		}
		#endregion

        private IVRAvatar _currentAvatar;

		/// <summary>
		/// Updates once per Tick from VRDeviceMonitor (const 0.5 seconds)
		/// </summary>
		public void Update ()
		{
			// Turn on or off controller depending on avatar state? 
            if (_currentAvatar != null)
            {
				//_currentAvatar.PrimaryHand.
            }
		}

        private void UpdateHandVisibility()
        {
        }

		public bool HasCapabilities(VRDeviceCapability capabilities)
		{
			return (_capabilities & capabilities) == capabilities;
		}

		public void SetupAvatar(IVRAvatar avatar)
		{
			Assert.IsNotNull(avatar);

            var unityAvatar = avatar.Transform.gameObject.AddComponent<UnityXRAvatar>();
            unityAvatar.gameObject.SetActive(true);

			var rig = CreateXRRig(avatar);
			SetupManager(avatar);
            SetupCameraRig(avatar, rig);

			unityAvatar.Initialize(avatar, this);

            _currentAvatar = avatar;

			SetupControllers(avatar, rig);
		}

        private XRRig CreateXRRig(IVRAvatar avatar)
        {
			// Maybe a way to pass in one?
			// Instantiate a new one
            var xrRig = avatar.Transform.GetComponentInChildren<XRRig>(true);
            if(xrRig == null)
                xrRig = GameObject.FindObjectOfType<XRRig>();
			
            if (xrRig == null)
            {
                var xrRigPrefab = Resources.Load("XR Rig");
                var rigObject = GameObject.Instantiate(xrRigPrefab) as GameObject;
                xrRig = rigObject.GetComponent<XRRig>();
            }

			// unless you need to offset the difference...
            xrRig.transform.SetParent(avatar.Transform);
			xrRig.transform.position = avatar.Transform.position;
			xrRig.transform.rotation = avatar.Transform.rotation;

            return xrRig;
        }

		private void SetupControllers(IVRAvatar avatar, XRRig rig)
        {
			var controllers = rig.GetComponentsInChildren<XRController>().ToList();
			var leftHand = controllers.FirstOrDefault(x => x.controllerNode == XRNode.LeftHand);
			var rightHand = controllers.FirstOrDefault(x => x.controllerNode == XRNode.RightHand);

			avatar.PrimaryHand.TrackedObject = new UnityXRTrackedControllerProxy(rightHand, avatar);
			avatar.SecondaryHand.TrackedObject = new UnityXRTrackedControllerProxy(leftHand, avatar);

			ActivatePointer(avatar.PrimaryHand, rightHand);
			ActivatePointer(avatar.SecondaryHand, leftHand);

			// Right, here we set the track object to be the XR hand. 
			// We also bind the pointer. 
			// We just need to put the pointer inside the Anchor?
		}

		public void ActivatePointer(IVRAvatarHand hand, XRController xrController)
        {
			// check if the hand even have a controller attached. // Do we include inactive ones?
            var controllerComponent = hand.Transform.GetComponentInChildren<VRAvatarController>(includeInactive:true);
            if (controllerComponent == null)
            {
				Debug.Log("No avatar controller found");
                return;
            }
			
            //var controllerPrefabName = xrController.controllerNode == XRNode.LeftHand ? "Neo3_L" : "Neo3_R";
            var controllerPrefabName = xrController.controllerNode == XRNode.LeftHand ? "PicoNeo_Controller_Visual_L" : "PicoNeo_Controller_Visual_R";
			
            var prefab = Resources.Load($"Prefabs/{controllerPrefabName}");
            var controllerInstance = GameObject.Instantiate(prefab, controllerComponent.transform) as GameObject;
            controllerInstance.transform.localPosition = Vector3.zero;
            controllerInstance.transform.localEulerAngles = Vector3.zero;

			// if the controller instance has laser pointer, no pointer making one
			var pointerVisual = controllerInstance.GetComponentInChildren<LaserPointerVisual>();

            if (pointerVisual == null)
            {
                var laserPointerPrefab = Resources.Load("LaserPointer");
                var pointerVisualGO = (GameObject) GameObject.Instantiate(laserPointerPrefab, controllerComponent.transform);
                pointerVisual = pointerVisualGO.GetComponent<LaserPointerVisual>();
            }

            pointerVisual.Bind(hand.InputDevice.Pointer);

			// Apparently it's binding to the wrong one.
			var device = hand.InputDevice as UnityXRController;
			Debug.Log($"Binding: Input Device: {device.Node}, Hand: {device.Hand}, Avatar Device: {hand.Transform.name}");

			hand.InputDevice.Pointer.Transform = pointerVisual.transform;
		}

        private void SetupManager(IVRAvatar avatar)
        {
            var interactionManager = GameObject.FindObjectOfType<XRInteractionManager>();
            var manager = interactionManager ?? new GameObject("XRInteractionManager").AddComponent<XRInteractionManager>();
			GameObject.DontDestroyOnLoad(manager.gameObject);
        }

        private void SetupCameraRig(IVRAvatar avatar, XRRig xrRig)
        {
			var centerEye = avatar.Head.CenterEyeCamera.gameObject;
			xrRig.cameraGameObject = centerEye.gameObject;

			var eyeDriver = avatar.Head.Transform.GetComponent<TrackedPoseDriver>();

			// Only modify eye setting if you're making a new one.
            if (eyeDriver == null)
            {
                xrRig.TrackingOriginMode = TrackingOriginModeFlags.TrackingReference;

				eyeDriver = avatar.Head.Transform.gameObject.AddComponent<TrackedPoseDriver>();
                eyeDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
                eyeDriver.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRDevice, TrackedPoseDriver.TrackedPose.Center);
                eyeDriver.UseRelativeTransform = false;
            }

        }
	}
}

#endif