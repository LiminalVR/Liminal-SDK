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
        public Vector3 Position => Controller.transform.position;
        public Quaternion Rotation => Controller.transform.rotation;

        public PicoVRTrackedControllerProxy(Pvr_ControllerModuleInit controller, Transform head, Transform avatar)
        {
            Controller = controller;
            _avatar = avatar;
            _head = head;
        }
    }
}