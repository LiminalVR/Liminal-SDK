using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

[InitializeOnLoad]
public class LiminalSDKResources : EditorWindow
{
    static LiminalSDKResources()
    {
        SetupLightweightShaders();
    }

    static void SetupLightweightShaders()
    {
        Directory.CreateDirectory(SDKResourcesConsts.LiminalSDKResourcesPath);
        Directory.CreateDirectory(SDKResourcesConsts.LWRPShaderResourcesPath);
        Directory.CreateDirectory(SDKResourcesConsts.GVRShaderResourcesPath);

        var lwrpShadersPath = $"{UnityPackageManagerUtils.FullPackageLocation}{SDKResourcesConsts.PackageLWRPShaders}";
        var gvrShadersPath = $"{UnityPackageManagerUtils.FullPackageLocation}{SDKResourcesConsts.PackageGVRShaders}";

        var lwrpFiles = Directory.GetFiles(lwrpShadersPath).Where(name => !name.EndsWith(".meta")).ToList();
        var gvrFiles = Directory.GetFiles(gvrShadersPath).Where(name => !name.EndsWith(".meta")).ToList();

        CopyShaders(SDKResourcesConsts.LWRPShaderResourcesPath, lwrpFiles);
        CopyShaders(SDKResourcesConsts.GVRShaderResourcesPath, gvrFiles);

        AssetDatabase.Refresh();
    }

    static void CopyShaders(string location, List<string> files)
    {
        foreach (var file in files)
        {
            if (File.Exists(location + $"/{Path.GetFileName(file)}"))
                continue;

            File.Copy(file, location + $"/{Path.GetFileName(file)}");
        }
    }
}
