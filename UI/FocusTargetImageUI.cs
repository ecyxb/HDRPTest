using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
using DG.Tweening;
using UnityEngine;

public class FocusTargetImageUI : UICommon
{
    public CameraTargetMono currentTarget { get; private set; }
    public CameraTargetCheckData latestCheckData { get; private set; }
    private Material focusImageMaterial;
    private UnityEngine.UI.Image _image;

    private bool _needPlayFadeAnim = false;
    private bool _isFadeOutAnimating = false;
    private Tween _animationTween;

    private bool IsFadeOutAnimating => _isFadeOutAnimating && _animationTween != null;
    private bool IsFadeInAnimating => !_isFadeOutAnimating && _animationTween != null;

    // Start is called before the first frame update
    protected override void OnInit()
    {
        base.OnInit();
        focusImageMaterial = new Material(Shader.Find("UI/CircleCutout"));
        _image = GetComponent<UnityEngine.UI.Image>();
        _image.material = focusImageMaterial;
        _image.color = new Color(1, 1, 1, 0); // 初始透明度为0
    }
    void OnDestroy()
    {

        if (focusImageMaterial != null)
        {
            Destroy(focusImageMaterial);
            focusImageMaterial = null;
        }
    }
    public int TrySelectTarget_OldTarget(List<CameraTargetMono> cameraTargetMonos)
    {
        if (cameraTargetMonos == null || cameraTargetMonos.Count == 0)
        {
            return -1;
        }
        if (currentTarget != null)
        {
            int index = cameraTargetMonos.IndexOf(currentTarget);
            if (index >= 0)
            {
                latestCheckData = cameraTargetMonos[index].latestCheckData;
                return index; // Already selected target is still valid
            }
        }
        if (_animationTween != null)
        {
            return -2;
        }
        return -1;
    }

    public void SetTargetAndCheckData(CameraTargetMono _target)
    {
        this.Assert(_target != null, "Target cannot be null");
        latestCheckData = _target.latestCheckData;
        if (currentTarget != _target)
        {
            currentTarget = _target;
            if (_animationTween != null)
            {
                _animationTween.Kill();
                _animationTween = null;
            }
            _isFadeOutAnimating = false;
            _image.enabled = true;
            float percent = 1 - _image.color.a;
            _SetTransformCCS(percent);
            _animationTween = DOTween.To(() => 1 - _image.color.a, _SetTransformCCS, 0, 0.15f * percent).SetEase(Ease.InCirc).OnComplete(OnFadeInComplete);
        }
        
    }
    public void ClearTarget()
    {
        if (currentTarget != null)
        {
            currentTarget = null;
            if (IsFadeOutAnimating)
            {
                return;
            }
            else if (IsFadeInAnimating)
            {
                _animationTween.Kill();
                _animationTween = null;
            }
            _isFadeOutAnimating = true;
            float percent = 1 - _image.color.a;
            _animationTween = DOTween.To(() => 1 - _image.color.a, _SetTransformCCS, 1, 0.15f * (1 - percent)).SetEase(Ease.OutExpo).OnComplete(OnFadeOutComplete);
            
        }
    }

    public void UpdateCircleFocusImage()
    {
        if (_animationTween != null)
        {
            return; // 如果动画正在播放，则不更新
        }
        if (currentTarget != null)
        {
            _SetTransformCCS(0.0f);
        }

    }

    private void OnFadeOutComplete()
    {
        _isFadeOutAnimating = false;
        _animationTween = null;
        _image.enabled = false; // 没有目标，淡出动画完成后隐藏
    }
    private void OnFadeInComplete()
    {
        _isFadeOutAnimating = false;
        _animationTween = null;
        // _SetTransformCCS(0.0f);
    }
    
    private void _SetTransformCCS(float fadePercent)
    {
        _image.color = new Color(_image.color.r, _image.color.g, _image.color.b, 1 - fadePercent);
        RectTransform rectTransform = GetComponent<RectTransform>();
        float radius = latestCheckData.GetRadius() * G.player.sfxComp.radialBlurFactor;
        Vector3 radialBlurPos = G.player.sfxComp.GetAfterRadialBlurScreenPosition(latestCheckData.screenPos);
        var radialBlurPosUIPos = G.UI.ScreenPos2UIWorldPos(radialBlurPos, GetComponent<RectTransform>());

        if (IsFadeOutAnimating)
        {
            radius = Mathf.Lerp(radius, radius * 1.3f, fadePercent);
            float innerRadius = latestCheckData.GetBorderSize() / radius;
            rectTransform.sizeDelta = new Vector2(radius * 2, radius * 2);
            focusImageMaterial.SetFloat("_InnerRadius", 1 - innerRadius);
            focusImageMaterial.SetFloat("_Smoothness", fadePercent * 0.1f);
        }
        else if (IsFadeInAnimating)
        {

            rectTransform.position = radialBlurPosUIPos;
            radius = Mathf.Lerp(radius * 1.3f, radius, 1 - fadePercent);
            float innerRadius = latestCheckData.GetBorderSize() / radius;
            rectTransform.sizeDelta = new Vector2(radius * 2, radius * 2);
            focusImageMaterial.SetFloat("_InnerRadius", 1 - innerRadius);
            focusImageMaterial.SetFloat("_Smoothness", fadePercent * 0.1f);
        }
        else
        {
            rectTransform.position = radialBlurPosUIPos;
            float innerRadius = latestCheckData.GetBorderSize() / radius;
            rectTransform.sizeDelta = new Vector2(radius * 2, radius * 2);
            focusImageMaterial.SetFloat("_InnerRadius", 1 - innerRadius);
            focusImageMaterial.SetFloat("_Smoothness", fadePercent * 0.1f);
        }
    }

}
