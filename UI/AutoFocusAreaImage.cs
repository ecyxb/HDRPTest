
using UnityEngine;
using UnityEngine.UI;
using EventFramework;

public class AutoFocusAreaImage : UICommon
{
    private RectTransform imageTransform => transform.Find("TargetImage") as RectTransform;
    private Const.AutoFocusArea currentMode = Const.AutoFocusArea.SPOT;
    protected override void OnLoad()
    {
        imageTransform.gameObject.GetComponent<Image>().sprite = GetSpriteByMode(currentMode);
        SetBgVisibility(false);
    }
    public void SetBgVisibility(bool isVisible)
    {
        gameObject.GetComponent<Image>().enabled = isVisible;
    }
    public void SetSelected(bool isSelected)
    {
        if (isSelected)
        {
            imageTransform.GetComponent<Image>().color = Const.ColorConst.highlightColor;
        }
        else
        {
            imageTransform.GetComponent<Image>().color = Const.ColorConst.normalColor;
        }
    }

    public void SetAutoFocusArea(Const.AutoFocusArea mode)
    {
        if (currentMode != mode)
        {
            currentMode = mode;
            imageTransform.gameObject.GetComponent<Image>().sprite = GetSpriteByMode(currentMode);
        }
    }
    public Sprite GetSpriteByMode(Const.AutoFocusArea mode)
    {
        switch (mode)
        {
            case Const.AutoFocusArea.MIDDLE:
                return G.LoadSprite("img/autoFocusAreaMode", 1);
            case Const.AutoFocusArea.SPOT:
                return G.LoadSprite("img/autoFocusAreaMode", 3);
            case Const.AutoFocusArea.FULL:
                return G.LoadSprite("img/autoFocusAreaMode", 0);
            default:
                Debug.LogError($"Unknown Light Metering Mode: {mode}");
                return null;
        }
    }
    
}
