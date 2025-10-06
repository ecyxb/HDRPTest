
using UnityEngine;
using UnityEngine.UI;
using EventFramework;

public class LightMeteringModeImage : UICommon
{
    // Start is called before the first frame update
    private RectTransform imageTransform => transform.Find("TargetImage") as RectTransform;

    private LightMeteringMode currentMode = LightMeteringMode.CenterWeighted;
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
                return G.LoadSprite("img/lightMeteringMode", 3);
            case LightMeteringMode.Spot:
                return G.LoadSprite("img/lightMeteringMode", 1);
            case LightMeteringMode.Matrix:
                return G.LoadSprite("img/lightMeteringMode", 0);
            case LightMeteringMode.Average:
                return G.LoadSprite("img/lightMeteringMode", 2);
            default:
                Debug.LogError($"Unknown Light Metering Mode: {mode}");
                return null;
        }
    }
    
}
