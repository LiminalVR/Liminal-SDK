using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Liminal.SDK.VR.Avatars.Controllers;

namespace Liminal.SDK.XR
{
	public class UnityXRControllerVisual : VRControllerVisual
	{
		#region Constants
		private const string OculusTouchQuestAndRiftSLeftControllerName = "Oculus Touch Controller - Left";
		private const string OculusTouchQuestAndRiftSRightControllerName = "Oculus Touch Controller - Right";
		#endregion

		#region Statics

		#endregion

		#region Fields
		#region Publics

		#endregion

		#region Privates
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
		#region Publics
		public string ActiveControllerName
		{
			get => _activeControllerName;
			set
			{
				GameObject model;
				if (!string.IsNullOrEmpty(ActiveControllerName) && _allModels.TryGetValue(ActiveControllerName, out model))
				{
					model?.SetActive(false);
				}

				_activeControllerName = value;

				if (!string.IsNullOrEmpty(ActiveControllerName))
				{
					if (_allModels.TryGetValue(ActiveControllerName, out model))
					{
						model?.SetActive(true);
					}
					else
					{
						Debug.LogError($"[{GetType().Name}] No key exists for '{ActiveControllerName}'");
					}
				}
			}
		}
		#endregion

		#region Privates

		#endregion
		#endregion

		#region Mono
		protected override void Awake()
		{
			base.Awake();

			//_allModels.Add(_modelGearVrController);
			//_allModels.Add(_modelOculusGoController);
			_allModels.Add(OculusTouchQuestAndRiftSLeftControllerName, _modelOculusTouchQuestAndRiftSLeftController);
			_allModels.Add(OculusTouchQuestAndRiftSRightControllerName, _modelOculusTouchQuestAndRiftSRightController);
			//_allModels.Add(_modelOculusTouchRiftLeftController);
			//_allModels.Add(_modelOculusTouchRiftRightController);

			DisableAll();
		}
		#endregion

		#region UnityXRControllerVisual
		private void DisableAll()
		{
			foreach (GameObject model in _allModels.Values)
			{
				model.SetActive(false);
			}
		}
		#endregion
	}
}