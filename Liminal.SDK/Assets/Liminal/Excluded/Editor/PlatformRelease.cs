using UnityEditor;

namespace Liminal.SDK.Build
{
    public static class PlatformRelease
    {
        //
        // The following folders are included in the unity package when built.
        //
        private static readonly string[] AssetFolders = new string[]
        {
            "Assets/Liminal/SDK/Assemblies",
            "Assets/Liminal/SDK/Assets",
            "Assets/Liminal/SDK/Frameworks",
            "Assets/Liminal/SDK/Prefabs",
            "Assets/Liminal/SDK/Tests",
            "Assets/Liminal/SDK/VR",
        };

        /// <summary>
        /// Builds the platform release unitypackage.
        /// </summary>
        [MenuItem("Liminal/SDK/Build Platform Release package")]
        public static void Build()
        {
            var filename = "../../Builds/liminalsdk.platformrelease.unitypackage";
            AssetDatabase.ExportPackage(AssetFolders, filename, ExportPackageOptions.IncludeDependencies | ExportPackageOptions.Recurse | ExportPackageOptions.Interactive);
        }
    }
}