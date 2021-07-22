using Liminal.SDK.VR.Avatars;
using System.Collections;
using System.Collections.Generic;
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

        public XRController Controller;

        public UnityXRTrackedControllerProxy(XRController controller, IVRAvatar avatar)
        {
            Controller = controller;
        }
    }
}