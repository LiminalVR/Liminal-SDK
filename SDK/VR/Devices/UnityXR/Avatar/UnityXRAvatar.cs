#if UNITY_XR
using System.Collections.Generic;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Avatars.Controllers;
using Liminal.SDK.VR.Input;
using Liminal.SDK.VR.Pointers;
using UnityEngine;
using UnityEngine.XR;
using System;
using System.Linq;
using Liminal.SDK;
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
		#endregion

		#region Fields
		#region Public

		#endregion

		#region Private
		private IVRAvatar _avatar;

		/// <summary>
		/// Not in use at the moment
		/// TODO: Implement GazeInput functionality
		/// </summary>
		private GazeInput _gazeInput = null;

		private readonly List<UnityXRControllerVisual> _remotes = new List<UnityXRControllerVisual>();
		#endregion
		#endregion

		#region Properties
		#region Public
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

		#region Private

		#endregion
		#endregion

		#region MonoBehaviour
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

		/// <summary>
		/// When the parent changes, ensure the Avatar is correct
		/// </summary>
		private void OnTransformParentChanged()
		{
			_avatar = GetComponentInParent<IVRAvatar>();
		}

		private void Update()
		{
			// this needs to be called each frame so that the input values are properly updated
			VRDevice.Device.Update();
		}

		private void LateUpdate()
		{
			(Device as UnityXRDevice)?.LateUpdate();
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

			// turn on the pointers
			Device?.PrimaryInputDevice?.Pointer?.Activate();
			Device?.SecondaryInputDevice?.Pointer?.Activate();
		}

		#region Setup
		private void SetupInitialControllerState()
		{
			// there are controllers present
			if (Device.InputDevices.Any(x => x is UnityXRController))
			{
				// So enable all of them
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
			InputDevice inputDevice = default;
			// assign the name based on the limb
			if (limb.LimbType.TryConvertToXRNode(out XRNode node))
			{
				inputDevice = InputDevices.GetDeviceAtXRNode(node);

				if (inputDevice.isValid)
				{
					xrControllerVisual.ActiveControllerName = inputDevice.name;
				}

				// if this fails, some kind of error?
			}

			UnityXRDevice xrDevice = Device as UnityXRDevice;
			IVRInputDevice vrInputDevice = xrDevice.XRInputs.Where(xr => xr.InputDevice == inputDevice).FirstOrDefault();

			var pointerVisual = avatarController.GetComponentInChildren<LaserPointerVisual>(includeInactive: true);
			pointerVisual?.Bind(vrInputDevice.Pointer);
			if (pointerVisual != null)
				vrInputDevice.Pointer.Transform = pointerVisual.transform;

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

			// there is no controller visual is the limb is not a hand
			switch (limb.LimbType)
			{
				// this two cases will catch the hands and allows more types to be added to VRAvatarLimbType without this needing to be changed
				case VRAvatarLimbType.LeftHand:
				case VRAvatarLimbType.RightHand:
					break;
				default:
					return null;
			}

			var prefab = VRAvatarHelper.EnsureLoadPrefab<VRControllerVisual>(ControllerVisualPrefabName);
			var instance = Instantiate(prefab);

			instance.gameObject.SetActive(true);

			return instance;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="controller"></param>
		private void EnableControllerVisual(UnityXRController controller)
		{
			if (controller == null)
			{
				return;
			}

			// Find the visual for the hand that matches the controller
			UnityXRControllerVisual remote = _remotes.FirstOrDefault(r => r.ActiveControllerName == controller.InputDevice.name);
			if (remote != null)
			{
				remote.gameObject.SetActive(true);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		private void DisableAllControllerVisuals()
		{
			// Disable all controller visuals
			foreach (var remote in _remotes)
			{
				remote.gameObject.SetActive(false);
			}
		}
		#endregion

		#region Event Handlers
		private void OnInputDeviceConnected(IVRDevice vrDevice, IVRInputDevice inputDevice)
		{
			var unityController = inputDevice as UnityXRController;
			if (unityController != null)
			{
				// A controller was connected
				// Disable gaze controls
				// Enable visual
				EnableControllerVisual(unityController);
			}
		}

		private void OnInputDeviceDisconnected(IVRDevice vrDevice, IVRInputDevice inputDevice)
		{
			// if there aren't any more controllers connected
			if (!vrDevice.InputDevices.Any(x => x is UnityXRController))
			{
				// Disable all visuals
				DisableAllControllerVisuals();
				// Enable gaze controls
			}
		}

		private void OnActiveCameraChanged(IVRAvatarHead head)
		{

		}
		#endregion
	}
}
#endif