using System.Collections.Generic;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Input;
using Liminal.SDK.VR.Pointers;
using UnityEngine;
using UnityEngine.SpatialTracking;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Object = UnityEngine.Object;
using UnityEngine.Assertions;
using Liminal.SDK.Extensions;

namespace Liminal.SDK.XR
{
	/// <summary>
	/// IVRDevice implementation for the UnityXR system
	/// 
	/// UnityXR supports many systems, so individual UnityXR-prefixed scripts will handle internal wrapping or feature-specific restrictions for now.
	/// </summary>
	public class UnityXRDevice : IVRDevice
	{
		#region Constants

		#endregion

		#region Statics
		private static readonly VRDeviceCapability _capabilities =
			VRDeviceCapability.Controller |
			// Is this VRDeviceCapability needed? Will having it in break things? ... only time will tell
			VRDeviceCapability.DualController |
			VRDeviceCapability.UserPrescenceDetection;
		#endregion

		#region Fields
		#region Publics

		#endregion

		#region Privates
		private UnityXRController _rightController;
		private UnityXRController _leftController;
		#endregion
		#endregion

		#region Properties
		#region Publics
		public string Name => "UnityXR";
		public int InputDeviceCount => XRInputs.Count;

		public IVRHeadset Headset { get; protected set; }
		public IEnumerable<IVRInputDevice> InputDevices { get => XRInputs; }

		public IVRInputDevice PrimaryInputDevice { get; private set; }
		public IVRInputDevice SecondaryInputDevice { get; private set; }

		public List<UnityXRInputDevice> XRInputs { get; } = new List<UnityXRInputDevice>();

		public int CpuLevel { get; set; }
		public int GpuLevel { get; set; }
		#endregion

		#region Privates
		#endregion
		#endregion

		#region Events
		public event VRInputDeviceEventHandler InputDeviceConnected;
		public event VRInputDeviceEventHandler InputDeviceDisconnected;
		public event VRDeviceEventHandler PrimaryInputDeviceChanged;
		#endregion

		#region Constructors
		public UnityXRDevice()
		{
//<<<<<<< HEAD
			// setup the headset
			InputDevice headsetDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.Head);

#if !UNITY_EDITOR
			if (!headsetDevice.isValid)
				throw new System.Exception("headsetDevice is not valid");
#endif

			UnityXRHeadset xrHeadset = new UnityXRHeadset(headsetDevice);
			XRInputs.Add(xrHeadset);
			Headset = xrHeadset;

			XRDevice.deviceLoaded += XRDevice_deviceLoaded;
			UnityEngine.XR.InputDevices.deviceConfigChanged += InputDevices_deviceConfigChanged;
			UnityEngine.XR.InputDevices.deviceConnected += InputDevices_deviceConnected;
			UnityEngine.XR.InputDevices.deviceDisconnected += InputDevices_deviceDisconnected;
//=======
//			Headset = new UnityXRHeadset();
//			PrimaryInputDevice = mRightController = new UnityXRController(VRInputDeviceHand.Right);
//			SecondaryInputDevice = mLeftController = new UnityXRController(VRInputDeviceHand.Left);

//			UpdateConnectedControllers();
//>>>>>>> develop
		}
		#endregion
		/// <summary>
		/// Updates once per Tick from VRDeviceMonitor (const 0.5 seconds)
		/// </summary>
		public void Update()
		{
			foreach (var input in XRInputs)
				input.Update();
		}

		public bool HasCapabilities(VRDeviceCapability capabilities)
		{
			return (_capabilities & capabilities) == capabilities;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="avatar"></param>
		public void SetupAvatar(IVRAvatar avatar)
		{
			Assert.IsNotNull(avatar);

            var unityAvatar = avatar.Transform.gameObject.AddComponent<UnityXRAvatar>();
            unityAvatar.gameObject.SetActive(true);

			var rig = CreateXrRig(avatar);
			SetupManager(avatar);
            SetupCameraRig(avatar, rig);
            SetupControllers(avatar, rig);

            unityAvatar.Initialize();
		}

        private Transform CreateXrRig(IVRAvatar avatar)
        {
            var rig = new GameObject("Rig");
            rig.transform.SetParent(avatar.Transform);
            rig.transform.position = avatar.Head.Transform.position;
            rig.transform.rotation = avatar.Head.Transform.rotation;

            return rig.transform;
        }

		/// <summary>
		/// Setup the Manager
		/// </summary>
		/// <param name="avatar"></param>
		private void SetupManager(IVRAvatar avatar)
		{
			// find the XRInteractionManager
			var interactionManager = GameObject.FindObjectOfType<XRInteractionManager>();

			// if there is one, do no more
			if (interactionManager != null)
				return;

			// create one
			var manager = new GameObject("XRInteractionManager").AddComponent<XRInteractionManager>();
			GameObject.DontDestroyOnLoad(manager.gameObject);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="avatar"></param>
		/// <param name="rig"></param>
		private void SetupCameraRig(IVRAvatar avatar, Transform rig)
		{
			var avatarGo = avatar.Transform.gameObject;
			var xrRig = avatarGo.AddComponent<XRRig>();
			var centerEye = avatar.Head.CenterEyeCamera.gameObject;
			var eyeDriver = centerEye.AddComponent<TrackedPoseDriver>();
			eyeDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
			eyeDriver.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRDevice, TrackedPoseDriver.TrackedPose.Center);
			xrRig.cameraGameObject = centerEye.gameObject;
			xrRig.TrackingOriginMode = TrackingOriginModeFlags.TrackingReference;

			avatar.Head.Transform.SetParent(rig.transform);
			avatar.Head.Transform.localPosition = Vector3.zero;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="avatar"></param>
		/// <param name="rig"></param>
		private void SetupControllers(IVRAvatar avatar, Transform rig)
		{
			// create Hand Controllers?
			InputDevice workingHandDevice = default;

			// Check right hand
			workingHandDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
			if (workingHandDevice.isValid)
			{
				PrimaryInputDevice = _rightController = new UnityXRController(VRInputDeviceHand.Right, workingHandDevice);
				XRInputs.Add(_rightController);
			}

			// Check left hand
			workingHandDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
			if (workingHandDevice.isValid)
			{
				SecondaryInputDevice = _leftController = new UnityXRController(VRInputDeviceHand.Left, workingHandDevice);
				XRInputs.Add(_leftController);
			}

			// need to go in 
			var primaryHandPrefab = Resources.Load("RightHand Controller");
			var primaryHand = Object.Instantiate(primaryHandPrefab, rig) as GameObject;
			var secondaryHandPrefab = Resources.Load("LeftHand Controller");
			var secondaryHand = Object.Instantiate(secondaryHandPrefab, rig) as GameObject;
			SetupControllers(PrimaryInputDevice, avatar.PrimaryHand, primaryHand.transform);
			SetupControllers(SecondaryInputDevice, avatar.SecondaryHand, secondaryHand.transform);
			avatar.Head.Transform.localPosition = Vector3.zero;

			SetDefaultPointerActivation();
		}

		/// <summary>
		/// TODO: Come up with a better name?
		/// </summary>
		/// <param name="inputDevice">not actually used?</param>
		/// <param name="hand"></param>
		/// <param name="xrHand"></param>
		public void SetupControllers(IVRInputDevice inputDevice, IVRAvatarHand hand, Transform xrHand)
		{
			hand.Transform.SetParent(xrHand);
			hand.Transform.localPosition = Vector3.zero;
			hand.Transform.localRotation = Quaternion.identity;
		}

		/// <summary>
		/// 
		/// </summary>
		private void SetDefaultPointerActivation()
		{
			PrimaryInputDevice?.Pointer?.Activate();
			SecondaryInputDevice?.Pointer?.Activate();
		}

		#region Events
		/// <summary>
		/// 
		/// </summary>
		/// <param name="loadedDevice"></param>
		private void XRDevice_deviceLoaded(string loadedDevice)
		{
			// is this needed for anything?
			Debug.Log($"[{GetType().Name}] XRDevice_deviceLoaded({nameof(loadedDevice)}:{loadedDevice})");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="obj"></param>
		private void InputDevices_deviceConnected(InputDevice obj)
		{
			Debug.Log($"[{GetType().Name}] InputDevices_deviceConnected({nameof(obj)}:{InputDeviceToString(obj)})");

			IVRInputDevice inputDevice = null;

			bool isHeadset = (obj.characteristics & InputDeviceCharacteristics.HeadMounted) != 0;
			bool isHeldInHand = (obj.characteristics & InputDeviceCharacteristics.HeldInHand) != 0;

			if (isHeadset)
			{
				// headset was connected, so create a new one
				UnityXRHeadset headset = new UnityXRHeadset(obj);

				Headset = headset;
				inputDevice = headset;
			}
			else if (isHeldInHand)
			{
				bool isLeftHand = (obj.characteristics & InputDeviceCharacteristics.Left) != 0;
				bool isRightHand = (obj.characteristics & InputDeviceCharacteristics.Right) != 0;

				if (isLeftHand)
				{
					SecondaryInputDevice = _leftController = new UnityXRController(VRInputDeviceHand.Left, obj);
					XRInputs.Add(_leftController);
				}
				else if (isRightHand)
				{
					PrimaryInputDevice = _rightController = new UnityXRController(VRInputDeviceHand.Right, obj);
					XRInputs.Add(_rightController);
				}
			}
			else
			{
				Debug.LogError($"[{GetType().Name}] What is this? How'd you get here?! {obj.characteristics} is somehow unaccounted for ...");
			}

			InputDeviceConnected?.Invoke(this, inputDevice);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="obj"></param>
		private void InputDevices_deviceDisconnected(InputDevice obj)
		{
			Debug.Log($"[{GetType().Name}] InputDevices_deviceDisconnected({nameof(obj)}:{InputDeviceToString(obj)})");

			IVRInputDevice inputDevice = null;

			bool isHeadset = (obj.characteristics & InputDeviceCharacteristics.HeadMounted) != 0;
			bool isHeldInHand = (obj.characteristics & InputDeviceCharacteristics.HeldInHand) != 0;

			if (isHeadset)
			{
				UnityXRHeadset xrHeadset = Headset as UnityXRHeadset;
				inputDevice = xrHeadset;
				XRInputs.Remove(xrHeadset);
				Headset = null;
			}
			else if (isHeldInHand)
			{
				bool isLeftHand = (obj.characteristics & InputDeviceCharacteristics.Left) != 0;
				bool isRightHand = (obj.characteristics & InputDeviceCharacteristics.Right) != 0;

				if (isLeftHand)
				{
					inputDevice = SecondaryInputDevice;
					XRInputs.Remove(_leftController);
					SecondaryInputDevice = _leftController = null;
				}
				else if (isRightHand)
				{
					inputDevice = PrimaryInputDevice;
					XRInputs.Remove(_rightController);
					PrimaryInputDevice = _rightController = null;
				}
			}
			else
			{
				Debug.LogError($"[{GetType().Name}] What is this? How'd you get here?! {obj.characteristics} is somehow unaccounted for ...");
			}

			InputDeviceDisconnected?.Invoke(this, inputDevice);
		}

		/// <summary>
		/// Not sure when this is triggered ...
		/// </summary>
		/// <param name="obj"></param>
		private void InputDevices_deviceConfigChanged(InputDevice obj)
		{
			Debug.Log($"[{GetType().Name}] InputDevices_deviceConfigChanged({nameof(obj)}:{InputDeviceToString(obj)})");
		}

		private string InputDeviceToString(InputDevice device)
		{
			return $"N>{device.name} :: C>{device.characteristics} :: iV>{device.isValid}";
		}
		#endregion
	}
}