using Liminal.SDK.Build;

[System.Serializable]
public class BuildWindowConfig
{
    public string PreviousScene = "";
    public string TargetScene = "";
    public BuildPlatform SelectedPlatform = BuildPlatform.Current;
}