using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Avatars.Controllers;
using UnityEngine;
using UnityEngine.XR;

namespace Liminal.SDK.OpenVR
{
    public class OpenVRAvatar : MonoBehaviour, IVRDeviceAvatar
    {
        public static bool UseHeadY = true;
        public static bool UseHeadXZ = true;

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

        private void Start()
        {
            var eyePosition = InputTracking.GetLocalPosition(XRNode.CenterEye);
            var head = GetComponentInChildren<VRAvatarHead>();

            var headPosition = head.transform.position;

            if (UseHeadY)
                headPosition.y -= eyePosition.y;
            else
            {
                headPosition.y = 0;
            }

            if (UseHeadXZ)
            {
                head.transform.localPosition = Vector3.zero;
                headPosition = new Vector3(head.transform.position.x, headPosition.y, head.transform.position.z);

                headPosition.x -= eyePosition.x;
                headPosition.z -= eyePosition.z;
            }
            else
            {
                headPosition.x = 0;
                headPosition.z = 0;
            }

            head.transform.position = headPosition;
        }

        public IVRAvatar Avatar { get; }
    }
}
