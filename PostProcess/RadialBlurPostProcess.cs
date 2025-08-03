using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System;
using UnityEngine.Rendering;

[Serializable, VolumeComponentMenu("Post-processing/Custom/RadialBlur")]
public sealed class RadialBlurPostProcess : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    [Tooltip("模糊强度")]
    public ClampedFloatParameter blurFactor = new ClampedFloatParameter(0f, 0f, 0.2f);
    
    [Tooltip("采样次数")]
    public ClampedIntParameter sampleCount = new ClampedIntParameter(6, 1, 10);
    
    [Tooltip("中心点（屏幕UV坐标）")]
    public Vector2Parameter blurCenter = new Vector2Parameter(new Vector2(0.5f, 0.5f));
    
    [Tooltip("清晰区域半径")]
    public ClampedFloatParameter clearRadius = new ClampedFloatParameter(0.1f, 0f, 0.5f);
    
    [Tooltip("过渡宽度")]
    public ClampedFloatParameter transitionWidth = new ClampedFloatParameter(0.1f, 0f, 0.3f);

    private Material _material;
    
    public bool IsActive() => blurFactor.value > 0 && _material != null;
    
    public override CustomPostProcessInjectionPoint injectionPoint => 
        CustomPostProcessInjectionPoint.AfterPostProcess; // 在运动模糊后介入:cite[1]:cite[9]

    public override void Setup()
    {
        _material = CoreUtils.CreateEngineMaterial("Post-processing/Custom/RadialBlur");
    }

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
    {
        if (_material == null) return;
        // if(camera.name.Contains("ComputeCamera")) return; // 避免在计算相机上渲染
        // if(camera.name.Contains("SceneCamera")) return; // 避免在场景相机上渲染   
        // Debug.Log($"Rendering RadialBlurPostProcess on camera: {camera.name}");
        // if(camera.Get)
        _material.SetFloat("_BlurFactor", blurFactor.value);
        _material.SetInt("_SampleCount", sampleCount.value);
        _material.SetVector("_BlurCenter", blurCenter.value);
        _material.SetFloat("_ClearRadius", clearRadius.value);
        _material.SetFloat("_TransitionWidth", transitionWidth.value);
        
        // 传递HDRP纹理
        _material.SetTexture("_InputTexture", source);
        
        // 绘制全屏效果
        HDUtils.DrawFullScreen(cmd, _material, destination);
    }

    public override void Cleanup() => CoreUtils.Destroy(_material); 
}
