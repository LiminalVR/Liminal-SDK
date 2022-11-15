using Liminal.SDK.Core;
using System;
using UnityEngine;
using App;

namespace Liminal.SDK.VR
{
    /// <summary>
    /// The entry component for initializing the <see cref="VRDevice"/> system using emulation for the editor.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("VR/Emulator Setup")]
    public class VREmulator : MonoBehaviour, IVRDeviceInitializer
    {
        [Header("Quest Link is not supported with Emulator mode. Using OVR may lead to camera calculation errors.")]
        public ESDKType BuildType = ESDKType.OVR;
        public ESDKType EditorType = ESDKType.Emulator;

        /// <summary>
        /// Creates a new <see cref="IVRDevice"/> and returns it.
        /// </summary>
        /// <returns>The <see cref="IVRDevice"/> that was created.</returns>
        public IVRDevice CreateDevice()
        {
            EditorType = ESDKType.Emulator;
            BuildType = ESDKType.OVR;

#if UNITY_EDITOR
            return DeviceUtils.CreateDevice(EditorType);
#else
            return DeviceUtils.CreateDevice(BuildType);
#endif
        }
    }
}
