using Liminal.SDK.Build;

[System.Serializable]
public class BuildWindowConfig
{
    public string PreviousScene = "";
    public BuildPlatform SelectedPlatform = BuildPlatform.Current;
}