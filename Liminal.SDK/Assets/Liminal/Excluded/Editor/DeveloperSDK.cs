using UnityEditor;

namespace Liminal.SDK.Build
{
    public static class DeveloperSDK
    {
        //
        // The following folders are included in the unity package when built.
        //
        private static readonly string[] AssetFolders = new string[]
        {
            "Assets/Liminal/liminalapp.json",
            "Assets/Liminal/TemplateScene.unity",
            "Assets/Liminal/Examples",
            "Assets/Liminal/SDK/Assemblies",
            "Assets/Liminal/SDK/Assets",
            "Assets/Liminal/SDK/Frameworks",
            "Assets/Liminal/SDK/Plugins",
            "Assets/Liminal/SDK/Prefabs",
            "Assets/Liminal/SDK/Tools",
            "Assets/Liminal/SDK/VR",
        };
        
        /// <summary>
        /// Builds the Developer SDK unitypackage.
        /// </summary>
        [MenuItem("Liminal/SDK/Build Developer SDK package")]
        public static void Build()
        {
            var filename = "../../Builds/liminalsdk.unitypackage";
            AssetDatabase.ExportPackage(AssetFolders, filename, ExportPackageOptions.IncludeDependencies | ExportPackageOptions.Recurse | ExportPackageOptions.Interactive);
        }
    }
}