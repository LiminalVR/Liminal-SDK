public static class BuildWindowConsts
{
    public const string PlatformViewerFolderName = "PlatformViewer";

    /// <summary>
    /// Path to Platform Folder
    /// </summary>
    public const string PlatformFolderPath = "Assets/Liminal/" + PlatformViewerFolderName;

    /// <summary>
    /// Path to Platform Folder without assets
    /// </summary>
    public const string PackagePreviewAppScenePath = "/PlatformViewer/Scenes/PlatformAppViewer.unity";

    /// <summary>
    /// Path to Platform Folder
    /// </summary>
    public const string PlatformSceneFolderPath = PlatformFolderPath + "/Scenes";

    /// <summary>
    /// Path to the PlatformAppViewer scene
    /// </summary>
    public const string PreviewAppScenePath = PlatformSceneFolderPath + "/PlatformAppViewer.unity";

    /// <summary>
    /// Path to limapp builds
    /// </summary>
    public const string BuildPath = "Assets/_Builds";

    /// <summary>
    /// Path to Build Window Configuration
    /// </summary>
    public const string BuildWindowConfigPath = BuildPath + "/BuildWindowConfig.json";
}