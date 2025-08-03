using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

public class SelectTakePhotoParamsUI : UICommon
{

    private static SettingParamType[] settingParamQueue = new SettingParamType[]
    {
        SettingParamType.AutoFocusArea,
        SettingParamType.LightMeteringMode,
        SettingParamType.ExposurePriority,
        // SettingParamType.ExposureCompensation
    };
    // Start is called before the first frame update
    public RectTransform content;

    private ScrollViewHelper m_Sih;
    private RectTransform m_settingTypeUIOrigin => this["SettingParamTypeUIAP"];
    private SettingParamType m_currentSettingType = SettingParamType.None;

    protected override string[] SHORTCUT_OBJECTS => new string[]
    {
        "SettingParamTypeUIAP",
        "SelectTakePhotoParamsValueUIAP",
    };

    protected override List<BindUICommonArgs> UICOMMON_BIND => new List<BindUICommonArgs>
    {
        new BindUICommonArgs("SettingParamTypeUI", "SettingParamTypeUIAP", null, UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent),
        new BindUICommonArgs("SelectTakePhotoParamsValueUI", "SelectTakePhotoParamsValueUIAP", null, UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent)
    };
    protected override void OnLoad()
    {
        m_Sih = new ScrollViewHelper(this, content, null, isVertical: false, isHorizontal: true);
        m_settingTypeUIOrigin.gameObject.SetActive(false);
        GetUICommon<SelectTakePhotoParamsValueUI>("SelectTakePhotoParamsValueUI").Show();

        m_Sih.ClearItems();
        foreach (var settingType in settingParamQueue)
        {
            m_Sih.AddItem(m_settingTypeUIOrigin, activeItem: true).GetComponentInChildren<SettingParamTypeUI>(); ;
        }
        m_Sih.UpdateLayout();


    }

    protected override void Register()
    {
        base.Register();
        if (G.InputMgr)
        {
            G.InputMgr.RegisterInput("MainTab", InputEventType.Canceled | InputEventType.SWALLOW_ALL, OnReactEClick, gameObject);
        }
        if (G.player)
        {
            G.player.takePhotoCameraComp.RegisterProp("exposurePriority", (Action<int, int>)OnExposurePriorityChanged);
            G.player.takePhotoCameraComp.RegisterProp("lightMeteringMode", (Action<int, int>)OnLightMeteringModeChanged);
            G.player.takePhotoCameraComp.RegisterProp("autoFocusArea", (Action<int, int>)OnAutoFocusAreaChanged);
        }
    }
    protected override void UnRegister()
    {
        base.UnRegister();
        if (G.InputMgr)
        {
            G.InputMgr.UnRegisterInput("MainTab", OnReactEClick);
        }
        if (G.player)
        {
            G.player.takePhotoCameraComp.UnRegisterProp("exposurePriority", (Action<int, int>)OnExposurePriorityChanged);
            G.player.takePhotoCameraComp.UnRegisterProp("lightMeteringMode", (Action<int, int>)OnLightMeteringModeChanged);
            G.player.takePhotoCameraComp.UnRegisterProp("autoFocusArea", (Action<int, int>)OnAutoFocusAreaChanged);
        }
    }
    public bool OnReactEClick(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        var index = Array.IndexOf(settingParamQueue, m_currentSettingType);
        if (index < 0 || index >= settingParamQueue.Length - 1)
        {
            index = -1;
        }
        ShowSettingSelect(settingParamQueue[index + 1]);
        return true;
    }

    public void ShowSettingSelect(SettingParamType currentType)
    {

        if (m_currentSettingType == currentType)
        {
            return; // No change needed
        }
        SelectTakePhotoParamsValueUI selectTakePhotoParamsValueUI = GetUICommon<SelectTakePhotoParamsValueUI>("SelectTakePhotoParamsValueUI");
        for (int i = 0; i < m_Sih.Items.Count; i++)
        {
            var settingTypeUI = m_Sih.Items[i].GetComponentInChildren<SettingParamTypeUI>();
            var settingType = settingParamQueue[i];
            switch (settingType)
            {
                case SettingParamType.LightMeteringMode:
                    settingTypeUI.SetLightMeteringModeValue(G.player.takePhotoCameraComp.MeteringMode, settingType == currentType);
                    break;
                case SettingParamType.ExposurePriority:
                    settingTypeUI.SetExposurePriorityValue(G.player.takePhotoCameraComp.EVPriority, settingType == currentType);
                    break;
                case SettingParamType.AutoFocusArea:
                    settingTypeUI.SetAutoFocusAreaValue(G.player.takePhotoCameraComp.AutoFocusArea, settingType == currentType);
                    break;
                default:
                    // settingTypeUI.SetSettingParamType(SettingParamType.None);
                    break;
            }
        }
        m_currentSettingType = currentType;
        switch (currentType)
        {
            case SettingParamType.LightMeteringMode:
                selectTakePhotoParamsValueUI.SetLightMeteringModeValues(G.player.takePhotoCameraComp.AllLightMeteringModes, G.player.takePhotoCameraComp.MeteringMode);
                break;
            case SettingParamType.ExposurePriority:
                selectTakePhotoParamsValueUI.SetExposurePriorityValues(G.player.takePhotoCameraComp.AllEVPriority, G.player.takePhotoCameraComp.EVPriority);
                break;
            case SettingParamType.AutoFocusArea:
                selectTakePhotoParamsValueUI.SetAutoFocusAreaValues(G.player.takePhotoCameraComp.AllAutoFocusAreas, G.player.takePhotoCameraComp.AutoFocusArea);
                break;
            default:
                break;
        }
    }

    #region propEvent
    private void OnExposurePriorityChanged(int oldPriority, int newPriority)
    {
        if (m_currentSettingType == SettingParamType.ExposurePriority)
        {
            ExposurePriority exposurePriority = G.player.takePhotoCameraComp.EVPriority;
            int idx = Array.IndexOf(settingParamQueue, SettingParamType.ExposurePriority);
            m_Sih.Items[idx].GetComponentInChildren<SettingParamTypeUI>().SetExposurePriorityValue(exposurePriority, true);


            SelectTakePhotoParamsValueUI selectTakePhotoParamsValueUI = GetUICommon<SelectTakePhotoParamsValueUI>("SelectTakePhotoParamsValueUI");
            selectTakePhotoParamsValueUI.SetExposurePriorityValues(G.player.takePhotoCameraComp.AllEVPriority, exposurePriority);
        }
    }

    public void OnLightMeteringModeChanged(int oldMode, int newMode)
    {
        if (m_currentSettingType == SettingParamType.LightMeteringMode)
        {
            LightMeteringMode lightMeteringMode = G.player.takePhotoCameraComp.MeteringMode;
            int idx = Array.IndexOf(settingParamQueue, SettingParamType.LightMeteringMode);
            m_Sih.Items[idx].GetComponentInChildren<SettingParamTypeUI>().SetLightMeteringModeValue(lightMeteringMode, true);

            SelectTakePhotoParamsValueUI selectTakePhotoParamsValueUI = GetUICommon<SelectTakePhotoParamsValueUI>("SelectTakePhotoParamsValueUI");
            selectTakePhotoParamsValueUI.SetLightMeteringModeValues(G.player.takePhotoCameraComp.AllLightMeteringModes, lightMeteringMode);
        }
    }
    public void OnAutoFocusAreaChanged(int oldArea, int newArea)
    {
        if (m_currentSettingType == SettingParamType.AutoFocusArea)
        {
            Const.AutoFocusArea autoFocusArea = G.player.takePhotoCameraComp.AutoFocusArea;
            int idx = Array.IndexOf(settingParamQueue, SettingParamType.AutoFocusArea);
            m_Sih.Items[idx].GetComponentInChildren<SettingParamTypeUI>().SetAutoFocusAreaValue(autoFocusArea, true);

            SelectTakePhotoParamsValueUI selectTakePhotoParamsValueUI = GetUICommon<SelectTakePhotoParamsValueUI>("SelectTakePhotoParamsValueUI");
            selectTakePhotoParamsValueUI.SetAutoFocusAreaValues(G.player.takePhotoCameraComp.AllAutoFocusAreas, autoFocusArea);
        }
    }
    #endregion
}