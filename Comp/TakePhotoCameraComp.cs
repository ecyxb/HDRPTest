using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EventFramework;
using System;
using UnityEngine.InputSystem.Interactions;
using DG.Tweening;
using System.Linq;
using UnityEditor;
public enum ExposurePriority
{
    Manual = 0, // 手动曝光
    Aperture = 1, // 光圈优先
    ShutterSpeed = 2, // 快门速度优先
    Program = 3, // 程序曝光

}
public enum ExposureControlType
{
    Aperture, // 光圈
    ShutterSpeed, // 快门速度
    ISO, // 感光度
    ExposureCompensation, // 曝光补偿
}

public enum LightMeteringMode
{
    CenterWeighted, // 中央重点测光
    Spot, // 点测光
    Matrix, // 矩阵测光
    Average, // 平均测光
}

public enum SettingParamType
{
    None = 0,
    Aperture = 1 << 0, // 光圈
    ShutterSpeed = 1 << 1, // 快门速度
    ISO = 1 << 2, // 感光度
    ExposureCompensation = 1 << 3, // 曝光补偿
    LightMeteringMode = 1 << 4, // 测光模式
    AutoFocusArea = 1 << 5, // 自动对焦区域
    ExposurePriority = 1 << 6 // 曝光优先级
}

public struct ExposureParaValue
{
    public float[] FValues;
    public float[] SSValues;
    public int[] isoValues;
    public float[][] baseEvCache;
    public float[] isoEvCache;

    public void InitData()
    {
        FValues = Helpers.GenerateApertureParaValues();
        SSValues = Helpers.GenerateShutterSpeedParaInvValues();
        isoValues = Helpers.GenerateISOParaValues();
        baseEvCache = Helpers.GenerateExposureCache(FValues, SSValues);
        isoEvCache = Helpers.GenerateISOExposureCache(isoValues);
    }
}


public class TakePhotoCameraComp : EventCompBase
{
    PrimaryPlayer m_player => GetParentDict() as PrimaryPlayer;    private ExposureParaValue evParaValue;
    private float RealWantBaseEV100;
    private float WantBaseEV => RealWantBaseEV100 - EVCompensation + ISOEV;
    private uint focalLengthChangeTimer;

    protected static Dictionary<string, UnionInt64> slotMap = new Dictionary<string, UnionInt64>
    {
        { "cameraID", 1 },
        { "lensData", 1 },
        { "plugData", 1 },

        { "shutterSpeedIdx", 3 },
        { "isoIdx", 3 },
        { "apertureIdx",  3 },

        { "exposureCompensationPara", 0 }, // 曝光补偿档位
        
        { "exposureControlType", (int)ExposureControlType.ShutterSpeed }, // 滚轮控制类型
        { "exposurePriority", (int)ExposurePriority.Aperture }, // 曝光优先级

        { "lightMeteringMode", (int)LightMeteringMode.Spot }, // 测光模式
        { "autoFocusArea", (int)Const.AutoFocusArea.SPOT }, // 自动对焦区域

        { "targetOutRangeSettings", 0}, // 是否有参数需要超出设置范围

        { "focusIdxCenterOffset", new Vector2Int(0, 0)}, // 中心对焦坐标
        { "focalLength", 24.0f},
        // { "enableAutoFocus", 1 }, // 是否启用自动对焦
        { "focusDistance", 1.0f}, // 对焦距离
        // {"focusSize", new Vector2Int(1, 1)}
    };
    public delegate void ClampBlockIdx_CenterFunc(Vector2Int blockSize, out Vector2Int minClamp, out Vector2Int maxClamp);
    public ClampBlockIdx_CenterFunc ClampBlockIdx_Center;

    public TakePhotoCameraComp() : base(slotMap)
    {
    }

    #region Init

    public override void CompStart()
    {
        InitCamera();
        ClampBlockIdx_Center += m_player.photoCalculator.ClampBlockIdx_Center;
        // InitEvent();
    }
    public override void CompDestroy()
    {
        base.CompDestroy();
        if (focalLengthChangeTimer != 0)
        {
            G.UnRegisterTimer(focalLengthChangeTimer);
            focalLengthChangeTimer = 0;
        }
    }

    public void InitCamera()
    {
        evParaValue.InitData();

        SetShutterSpeedIdx(evParaValue.SSValues.Length - 3);
        m_player.SetExposureMainParams(ShutterSpeed, Aperture, ISO);
        RealWantBaseEV100 = ShutterApertureEV - ISOEV;
        SetExposurePriority(ExposurePriority.Aperture);

    }
    #endregion


    #region GetFunction
    // EVCompensation是曝光补偿，需要增加曝光量时为正值
    // ISOEV是ISO对应的EV增益，ISO越高，ISOEV越大
    // RealWantBaseEV100 在ISO100下，为了到达18灰度的曝光量需要的光圈·快门 【+ISOEV后，为在目标ISO下的快门·光圈】 【-EVCompensation后，为在目标曝光补偿下的快门·光圈】

    // ISO、快门、光圈在这里都是目标值，而不是渲染在相机上的值，中间可能会有过渡动画

    public int ISO => evParaValue.isoValues[GetISOIdx()];
    public float ShutterSpeed => 1.0f / evParaValue.SSValues[GetShutterSpeedIdx()];
    public float ShutterSpeedInv => evParaValue.SSValues[GetShutterSpeedIdx()];
    public float Aperture => evParaValue.FValues[GetApertureIdx()];
    public float ISOEV => evParaValue.isoEvCache[GetISOIdx()]; // = log2(ISO) - log2(100)
    public float ShutterApertureEV => evParaValue.baseEvCache[GetApertureIdx()][GetShutterSpeedIdx()]; // = EV(capture.shutter)_100 - log2(100)
    public int EVCompensationPara => (int)this["exposureCompensationPara"];
    public float EVCompensation => (float)Math.Round(EVCompensationPara * 0.333, 1);
    public LightMeteringMode MeteringMode => (LightMeteringMode)(int)this["lightMeteringMode"];
    public Const.AutoFocusArea AutoFocusArea => (Const.AutoFocusArea)(int)this["autoFocusArea"];
    public Vector2Int FocusIdxCenterOffset => (Vector2Int)this["focusIdxCenterOffset"];
    public ExposurePriority EVPriority => (ExposurePriority)(int)this["exposurePriority"];
    public float FocalLength => (float)this["focalLength"]; // 焦距
    public float FocusDistance => (float)this["focusDistance"]; // 对焦距离

    public ExposurePriority[] AllEVPriority => new ExposurePriority[] { ExposurePriority.Manual, ExposurePriority.Aperture, ExposurePriority.ShutterSpeed, ExposurePriority.Program };
    public LightMeteringMode[] AllLightMeteringModes => new LightMeteringMode[] { LightMeteringMode.CenterWeighted, LightMeteringMode.Spot, LightMeteringMode.Matrix, LightMeteringMode.Average };
    public Const.AutoFocusArea[] AllAutoFocusAreas => new Const.AutoFocusArea[] { Const.AutoFocusArea.SPOT, Const.AutoFocusArea.MIDDLE, Const.AutoFocusArea.FULL };





    //修改曝光的控制类型

    public int GetApertureIdx(int offset = 0)
    {
        GetValue("apertureIdx", out int idx);
        return Math.Clamp(idx + offset, 0, evParaValue.FValues.Length - 1);
    }
    public int GetShutterSpeedIdx(int offset = 0)
    {
        GetValue("shutterSpeedIdx", out int idx);
        return Math.Clamp(idx + offset, 0, evParaValue.SSValues.Length - 1);
    }
    public int GetEVCompensationPara(int offset = 0)
    {
        GetValue("exposureCompensationPara", out int exposureCompensation);
        return Math.Clamp(exposureCompensation + offset, -5 * 3, 5 * 3);
    }

    public int GetISOIdx(int offset = 0)
    {
        GetValue("isoIdx", out int isoIdx);
        return Math.Clamp(isoIdx + offset, 0, evParaValue.isoValues.Length - 1);
    }

    public ExposurePriority GetExposurePriority(int offset = 0)
    {
        if (offset == 0)
        {
            return EVPriority;
        }
        int index = Array.IndexOf(AllEVPriority, EVPriority);
        index += offset;
        while (index < 0)
        {
            index += AllEVPriority.Length; // 循环到最后一个
        }
        while (index > AllEVPriority.Length - 1)
        {
            index -= AllEVPriority.Length; // 循环到第一个
        }
        return AllEVPriority[index];
    }
    public ExposureControlType GetExposureControlType(int offset = 0)
    {
        GetValue("exposureControlType", out int controlType);
        var validTypes = Helpers.GetDefaultExposureControlType(EVPriority);
        int newTypeIndex = Array.FindIndex(validTypes, type => type == (ExposureControlType)controlType) + offset;
        while (newTypeIndex < 0)
        {
            newTypeIndex += validTypes.Length; // 循环到最后一个
        }
        while (newTypeIndex > validTypes.Length - 1)
        {
            newTypeIndex -= validTypes.Length; // 循环到第一个
        }
        return validTypes[newTypeIndex];
    }

    public void GetInitFocusData(out Vector2Int meteringSize, out Vector2Int blockPixelSize)
    {
        meteringSize = m_player.photoCalculator.MeteringSize;
        blockPixelSize = PhotoCalculator.GroupPixelSize;
    }
    public void GetCurrentFocusData(out Vector2Int blockNumSize, out Vector2Int centerOffset)
    {
        blockNumSize = m_player.photoCalculator.GetFocusNumSize(AutoFocusArea);
        centerOffset = FocusIdxCenterOffset;
    }

    // 对焦点绘制的数据
    // 测光
    public float CalcBrightness()
    {
        return m_player.photoCalculator.CalcBrightness(MeteringMode, FocusIdxCenterOffset + m_player.photoCalculator.CenterIdx);
    }
    public float CalcLaplacian()
    {
        return m_player.photoCalculator.CalcLaplacian(FocusIdxCenterOffset + m_player.photoCalculator.CenterIdx);
    }
    #endregion


    private void SetShutterSpeedIdx(int idx)
    {
        idx = Math.Clamp(idx, 0, evParaValue.SSValues.Length - 1);
        SetValue("shutterSpeedIdx", idx);
    }
    private void SetApertureIdx(int idx)
    {
        idx = Math.Clamp(idx, 0, evParaValue.FValues.Length - 1);
        SetValue("apertureIdx", idx);
    }
    private void SetExposureCompensation(int para)
    {
        para = Math.Clamp(para, -5 * 3, 5 * 3);
        SetValue("exposureCompensationPara", para);
    }
    private void SetISOIdx(int idx)
    {
        idx = Math.Clamp(idx, 0, evParaValue.isoValues.Length - 1);
        SetValue("isoIdx", idx);
    }
    public void SetFocusIdxCenterOffset(Vector2Int offset, Const.AutoFocusArea mode)
    {
        // 计算组件的范围
        Vector2Int _max = new Vector2Int(999, 999);
        Vector2Int _min = Vector2Int.zero - _max;

        foreach (var _func in ClampBlockIdx_Center.GetInvocationList())
        {
            var func = (ClampBlockIdx_CenterFunc)_func;
            func.Invoke(m_player.photoCalculator.GetFocusNumSize(mode), out Vector2Int minClamp, out Vector2Int maxClamp);
            _min = Vector2Int.Max(minClamp, _min);
            _max = Vector2Int.Min(maxClamp, _max);
        }
        this.Assert(_min.x <= _max.x && _min.y <= _max.y, "ClampBlockIdx_CenterFunc should return valid range");
        offset = Vector2Int.Max(_min, Vector2Int.Min(_max, offset));
        SetValue("focusIdxCenterOffset", offset);
    }
    public void SetAutoFocusArea(Const.AutoFocusArea mode)
    {
        var offset = FocusIdxCenterOffset;
        var oldSize = m_player.photoCalculator.GetFocusNumSize(AutoFocusArea);
        var newSize = m_player.photoCalculator.GetFocusNumSize(mode);
        offset -= (newSize - oldSize) / 2;
        SetFocusIdxCenterOffset(offset, mode);
        SetValue("autoFocusArea", (int)mode);
    }

    public void SetLightMeteringMode(LightMeteringMode mode)
    {
        SetValue("lightMeteringMode", (int)mode);
    }

    public void SetExposurePriority(ExposurePriority priority)
    {
        SetValue("exposurePriority", (int)priority);
        var validTypes = Helpers.GetDefaultExposureControlType(priority);
        var nowType = GetExposureControlType();
        if (priority == ExposurePriority.Manual && (nowType == ExposureControlType.Aperture || nowType == ExposureControlType.ShutterSpeed))
        {
            // 如果是手动曝光模式，且当前控制类型是光圈或快门速度，则不重置
            return;
        }
        //其他模式都重置成第一个有效类型
        SetValue("exposureControlType", (int)validTypes[0]);
        UpdateTargetOutRangeSettings();
    }
    public void SetExposureControlType(ExposureControlType type)
    {
        SetValue("exposureControlType", (int)type);
    }


    public void UpdateShutterAndAperture(float targetEV)
    {
        switch (EVPriority)
        {
            case ExposurePriority.Aperture:
                int newShutterSpeedIdx = Helpers.GetShutterSpeedBy_Aperture_Exposure(GetApertureIdx(), targetEV, evParaValue.baseEvCache);
                SetShutterSpeedIdx(newShutterSpeedIdx);
                break;
            case ExposurePriority.ShutterSpeed:
                int newApertureIdx = Helpers.GetApertureBy_ShutterSpeed_Exposure(GetShutterSpeedIdx(), targetEV, evParaValue.baseEvCache);
                SetApertureIdx(newApertureIdx);
                break;
            default:
                break;
        }
    }
    public void GetComputeCameraParams(out float shutterSpeed, out float aperture, out int iso)
    {
        shutterSpeed = ShutterSpeed;
        aperture = Aperture;
        iso = ISO;
        switch (EVPriority)
        {
            case ExposurePriority.Aperture:
                int newShutterSpeedIdx = Helpers.GetShutterSpeedBy_Aperture_Exposure(GetApertureIdx(), RealWantBaseEV100 + ISOEV, evParaValue.baseEvCache);
                shutterSpeed = 1.0f / evParaValue.SSValues[newShutterSpeedIdx];
                break;
            case ExposurePriority.ShutterSpeed:
                int newApertureIdx = Helpers.GetApertureBy_ShutterSpeed_Exposure(GetShutterSpeedIdx(), RealWantBaseEV100 + ISOEV, evParaValue.baseEvCache);
                aperture = evParaValue.FValues[newApertureIdx];
                break;
            default:
                break;
        }

    }

    public void ChangedRealWantBaseEV100(float diff)
    {
        diff = Mathf.Clamp(diff, -1.0f, 1.0f); // 限制变化范围
        RealWantBaseEV100 -= diff;
        UpdateShutterAndAperture(WantBaseEV);
        RealWantBaseEV100 = ShutterApertureEV + EVCompensation - ISOEV;
        UpdateTargetOutRangeSettings();
    }

    public void OffsetExposure(int offset)
    {
        switch (GetExposureControlType())
        {
            case ExposureControlType.Aperture:
                // 修改光圈
                SetApertureIdx(GetApertureIdx(offset));
                UpdateShutterAndAperture(WantBaseEV);
                break;
            case ExposureControlType.ShutterSpeed:
                SetShutterSpeedIdx(GetShutterSpeedIdx(offset));
                UpdateShutterAndAperture(WantBaseEV);
                break;
            case ExposureControlType.ExposureCompensation:
                SetExposureCompensation(GetEVCompensationPara(offset));
                UpdateShutterAndAperture(WantBaseEV);
                break;
            case ExposureControlType.ISO:
                SetISOIdx(GetISOIdx(offset));
                UpdateShutterAndAperture(WantBaseEV);
                break;
        }
        m_player.SetExposureMainParams(ShutterSpeed, Aperture, ISO);
        UpdateTargetOutRangeSettings();
    }

    public void UpdateTargetOutRangeSettings()
    {
        var shutterApertureEV = ShutterApertureEV;
        var validTypes = Helpers.GetDefaultExposureControlType(EVPriority);
        int rangeSetting = 0;
        if (!validTypes.Contains(ExposureControlType.Aperture))
        {
            GetValue("apertureIdx", out int apertureIdx);
            if (apertureIdx == evParaValue.FValues.Length - 1 && shutterApertureEV < WantBaseEV)
            {
                // 如果光圈是最小值，且曝光量小于目标曝光量，则认为光圈过小
                rangeSetting |= (int)SettingParamType.Aperture;
            }
            if (apertureIdx == 0 && shutterApertureEV > WantBaseEV)
            {
                // 如果光圈是最大值，且曝光量大于目标曝光量，则认为光圈过大
                rangeSetting |= (int)SettingParamType.Aperture;
            }
        }
        if (!validTypes.Contains(ExposureControlType.ShutterSpeed))
        {
            GetValue("shutterSpeedIdx", out int shutterSpeedIdx);
            if (shutterSpeedIdx == evParaValue.SSValues.Length - 1 && shutterApertureEV < WantBaseEV)
            {
                // 如果快门速度是最小值，且曝光量小于目标曝光量，则认为快门速度过小
                rangeSetting |= (int)SettingParamType.ShutterSpeed;
            }
            if (shutterSpeedIdx == 0 && shutterApertureEV > WantBaseEV)
            {
                // 如果快门速度是最大值，且曝光量大于目标曝光量，则认为快门速度过大
                rangeSetting |= (int)SettingParamType.ShutterSpeed;
            }
        }
        SetValue("targetOutRangeSettings", rangeSetting);
    }

    public void LateUpdate_TryAutoFocus()
    {
        if(!m_player.stateComp.HasState(Const.StateConst.FOCUS))
        {
            return; // 如果没有对焦状态，则不进行对焦
        }
        bool screenPosValid = m_player.playerMovementComp.GetFocusPointScreenPos(out Vector2 screenPos);
        var targetMono = m_player.playerMovementComp.focusTargetMono;
        if(targetMono == null && !screenPosValid)
        {
            // 如果没有对焦目标，且屏幕位置无效，则不进行对焦
            return;
        }
        bool useFreeFocus = AutoFocusArea == Const.AutoFocusArea.SPOT || targetMono == null;
        if (useFreeFocus)
        {
            Ray r = m_player.RenderCamera.ScreenPointToRay(screenPos); //这个要用玩家实际看到的相机，因为计算用的相机大小和UI不一致的。
            if (Physics.Raycast(r, out RaycastHit hitInfo, 200, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore))
            {
                SetFocusDistance(hitInfo.distance);
            }
            return;
        }
        else
        {
            var positions = targetMono.GetCanFocusPositions();
            var cameraPosition = m_player.RenderCamera.transform.position;
            float minDistance = 1e9f + 1;
            foreach (var p in positions)
            {
                Ray r = new Ray(cameraPosition, p - cameraPosition);
                if (Physics.Raycast(r, out RaycastHit hitInfo, 200, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore))
                {
                    minDistance = Mathf.Min(minDistance, hitInfo.distance);
                }
            }
            if(minDistance < 1e9f)
            {
                SetFocusDistance(minDistance);
            }
        }

    }

    public void UpdateFocalLength(float offset)
    {
        float FocalLength = this.FocalLength;
        if (FocalLength > 40)
        {
            offset *= (FocalLength - 40) / 40 + 1;
        }
        else
        {
            offset *= 1 - (40 - FocalLength) / 40;
        }
        SetValue("focalLength", Mathf.Clamp(FocalLength + offset, 16, 120));
    }

    public void SetFocusDistance(float distance)
    {
        SetValue("focusDistance", Mathf.Max(distance, 0.1f)); // 确保对焦距离大于0
    }
}