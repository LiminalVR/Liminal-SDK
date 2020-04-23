using System;
using System.IO;
using Liminal.SDK.Extensions;
using UnityEngine;

public static class UnityPackageManagerUtils
{
    public const string sdkName = "Liminal.SDK";
    public const string sdkSeperator = "SDK\\Assemblies";

    /// <summary>
    /// Return the full package location to the package folder
    /// After the SDK is imported into a Third Party project, Application.Data path will return ThirdPartyProjectPath
    /// </summary>
    public static string FullPackageLocation
    {
        get
        {
            var sdkAssembly = AppDomain.CurrentDomain.GetLoadedAssembly(sdkName);
            var sdkLocation = sdkAssembly.Location;
            var liminalLocation = sdkLocation.Split(new string[] { sdkSeperator }, StringSplitOptions.None)[0];
            liminalLocation = DirectoryUtils.ReplaceBackWithForwardSlashes(liminalLocation);

            return liminalLocation;
        }
    }

    /// <summary>
    /// Return the path to the manifest at /Packages/manifest.json
    /// </summary>
    public static string ManifestPath
    {
        get
        {
            var rootProjectFolder = Directory.GetParent(Application.dataPath);
            var manifestPath = $"{rootProjectFolder}/Packages/manifest.json";
            return manifestPath;
        }
    }

    /// <summary>
    /// Return the manifest packages 
    /// </summary>
    public static string ManifestWithoutLock
    {
        get
        {
            var Quote = '"';
            var LockString = $"{Quote}lock";

            var manifestJson = File.ReadAllText(ManifestPath);
            var manifestWithoutLock = manifestJson.Split(new string[] { "}," }, StringSplitOptions.RemoveEmptyEntries)[0];
            manifestWithoutLock += "}\n}";

            return manifestWithoutLock;
        }
    }

    public static string ManifestWithoutXR
    {
        get
        {
            var manifestJson = File.ReadAllText(ManifestPath);
            var toolKit = "\"com.unity.xr.interaction.toolkit\": \"0.9.4-preview\",";
            var interaction = "\"com.unity.xr.interactionsubsystems\": \"1.0.1\",";
            var management = "\"com.unity.xr.management\": \"3.2.7\",";

            manifestJson = manifestJson.Replace(toolKit, "");
            manifestJson = manifestJson.Replace(interaction, "");
            manifestJson = manifestJson.Replace(management, "");

            return manifestJson;
        }
    }
    public static string ManifestWithXR
    {
        get
        {
            var manifestJson = File.ReadAllText(ManifestPath);

            var toolKit = "\"com.unity.xr.interaction.toolkit\": \"0.9.4-preview\",";
            var interaction = "\"com.unity.xr.interactionsubsystems\": \"1.0.1\",";
            var management = "\"com.unity.xr.management\": \"3.2.7\",";

            manifestJson = manifestJson.Replace(toolKit, "");
            manifestJson = manifestJson.Replace(interaction, "");
            manifestJson = manifestJson.Replace(management, "");

            return manifestJson;
        }
    }
}