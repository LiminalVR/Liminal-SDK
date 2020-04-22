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
	public class UnityXRDevice : IVRDevice
	{
		private static readonly VRDeviceCapability _capabilities = VRDeviceCapability.Controller | VRDeviceCapability.UserPrescenceDetection;

		public string Name => "UnityXR";
		public int InputDeviceCount => 2;

		public IVRHeadset Headset => new UnityXRHeadset();
		public IEnumerable<IVRInputDevice> InputDevices { get; }

		public IVRInputDevice PrimaryInputDevice { get; }
		public IVRInputDevice SecondaryInputDevice { get; }

		public List<UnityXRInputDevice> XRInputs = new List<UnityXRInputDevice>();

		public int CpuLevel { get; set; }
		public int GpuLevel { get; set; }

		public event VRInputDeviceEventHandler InputDeviceConnected;
		public event VRInputDeviceEventHandler InputDeviceDisconnected;
		public event VRDeviceEventHandler PrimaryInputDeviceChanged;

		public UnityXRDevice()
		{
			var primary = new UnityXRInputDevice(VRInputDeviceHand.Right);
			var secondary = new UnityXRInputDevice(VRInputDeviceHand.Left);

			PrimaryInputDevice = primary;
			SecondaryInputDevice = secondary;

			XRInputs.Add(primary);
			XRInputs.Add(secondary);

			InputDevices = new List<IVRInputDevice>
			{
				PrimaryInputDevice,
				SecondaryInputDevice,
			};
		}

		public bool HasCapabilities(VRDeviceCapability capabilities)
		{
			return (_capabilities & capabilities) == capabilities;
		}

		public void SetupAvatar(IVRAvatar avatar)
		{
			avatar.Transform.gameObject.AddComponent<UnityXRAvatar>();
			var manager = new GameObject().AddComponent<XRInteractionManager>();

			var avatarGo = avatar.Transform.gameObject;

			var xrRig = avatarGo.AddComponent<XRRig>();
			var centerEye = avatar.Head.CenterEyeCamera.gameObject;
			var eyeDriver = centerEye.AddComponent<TrackedPoseDriver>();
			eyeDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
			eyeDriver.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRDevice, TrackedPoseDriver.TrackedPose.Center);
			xrRig.cameraGameObject = centerEye.gameObject;
			xrRig.TrackingOriginMode = TrackingOriginModeFlags.Floor;

			// primary hand
			var primaryHandPrefab = Resources.Load("RightHand Controller");
			var primaryHand = Object.Instantiate(primaryHandPrefab, avatar.Transform) as GameObject;
			avatar.PrimaryHand.Transform.SetParent(primaryHand.transform);
			var primaryPointer = primaryHand.GetComponentInChildren<LaserPointerVisual>();
			primaryPointer?.Bind(PrimaryInputDevice.Pointer);
			if (primaryPointer != null)
				PrimaryInputDevice.Pointer.Transform = primaryPointer.transform;

			// secondary hand
			var secondaryHandPrefab = Resources.Load("LeftHand Controller");
			var secondaryHand = Object.Instantiate(secondaryHandPrefab, avatar.Transform) as GameObject;

			avatar.SecondaryHand.Transform.SetParent(secondaryHand.transform);
			var secondaryPointer = secondaryHand.GetComponentInChildren<LaserPointerVisual>();
			secondaryPointer?.Bind(SecondaryInputDevice.Pointer);
			if (secondaryPointer != null)
				SecondaryInputDevice.Pointer.Transform = secondaryPointer.transform;

			avatar.Head.Transform.localPosition = Vector3.zero;
			//UpdateConnectedControllers();
			//SetDefaultPointerActivation();
		}

		public void Update()
		{
			foreach (var input in XRInputs)
			{
				input.Update();
			}
		}
	}
}