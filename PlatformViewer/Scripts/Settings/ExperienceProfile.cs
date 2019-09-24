using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ExperienceProfile : ScriptableObject
{
    public RenderPipelineAsset PipelineAsset;

    public void Init()
    {
        PipelineAsset = GraphicsSettings.renderPipelineAsset;
    }
}
