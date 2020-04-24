#if UNITY_XR
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
	/// <summary>
	/// Mappings and further manual information available here: https://docs.unity3d.com/Manual/xr_input.html
	/// All of the below are on a per-controller basis and may or may not exist depending on the platform that it currently running
	/// 
	/// Buttons:
	/// - primaryButton
	/// - secondaryButton
	/// - secondaryTouch
	/// - gripButton
	/// - triggerButton
	/// - menuButton
	/// - primary2DAxisClick
	/// - primary2DAxisTouch
	/// - userPresence (WMR, Oculus)
	/// 
	/// Axis:
	/// - trigger
	/// - grip
	/// - batteryLevel (only WMR)
	/// 
	/// 2D Axis:
	/// - primary2DAxis
	/// - secondary2DAxis
	/// </summary>
	public class UnityXRController : UnityXRInputDevice
	{
		#region Constants
		private static string OculusTouchControllerPartName = "Oculus Touch Controller";
		#endregion

		#region Statics
		/// <summary>
		/// TODO: Update this as we expand on controller support
		/// It may need to become non-static, and it could potentially by determined based on the available 
		/// characteristics and inputFeatures of the assigned input device
		/// </summary>
		private static readonly VRInputDeviceCapability _capabilities = VRInputDeviceCapability.DirectionalInput |
																		VRInputDeviceCapability.Touch |
																		VRInputDeviceCapability.TriggerButton;
		#endregion

		#region Fields
		#region Public

		#endregion

		#region Private
		private VRInputDeviceHand _hand;
		#endregion
		#endregion

		#region Properties
		#region Public
		public override int ButtonCount => 3;
		public override bool IsTouching { get => GetButton(VRButton.Touch); }
		public override VRInputDeviceHand Hand => _hand;
		#endregion

		#region Private
		private Dictionary<string, InputFeature> ActiveInputFeatures { get; set; }
		#endregion
		#endregion

		#region Constructor
		public UnityXRController(VRInputDeviceHand hand, InputDevice inputDevice) : base(inputDevice)
		{
			_hand = hand;

			Pointer?.Activate();

			if (inputDevice.name.StartsWith(OculusTouchControllerPartName))
			{
				ActiveInputFeatures = CreateOculusQuestTouchMappings(inputDevice);
			}
			else
			{
				ActiveInputFeatures = CreateDefaultMappings(inputDevice);
			}

			foreach (var pairs in ActiveInputFeatures.ToArray())
			{
				InputFeature inputFeature = pairs.Value;

				// also register axes with their raw counterpart
				if (inputFeature is AxisInputFeature && !pairs.Key.EndsWith("Raw"))
				{
					string rawKey = $"{pairs.Key}Raw";

					if (!ActiveInputFeatures.ContainsKey(rawKey))
					{
						ActiveInputFeatures.Add(rawKey, inputFeature);
					}
				}

				inputFeature.AssignDevice(InputDevice);
			}
		}
		#endregion

		#region Mappings
		/// <summary>
		/// 
		/// </summary>
		/// <param name="inputDevice"></param>
		/// <returns></returns>
		private static Dictionary<string, InputFeature> CreateDefaultMappings(InputDevice inputDevice)
		{
			Debug.LogWarning($"[{typeof(UnityXRController).Name}] CreateDefaultMappings() used. Developers, create a suitable mapping for controllers with the name: '{inputDevice.name}'");

			var mappings = new Dictionary<string, InputFeature>();

			// buttons
			mappings.Add(VRButton.Back, new ButtonInputFeature(CommonUsages.secondaryButton));
			mappings.Add(VRButton.Touch, new ButtonInputFeature(CommonUsages.primaryTouch));

			var triggerButtonFeature = new ButtonInputFeature(CommonUsages.triggerButton);
			mappings.Add(VRButton.Trigger, triggerButtonFeature);
			mappings.Add(VRButton.One, triggerButtonFeature);
			mappings.Add(VRButton.Two, new ButtonInputFeature(CommonUsages.gripButton));
			mappings.Add(VRButton.Three, new ButtonInputFeature(CommonUsages.primary2DAxisTouch));
			mappings.Add(VRButton.Four, new ButtonInputFeature(CommonUsages.primary2DAxisClick));

			// axis 2D
			mappings.Add(VRAxis.One, new Axis2DInputFeature(CommonUsages.primary2DAxis));

			// axis 1D
			mappings.Add(VRAxis.Two, new Axis1DInputFeature(CommonUsages.trigger));
			mappings.Add(VRAxis.Three, new Axis1DInputFeature(CommonUsages.grip));

			return mappings;
		}

		/// <summary>
		/// This mapping is functional for Oculus Quest.
		/// Will need to define a minimum functional mapping to use with controllers/systems with fewer input features, as per the table:
		/// https://docs.unity3d.com/Manual/xr_input.html
		/// </summary>
		/// <param name="inputDevice"></param>
		/// <returns></returns>
		private static Dictionary<string, InputFeature> CreateOculusQuestTouchMappings(InputDevice inputDevice)
		{
			var mappings = new Dictionary<string, InputFeature>();

			// { VRButton.Back, OVRInput.Button.Back | OVRInput.Button.Two}
			mappings.Add(VRButton.Back, new ButtonInputFeature(CommonUsages.secondaryButton));

			// { VRButton.One, OVRInput.Button.PrimaryIndexTrigger},
			// also VRButton.Primary
			var triggerButtonFeature = new ButtonInputFeature(CommonUsages.triggerButton);
            mappings.Add(VRButton.One, triggerButtonFeature);

			// { VRButton.Two, OVRInput.Button.PrimaryTouchpad },
			// also VRButton.Secondary
			mappings.Add(VRButton.Two, new ButtonInputFeature(CommonUsages.primaryButton));

			// { VRButton.Touch, OVRInput.Button.PrimaryTouchpad },
			mappings.Add(VRButton.Touch, new ButtonInputFeature(CommonUsages.primaryTouch));

			// { VRButton.Trigger, OVRInput.Button.PrimaryIndexTrigger},
			mappings.Add(VRButton.Trigger, triggerButtonFeature);

			// remaining unbound button and axis values
			// VRButton.Three
			// VRButton.Four
			// VRButton.DPadUp
			// VRButton.DPadDown
			// VRButton.DPadLeft
			// VRButton.DPadRight
			// VRAxis.One | Primary
			// VRAxis.OneRaw | PrimaryRaw
			// VRAxis.Two | Secondary
			// VRAxis.TwoRaw | SecondaryRaw
			// VRAxis.Three
			// VRAxis.ThreeRaw 

			return mappings;
		}
	#endregion

	#region IVRInputDevice
	protected override IVRPointer CreatePointer()
		{
			return new InputDevicePointer(this);
		}

		public override bool HasCapabilities(VRInputDeviceCapability capabilities)
		{
			return ((_capabilities & capabilities) == capabilities);
		}

		public override bool HasAxis1D(string axis)
		{
			return ActiveInputFeatures.TryGetValue(axis, out var feature) && feature is Axis1DInputFeature;
		}

		public override bool HasAxis2D(string axis)
		{
			return ActiveInputFeatures.TryGetValue(axis, out var feature) && feature is Axis2DInputFeature;
		}

		public override bool HasButton(string button)
		{
			return ActiveInputFeatures.TryGetValue(button, out var feature) && feature is ButtonInputFeature;
		}

		public override float GetAxis1D(string axis)
		{
			if (!HasAxis1D(axis)) return 0f;

			var axis1DFeature = ActiveInputFeatures[axis] as Axis1DInputFeature;
			return axis.Contains("Raw") ? axis1DFeature.RawValue : axis1DFeature.Value;
		}

		public override Vector2 GetAxis2D(string axis)
		{
			if (!HasAxis2D(axis)) return Vector2.zero;

			var axis2DFeature = ActiveInputFeatures[axis] as Axis2DInputFeature;
			return axis.Contains("Raw") ? axis2DFeature.RawValue : axis2DFeature.Value;
		}

		public override bool GetButton(string button)
		{
			return GetButtonState(button) == EPressState.Pressing;
		}

		public override bool GetButtonDown(string button)
		{
			return GetButtonState(button) == EPressState.Down;
		}

		public override bool GetButtonUp(string button)
		{
			return GetButtonState(button) == EPressState.Up;
		}

		public EPressState GetButtonState(string button)
		{
			if (!HasButton(button)) return EPressState.None;

			var buttonFeature = ActiveInputFeatures[button] as ButtonInputFeature;
			return buttonFeature.PressState;
		}

		public override void Update()
		{
			// foreach input registered
			foreach (var feature in ActiveInputFeatures.Values)
			{
				// update it
				try
				{
					if (!feature.IsUpdated)
					{
						feature.UpdateState();
					}
				}
				catch (Exception)
				{
					Debug.LogError($"Problems occuring within {feature.Name}");
				}
			}
		}

		public override void LateUpdate()
		{
			// foreach input registered
			foreach (var feature in ActiveInputFeatures.Values)
			{
				// clean it
				try
				{
					feature.Clean();
				}
				catch (Exception)
				{
					Debug.LogError($"Problems occuring within {feature.Name}");
				}
			}
		}
		#endregion
	}
}
#endif