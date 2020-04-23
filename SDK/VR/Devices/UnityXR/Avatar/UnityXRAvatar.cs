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
using Liminal.SDK.Extensions;
using Liminal.SDK.VR.Avatars.Extensions;

namespace Liminal.SDK.XR
{
	public class UnityXRAvatar : MonoBehaviour, IVRDeviceAvatar
	{
		#region Constants
		private const string ControllerVisualPrefabName = "UnityXRController";
		#endregion

		#region Statics
		public static EPointerActivationType PointerActivationType = EPointerActivationType.Both;
		#endregion

		#region Fields
		#region Publics

		#endregion

		#region Privates
		private IVRAvatar _avatar;

		private GazeInput _gazeInput = null;

		private readonly List<UnityXRControllerVisual> _remotes = new List<UnityXRControllerVisual>();
		#endregion
		#endregion

		#region Properties
		#region Publics
		public IVRAvatar Avatar
		{
			get
			{
				if (_avatar == null)
				{
					_avatar = GetComponentInParent<IVRAvatar>();
				}

				return _avatar;
			}
		}

		public IVRDevice Device => VRDevice.Device;
		#endregion

		#region Privates

		#endregion
		#endregion

		#region MonoBehaviour
		//protected void Awake()
		//{
		//	Initialize();
		//}

		private void OnEnable()
		{
			TrySetLimbsActive();
		}

		private void OnDestroy()
		{
			// Clean up event handlers
			if (Avatar != null && Avatar.Head != null)
			{
				Avatar.Head.ActiveCameraChanged -= OnActiveCameraChanged;
			}

			if (Device != null)
			{
				Device.InputDeviceConnected -= OnInputDeviceConnected;
				Device.InputDeviceDisconnected -= OnInputDeviceDisconnected;
			}
		}

		private void OnTransformParentChanged()
		{
			_avatar = GetComponentInParent<IVRAvatar>();
		}

		private void Update()
		{
			RecenterHmdIfRequired();
			DetectAndUpdateControllerStates();

			//if (OVRUtils.IsOculusQuest)
			//{
				DetectPointerState();
			//}

			VRDevice.Device.Update();
		}
		#endregion

		public void Initialize()
		{
			_avatar = GetComponentInParent<IVRAvatar>();
			Avatar.InitializeExtensions();

			_gazeInput = GetComponent<GazeInput>();

			// Load controller visuals for any VRAvatarController objects attached to the avatar
			var avatarControllers = GetComponentsInChildren<VRAvatarController>(includeInactive: true);
			foreach (var controller in avatarControllers)
			{
				AttachControllerVisual(controller);
			}

			// Add event listeners
			Device.InputDeviceConnected += OnInputDeviceConnected;
			Device.InputDeviceDisconnected += OnInputDeviceDisconnected;
			Avatar.Head.ActiveCameraChanged += OnActiveCameraChanged;

			SetupInitialControllerState();
		}

		private void DetectPointerState()
		{
			var device = VRDevice.Device;

			// This block of code has a lot of Null-Coalescing, which usually is dangerous but in this case we do not want to block the app.
			// A controller may disconnect and reconnect anytime.
			switch (PointerActivationType)
			{
				case EPointerActivationType.ActiveController:
					// TODO: NYI
					break;

				case EPointerActivationType.Both:
					device?.PrimaryInputDevice?.Pointer?.Activate();
					device?.SecondaryInputDevice?.Pointer?.Activate();
					break;
			}
		}

		#region Setup

		private void SetupInitialControllerState()
		{
			if (Device.InputDevices.Any(x => x is UnityXRController))
			{
				foreach (var controller in Device.InputDevices)
				{
					EnableControllerVisual(controller as UnityXRController);
				}
			}
			else
			{
				// Disable controllers and enable gaze controls
				DisableAllControllerVisuals();
			}
		}

		private void AttachControllerVisual(VRAvatarController avatarController)
		{
			var limb = avatarController.GetComponentInParent<IVRAvatarLimb>();

			Debug.Log($"[{GetType().Name}] AttachControllerVisual(avatarController:{avatarController.name}) -> limb.LimbType == '{limb.LimbType}'");

			var instance = InstantiateControllerVisual(limb);

			if (!instance)
			{
				// maybe log an error?
				Debug.LogError($"[{GetType().Name}] AttachControllerVisual(avatarController) -> failed to create and instance of the controller visual prefab");
				return;
			}

			// Create controller instance
			instance.transform.SetParentAndIdentity(avatarController.transform);
			avatarController.ControllerVisual = instance;

			var xrControllerVisual = instance.GetComponent<UnityXRControllerVisual>();
			// assign the name based on the limb
			if (limb.LimbType.TryLimbToNode(out XRNode node))
			{
				InputDevice inputDevice = InputDevices.GetDeviceAtXRNode(node);

				if (inputDevice.isValid)
				{
					xrControllerVisual.ActiveControllerName = inputDevice.name;
				}

				// if this fails, some kind of error?
			}

			_remotes.Add(xrControllerVisual);

			// Activate the controller
			var active = !string.IsNullOrEmpty(xrControllerVisual.ActiveControllerName);

			Debug.Log($"Attached Controller: {limb.LimbType} and SetActive: {active} Controller Type set to: {xrControllerVisual.ActiveControllerName}");
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
			{
				throw new ArgumentNullException("limb");
			}

			if (limb.LimbType == VRAvatarLimbType.Head)
			{
				return null;
			}

			var prefab = VRAvatarHelper.EnsureLoadPrefab<VRControllerVisual>(ControllerVisualPrefabName);
			var instance = Instantiate(prefab);

			instance.gameObject.SetActive(true);

			return instance;
		}

		private void EnableControllerVisual(UnityXRController controller)
		{
			if (controller == null)
				return;

			Debug.Log($"[{GetType().Name}] EnableControllerVisual(controller:{controller.Name})");

			// Find the visual for the hand that matches the controller
			UnityXRControllerVisual remote = _remotes.FirstOrDefault(r => r.ActiveControllerName == controller.InputDevice.name);
			if (remote != null)
			{
				remote.gameObject.SetActive(true);
			}
		}

		private void DisableAllControllerVisuals()
		{
			Debug.Log($"[{GetType().Name}] DisableAllControllerVisuals()");

			// Disable all controller visuals
			foreach (var remote in _remotes)
			{
				remote.gameObject.SetActive(false);
			}
		}
		#endregion

		/// <summary>
		/// Detects and Updates the state of the controllers including the TouchPad on the GearVR headset
		/// </summary>
		public void DetectAndUpdateControllerStates()
		{
			TrySetLimbsActive();
			TrySetGazeInputActive(false);
			//TrySetGazeInputActive(!IsHandControllerActive);
		}

		/// <summary>
		/// A temporary method to split Oculus Quest changes with the other devices. 
		/// </summary>
		private void TrySetLimbsActive()
		{
			TrySetHandsActive(true);

			//if (OVRUtils.IsOculusQuest)
			//{
			TrySetHandActive(VRAvatarLimbType.RightHand);
			TrySetHandActive(VRAvatarLimbType.LeftHand);
			//}
			//else
			//{
			//    TrySetHandsActive(IsHandControllerActive);
			//}
		}

		private void TrySetHandActive(VRAvatarLimbType limbType)
		{
			if (limbType.TryLimbToNode(out XRNode node))
			{
				var isLimbConnected = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(node).isValid;
				var limb = Avatar.GetLimb(limbType);

				limb.SetActive(isLimbConnected);
			}
		}

		private void TrySetHandsActive(bool active)
		{
			if (Avatar != null)
			{
				Avatar.SetHandsActive(active);
			}
		}

		private void TrySetGazeInputActive(bool active)
		{
			// Ignore Always & Never Policy
			if (_gazeInput != null && _gazeInput.ActivationPolicy == GazeInputActivationPolicy.NoControllers)
			{
				if (active)
				{
					_gazeInput.Activate();
				}
				else
				{
					_gazeInput.Deactivate();
				}
			}
		}

		private void RecenterHmdIfRequired()
		{
			//throw new NotImplementedException();

			//if (mSettings != null && mSettings.HmdRecenterPolicy != HmdRecenterPolicy.OnControllerRecenter)
			//    return;

			//if (OVRInput.GetControllerWasRecentered())
			//{
			//    // Recenter the camera when the user recenters the controller
			//    UnityEngine.XR.InputTracking.Recenter();
			//}
		}


		#region Event Handlers
		//Notes: Device Connecting is difference than controller being active
		private void OnInputDeviceConnected(IVRDevice vrDevice, IVRInputDevice inputDevice)
		{
			var unityController = inputDevice as UnityXRController;
			if (unityController != null)
			{
				// A controller was connected
				// Disable gaze controls
				EnableControllerVisual(unityController);
			}
		}

		private void OnInputDeviceDisconnected(IVRDevice vrDevice, IVRInputDevice inputDevice)
		{
			if (!vrDevice.InputDevices.Any(x => x is UnityXRController))
			{
				// No controllers are connected
				DisableAllControllerVisuals();
				// Enable gaze controls
				TrySetGazeInputActive(true);
			}
		}

		private void OnActiveCameraChanged(IVRAvatarHead head)
		{

		}
		#endregion
	}
}