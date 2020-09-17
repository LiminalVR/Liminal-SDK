namespace App
{
    using UnityEngine;
    using Liminal.SDK.VR.Avatars;

    public class VrAvatarSelfInitializer
        : MonoBehaviour
    {
        private void Awake()
        {
            var avatar = GetComponent<VRAvatar>();
            new VrDeviceInitializer().Initialize(avatar);
        }
    }
}
