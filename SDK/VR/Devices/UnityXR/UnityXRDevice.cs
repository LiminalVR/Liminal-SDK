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

		/// <summary>
		/// Updates once per Tick from VRDeviceMonitor (const 0.5 seconds)
		/// </summary>
		public void Update ()
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

			SetupControllers(avatar, rig);
		}

        private XRRig CreateXRRig(IVRAvatar avatar)
        {
			var xrRig = GameObject.FindObjectOfType<XRRig>();
			xrRig.transform.SetParent(avatar.Transform);
			xrRig.transform.position = avatar.Head.Transform.position;
			xrRig.transform.rotation = avatar.Head.Transform.rotation;

            return xrRig;
        }

		private void SetupControllers(IVRAvatar avatar, XRRig rig)
        {
			var controllers = rig.GetComponentsInChildren<XRController>().ToList();
			var leftHand = controllers.FirstOrDefault(x => x.controllerNode == XRNode.LeftHand);
			var rightHand = controllers.FirstOrDefault(x => x.controllerNode == XRNode.RightHand);

			avatar.PrimaryHand.TrackedObject = new UnityXRTrackedControllerProxy(rightHand, avatar);
			avatar.SecondaryHand.TrackedObject = new UnityXRTrackedControllerProxy(leftHand, avatar);

			// Bind left

			// Bind right

			ActivatePointer(avatar.PrimaryHand);
			ActivatePointer(avatar.SecondaryHand);
		}

		public void ActivatePointer(IVRAvatarHand hand)
        {
			var laserPointerPrefab = Resources.Load("LaserPointer");
			var pointerVisualGO = (GameObject)GameObject.Instantiate(laserPointerPrefab, hand.Transform);
			var pointerVisual = pointerVisualGO.GetComponent<LaserPointerVisual>();
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
			xrRig.TrackingOriginMode = TrackingOriginModeFlags.TrackingReference;
			xrRig.cameraGameObject = centerEye.gameObject;

			// Attach Eye Driver to the head.
			var eyeDriver = avatar.Head.Transform.GetComponent<TrackedPoseDriver>();
			if (eyeDriver == null)
				eyeDriver = avatar.Head.Transform.gameObject.AddComponent<TrackedPoseDriver>();

			eyeDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
			eyeDriver.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRDevice, TrackedPoseDriver.TrackedPose.Center);
		}
	}
}

#endif