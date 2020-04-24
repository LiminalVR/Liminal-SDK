using Liminal.SDK.VR.Avatars;
using UnityEngine;
using Valve.VR;

namespace Liminal.SDK.OpenVR
{
    public class OpenVRTrackedControllerProxy : IVRTrackedObjectProxy
    {
        public SteamVR_Behaviour_Pose Controller;

        public bool IsActive => Controller.gameObject.activeInHierarchy;
        public Vector3 Position => Controller.transform.localPosition;
        public Quaternion Rotation => Controller.transform.localRotation;

        public OpenVRTrackedControllerProxy(SteamVR_Behaviour_Pose controller)
        {
            Controller = controller;
        }
    }
}