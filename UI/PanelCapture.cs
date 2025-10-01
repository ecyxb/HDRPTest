using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;




public class PanelCapture : PanelBase
{
    public override CanvasType CanvasType { get; } = CanvasType.PanelBase;

    private Image _image;
    private static RenderTexture _captureTexture;
    private Tween m_FadeTween;
    private uint clearTimer = 0;

    protected override void OnLoad()
    {
        base.OnLoad();
        _captureTexture = new RenderTexture(Screen.width, Screen.height, 24);
        _image = transform.Find("WhitePanel").GetComponent<Image>();
        transform.GetComponent<RawImage>().texture = _captureTexture;
    }
    protected override void OnUnload()
    {
        _captureTexture.Release();
        if (m_FadeTween != null)
        {
            m_FadeTween.Kill();
        }
        if (clearTimer > 0)
        {
            G.UnRegisterTimer(clearTimer);
            clearTimer = 0;
        }
        base.OnUnload();
    }

    public void ShowResult()
    {
        base.Show();
        if (m_FadeTween != null)
        {
            m_FadeTween.Kill();
        }
        if (clearTimer > 0)
        {
            G.UnRegisterTimer(clearTimer);
            clearTimer = 0;
        }
        G.player.RenderCamera.targetTexture = _captureTexture;
        G.player.RenderCamera.Render();
        G.player.RenderCamera.targetTexture = null;
        m_FadeTween = DOTween.To(() => Color.black, v => _image.color = v, Color.clear, 0.4f).OnComplete(OnFadeout);
    }

    private void OnFadeout()
    {
        m_FadeTween = null;
        clearTimer = G.RegisterTimer(0.3f, ShowResultFinish, repeateCount: 1);

    }
    private void ShowResultFinish()
    {
        base.Hide();
        clearTimer = G.RegisterTimer(5, OnFinishTempHide, repeateCount: 1);
    }
    private void OnFinishTempHide()
    {
        clearTimer = 0;
        G.UI.DestroyPanel(this.gameObject.name);
    }
}
