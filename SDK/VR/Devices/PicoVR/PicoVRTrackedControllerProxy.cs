using Liminal.SDK.VR.Avatars;
using UnityEngine;

namespace Liminal.SDK.PicoVR
{
    public class PicoVRTrackedControllerProxy : IVRTrackedObjectProxy
    {
        private Transform _avatar;
        private Transform _head;

        public Pvr_ControllerModuleInit Controller;

        public bool IsActive => Controller.gameObject.activeInHierarchy;
        public Vector3 Position => _head.TransformPoint(Controller.transform.localPosition);
        public Quaternion Rotation => _head.rotation * Controller.transform.localRotation;

        public PicoVRTrackedControllerProxy(Pvr_ControllerModuleInit controller, Transform head, Transform avatar)
        {
            Controller = controller;
            _avatar = avatar;
            _head = head;
        }
    }
}