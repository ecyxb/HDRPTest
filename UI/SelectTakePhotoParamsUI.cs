using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

public class SelectTakePhotoParamsUI : UICommon
{
    protected static new Dictionary<string, string> __shortcuts__ = new Dictionary<string, string>();
    protected override Dictionary<string, string> ShortCutsCache => __shortcuts__;
    protected override string[] SHORTCUT_OBJECTS => new string[]
    {
        "SettingParamTypeUIAP",
        "SelectTakePhotoParamsValueUIAP",
        "Content",
    };
    private static SettingParamType[] settingParamQueue = new SettingParamType[]
    {
        SettingParamType.AutoFocusArea,
        SettingParamType.LightMeteringMode,
        SettingParamType.ExposurePriority,
        // SettingParamType.ExposureCompensation
    };
    // Start is called before the first frame update
    private RectTransform content => this["Content"];

    private ScrollViewHelper m_Sih;
    private SettingParamType m_currentSettingType = SettingParamType.None;
    private SelectTakePhotoParamsValueUI m_selectTakePhotoParamsValueUI = null;

    protected override void OnLoad()
    {
        // var settingParamTypeUIOrigin = AttachUI<SettingParamTypeUI>("SettingParamTypeUI", "SettingParamTypeUIAP", UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent);
        m_selectTakePhotoParamsValueUI = AttachUI<SelectTakePhotoParamsValueUI>("SelectTakePhotoParamsValueUI", "SelectTakePhotoParamsValueUIAP", UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent);
        m_selectTakePhotoParamsValueUI.Show();

        m_Sih = new ScrollViewHelper(this, content, this["SettingParamTypeUIAP"], isVertical: false, isHorizontal: true);
        m_Sih.ClearItems();
        foreach (var _ in settingParamQueue)
        {
            m_Sih.AddItemAsUICommonFather<SettingParamTypeUI>("SettingParamTypeUI", aspectFit: UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent);
        }
        m_Sih.UpdateLayout();


    }

    protected override void Register()
    {
        base.Register();
        if (G.InputMgr)
        {
            G.InputMgr.RegisterInput("MainTab", InputEventType.Clicked, OnReactEClick, gameObject);
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
    public bool OnReactEClick(InputActionArgs args)
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
        for (int i = 0; i < m_Sih.Items.Count; i++)
        {
            var settingTypeUI = m_Sih.Items[i] as SettingParamTypeUI;
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
                m_selectTakePhotoParamsValueUI.SetLightMeteringModeValues(G.player.takePhotoCameraComp.AllLightMeteringModes, G.player.takePhotoCameraComp.MeteringMode);
                break;
            case SettingParamType.ExposurePriority:
                m_selectTakePhotoParamsValueUI.SetExposurePriorityValues(G.player.takePhotoCameraComp.AllEVPriority, G.player.takePhotoCameraComp.EVPriority);
                break;
            case SettingParamType.AutoFocusArea:
                m_selectTakePhotoParamsValueUI.SetAutoFocusAreaValues(G.player.takePhotoCameraComp.AllAutoFocusAreas, G.player.takePhotoCameraComp.AutoFocusArea);
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
            (m_Sih.Items[idx] as SettingParamTypeUI)?.SetExposurePriorityValue(exposurePriority, true);
            m_selectTakePhotoParamsValueUI.SetExposurePriorityValues(G.player.takePhotoCameraComp.AllEVPriority, exposurePriority);
        }
    }

    public void OnLightMeteringModeChanged(int oldMode, int newMode)
    {
        if (m_currentSettingType == SettingParamType.LightMeteringMode)
        {
            LightMeteringMode lightMeteringMode = G.player.takePhotoCameraComp.MeteringMode;
            int idx = Array.IndexOf(settingParamQueue, SettingParamType.LightMeteringMode);
            (m_Sih.Items[idx] as SettingParamTypeUI)?.SetLightMeteringModeValue(lightMeteringMode, true);
            m_selectTakePhotoParamsValueUI.SetLightMeteringModeValues(G.player.takePhotoCameraComp.AllLightMeteringModes, lightMeteringMode);
        }
    }
    public void OnAutoFocusAreaChanged(int oldArea, int newArea)
    {
        if (m_currentSettingType == SettingParamType.AutoFocusArea)
        {
            Const.AutoFocusArea autoFocusArea = G.player.takePhotoCameraComp.AutoFocusArea;
            int idx = Array.IndexOf(settingParamQueue, SettingParamType.AutoFocusArea);
            (m_Sih.Items[idx] as SettingParamTypeUI)?.SetAutoFocusAreaValue(autoFocusArea, true);
            m_selectTakePhotoParamsValueUI.SetAutoFocusAreaValues(G.player.takePhotoCameraComp.AllAutoFocusAreas, autoFocusArea);
        }
    }
    #endregion
}