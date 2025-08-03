
using UnityEngine;
using UnityEngine.UI;

public class LightMeteringModeImage : UICommon
{
    // Start is called before the first frame update
    public Sprite centerWeightedSprite;
    public Sprite spotSprite;
    public Sprite matrixSprite;
    public Sprite averageSprite;
    public RectTransform imageTransform;

    private LightMeteringMode currentMode = LightMeteringMode.CenterWeighted;
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

    public void SetLightMeteringMode(LightMeteringMode mode)
    {
        if (currentMode != mode)
        {
            currentMode = mode;
            imageTransform.gameObject.GetComponent<Image>().sprite = GetSpriteByMode(currentMode);
        }
    }
    public Sprite GetSpriteByMode(LightMeteringMode mode)
    {
        switch (mode)
        {
            case LightMeteringMode.CenterWeighted:
                return centerWeightedSprite;
            case LightMeteringMode.Spot:
                return spotSprite;
            case LightMeteringMode.Matrix:
                return matrixSprite;
            case LightMeteringMode.Average:
                return averageSprite;
            default:
                Debug.LogError($"Unknown Light Metering Mode: {mode}");
                return null;
        }
    }
    
}
