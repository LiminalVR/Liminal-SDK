using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Avatars.Controllers;
using UnityEngine;

namespace Liminal.SDK.OpenVR
{
    public class OpenVRAvatar : MonoBehaviour, IVRDeviceAvatar
    {
        public VRControllerVisual InstantiateControllerVisual(IVRAvatarLimb limb)
        {
            // Leave this to OpenVR
            return null;
        }

        public IVRAvatar Avatar { get; }
    }
}
