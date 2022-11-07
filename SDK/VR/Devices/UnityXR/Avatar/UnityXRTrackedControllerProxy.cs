using Liminal.SDK.VR.Avatars;
using System.Collections;
using System.Collections.Generic;
using OVR.OpenVR;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace Liminal.SDK.XR
{
    /// <summary>
    /// 
    /// </summary>
    [DisallowMultipleComponent]
    public class UnityXRTrackedControllerProxy : IVRTrackedObjectProxy
    {
        public bool IsActive => Controller.gameObject.activeInHierarchy;
        public Vector3 Position => Controller.transform.position;
        public Quaternion Rotation => Controller.transform.rotation;

        public ActionBasedController Controller;

        public UnityXRTrackedControllerProxy(ActionBasedController controller, IVRAvatar avatar)
        {
            Controller = controller;
        }
    }

    /// <summary>
    /// Not used because this approach doesn't scale to our use case of attached inside of head.
    /// </summary>
    public class UnityXRHeadTrackProxy : IVRTrackedObjectProxy
    {
        public bool IsActive { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }

        public UnityXRHeadTrackProxy(XROrigin XROrigin, IVRAvatar avatar)
        {
            var eyePosition = XROrigin.Camera.transform.localPosition;
        }
    }
}