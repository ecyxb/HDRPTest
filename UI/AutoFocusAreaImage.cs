
using UnityEngine;
using UnityEngine.UI;

public class AutoFocusAreaImage : UICommon
{
    // Start is called before the first frame update
    public Sprite middleSprite;
    public Sprite spotSprite;
    public Sprite fullSprite;
    public RectTransform imageTransform;

    private Const.AutoFocusArea currentMode = Const.AutoFocusArea.SPOT;
    protected override void OnLoad()
    {
        imageTransform.gameObject.GetComponent<Image>().sprite = GetSpriteByMode(currentMode);
        SetBgVisibility(false);
    }
    public void SetBgVisibility(bool isVisible)
    {
        GetComponent<Image>().enabled = isVisible;
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
                return middleSprite;
            case Const.AutoFocusArea.SPOT:
                return spotSprite;
            case Const.AutoFocusArea.FULL:
                return fullSprite;
            default:
                Debug.LogError($"Unknown Light Metering Mode: {mode}");
                return null;
        }
    }
    
}
