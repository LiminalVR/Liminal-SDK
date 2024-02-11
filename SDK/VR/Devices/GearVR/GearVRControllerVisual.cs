using Liminal.SDK.VR.Avatars.Controllers;
using UnityEngine;

namespace Liminal.SDK.VR.Devices.GearVR
{
    public class GearVRControllerVisual : VRControllerVisual
    {
        [SerializeField] private OVRControllerHelper trackedRemote = null;
        private bool isOculusGo;
        public Vector3 QuestPosition = new Vector3(0, -0.0095f, 0);

        public Transform Quest3LeftPosition;
        public Transform Quest3RightPosition;


        protected override void Awake()
        {
            isOculusGo = GearVRDevice.IsOculusGo;
            base.Awake();
        }

        private void LateUpdate()
        {
            PointerVisual.transform.position = trackedRemote.m_controller == OVRInput.Controller.LTouch ? Quest3LeftPosition.position : Quest3RightPosition.position;
        }
    }
}
