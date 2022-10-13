//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Prompt developers to use settings most compatible with SteamVR.
//
//=============================================================================

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Valve.VR
{
    [InitializeOnLoad]
    public class SteamVR_AutoEnableVR
    {
        static SteamVR_AutoEnableVR()
        {
            EditorApplication.update += Update;
        }

        protected const string openVRString = "OpenVR";
        protected const string openVRPackageString = "com.unity.xr.openvr.standalone";

#if UNITY_2018_2_OR_NEWER
        private enum PackageStates
        {
            None,
            WaitingForList,
            WaitingForAdd,
            WaitingForAddConfirm,
            Installed,
            Failed,
        }

        private static UnityEditor.PackageManager.Requests.ListRequest listRequest;
        private static UnityEditor.PackageManager.Requests.AddRequest addRequest;
        private static PackageStates packageState = PackageStates.None;
        private static System.Diagnostics.Stopwatch addingPackageTime = new System.Diagnostics.Stopwatch();
        private static System.Diagnostics.Stopwatch addingPackageTimeTotal = new System.Diagnostics.Stopwatch();
        private static float estimatedTimeToInstall = 80;
        private static int addTryCount = 0;
#endif

        public static void Update()
        {
            return;
        }
    }
}