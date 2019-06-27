using Liminal.SDK.VR.Avatars.Controllers;
using UnityEngine;
using UnityEngine.Assertions;

namespace Liminal.SDK.VR.Devices.GearVR
{
    public class GearVRControllerVisual : VRControllerVisual
    {
        [SerializeField] private OVRControllerHelper trackedRemote = null;
        private bool isOculusGo;

        protected override void Awake()
        {
            isOculusGo = GearVRDevice.IsOculusGo;
            base.Awake();
        }

        //#TODO set up proper listener
        void Update()
        {
            return;
            var model = isOculusGo ? trackedRemote.m_modelOculusGoController : trackedRemote.m_modelGearVrController;
            Assert.IsNotNull(model, "GearVRControllerVisual model is null");
            Assert.IsNotNull(PointerVisual, "GearVRControllerVisual PointerVisual is null");

            if ((model != null) && (PointerVisual != null))
            {
                PointerVisual.transform.SetParent(model.transform);
            }
        }
    }
}
