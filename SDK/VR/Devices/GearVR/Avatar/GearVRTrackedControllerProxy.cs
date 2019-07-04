using Liminal.SDK.VR.Avatars;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Liminal.SDK.VR.Devices.GearVR.Avatar
{
    /// <summary>
    /// A concrete implementation of <see cref="IVRTrackedObjectProxy"/> for wrapping around a tracked GearVR controller.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public class GearVRTrackedControllerProxy : IVRTrackedObjectProxy
    {
        private IVRAvatar mAvatar;
        private Transform mAvatarTransform;
        private Transform mHeadTransform;
        #region Properties

        public bool IsActive { get { return true; } }

        public Vector3 Position
        {
            get
            {
                // The controller position is relative to the head
                var localPos = mHeadTransform.localPosition + OVRInput.GetLocalControllerPosition(ActiveController());
                return mAvatarTransform.TransformPoint(localPos);
            }
        }

        public Quaternion Rotation
        {
            get
            {
                // Controller rotation is relative to the head
                return mHeadTransform.rotation * OVRInput.GetLocalControllerRotation(ActiveController());
            }
        }
        
        #endregion

        /// <summary>
        /// Creates a new <see cref="GearVRTrackedControllerProxy"/> for the specified avatar and controller type.
        /// </summary>
        /// <param name="avatar">The avatar that owns the controller.</param>
        /// <param name="controllerType">The controller type the proxy wraps.</param>
        public GearVRTrackedControllerProxy(IVRAvatar avatar)
        {
            mAvatar = avatar;
            mAvatarTransform = mAvatar.Transform;
            mHeadTransform = mAvatar.Head.Transform;
        }

        private static OVRInput.Controller ActiveController()
        {
            OVRInput.Controller controller = OVRInput.GetActiveController();

            var hasController = OVRInput.IsControllerConnected(OVRInput.Controller.Touch);

            // In Editor, active controller is Touch, representing both hand controllers.
            // But for querying position/rotation we need to specify
            if (hasController)
                controller = OVRInput.Controller.RTouch;

            return controller;
        }
    }
}
