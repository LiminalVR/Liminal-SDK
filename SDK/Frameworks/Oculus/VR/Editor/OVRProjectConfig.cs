/************************************************************************************

Copyright   :   Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Licensed under the Oculus SDK License Version 3.4.1 (the "License");
you may not use the Oculus SDK except in compliance with the License,
which is provided at the time of installation or download, or which
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

https://developer.oculus.com/licenses/sdk-3.4.1

Unless required by applicable law or agreed to in writing, the Oculus SDK
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

************************************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;

/// <summary>
/// This class has been modified.
/// Due to Liminal including OVR as a package, creating a config inside the package is not possible so instead
/// this script has been modifed to create and read from Liminal/Resources.
/// </summary>
[System.Serializable]
public class OVRProjectConfig : ScriptableObject
{
	public enum DeviceType
	{
		GearVrOrGo = 0,
		Quest = 1
	}

	public List<DeviceType> targetDeviceTypes;

	//public const string OculusProjectConfigAssetPath = "Assets/Oculus/OculusProjectConfig.asset";

	private static string GetOculusProjectConfigAssetPath()
	{
		var so = ScriptableObject.CreateInstance(typeof(OVRPluginUpdaterStub));
		var script = MonoScript.FromScriptableObject(so);
		string assetPath = AssetDatabase.GetAssetPath(script);
		string editorDir = Directory.GetParent(assetPath).FullName;
		string ovrDir = Directory.GetParent(editorDir).FullName;
		string oculusDir = Directory.GetParent(ovrDir).FullName;
		string configAssetPath = Path.GetFullPath(Path.Combine(oculusDir, "OculusProjectConfig.asset"));
		Uri configUri = new Uri(configAssetPath);
		Uri projectUri = new Uri(Application.dataPath);
		Uri relativeUri = projectUri.MakeRelativeUri(configUri);

		return relativeUri.ToString();
	}

    private static string ProjectConfigPath => "Assets/Liminal/Resources/OculusProjectConfig.asset";

	public static OVRProjectConfig GetProjectConfig()
	{
		OVRProjectConfig projectConfig = null;

        // The only change to the script's logic is here.
        // string oculusProjectConfigAssetPath = GetOculusProjectConfigAssetPath();

        var oculusProjectConfigAssetPath = ProjectConfigPath;

        try
        {
            projectConfig = AssetDatabase.LoadAssetAtPath(oculusProjectConfigAssetPath, typeof(OVRProjectConfig)) as OVRProjectConfig;
        }
        catch (System.Exception e)
		{
			Debug.LogWarningFormat("Unable to load ProjectConfig from {0}, error {1}", oculusProjectConfigAssetPath, e.Message);
		}
		if (projectConfig == null)
		{
            Debug.Log($"Recreating project config at {oculusProjectConfigAssetPath}");
			projectConfig = ScriptableObject.CreateInstance<OVRProjectConfig>();
			projectConfig.targetDeviceTypes = new List<DeviceType>();
			projectConfig.targetDeviceTypes.Add(DeviceType.GearVrOrGo);

            AssetDatabase.CreateAsset(projectConfig, oculusProjectConfigAssetPath);
		}

		return projectConfig;
	}

	public static void CommitProjectConfig(OVRProjectConfig projectConfig)
	{
		string oculusProjectConfigAssetPath = GetOculusProjectConfigAssetPath();
		if (AssetDatabase.GetAssetPath(projectConfig) != oculusProjectConfigAssetPath)
		{
			Debug.LogWarningFormat("The asset path of ProjectConfig is wrong. Expect {0}, get {1}", oculusProjectConfigAssetPath, AssetDatabase.GetAssetPath(projectConfig));
		}
		EditorUtility.SetDirty(projectConfig);
	}
}
