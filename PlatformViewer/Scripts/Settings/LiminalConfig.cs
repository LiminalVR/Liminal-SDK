using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.WSA.Input;

public class LiminalConfig
{
    public ExperienceProfile SavedProfile;
    public ExperienceProfile ProfileToApply;

    public void Apply()
    {
        SaveProfile();

        GraphicsSettings.renderPipelineAsset = ProfileToApply.PipelineAsset;
    }

    private void SaveProfile()
    {
        var newProfile = ScriptableObject.CreateInstance<ExperienceProfile>();

        newProfile.Init();

        SavedProfile = newProfile;
    }

    public void Release()
    {
        GraphicsSettings.renderPipelineAsset = SavedProfile.PipelineAsset;
    }
}
