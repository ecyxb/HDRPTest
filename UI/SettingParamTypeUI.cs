using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingParamTypeUI : UICommon
{
    protected static new Dictionary<string, string> __shortcuts__ = new Dictionary<string, string>();
    protected override Dictionary<string, string> ShortCutsCache => __shortcuts__;
    protected override string[] SHORTCUT_OBJECTS => new string[]
    {
        "LightMeteringModeImageAP",
        "ExposePriorityUIAP",
        "AutoFocusAreaImageAP"
    };

    public RectTransform meteringMode => this["LightMeteringModeImageAP"];
    public RectTransform exposePriority => this["ExposePriorityUIAP"];
    public RectTransform autoFocusArea => this["AutoFocusAreaImageAP"];

    private SettingParamType currentSettingType = SettingParamType.None;

    private LightMeteringModeImage meteringModeUI = null;
    private ExposePriorityUI exposePriorityUI = null;
    private AutoFocusAreaImage autoFocusAreaUI = null;

    protected override void OnLoad()
    {
        base.OnLoad();
        meteringModeUI = AttachUI<LightMeteringModeImage>("LightMeteringModeImage", "LightMeteringModeImageAP", UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent);
        exposePriorityUI = AttachUI<ExposePriorityUI>("ExposePriorityUI", "ExposePriorityUIAP", UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent);
        autoFocusAreaUI = AttachUI<AutoFocusAreaImage>("AutoFocusAreaImage", "AutoFocusAreaImageAP", UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent);
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
        exposePriorityUI.SetExposurePriority(exposurePriority);
        exposePriorityUI.SetSelected(isSelected);
    }

    public void SetLightMeteringModeValue(LightMeteringMode lightMeteringMode, bool isSelected = false)
    {
        SetSettingParamType(SettingParamType.LightMeteringMode);
        meteringModeUI.SetLightMeteringMode(lightMeteringMode);
        meteringModeUI.SetSelected(isSelected);
    }
    public void SetAutoFocusAreaValue(Const.AutoFocusArea autoFocusArea, bool isSelected = false)
    {
        SetSettingParamType(SettingParamType.AutoFocusArea);
        autoFocusAreaUI.SetAutoFocusArea(autoFocusArea);
        autoFocusAreaUI.SetSelected(isSelected);
    }
}
