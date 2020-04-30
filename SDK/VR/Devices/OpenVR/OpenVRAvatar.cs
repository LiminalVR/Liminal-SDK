using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Avatars.Controllers;
using UnityEngine;

namespace Liminal.SDK.OpenVR
{
    public class OpenVRAvatar : MonoBehaviour, IVRDeviceAvatar
    {
        public VRControllerVisual InstantiateControllerVisual(IVRAvatarLimb limb)
        {
            // We don't need to make the controllers. SteamVR handles it all. 
            return null;
        }

        private void Awake()
        {
            var avatar  = GetComponentInParent<IVRAvatar>();
            avatar.InitializeExtensions();
        }

        private void Update()
        {
        }

        public IVRAvatar Avatar { get; }
    }
}
