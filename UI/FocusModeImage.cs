using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FocusModeImage : UICommon
{
    public TextMeshProUGUI focusModeText;
    protected override void OnLoad()
    {
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
            focusModeText.color = Const.ColorConst.highlightColor;
        }
        else
        {
            focusModeText.color = Const.ColorConst.normalColor;
        }
        
    }

}
