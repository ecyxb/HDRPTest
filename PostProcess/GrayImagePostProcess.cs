using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System;
using Unity.VisualScripting;


[Serializable, VolumeComponentMenu("Post-processing/Custom/GrayImagePostProcess")]
public sealed class GrayImagePostProcess : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    public BoolParameter directOutput = new BoolParameter(false);
    public RenderTextureParameter outputTexture = new RenderTextureParameter(null);
    Material m_Material;

    public bool IsActive() => m_Material != null && (directOutput.GetValue<bool>() || outputTexture.GetValue<RenderTexture>() != null);

    // Do not forget to add this post process in the Custom Post Process Orders list (Project Settings > Graphics > HDRP Global Settings).
    public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

    const string kShaderName = "Shader Graphs/GrayImage";
    public string kRenderTextureName = "GrayCameraComputeRT";

    public override void Setup()
    {
        if (Shader.Find(kShaderName) != null)
            m_Material = new Material(Shader.Find(kShaderName));
        else
            Debug.LogError($"Unable to find shader '{kShaderName}'. Post Process Volume GrayImageVolum is unable to load.");
    }

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
    {
        if (!IsActive())
            return;
        m_Material.SetTexture("_InputTexture", source);
        var ot = outputTexture.GetValue<RenderTexture>();
        if (ot != null)
        {
            Graphics.Blit(source, ot, m_Material);
        }

        if (directOutput.GetValue<bool>())
        {
            // Directly output to the camera's render texture
            HDUtils.DrawFullScreen(cmd, m_Material, destination, shaderPassId: 0);
        } else {
            HDUtils.BlitCameraTexture(cmd, source, destination);
        } 
    }

    public override void Cleanup()
    {
        CoreUtils.Destroy(m_Material);
    }
}
