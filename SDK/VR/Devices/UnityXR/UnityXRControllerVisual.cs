using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Liminal.SDK.VR.Avatars.Controllers;
using Liminal.SDK.VR.Pointers;

namespace Liminal.SDK.XR
{
	public class UnityXRControllerVisual : VRControllerVisual
	{
		#region Constants
		// TODO: Figure out what these names/keys will be
		private const string GearVrControllerName = "UNKNOWN";
		private const string OculusGoControllerName = "UNKNOWN";
		private const string OculusTouchQuestAndRiftSLeftControllerName = "Oculus Touch Controller - Left";
		private const string OculusTouchQuestAndRiftSRightControllerName = "Oculus Touch Controller - Right";
		private const string OculusTouchRiftLeftControllerName = "UNKNOWN";
		private const string OculusTouchRiftRightControllerName = "UNKNOWN";
		// TODO: Also figure out what other models will be needed and add them as we do
		#endregion

		#region Statics

		#endregion

		#region Fields
		#region Public

		#endregion

		#region Private
		/// <summary>
		/// The root GameObject that represents the GearVr Controller model.
		/// </summary>
		[SerializeField] private GameObject _modelGearVrController;

		/// <summary>
		/// The root GameObject that represents the Oculus Go Controller model.
		/// </summary>
		[SerializeField] private GameObject _modelOculusGoController;

		/// <summary>
		/// The root GameObject that represents the Oculus Touch for Quest And RiftS Controller model (Left).
		/// </summary>
		[SerializeField] private GameObject _modelOculusTouchQuestAndRiftSLeftController;

		/// <summary>
		/// The root GameObject that represents the Oculus Touch for Quest And RiftS Controller model (Right).
		/// </summary>
		[SerializeField] private GameObject _modelOculusTouchQuestAndRiftSRightController;

		/// <summary>
		/// The root GameObject that represents the Oculus Touch for Rift Controller model (Left).
		/// </summary>
		[SerializeField] private GameObject _modelOculusTouchRiftLeftController;

		/// <summary>
		/// The root GameObject that represents the Oculus Touch for Rift Controller model (Right).
		/// </summary>
		[SerializeField] private GameObject _modelOculusTouchRiftRightController;

		private string _activeControllerName;

		private readonly Dictionary<string, GameObject> _allModels = new Dictionary<string, GameObject>();
		#endregion
		#endregion

		#region Properties
		#region Public
		public string ActiveControllerName
		{
			get => _activeControllerName;
			set
			{
				GameObject model;

				if (!string.IsNullOrEmpty(ActiveControllerName) && AllModels.TryGetValue(ActiveControllerName, out model))
				{
					model?.SetActive(false);
				}

				_activeControllerName = value;

				if (!string.IsNullOrEmpty(ActiveControllerName))
				{
					if (AllModels.TryGetValue(ActiveControllerName, out model))
					{
						model.SetActive(true);
					}
					else
					{
						Debug.LogError($"[{GetType().Name}] No key exists for '{ActiveControllerName}'");
					}
				}
			}
		}
		#endregion

		#region Private
		private Dictionary<string, GameObject> AllModels
		{
			get
			{
				if (_allModels.Count == 0)
				{
					// TODO: Once each name is determined, uncomment the appropriate line. And, fix the name above of course
					//_allModels.Add(GearVrControllerName, _modelGearVrController);
					//_allModels.Add(OculusGoControllerName, _modelOculusGoController);
					_allModels.Add(OculusTouchQuestAndRiftSLeftControllerName, _modelOculusTouchQuestAndRiftSLeftController);
					_allModels.Add(OculusTouchQuestAndRiftSRightControllerName, _modelOculusTouchQuestAndRiftSRightController);
					//_allModels.Add(OculusTouchRiftLeftControllerName, _modelOculusTouchRiftLeftController);
					//_allModels.Add(OculusTouchRiftRightControllerName, _modelOculusTouchRiftRightController);
				}

				return _allModels;
			}
		}
		#endregion
		#endregion

		#region Mono
		protected override void Awake()
		{
			base.Awake();

			// disable all children
			foreach (Transform child in transform)
			{
				child.gameObject.SetActive(false);
			}

			// ensure the pointer is re-enabled
			PointerVisual.transform.gameObject.SetActive(true);
			PointerVisual.SetActive(true);
		}
		#endregion
	}
}