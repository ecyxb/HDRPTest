using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using UnityEngine.UI;

public class PanelTakePhotoMain : PanelBase
{
    [Space(10)]
    public Image shutterPanelOutline;
    public TextMeshProUGUI shutterLTText;
    public TextMeshProUGUI shutterSpeedText;

    [Space(10)]
    public Image aperturePanelOutline;
    public TextMeshProUGUI apertureLBText;
    public TextMeshProUGUI apertureText;

    [Space(10)]
    public Image isoPanelOutline;
    public TextMeshProUGUI isoLTDocuText; //左上角标注是iso的文字
    public TextMeshProUGUI isoText;
    // public TextMeshPro

    [Space(10)]
    public Image exposureCompensationImage;
    public TextMeshProUGUI exposureCompensationText;
    private RectTransform exposeCompensationAP => this["PanelExposeCompensationAP"];


    private TextMeshProUGUI[] hightLightTexts;
    private Image[] hightLightImages;

    private int exposureOperation = 0;

    // 长按调整曝光按钮时连续修改的定时器
    private uint exposureOpTimer = 0;
    private SelectTakePhotoParamsUI selectTakePhotoParamsUI;

    // 当曝光需要的参数超出可以设置的范围时，闪烁
    private uint blinkTargetOutRangeSettingsTimer = 0;

    private FocusPositionUI focusPositionUI;

    protected override string[] SHORTCUT_OBJECTS => new string[]
    {
        "PanelExposeCompensationAP",
        "CurrentLightMeteringModeImageAP",
        "SelectTakePhotoParamsAP",
        "PanelExposePriorityAP",
        "NormalItems",
        "FocusPositionAP",
    };
    protected override List<BindUICommonArgs> UICOMMON_BIND => new List<BindUICommonArgs>
    {
        new BindUICommonArgs("ExposureCompensationRule", "PanelExposeCompensationAP", null, AspectRatioFitter.AspectMode.FitInParent),
        new BindUICommonArgs("LightMeteringModeImage", "CurrentLightMeteringModeImageAP", null, AspectRatioFitter.AspectMode.FitInParent),
        new BindUICommonArgs("ExposePriorityUI", "PanelExposePriorityAP", null, AspectRatioFitter.AspectMode.FitInParent),
        new BindUICommonArgs("FocusPositionUI", "FocusPositionAP", null, AspectRatioFitter.AspectMode.None),
    };

    protected override void OnLoad()
    {
        selectTakePhotoParamsUI = AttachUI(new BindUICommonArgs("SelectTakePhotoParamsUI", "SelectTakePhotoParamsAP", null, AspectRatioFitter.AspectMode.FitInParent)) as SelectTakePhotoParamsUI;
        selectTakePhotoParamsUI.ShowSettingSelect(SettingParamType.AutoFocusArea);
        selectTakePhotoParamsUI.Hide();

        LightMeteringModeImage currentLightMeteringModeImage = GetUICommon<LightMeteringModeImage>("LightMeteringModeImage");
        currentLightMeteringModeImage.SetBgVisibility(true);
        currentLightMeteringModeImage.SetLightMeteringMode(G.player.takePhotoCameraComp.MeteringMode);

        this.focusPositionUI = GetUICommon<FocusPositionUI>("FocusPositionUI");
        InitializeFocusPositionUI();
        


        hightLightTexts = new TextMeshProUGUI[0];
        hightLightImages = new Image[0];
        var comp = G.player.takePhotoCameraComp;
        if (!G.player)
        {
            return;
        }
        UpdateShutterSpeedText();
        UpdateExposurePriority(anim: false);
        UpdateExposureControlType(anim: false);
        UpdateExposureCompensationPara(anim: false);
        apertureText.text = comp.Aperture.ToString();
        isoText.text = comp.ISO.ToString();

        comp.RegisterProp("apertureIdx", (Action<int, int>)OnApertureIdxChanged);
        comp.RegisterProp("shutterSpeedIdx", (Action<int, int>)OnShutterSpeedIdxChanged);
        comp.RegisterProp("isoIdx", (Action<int, int>)OnISOIdxChanged);
        comp.RegisterProp("exposureCompensationPara", (Action<int, int>)OnExposureCompensationParaChanged);
        comp.RegisterProp("exposurePriority", (Action<int, int>)OnExposurePriorityChanged);
        comp.RegisterProp("exposureControlType", (Action<int, int>)OnExposureControlTypeChanged);
        comp.RegisterProp("lightMeteringMode", (Action<int, int>)OnLightMeteringModeChanged);
        comp.RegisterProp("targetOutRangeSettings", (Action<int, int>)OnTargetOutRangeSettingsChanged);
        comp.RegisterProp("focusIdxCenterOffset", (Action<Vector2Int, Vector2Int>)OnFocusIdxCenterOffsetChanged);
        comp.RegisterProp("autoFocusArea", (Action<int, int>)OnAutoFocusAreaChanged);
        
        comp.ClampBlockIdx_Center += focusPositionUI.ClampBlockIdx_Center;

        }
    protected override void OnUnload()
    {
        if (G.player)
        {
            var takePhotoCameraComp = G.player.takePhotoCameraComp;
            takePhotoCameraComp.UnRegisterProp("apertureIdx", (Action<int, int>)OnApertureIdxChanged);
            takePhotoCameraComp.UnRegisterProp("shutterSpeedIdx", (Action<int, int>)OnShutterSpeedIdxChanged);
            takePhotoCameraComp.UnRegisterProp("isoIdx", (Action<int, int>)OnISOIdxChanged);
            takePhotoCameraComp.UnRegisterProp("exposureCompensationPara", (Action<int, int>)OnExposureCompensationParaChanged);
            takePhotoCameraComp.UnRegisterProp("exposurePriority", (Action<int, int>)OnExposurePriorityChanged);
            takePhotoCameraComp.UnRegisterProp("exposureControlType", (Action<int, int>)OnExposureControlTypeChanged);
            takePhotoCameraComp.UnRegisterProp("lightMeteringMode", (Action<int, int>)OnLightMeteringModeChanged);
            takePhotoCameraComp.UnRegisterProp("targetOutRangeSettings", (Action<int, int>)OnTargetOutRangeSettingsChanged);
            takePhotoCameraComp.UnRegisterProp("focusIdxCenterOffset", (Action<Vector2Int, Vector2Int>)OnFocusIdxCenterOffsetChanged);
            takePhotoCameraComp.UnRegisterProp("autoFocusArea", (Action<int, int>)OnAutoFocusAreaChanged);
            focusPositionUI = GetUICommon<FocusPositionUI>("FocusPositionUI");
            takePhotoCameraComp.ClampBlockIdx_Center -= focusPositionUI.ClampBlockIdx_Center;

        }
        if (exposureOpTimer != 0)
        {
            G.UnRegisterTimer(exposureOpTimer);
            exposureOpTimer = 0;
        }
        if (blinkTargetOutRangeSettingsTimer != 0)
        {
            G.UnRegisterTimer(blinkTargetOutRangeSettingsTimer);
            blinkTargetOutRangeSettingsTimer = 0;
        }


    }
    protected override void Register()
    {
        base.Register();
        if (G.InputMgr)
        {
            G.InputMgr.RegisterInput("REACT_E", InputEventType.Canceled | InputEventType.Started | InputEventType.SWALLOW_ALL, OnExposureChange, gameObject, CheckBaseInputValid);
            G.InputMgr.RegisterInput("REACT_Q", InputEventType.Canceled | InputEventType.Started | InputEventType.SWALLOW_ALL, OnExposureChange, gameObject, CheckBaseInputValid);
            G.InputMgr.RegisterInput("EVPriority", InputEventType.Canceled | InputEventType.SWALLOW_ALL, OnExposurePriority, gameObject, CheckBaseInputValid);
            G.InputMgr.RegisterInput("MainTab", InputEventType.Canceled | InputEventType.SWALLOW_ALL, OnExposureControlType, gameObject, CheckBaseInputValid);
            G.InputMgr.RegisterInput("SecondMenu", InputEventType.Canceled | InputEventType.SWALLOW_ALL, OnOpenSecondMenu, gameObject);
            G.InputMgr.RegisterInput("UIMoveX", InputEventType.Started | InputEventType.SWALLOW_ALL, OnUIMoveX, gameObject);
            G.InputMgr.RegisterInput("UIMoveY", InputEventType.Started | InputEventType.SWALLOW_ALL, OnUIMoveY, gameObject);
            G.InputMgr.RegisterInput("RightMouseClick", InputEventType.Started | InputEventType.Canceled, OnRightMouseClick, gameObject);
            G.InputMgr.RegisterInput("MouseScroll", InputEventType.Performed, OnMouseScroll, gameObject);
        }
    }

    protected override void UnRegister()
    {
        base.UnRegister();
        if (G.InputMgr)
        {
            G.InputMgr.UnRegisterInput("REACT_E", OnExposureChange);
            G.InputMgr.UnRegisterInput("REACT_Q", OnExposureChange);
            G.InputMgr.UnRegisterInput("EVPriority", OnExposurePriority);
            G.InputMgr.UnRegisterInput("MainTab", OnExposureControlType);
            G.InputMgr.UnRegisterInput("SecondMenu", OnOpenSecondMenu);
            G.InputMgr.UnRegisterInput("UIMoveX", OnUIMoveX);
            G.InputMgr.UnRegisterInput("UIMoveY", OnUIMoveY);
            G.InputMgr.UnRegisterInput("RightMouseClick", OnRightMouseClick);
            G.InputMgr.UnRegisterInput("MouseScroll", OnMouseScroll);
        }
    }


    void LateUpdate()
    {
        focusPositionUI.UpdateFocusTargetData();
        if (G.player.stateComp.HasState(Const.StateConst.FOCUS))
        {
            G.player.playerMovementComp.SetFocusPointScreenPosition(focusPositionUI.GetFocusPointScreenPos());
            if (G.player.takePhotoCameraComp.AutoFocusArea == Const.AutoFocusArea.FULL || G.player.takePhotoCameraComp.AutoFocusArea == Const.AutoFocusArea.MIDDLE)
            {
                G.player.playerMovementComp.TrySelectFocusTarget(focusPositionUI.GetAllValidFocusTarget());
            }
        }
        else
        {
            G.player.playerMovementComp.SetFocusPointScreenPosition(Vector2.zero, false);
            G.player.playerMovementComp.SetFocusTargetMono(null);
        }
        focusPositionUI.UpdateCircleFocusImage();
    }
    // void LateUpdate()
    // {

    // }
    private void UpdateExposurePriority(bool anim = false)
    {
        if (G.player)
        {
            var validTypes = Helpers.GetDefaultExposureControlType(G.player.takePhotoCameraComp.EVPriority);
            shutterPanelOutline.enabled = Array.IndexOf(validTypes, ExposureControlType.ShutterSpeed) >= 0;
            aperturePanelOutline.enabled = Array.IndexOf(validTypes, ExposureControlType.Aperture) >= 0;

            var exposureUI = GetUICommon<ExposePriorityUI>("ExposePriorityUI");
            exposureUI.SetExposurePriority(G.player.takePhotoCameraComp.EVPriority);
            if (anim)
            {
                exposureUI.PlayBgTween();
            }

        }
    }

    private void UpdateExposureControlType(bool anim = false)
    {
        if (G.player)
        {
            var ctrlType = G.player.takePhotoCameraComp.GetExposureControlType();
            foreach (var text in hightLightTexts)
            {
                text.color = Color.white;
            }
            foreach (var image in hightLightImages)
            {
                image.color = Color.white;
            }

            if (ctrlType == ExposureControlType.ExposureCompensation)
            {
                exposureCompensationText.gameObject.SetActive(true);
                exposureCompensationImage.gameObject.SetActive(true);
                isoLTDocuText.gameObject.SetActive(false);
                isoText.gameObject.SetActive(false);
                exposeCompensationAP.GetComponent<Image>().color = new Color(0, 0, 0, 235 / 255f);
                exposeCompensationAP.GetComponent<Image>().enabled = true;
                GetUICommon<ExposureCompensationRule>("ExposureCompensationRule").gameObject.SetActive(true);
            }
            else
            {
                exposureCompensationText.gameObject.SetActive(false);
                exposureCompensationImage.gameObject.SetActive(false);
                isoLTDocuText.gameObject.SetActive(true);
                isoText.gameObject.SetActive(true);
                var EVCompensationPara = G.player.takePhotoCameraComp.EVCompensationPara;
                exposeCompensationAP.GetComponent<Image>().enabled = G.player.takePhotoCameraComp.EVCompensationPara != 0;
                exposeCompensationAP.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
                GetUICommon<ExposureCompensationRule>("ExposureCompensationRule").gameObject.SetActive(EVCompensationPara != 0);
            }

            switch (ctrlType)
            {

                case ExposureControlType.ShutterSpeed:
                    shutterLTText.color = Const.ColorConst.highlightColorBg;
                    shutterSpeedText.color = Const.ColorConst.highlightColorBg;
                    hightLightTexts = new TextMeshProUGUI[] { shutterLTText, shutterSpeedText };
                    hightLightImages = new Image[] { };
                    break;
                case ExposureControlType.Aperture:
                    apertureLBText.color = Const.ColorConst.highlightColorBg;
                    apertureText.color = Const.ColorConst.highlightColorBg;
                    hightLightTexts = new TextMeshProUGUI[] { apertureLBText, apertureText };
                    hightLightImages = new Image[] { };
                    break;
                case ExposureControlType.ISO:
                    isoLTDocuText.color = Const.ColorConst.highlightColorBg;
                    isoText.color = Const.ColorConst.highlightColorBg;
                    // isoPanelOutline.color = highlightColor;
                    hightLightTexts = new TextMeshProUGUI[] { isoLTDocuText, isoText };
                    hightLightImages = new Image[] { };
                    break;
                case ExposureControlType.ExposureCompensation:
                    exposureCompensationText.color = Const.ColorConst.highlightColorBg;
                    exposureCompensationImage.color = Const.ColorConst.highlightColorBg;
                    // isoPanelOutline.color = highlightColor;
                    hightLightTexts = new TextMeshProUGUI[] { exposureCompensationText };
                    hightLightImages = new Image[] { exposureCompensationImage };
                    break;
                default:
                    break;
            }
            if (anim)
            {

            }
        }
    }

    private void UpdateExposureCompensationPara(bool anim = false)
    {
        var exposureCompensation = G.player.takePhotoCameraComp.EVCompensation;
        int para = G.player.takePhotoCameraComp.GetEVCompensationPara();
        if (para == 0)
        {
            exposureCompensationText.text = "0.0";
        }
        else if (para > 0)
        {
            exposureCompensationText.text = "+" + exposureCompensation.ToString("F1");
        }
        else
        {
            exposureCompensationText.text = exposureCompensation.ToString("F1");
        }
        GetUICommon<ExposureCompensationRule>("ExposureCompensationRule").ShowEVCompensation(para);
    }

    private void UpdateShutterSpeedText()
    {
        if (G.player)
        {
            shutterSpeedText.text = G.player.takePhotoCameraComp.ShutterSpeedInv.ToString();
        }
    }
    private void InitializeFocusPositionUI()
    {
        // 初始化对焦区域的大小和区块大小
        G.player.takePhotoCameraComp.GetInitFocusData(out Vector2Int meteringSize, out Vector2Int blockPixelSize);
        focusPositionUI.InitFocusData(meteringSize, blockPixelSize);
        UpdateFocusPositionUI();
    }
    private void UpdateFocusPositionUI()
    {
        // 更新焦点的大小和位置
        G.player.takePhotoCameraComp.GetCurrentFocusData(out Vector2Int blockNumSize, out Vector2Int centerOffset);
        focusPositionUI.SetBlock_Center(blockNumSize, centerOffset);
    }

    private void StartOffsetExposure(float time)
    {
        if (exposureOpTimer != 0)
        {
            G.Timer.UnRegisterTimer(exposureOpTimer);
        }
        exposureOpTimer = 0;
        if (exposureOperation == 0)
        {
            return;
        }
        if (!G.player)
        {
            return;
        }
        G.player.takePhotoCameraComp.OffsetExposure(exposureOperation > 0 ? 1 : -1);
        exposureOpTimer = G.Timer.RegisterTimer(time, () =>
        {
            G.player.takePhotoCameraComp.OffsetExposure(exposureOperation > 0 ? 1 : -1);
        }, TimerUpdateMode.Update, -1);
    }

    public bool CheckBaseInputValid()
    {
        if (selectTakePhotoParamsUI && selectTakePhotoParamsUI.gameObject.activeSelf)
        {
            return false;
        }
        return true;
    }

    #region input events
    public bool OnMouseScroll(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            var value = context.ReadValue<float>();
            if (value > 0)
            {
                G.player.takePhotoCameraComp.UpdateFocalLength(1);
            }
            else
            {
                G.player.takePhotoCameraComp.UpdateFocalLength(-1);
            }
        }
        return true;
    }
    public bool OnExposureChange(InputAction.CallbackContext context)
    {
        int op = 0;
        if (context.action.name == "REACT_E")
        {
            op = 1;
        }
        else if (context.action.name == "REACT_Q")
        {
            op = -1;
        }
        if (context.phase == InputActionPhase.Canceled)
        {
            op = -op;
        }
        exposureOperation += op;
        StartOffsetExposure(0.1f);
        return true;
    }
    private bool OnExposurePriority(InputAction.CallbackContext context)
    {
        if (G.player)
        {
            G.player.takePhotoCameraComp.SetExposurePriority(G.player.takePhotoCameraComp.GetExposurePriority(1));
        }
        return true;
    }
    private bool OnExposureControlType(InputAction.CallbackContext context)
    {
        if (G.player)
        {
            G.player.takePhotoCameraComp.SetExposureControlType(G.player.takePhotoCameraComp.GetExposureControlType(1));
        }
        return true;
    }
    private bool OnOpenSecondMenu(InputAction.CallbackContext context)
    {
        if (selectTakePhotoParamsUI.gameObject.activeSelf)
        {
            selectTakePhotoParamsUI.Hide();
            this["NormalItems"].GetComponent<CanvasGroup>().alpha = 1f;
        }
        else
        {
            selectTakePhotoParamsUI.Show();
            this["NormalItems"].GetComponent<CanvasGroup>().alpha = 0.5f;
        }
        return true;
    }
    private bool OnUIMoveX(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            var value = context.ReadValue<float>();
            if (value > 0)
            {
                _OnUIMove(Vector2Int.right);
            }
            else if (value < 0)
            {
                _OnUIMove(Vector2Int.left);
            }
        }
        return true;
    }
    private bool OnUIMoveY(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            var value = context.ReadValue<float>();
            if (value > 0)
            {
                _OnUIMove(Vector2Int.up);
            }
            else if (value < 0)
            {
                _OnUIMove(Vector2Int.down);
            }
        }
        return true;
    }
    private void _OnUIMove(Vector2Int dir)
    {
        var old_offset = G.player.takePhotoCameraComp.FocusIdxCenterOffset;
        G.player.takePhotoCameraComp.SetFocusIdxCenterOffset(old_offset + dir, G.player.takePhotoCameraComp.AutoFocusArea);
    }
    private bool OnRightMouseClick(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            G.player.stateComp.AddState(Const.StateConst.FOCUS);
        }
        else if (context.phase == InputActionPhase.Canceled)
        {
            G.player.stateComp.RemoveState(Const.StateConst.FOCUS);
        }
        return true;
    }
    #endregion

    #region customdic event
    public void OnShutterSpeedIdxChanged(int oldIdx, int newIdx)
    {
        UpdateShutterSpeedText();
    }
    public void OnApertureIdxChanged(int oldIdx, int newIdx)
    {
        apertureText.text = G.player.takePhotoCameraComp.Aperture.ToString();
    }
    public void OnISOIdxChanged(int oldIdx, int newIdx)
    {
        isoText.text = G.player.takePhotoCameraComp.ISO.ToString();
    }
    private void OnExposurePriorityChanged(int oldValue, int newValue)
    {
        UpdateExposurePriority(anim: true);
    }
    private void OnExposureControlTypeChanged(int oldValue, int newValue)
    {
        UpdateExposureControlType(anim: true);
    }
    private void OnExposureCompensationParaChanged(int oldValue, int newValue)
    {
        UpdateExposureCompensationPara(anim: true);
    }
    private void OnLightMeteringModeChanged(int oldValue, int newValue)
    {
        var currentLightMeteringModeImage = GetUICommon<LightMeteringModeImage>("LightMeteringModeImage");
        currentLightMeteringModeImage.SetLightMeteringMode(G.player.takePhotoCameraComp.MeteringMode);
    }
    private void OnFocusIdxCenterOffsetChanged(Vector2Int oldValue, Vector2Int newValue)
    {
        UpdateFocusPositionUI();
    }

    private void OnTargetOutRangeSettingsChanged(int oldValue, int newValue)
    {
        if (newValue == oldValue)
        {
            return;
        }
        if (newValue == 0)
        {
            if (blinkTargetOutRangeSettingsTimer != 0)
            {
                G.UnRegisterTimer(blinkTargetOutRangeSettingsTimer);
                blinkTargetOutRangeSettingsTimer = 0;
            }
            shutterSpeedText.enabled = true;
            apertureText.enabled = true;
        }
        else
        {
            if (blinkTargetOutRangeSettingsTimer == 0)
            {
                blinkTargetOutRangeSettingsTimer = G.Timer.RegisterTimer(1.0f, BlinkTargetOutRangeSettings, TimerUpdateMode.Update, -1);
            }
        }

    }
    private void OnAutoFocusAreaChanged(int old, int newValue) {
        UpdateFocusPositionUI();
    }

    private void BlinkTargetOutRangeSettings()
    {
        G.player.takePhotoCameraComp.GetValue("targetOutRangeSettings", out int targetOutRangeSettings);
        SettingParamType settingParamType = (SettingParamType)targetOutRangeSettings;
        bool nowVis = true;
        if (settingParamType.HasFlag(SettingParamType.ShutterSpeed))
        {
            nowVis = shutterSpeedText.enabled;
        }
        if (settingParamType.HasFlag(SettingParamType.Aperture))
        {
            nowVis = apertureText.enabled;
        }

        if (settingParamType.HasFlag(SettingParamType.ShutterSpeed))
        {
            shutterSpeedText.enabled = !nowVis;
        }
        if (settingParamType.HasFlag(SettingParamType.Aperture))
        {
            apertureText.enabled = !nowVis;
        }

    }
    #endregion


}
