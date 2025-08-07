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
    protected static new Dictionary<string, string> __shortcuts__ = new Dictionary<string, string>();
    protected override Dictionary<string, string> ShortCutsCache => __shortcuts__;
    protected override string[] SHORTCUT_OBJECTS => new string[]
    {
        "PanelExposeCompensationAP",
        "CurrentLightMeteringModeImageAP",
        "SelectTakePhotoParamsAP",
        "PanelExposePriorityAP",
        "NormalItems",
        "FocusPositionAP",
        "PanelShutter",
        "TextShutter",
        "TextShutterNum",
        "PanelAperture",
        "TextAperture",
        "TextApertureNum",
        "PanelIso",
        "TextIsoLTDoc",
        "TextIsoNum",
        "ImageExposeCompensation",
        "TextExposeCompensation",
    };
    private TextMeshProUGUI shutterSpeedText => this["TextShutterNum"].GetComponent<TextMeshProUGUI>();
    private TextMeshProUGUI apertureText => this["TextApertureNum"].GetComponent<TextMeshProUGUI>();
    private TextMeshProUGUI isoLTDocuText => this["TextIsoLTDoc"].GetComponent<TextMeshProUGUI>(); //左上角标注是iso的文字
    private TextMeshProUGUI isoText => this["TextIsoNum"].GetComponent<TextMeshProUGUI>();
    private Image exposureCompensationImage => this["ImageExposeCompensation"].GetComponent<Image>();
    private TextMeshProUGUI exposureCompensationText => this["TextExposeCompensation"].GetComponent<TextMeshProUGUI>();
    private RectTransform exposeCompensationAP => this["PanelExposeCompensationAP"];


    private TextMeshProUGUI[] hightLightTexts = new TextMeshProUGUI[0];
    private Image[] hightLightImages = new Image[0];

    private bool[] exposureOperationState =  new bool[2] { false, false }; // 0: E, 1: Q
    private int exposureOperation => (exposureOperationState[0] ? 1 : 0) + (exposureOperationState[1] ? -1 : 0); // E: +1, Q: -1

    // 长按调整曝光按钮时连续修改的定时器
    private uint exposureOpTimer = 0;
    

    // 当曝光需要的参数超出可以设置的范围时，闪烁
    private uint blinkTargetOutRangeSettingsTimer = 0;

    // 绑定的item
    private FocusPositionUI focusPositionUI = null;
    private SelectTakePhotoParamsUI selectTakePhotoParamsUI = null;
    private ExposureCompensationRule exposureCompensationRule = null;
    private LightMeteringModeImage currentLightMeteringModeImage = null;
    private ExposePriorityUI exposePriorityUI = null;

    protected override void OnLoad()
    {
        exposureCompensationRule = AttachUI<ExposureCompensationRule>("ExposureCompensationRule", "PanelExposeCompensationAP", AspectRatioFitter.AspectMode.FitInParent);
        currentLightMeteringModeImage = AttachUI<LightMeteringModeImage>("LightMeteringModeImage", "CurrentLightMeteringModeImageAP", AspectRatioFitter.AspectMode.FitInParent);
        exposePriorityUI = AttachUI<ExposePriorityUI>("ExposePriorityUI", "PanelExposePriorityAP", AspectRatioFitter.AspectMode.FitInParent);
        focusPositionUI = AttachUI<FocusPositionUI>("FocusPositionUI", "FocusPositionAP", AspectRatioFitter.AspectMode.None);
        selectTakePhotoParamsUI = AttachUI<SelectTakePhotoParamsUI>("SelectTakePhotoParamsUI", "SelectTakePhotoParamsAP", AspectRatioFitter.AspectMode.FitInParent);

        selectTakePhotoParamsUI.ShowSettingSelect(SettingParamType.AutoFocusArea);
        selectTakePhotoParamsUI.Hide();

        currentLightMeteringModeImage.SetBgVisibility(true);
        currentLightMeteringModeImage.SetLightMeteringMode(G.player.takePhotoCameraComp.MeteringMode);

        if (!G.player)
        {
            return;
        }
        UpdateFocusPositionUI(true);
        UpdateShutterSpeedText();
        UpdateExposurePriority(anim: false);
        UpdateExposureControlType(anim: false);
        UpdateExposureCompensationPara(anim: false);
        UpdateApertureText();
        UpdateISOText();

        var comp = G.player.takePhotoCameraComp;
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
        G.UI.RegisterLateUpdate(LateUpdate);

    }
    protected override void OnUnload()
    {
        if (G.UI)
        {
            G.UI.UnregisterLateUpdate(LateUpdate);
        }
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
            G.InputMgr.RegisterInput("REACT_E", InputEventType.Deactivated | InputEventType.Started, OnExposureChangeE, gameObject, CheckBaseInputValid);
            G.InputMgr.RegisterInput("REACT_Q", InputEventType.Deactivated | InputEventType.Started, OnExposureChangeQ, gameObject, CheckBaseInputValid);
            G.InputMgr.RegisterInput("EVPriority", InputEventType.Deactivated, OnExposurePriority, gameObject, CheckBaseInputValid);
            G.InputMgr.RegisterInput("MainTab", InputEventType.Deactivated, OnExposureControlType, gameObject, CheckBaseInputValid);
            G.InputMgr.RegisterInput("SecondMenu", InputEventType.Deactivated, OnOpenSecondMenu, gameObject);
            G.InputMgr.RegisterInput("UIMoveX", InputEventType.Started, OnUIMoveX, gameObject);
            G.InputMgr.RegisterInput("UIMoveY", InputEventType.Started, OnUIMoveY, gameObject);
            G.InputMgr.RegisterInput("RightMouseClick", InputEventType.Started | InputEventType.Deactivated, OnRightMouseClick, gameObject);
            G.InputMgr.RegisterInput("MouseScroll", InputEventType.Performed, OnMouseScroll, gameObject);
            G.InputMgr.RegisterInput("Attack", InputEventType.Started, OnAttack, gameObject);
        }
    }

    protected override void UnRegister()
    {
        base.UnRegister();
        if (G.InputMgr)
        {
            G.InputMgr.UnRegisterInput("REACT_E", OnExposureChangeE);
            G.InputMgr.UnRegisterInput("REACT_Q", OnExposureChangeQ);
            G.InputMgr.UnRegisterInput("EVPriority", OnExposurePriority);
            G.InputMgr.UnRegisterInput("MainTab", OnExposureControlType);
            G.InputMgr.UnRegisterInput("SecondMenu", OnOpenSecondMenu);
            G.InputMgr.UnRegisterInput("UIMoveX", OnUIMoveX);
            G.InputMgr.UnRegisterInput("UIMoveY", OnUIMoveY);
            G.InputMgr.UnRegisterInput("RightMouseClick", OnRightMouseClick);
            G.InputMgr.UnRegisterInput("MouseScroll", OnMouseScroll);
            G.InputMgr.UnRegisterInput("Attack", OnAttack);
        }
    }


    void LateUpdate(float dt)
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
    
    private void UpdateExposurePriority(bool anim = false)
    {
        if (G.player)
        {
            var validTypes = Helpers.GetDefaultExposureControlType(G.player.takePhotoCameraComp.EVPriority);
            this["PanelShutter"].GetComponent<Image>().enabled = Array.IndexOf(validTypes, ExposureControlType.ShutterSpeed) >= 0;
            this["PanelAperture"].GetComponent<Image>().enabled = Array.IndexOf(validTypes, ExposureControlType.Aperture) >= 0;

            exposePriorityUI.SetExposurePriority(G.player.takePhotoCameraComp.EVPriority);
            if (anim)
            {
                exposePriorityUI.PlayBgTween();
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
                exposureCompensationRule.gameObject.SetActive(true);
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
                exposureCompensationRule.gameObject.SetActive(EVCompensationPara != 0);
            }

            switch (ctrlType)
            {

                case ExposureControlType.ShutterSpeed:
                    TextMeshProUGUI shutterLTText = this["TextShutter"].GetComponent<TextMeshProUGUI>();
                    shutterLTText.color = Const.ColorConst.highlightColorBg;
                    shutterSpeedText.color = Const.ColorConst.highlightColorBg;
                    hightLightTexts = new TextMeshProUGUI[] { shutterLTText, shutterSpeedText };
                    hightLightImages = new Image[] { };
                    break;
                case ExposureControlType.Aperture:
                    TextMeshProUGUI apertureLBText = this["TextAperture"].GetComponent<TextMeshProUGUI>();
                    apertureLBText.color = Const.ColorConst.highlightColorBg;
                    apertureText.color = Const.ColorConst.highlightColorBg;
                    hightLightTexts = new TextMeshProUGUI[] { apertureLBText, apertureText };
                    hightLightImages = new Image[] { };
                    break;
                case ExposureControlType.ISO:
                    isoLTDocuText.color = Const.ColorConst.highlightColorBg;
                    isoText.color = Const.ColorConst.highlightColorBg;
                    hightLightTexts = new TextMeshProUGUI[] { isoLTDocuText, isoText };
                    hightLightImages = new Image[] { };
                    break;
                case ExposureControlType.ExposureCompensation:
                    exposureCompensationText.color = Const.ColorConst.highlightColorBg;
                    exposureCompensationImage.color = Const.ColorConst.highlightColorBg;
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
        exposureCompensationRule.ShowEVCompensation(para);
    }

    private void UpdateShutterSpeedText()
    {
        if (G.player)
        {
            shutterSpeedText.text = G.player.takePhotoCameraComp.ShutterSpeedInv.ToString();
        }
    }
    private void UpdateFocusPositionUI(bool need_init = false)
    {
        if (need_init)
        {
            // 初始化对焦区域的大小和区块大小
            G.player.takePhotoCameraComp.GetInitFocusData(out Vector2Int meteringSize, out Vector2Int blockPixelSize);
            focusPositionUI.InitFocusData(meteringSize, blockPixelSize);
        }
        // 更新焦点的大小和位置
        G.player.takePhotoCameraComp.GetCurrentFocusData(out Vector2Int blockNumSize, out Vector2Int centerOffset);
        focusPositionUI.SetBlock_Center(blockNumSize, centerOffset);
    }
    public void UpdateApertureText()
    {
        apertureText.text = G.player.takePhotoCameraComp.Aperture.ToString();
    }
    public void UpdateISOText()
    {
        isoText.text = G.player.takePhotoCameraComp.ISO.ToString();
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
        if (selectTakePhotoParamsUI != null && selectTakePhotoParamsUI.gameObject.activeSelf)
        {
            return false;
        }
        return true;
    }

    #region input events
    private bool OnAttack(InputActionArgs args)
    {
        if (InputEventType.Started.HasFlag(args.eventType))
        {
            var panelCapture = G.UI.EnsurePanel<PanelCapture>("PanelCapture");
            panelCapture?.ShowResult();
        }
        return true;
    }
    public bool OnMouseScroll(InputActionArgs args)
    {
        if (InputEventType.Performed.HasFlag(args.eventType))
        {
            var value = args.ReadValue<float>();
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
    public bool OnExposureChangeE(InputActionArgs args)
    {
        exposureOperationState[0] = InputEventType.Actived.HasFlag(args.eventType);
        StartOffsetExposure(0.1f);
        return true;
    }
    public bool OnExposureChangeQ(InputActionArgs args)
    {
        exposureOperationState[1] = InputEventType.Actived.HasFlag(args.eventType);
        StartOffsetExposure(0.1f);
        return true;
    }
    private bool OnExposurePriority(InputActionArgs args)
    {
        if (G.player)
        {
            G.player.takePhotoCameraComp.SetExposurePriority(G.player.takePhotoCameraComp.GetExposurePriority(1));
        }
        return true;
    }
    private bool OnExposureControlType(InputActionArgs args)
    {
        if (G.player)
        {
            G.player.takePhotoCameraComp.SetExposureControlType(G.player.takePhotoCameraComp.GetExposureControlType(1));
        }
        return true;
    }
    private bool OnOpenSecondMenu(InputActionArgs args)
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
    private bool OnUIMoveX(InputActionArgs args)
    {
        if (InputEventType.Started.HasFlag(args.eventType))
        {
            var value = args.ReadValue<float>();
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
    private bool OnUIMoveY(InputActionArgs args)
    {
        if (InputEventType.Started.HasFlag(args.eventType))
        {
            var value = args.ReadValue<float>();
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
    private bool OnRightMouseClick(InputActionArgs args)
    {
        if (InputEventType.Started.HasFlag(args.eventType))
        {
            G.player.stateComp.AddState(Const.StateConst.FOCUS);
        }
        else if (InputEventType.Deactivated.HasFlag(args.eventType))
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
        UpdateApertureText();
    }
    public void OnISOIdxChanged(int oldIdx, int newIdx)
    {
        UpdateISOText();
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
