using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

public class LiminalSDKResources : EditorWindow
{
    [MenuItem("Examples/GUILayout TextField")]
    static void Init()
    {
        EditorWindow window = GetWindow(typeof(LiminalSDKResources));
        window.Show();
    }
    void OnGUI()
    {
        if (GUILayout.Button("Test"))
        {
            SetupLightweightShaders();
        }
    }

    private void SetupLightweightShaders()
    {
        if (!Directory.Exists(SDKResourcesConsts.LiminalSDKResourcesPath))
            Directory.CreateDirectory(SDKResourcesConsts.LiminalSDKResourcesPath);

        if (!Directory.Exists(SDKResourcesConsts.LWRPShaderResourcesPath))
            Directory.CreateDirectory(SDKResourcesConsts.LWRPShaderResourcesPath);

        var shadersPath = $"{UnityPackageManagerUtils.FullPackageLocation}{SDKResourcesConsts.PackageLWRPShaders}";

        var files = Directory.GetFiles(shadersPath).Where(name => !name.EndsWith(".meta"));

        foreach (var file in files)
        {
            File.Copy(file, SDKResourcesConsts.LWRPShaderResourcesPath + $"/{Path.GetFileName(file)}");
        }


        //check if shaders exist


        //Debug.Log(shadersPath);
        //File.Copy(shadersPath, BuildWindowConsts.PreviewAppScenePath);
        AssetDatabase.Refresh();
    }
}
