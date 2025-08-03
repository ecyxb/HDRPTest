using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingParamTypeUI : UICommon
{
    public RectTransform meteringMode => this["LightMeteringModeImageAP"];
    public RectTransform exposePriority => this["ExposePriorityUIAP"];
    public RectTransform autoFocusArea => this["AutoFocusAreaImageAP"];

    private SettingParamType currentSettingType = SettingParamType.None;

    protected override string[] SHORTCUT_OBJECTS => new string[]
    {
        "LightMeteringModeImageAP",
        "ExposePriorityUIAP",
        "AutoFocusAreaImageAP"
    };

    protected override List<BindUICommonArgs> UICOMMON_BIND
    {
        get
        {
            return new List<BindUICommonArgs>
            {
                new BindUICommonArgs("LightMeteringModeImage", "LightMeteringModeImageAP", null, UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent),
                new BindUICommonArgs("ExposePriorityUI", "ExposePriorityUIAP", null, UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent),
                new BindUICommonArgs("AutoFocusAreaImage", "AutoFocusAreaImageAP", null, UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent)
            };
        }
    }

    private void SetSettingParamType(SettingParamType type)
    {
        if (currentSettingType == type)
        {
            return; // No change needed
        }
        currentSettingType = type;
        switch (type)
        {
            case SettingParamType.LightMeteringMode:
                meteringMode.gameObject.SetActive(true);
                exposePriority.gameObject.SetActive(false);
                autoFocusArea.gameObject.SetActive(false);
                break;
            case SettingParamType.ExposurePriority:
                meteringMode.gameObject.SetActive(false);
                exposePriority.gameObject.SetActive(true);
                autoFocusArea.gameObject.SetActive(false);
                break;
            case SettingParamType.AutoFocusArea:
                meteringMode.gameObject.SetActive(false);
                exposePriority.gameObject.SetActive(false);
                autoFocusArea.gameObject.SetActive(true);
                break;
            default:
                meteringMode.gameObject.SetActive(false);
                exposePriority.gameObject.SetActive(false);
                autoFocusArea.gameObject.SetActive(false);
                break;
        }
    }

    public void SetExposurePriorityValue(ExposurePriority exposurePriority, bool isSelected = false)
    {
        SetSettingParamType(SettingParamType.ExposurePriority);
        var exposePriorityUI = GetUICommon<ExposePriorityUI>("ExposePriorityUI");
        exposePriorityUI.SetExposurePriority(exposurePriority);
        exposePriorityUI.SetSelected(isSelected);
    }

    public void SetLightMeteringModeValue(LightMeteringMode lightMeteringMode, bool isSelected = false)
    {
        SetSettingParamType(SettingParamType.LightMeteringMode);
        var lightMeteringModeUI = GetUICommon<LightMeteringModeImage>("LightMeteringModeImage");
        lightMeteringModeUI.SetLightMeteringMode(lightMeteringMode);
        lightMeteringModeUI.SetSelected(isSelected);
    }
    public void SetAutoFocusAreaValue(Const.AutoFocusArea autoFocusArea, bool isSelected = false)
    {
        SetSettingParamType(SettingParamType.AutoFocusArea);
        var autoFocusAreaUI = GetUICommon<AutoFocusAreaImage>("AutoFocusAreaImage");
        autoFocusAreaUI.SetAutoFocusArea(autoFocusArea);
        autoFocusAreaUI.SetSelected(isSelected);
    }
}
