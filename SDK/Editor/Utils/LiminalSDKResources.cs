using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using Newtonsoft.Json;

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

        if (ContainsLightweightRenderPipeline())
        {
            CopyShaders(SDKResourcesConsts.LWRPShaderResourcesPath, lwrpFiles);
        }

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

    static bool ContainsLightweightRenderPipeline()
    {
        var lightweightEnabled = false;

        if (!File.Exists(UnityPackageManagerUtils.ManifestPath))
            return false;

        var manifest = JsonConvert.DeserializeObject<Manifest>(File.ReadAllText(UnityPackageManagerUtils.ManifestPath));
        lightweightEnabled = manifest.Dependencies.ComUnityRenderPipelinesLightweight != null;

        return lightweightEnabled;
    }

    public partial class Manifest
    {
        [JsonProperty("dependencies")]
        public Dependencies Dependencies { get; set; }
    }

    public partial class Dependencies
    {
        [JsonProperty("com.unity.render-pipelines.lightweight")]
        public string ComUnityRenderPipelinesLightweight { get; set; }
    }
}
