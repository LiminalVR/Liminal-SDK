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
using UnityEngine.Assertions;
using System.Runtime.InteropServices.WindowsRuntime;

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
		public IEnumerable<IVRInputDevice> InputDevices { get => mInputDevicesList; }
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

			// this is needed for the system to know that there are connected controllers
            OVRInput.Update();

			PrimaryInputDevice = mRightController = new UnityXRController(VRInputDeviceHand.Right);
			SecondaryInputDevice = mLeftController = new UnityXRController(VRInputDeviceHand.Left);

			UpdateConnectedControllers();

			XRDevice.deviceLoaded += XRDevice_deviceLoaded;
			UnityEngine.XR.InputDevices.deviceConfigChanged += InputDevices_deviceConfigChanged;
			UnityEngine.XR.InputDevices.deviceConnected += InputDevices_deviceConnected;
			UnityEngine.XR.InputDevices.deviceDisconnected += InputDevices_deviceDisconnected;
		}

		private void InputDevices_deviceDisconnected(InputDevice obj)
		{
			Debug.Log($"[{GetType().Name}] InputDevices_deviceDisconnected({nameof(obj)}:{InputDeviceToString(obj)})");
		}

		private void InputDevices_deviceConnected(InputDevice obj)
		{
			Debug.Log($"[{GetType().Name}] InputDevices_deviceConnected({nameof(obj)}:{InputDeviceToString(obj)})");
		}

		private void InputDevices_deviceConfigChanged(InputDevice obj)
		{
			Debug.Log($"[{GetType().Name}] InputDevices_deviceConfigChanged({nameof(obj)}:{InputDeviceToString(obj)})");
		}

		private void XRDevice_deviceLoaded(string loadedDevice)
		{
			Debug.Log($"[{GetType().Name}] XRDevice_deviceLoaded({nameof(loadedDevice)}:{loadedDevice})");
		}

		private string InputDeviceToString(InputDevice device)
		{
			return device.ToString();
		}
		#endregion

		private UnityXRControllerMask GetControllerMask()
		{
			UnityXRControllerMask mask = UnityXRControllerMask.None;
			
			foreach (var kvp in mNodes)
			{ 
				if (UnityEngine.XR.InputDevices.GetDeviceAtXRNode(kvp.Key).isValid)
					mask |= kvp.Value;
			}

			return mask;
		}

		/// <summary>
		/// Updates once per Tick from VRDeviceMonitor (const 0.5 seconds)
		/// </summary>
		public void Update()
		{
			// check if the controller state has changed
			if (mControllerMask != GetControllerMask())
			{
				UpdateConnectedControllers();
			}

			foreach (var input in XRInputs)
			{
				input.Update();
			}
		}

		public bool HasCapabilities(VRDeviceCapability capabilities)
		{
			return (_capabilities & capabilities) == capabilities;
		}

		public void SetupAvatar(IVRAvatar avatar)
		{
			Assert.IsNotNull(avatar);

			// allow the UnityXRAvatar to handle the rest of the setup
			avatar.Transform.gameObject.AddComponent<UnityXRAvatar>();
			
			UpdateConnectedControllers();

			var primaryHandPrefab = Resources.Load("RightHand Controller");
			var primaryHand = Object.Instantiate(primaryHandPrefab, avatar.Transform) as GameObject;
			var secondaryHandPrefab = Resources.Load("LeftHand Controller");
			var secondaryHand = Object.Instantiate(secondaryHandPrefab, avatar.Transform) as GameObject;
			SetupControllerPointer(PrimaryInputDevice, avatar.PrimaryHand, primaryHand.transform);
			SetupControllerPointer(SecondaryInputDevice, avatar.SecondaryHand, secondaryHand.transform);
			avatar.Head.Transform.localPosition = Vector3.zero;
			
			SetDefaultPointerActivation();
		}

		public void SetupControllerPointer(IVRInputDevice inputDevice, IVRAvatarHand hand, Transform xrHand)
		{
			hand.Transform.SetParent(xrHand);
			var pointer = xrHand.GetComponentInChildren<LaserPointerVisual>(includeInactive: true);
			var controllerVisual = hand.Transform.GetComponentInChildren<VRAvatarController>(includeInactive: true);
			if (pointer != null)
			{
				if (controllerVisual != null)
				{
					pointer.Bind(inputDevice.Pointer);
					inputDevice.Pointer.Transform = pointer.transform;
					pointer.transform.SetParent(controllerVisual.transform);
				}
				else
				{
					Object.Destroy(pointer.gameObject);
				}
			}
		}

		private void SetDefaultPointerActivation()
		{
			PrimaryInputDevice?.Pointer?.Activate();
			SecondaryInputDevice?.Pointer?.Activate();
		}

		private void UpdateConnectedControllers()
		{
			var allControllers = new List<IVRInputDevice>();
			var disconnectedList = new List<IVRInputDevice>();
			var connectedList = new List<IVRInputDevice>();
			XRInputs.Clear();

			var ctrlMask = GetControllerMask();

			#region Controllers
			bool isRightHandPresent = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.RightHand).isValid;

			if (isRightHandPresent)
			{
				mRightController = mRightController ?? new UnityXRController(VRInputDeviceHand.Right);
				if (!mInputDevicesList.Contains(mRightController))
				{
					connectedList.Add(mRightController);
				}

				XRInputs.Add(mRightController);
				allControllers.Add(mRightController);
			}
			else
			{
				//if (mInputDevicesList.Contains(mRightController))
				//{
				disconnectedList.Add(mRightController);
				//}
			}

			bool isLeftHandPresent = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).isValid;

			if (isLeftHandPresent)
			{
				mLeftController = mLeftController ?? new UnityXRController(VRInputDeviceHand.Left);

				if (!mInputDevicesList.Contains(mLeftController))
				{
					connectedList.Add(mLeftController);
				}

				XRInputs.Add(mLeftController);
				allControllers.Add(mLeftController);
			}
			else
			{
				//if (mInputDevicesList.Contains(mLeftController))
				//{
				disconnectedList.Add(mLeftController);
				//}
			} 
			#endregion

			mInputDevicesList.Clear();
			mInputDevicesList.AddRange(allControllers);
			mControllerMask = ctrlMask;

			disconnectedList.ForEach(device => InputDeviceDisconnected?.Invoke(this, device));
			connectedList.ForEach(device => InputDeviceConnected?.Invoke(this, device));

			UpdateInputDevices();
		}

		private void UpdateInputDevices()
		{
			//UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.)

			// determined by handedness?
			PrimaryInputDevice = mRightController;
			SecondaryInputDevice = mLeftController;

			PrimaryInputDeviceChanged?.Invoke(this);
		}
	}
}