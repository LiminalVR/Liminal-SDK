using System;
using Liminal.SDK.Extensions;

public static class UnityPackageManagerUtils
{
    public const string sdkName = "Liminal.SDK";
    public const string sdkSeperator = "Liminal\\SDK\\Assemblies";

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
}