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

		private readonly List<IVRInputDevice> _inputDevicesList = new List<IVRInputDevice>();
		private readonly List<InputDevice> _workingXRInputDevicesList = new List<InputDevice>();
		#endregion
		#endregion

		#region Properties
		#region Publics
		public string Name => "UnityXR";
		public int InputDeviceCount => _inputDevicesList.Count;

		public IVRHeadset Headset { get; }
		public IEnumerable<IVRInputDevice> InputDevices { get => _inputDevicesList; }

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
			// setup the headset
			InputDevice headsetDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.Head);
			if (!headsetDevice.isValid)
				throw new System.Exception("headsetDevice is not valid");
			Headset = new UnityXRHeadset(headsetDevice);
			
			//PrimaryInputDevice = _rightController = new UnityXRController(VRInputDeviceHand.Right);
			//SecondaryInputDevice = _leftController = new UnityXRController(VRInputDeviceHand.Left);
			UpdateConnectedControllers();

			XRDevice.deviceLoaded += XRDevice_deviceLoaded;
			UnityEngine.XR.InputDevices.deviceConfigChanged += InputDevices_deviceConfigChanged;
			UnityEngine.XR.InputDevices.deviceConnected += InputDevices_deviceConnected;
			UnityEngine.XR.InputDevices.deviceDisconnected += InputDevices_deviceDisconnected;
		}
		#endregion
		/// <summary>
		/// Updates once per Tick from VRDeviceMonitor (const 0.5 seconds)
		/// </summary>
		public void Update ()
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
            var rig = new GameObject("Rig");
            rig.transform.SetParent(avatar.Transform);
            rig.transform.position = avatar.Head.Transform.position;

			unityAvatar.gameObject.SetActive(true);
			SetupManager(avatar);
			SetupCameraRig(avatar, rig.transform);
            SetupControllers(avatar, rig.transform);
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
			}

			// Check left hand
			workingHandDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
			if (workingHandDevice.isValid)
			{
				SecondaryInputDevice = _leftController = new UnityXRController(VRInputDeviceHand.Left, workingHandDevice);
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
		/// 
		/// </summary>
		/// <param name="inputDevice"></param>
		/// <param name="hand"></param>
		/// <param name="xrHand"></param>
		public void SetupControllers(IVRInputDevice inputDevice, IVRAvatarHand hand, Transform xrHand)
		{
			hand.Transform.SetParent(xrHand);
			hand.Transform.localPosition = Vector3.zero;
        }

		private void SetDefaultPointerActivation()
		{
			PrimaryInputDevice?.Pointer?.Activate();
			SecondaryInputDevice?.Pointer?.Activate();
		}

		private void UpdateConnectedControllers()
		{
			//var allControllers = new List<IVRInputDevice>();
			//var disconnectedList = new List<IVRInputDevice>();
			//var connectedList = new List<IVRInputDevice>();
			//XRInputs.Clear();

			//var ctrlMask = GetControllerMask();

			//#region Controllers
			//bool isRightHandPresent = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.RightHand).isValid;

			//if (isRightHandPresent)
			//{
			//	_rightController = _rightController ?? new UnityXRController(VRInputDeviceHand.Right);
			//	if (!_inputDevicesList.Contains(_rightController))
			//	{
			//		connectedList.Add(_rightController);
			//	}

			//	XRInputs.Add(_rightController);
			//	allControllers.Add(_rightController);
			//}
			//else
			//{
			//	//if (mInputDevicesList.Contains(mRightController))
			//	//{
			//	disconnectedList.Add(_rightController);
			//	//}
			//}

			//bool isLeftHandPresent = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).isValid;

			//if (isLeftHandPresent)
			//{
			//	_leftController = _leftController ?? new UnityXRController(VRInputDeviceHand.Left);

			//	if (!_inputDevicesList.Contains(_leftController))
			//	{
			//		connectedList.Add(_leftController);
			//	}

			//	XRInputs.Add(_leftController);
			//	allControllers.Add(_leftController);
			//}
			//else
			//{
			//	//if (mInputDevicesList.Contains(mLeftController))
			//	//{
			//	disconnectedList.Add(_leftController);
			//	//}
			//} 
			//#endregion

			//_inputDevicesList.Clear();
			//_inputDevicesList.AddRange(allControllers);
			//_ControllerMask = ctrlMask;

			//disconnectedList.ForEach(device => InputDeviceDisconnected?.Invoke(this, device));
			//connectedList.ForEach(device => InputDeviceConnected?.Invoke(this, device));

			//UpdateInputDevices();
		}

		private void UpdateInputDevices()
		{
			// determined by handedness?
			PrimaryInputDevice = _rightController;
			SecondaryInputDevice = _leftController;

			PrimaryInputDeviceChanged?.Invoke(this);
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
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="obj"></param>
		private void InputDevices_deviceDisconnected(InputDevice obj)
		{
			Debug.Log($"[{GetType().Name}] InputDevices_deviceDisconnected({nameof(obj)}:{InputDeviceToString(obj)})");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="obj"></param>
		private void InputDevices_deviceConfigChanged(InputDevice obj)
		{
			Debug.Log($"[{GetType().Name}] InputDevices_deviceConfigChanged({nameof(obj)}:{InputDeviceToString(obj)})");
		}

		private string InputDeviceToString(InputDevice device)
		{
			return device.ToString();
		}
		#endregion
	}
}