using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ExposePriorityUI : UICommon
{
    [SerializeField]
    private TextMeshProUGUI text;
    [SerializeField]
    private Image bg;

    private Tweener bgTween;
    // Start is called before the first frame update


    protected override void OnLoad()
    {
        base.OnLoad();
        SetBgVisibility(false);
    }
    protected override void OnUnload()
    {
        if (bgTween != null)
        {
            bgTween.Kill();
            bgTween = null;
        }
    }

    public void PlayBgTween()
    {
        if (bgTween != null)
        {
            bgTween.Kill();
            bgTween = null;
        }
        bgTween = DOTween.To(() => Const.ColorConst.highlightColorBg, x => bg.color = x, new Color(0, 0, 0, 235 / 255f), 0.5f)
            .SetEase(Ease.InOutSine).OnComplete(() => bgTween = null);
    }


    public void SetExposurePriority(ExposurePriority exposurePriority)
    {
        switch (exposurePriority)
        {
            case ExposurePriority.ShutterSpeed:
                text.text = "S";
                break;
            case ExposurePriority.Aperture:
                text.text = "A";
                break;
            case ExposurePriority.Program:
                text.text = "P";
                break;
            default:
                text.text = "M";
                break;
        }
    }

    public void SetBgVisibility(bool isVisible)
    {
        bg.enabled = isVisible;
    }
    public void SetSelected(bool isSelected)
    {
        if (isSelected)
        {
            text.color = Const.ColorConst.highlightColor;
        }
        else
        {
            text.color = Const.ColorConst.normalColor;
        }
    }

}
