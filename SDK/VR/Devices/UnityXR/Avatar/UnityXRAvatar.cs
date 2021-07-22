#if UNITY_XR
using System.Collections.Generic;
using Liminal.SDK.VR;
using Liminal.SDK.VR.Avatars;
using Liminal.SDK.VR.Avatars.Controllers;
using Liminal.SDK.VR.Input;
using UnityEngine;
using System;
using System.Linq;
using Liminal.SDK.Extensions;
using Liminal.SDK.VR.Avatars.Extensions;

namespace Liminal.SDK.XR
{
    public class UnityXRAvatar : MonoBehaviour, IVRDeviceAvatar
    {
        public IVRAvatar Avatar { get; set; }
        private UnityXRDevice Device { get; set; }

        public VRControllerVisual InstantiateControllerVisual(IVRAvatarLimb limb)
        {
            return null;
        }

        public void Initialize(IVRAvatar avatar, UnityXRDevice unityXRDevice)
        {
            Avatar = avatar;
            avatar.InitializeExtensions();
            Device = unityXRDevice;
        }

        private void Update()
        {
            foreach (var input in Device.XRInputs)
                input.Update();
        }
    }
}
#endif