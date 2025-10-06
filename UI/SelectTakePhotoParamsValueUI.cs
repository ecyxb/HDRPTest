using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using EventFramework;

public class SelectTakePhotoParamsValueUI : UICommon
{
    protected static new Dictionary<string, string> __shortcuts__ = new Dictionary<string, string>();
    protected override Dictionary<string, string> ShortCutsCache => __shortcuts__;
    protected override string[] SHORTCUT_OBJECTS => new string[]
    {
        "SettingParamTypeUIAP",
        "HintText",
        "Content",
    };

    private RectTransform content => this["Content"];
    private ScrollViewHelper m_Sih;

    private object m_Datas = null;
    private int m_DataIndex = 0;
    private SettingParamType m_currentSettingType = SettingParamType.None;

    protected override void OnLoad()
    {
        m_Sih = new ScrollViewHelper(this, content, this["SettingParamTypeUIAP"], isVertical: false, isHorizontal: true);
    }

    protected override void Register()
    {
        base.Register();
        if (G.InputMgr)
        {
            G.InputMgr.RegisterInput("REACT_E", InputEventType.Clicked, OnReactEClick, gameObject);
            G.InputMgr.RegisterInput("REACT_Q", InputEventType.Clicked, OnReactQClick, gameObject);
        }
    }
    protected override void UnRegister()
    {
        base.UnRegister();
        if (G.InputMgr)
        {
            G.InputMgr.UnRegisterInput("REACT_E", OnReactEClick);
            G.InputMgr.UnRegisterInput("REACT_Q", OnReactQClick);
        }
    }
    public bool OnReactEClick(InputActionArgs args)
    {
        OffsetIndex(false);
        return true;
    }
    public bool OnReactQClick(InputActionArgs args)
    {
        OffsetIndex(true);
        return true;
    }

    private void OffsetIndex(bool neg)
    {
        switch (m_currentSettingType)
        {
            case SettingParamType.LightMeteringMode:
                // Handle Light Metering Mode selection
                var datas = (LightMeteringMode[])m_Datas;
                m_DataIndex = m_DataIndex + (neg ? -1 : 1);
                m_DataIndex = m_DataIndex < 0 ? datas.Length - 1 : m_DataIndex >= datas.Length ? 0 : m_DataIndex;
                G.player.takePhotoCameraComp.SetLightMeteringMode(datas[m_DataIndex]);
                break;
            case SettingParamType.ExposurePriority:
                var exposurePriorities = (ExposurePriority[])m_Datas;
                m_DataIndex = m_DataIndex + (neg ? -1 : 1);
                m_DataIndex = m_DataIndex < 0 ? exposurePriorities.Length - 1 : m_DataIndex >= exposurePriorities.Length ? 0 : m_DataIndex;
                G.player.takePhotoCameraComp.SetExposurePriority(exposurePriorities[m_DataIndex]);
                // this["HintText"].GetComponent<TMPro.TextMeshProUGUI>().text = Helpers.GetExposurePriorityString(exposurePriorities[m_DataIndex]);
                break;
            case SettingParamType.AutoFocusArea:
                var autoFocusAreas = (Const.AutoFocusArea[])m_Datas;
                m_DataIndex = m_DataIndex + (neg ? -1 : 1);
                m_DataIndex = m_DataIndex < 0 ? autoFocusAreas.Length - 1 : m_DataIndex >= autoFocusAreas.Length ? 0 : m_DataIndex;
                G.player.takePhotoCameraComp.SetAutoFocusArea(autoFocusAreas[m_DataIndex]);
                break;
            default:
                Debug.LogWarning("No valid setting type selected.");
                return;
        }

    }

    public void SetExposurePriorityValues(ExposurePriority[] allPriorities, ExposurePriority selectedPriority)
    {
        m_Sih.ClearItems();
        for (int i = 0; i < allPriorities.Length; i++)
        {
            bool isSelected = allPriorities[i] == selectedPriority;
            var newObj = m_Sih.AddItemAsUICommonFather<SettingParamTypeUI>("SettingParamTypeUI", aspectFit: UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent);
            newObj.SetExposurePriorityValue(allPriorities[i], isSelected);
            if (isSelected)
            {
                m_DataIndex = i;
            }
        }
        m_Sih.UpdateLayout();
        m_currentSettingType = SettingParamType.ExposurePriority;
        m_Datas = allPriorities;
        this["HintText"].GetComponent<TMPro.TextMeshProUGUI>().text = Helpers.GetExposurePriorityString(selectedPriority);
    }
    public void SetLightMeteringModeValues(LightMeteringMode[] allModes, LightMeteringMode selectedMode)
    {
        m_Sih.ClearItems();
        for (int i = 0; i < allModes.Length; i++)
        {
            bool isSelected = allModes[i] == selectedMode;
            var newObj = m_Sih.AddItemAsUICommonFather<SettingParamTypeUI>("SettingParamTypeUI", aspectFit: UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent);
            newObj.SetLightMeteringModeValue(allModes[i], isSelected);
            if (isSelected)
            {
                m_DataIndex = i;
            }
        }
        m_Sih.UpdateLayout();
        m_currentSettingType = SettingParamType.LightMeteringMode;
        m_Datas = allModes;

        this["HintText"].GetComponent<TMPro.TextMeshProUGUI>().text = Helpers.GetLightMeteringModeString(selectedMode);
    }
    public void SetAutoFocusAreaValues(Const.AutoFocusArea[] allAreas, Const.AutoFocusArea selectedArea)
    {
        m_Sih.ClearItems();
        for (int i = 0; i < allAreas.Length; i++)
        {
            bool isSelected = allAreas[i] == selectedArea;
            var newObj = m_Sih.AddItemAsUICommonFather<SettingParamTypeUI>("SettingParamTypeUI", aspectFit: UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent);
            newObj.SetAutoFocusAreaValue(allAreas[i], isSelected);
            if (isSelected)
            {
                m_DataIndex = i;
            }
        }
        m_Sih.UpdateLayout();
        m_currentSettingType = SettingParamType.AutoFocusArea;
        m_Datas = allAreas;

        this["HintText"].GetComponent<TMPro.TextMeshProUGUI>().text = Helpers.GetAutoFocusAreaString(selectedArea);
    }
}
