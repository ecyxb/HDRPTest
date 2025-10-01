
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using System.Text;

public static class Helpers
{
    public static float GetExposureValue(float aperture, float shutterSpeedInv)
    {
        // 计算曝光值
        return Mathf.Log(aperture * aperture * shutterSpeedInv, 2);
    }
    public static float[] GenerateApertureParaValues()
    {
        return new float[] { 0.95f, 1.2f, 1.4f, 1.8f, 2.0f, 2.2f, 2.5f, 2.8f, 3.2F, 3.5F, 4.0f, 4.5f, 5f, 5.6f, 6.3f, 7.1f, 8.0f, 9.0f, 10f, 11.0f, 13f, 14f, 16f, 18f, 20f, 22f };
    }
    public static float[] GenerateShutterSpeedParaInvValues()
    {
        var data = new float[] { 2000, 1600, 1250, 1000, 800, 640, 500, 400, 320, 250
        , 200, 160, 125, 100, 80, 60, 50, 40, 30, 25, 20, 15
        , 13, 10, 8, 6, 5, 4, 3, 2.5f, 2, 1.6f, 1.3f, 1};
        Array.Reverse(data);
        return data;
    }
    public static int[] GenerateISOParaValues()
    {
        return new int[] { 64, 80, 100, 125, 160, 200, 250, 320, 400, 500, 640, 800, 1000, 1250, 1600, 2000, 2500, 3200, 4000, 5000, 6400, 8000, 10000, 12800, 16000, 20000, 25600, 32000, 40000, 51200, 64000 };
    }
    public static float[] GenerateISOExposureCache(int[] isoValues)
    {
        float[] result = new float[isoValues.Length];
        for (int i = 0; i < isoValues.Length; i++)
        {
            result[i] = Mathf.Log(isoValues[i] / 100.0f, 2);
        }
        return result;
    }

    public static float[][] GenerateExposureCache(float[] aperture, float[] shutterSpeedInv)
    {
        float[][] result = new float[aperture.Length][];
        for (int i = 0; i < aperture.Length; i++)
        {
            result[i] = new float[shutterSpeedInv.Length];
            for (int j = 0; j < shutterSpeedInv.Length; j++)
            {
                result[i][j] = GetExposureValue(aperture[i], shutterSpeedInv[j]);
            }
        }
        return result;
    }

    public static float GetExposureBy_Aperture_ShutterSpeed(int apertureIdx, int shutterSpeedIdx, float[][] exposureCache)
    {
        if (apertureIdx < 0 || apertureIdx >= exposureCache.Length || shutterSpeedIdx < 0 || shutterSpeedIdx >= exposureCache[0].Length)
            throw new ArgumentOutOfRangeException("Aperture or Shutter Speed index is out of range.");
        return exposureCache[apertureIdx][shutterSpeedIdx];
    }


    public static int GetISOBy_ISOExposure(float targetISOExposureValue, float[] isoEvCache)
    {
        int right = isoEvCache.Length - 1;
        int left = 0;
        while (left < right)
        {
            int idx_l = (left + right) / 2;
            int idx_r = idx_l + 1;
            float leftExposure = isoEvCache[idx_l];
            float rightExposure = isoEvCache[idx_r];
            if (targetISOExposureValue <= rightExposure && targetISOExposureValue >= leftExposure)
            {
                return Mathf.Abs(leftExposure - targetISOExposureValue) < Mathf.Abs(rightExposure - targetISOExposureValue) ? idx_l : idx_r;
            }
            if (targetISOExposureValue < leftExposure)
            {
                right = idx_l;
            }
            else
            {
                left = idx_r;
            }
        }
        return left;
    }

    public static int GetShutterSpeedBy_Aperture_Exposure(int apertureIdx, float targetExposure, float[][] exposureCache)
    {
        int right = exposureCache[0].Length - 1;
        int left = 0;
        while (left < right)
        {
            int idx_l = (left + right) / 2;
            int idx_r = idx_l + 1;
            float leftExposure = exposureCache[apertureIdx][idx_l];
            float rightExposure = exposureCache[apertureIdx][idx_r];
            if (targetExposure <= rightExposure && targetExposure >= leftExposure)
            {
                return Mathf.Abs(leftExposure - targetExposure) < Mathf.Abs(rightExposure - targetExposure) ? idx_l : idx_r;
            }
            if (targetExposure < leftExposure)
            {
                right = idx_l;
            }
            else
            {
                left = idx_r;
            }
        }
        return left;
    }

    public static int GetApertureBy_ShutterSpeed_Exposure(int shutterSpeedIdx, float targetExposure, float[][] exposureCache)
    {
        int right = exposureCache.Length - 1;
        int left = 0;
        while (left < right)
        {
            int idx_l = (left + right) / 2;
            int idx_r = idx_l + 1;
            float leftExposure = exposureCache[idx_l][shutterSpeedIdx];
            float rightExposure = exposureCache[idx_r][shutterSpeedIdx];
            if (targetExposure <= rightExposure && targetExposure >= leftExposure)
            {
                return Mathf.Abs(leftExposure - targetExposure) < Mathf.Abs(rightExposure - targetExposure) ? idx_l : idx_r;
            }
            if (targetExposure < leftExposure)
            {
                right = idx_l;
            }
            else
            {
                left = idx_r;
            }
        }
        return left;
    }


    // 这里表示可以主动修改的
    public static ExposureControlType[] GetDefaultExposureControlType(ExposurePriority priority)
    {
        switch (priority)
        {
            case ExposurePriority.ShutterSpeed:
                return new[] { ExposureControlType.ShutterSpeed, ExposureControlType.ISO, ExposureControlType.ExposureCompensation };
            case ExposurePriority.Aperture:
                return new[] { ExposureControlType.Aperture, ExposureControlType.ISO, ExposureControlType.ExposureCompensation };
            case ExposurePriority.Program:
                return new[] { ExposureControlType.ISO, ExposureControlType.ExposureCompensation };
            default:
                return new[] { ExposureControlType.ShutterSpeed, ExposureControlType.Aperture, ExposureControlType.ISO, ExposureControlType.ExposureCompensation };
        }
    }

    public static string GetPathBetweenFatherAndChild(GameObject father, GameObject child)
    {
        if (father == null || child == null)
        {
            return string.Empty;
        }

        List<string> path = new List<string>();
        Transform current = child.transform;

        while (current != null && current != father.transform)
        {
            path.Add(current.name);
            current = current.parent;
        }

        if (current == null)
        {
            return string.Empty; // No valid path found
        }

        path.Reverse();
        return string.Join("/", path);
    }
    public static string GetExposurePriorityString(ExposurePriority priority)
    {
        switch (priority)
        {
            case ExposurePriority.ShutterSpeed:
                return DataLoader.Tr("快门优先", 7);
            case ExposurePriority.Aperture:
                return DataLoader.Tr("光圈优先", 6);
            case ExposurePriority.Program:
                return DataLoader.Tr("程序自动", 51);
            default:
                return DataLoader.Tr("手动模式", 8);
        }
    }

    public static string GetLightMeteringModeString(LightMeteringMode mode)
    {
        switch (mode)
        {
            case LightMeteringMode.CenterWeighted:
                return DataLoader.Tr("中央重点测光", 5);
            case LightMeteringMode.Spot:
                return DataLoader.Tr("点测光", 4);
            case LightMeteringMode.Matrix:
                return DataLoader.Tr("矩阵曝光", 3);
            default:
                return DataLoader.Tr("平均測光", 52);
        }
    }
    public static string GetAutoFocusAreaString(Const.AutoFocusArea area)
    {
        switch (area)
        {
            case Const.AutoFocusArea.FULL:
                return DataLoader.Tr("全屏对焦", 54);
            case Const.AutoFocusArea.SPOT:
                return DataLoader.Tr("单点对焦", 13);
            case Const.AutoFocusArea.MIDDLE:
                return DataLoader.Tr("宽区域对焦", 53);
            default:
                return DataLoader.Tr("宽区域对焦", 53);
        }
    }

    public static float FocalLengthToVerticalFOV(float SensorSizeY, float focalLength)
    {
        if (focalLength < 0.001f)
            return 180f;
        return Mathf.Rad2Deg * 2.0f * Mathf.Atan(SensorSizeY * 0.5f / focalLength);
    }

    public static float VerticalFOVToFocalLength(float SensorSizeY, float fov)
    {
        return SensorSizeY * 0.5f / Mathf.Tan(Mathf.Deg2Rad * fov * 0.5f);
    }

    public static float FastFloatDiffLerp(float origin, float target, float minDiff, float ratio)
    {
        //需要快速lerp的数是线性的
        var diff = target - origin;
        if (diff > 0)
        {
            return Mathf.Min(origin + Mathf.Max(diff, minDiff) * ratio, target);
        }
        else if (diff < 0)
        {
            return Mathf.Max(origin + Mathf.Min(diff, -minDiff) * ratio, target);
        }
        return origin;
    }

    public static float FastFloatMultiDiffLerp(float origin, float target, float minMultiDiff, float ratio)
    {
        //需要快速lerp的数是倍数上线性的
        if (origin == 0 || target == 0 || target / origin < 0)
        {
            return origin;
        }
        if (target > origin)//target > origin
        {
            return Mathf.Min(origin + origin * Mathf.Max(target / origin - 1, minMultiDiff) * ratio, target);
        }
        else if (origin > target)
        {
            return Mathf.Max(origin - origin / Mathf.Max(origin / target - 1, minMultiDiff) * ratio, target);
        }
        return origin;
    }


    public static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    #region  UIHelpers
    public static string GetGameObjectPath(Transform obj, Transform stopAt, StringBuilder sb = null)
    {
        sb = sb ?? new StringBuilder();
        sb.Clear();
        sb.Append(obj.name);
        while (obj.parent != null)
        {
            if (obj.parent == stopAt)
            {
                // If the parent is a UICommon, we stop here
                break;
            }
            obj = obj.parent;
            sb.Insert(0, obj.name + "/");
        }
        return sb.ToString();
    }

    public static GameObject FindUIPrefab(string uiName)
    {
        GameObject prefab = Resources.Load<GameObject>($"UIPrefabs/{uiName}");
        if (prefab == null)
        {
            Debug.LogError($"UI prefab {uiName} not found in Resources/UIPrefabs.");
            return null;
        }
        return prefab;
    }

    public static void EnsureAspectRatioFitter(RectTransform obj, AspectRatioFitter.AspectMode aspectFit)
    {
        AspectRatioFitter fitter = obj.GetComponent<AspectRatioFitter>();
        if (aspectFit == AspectRatioFitter.AspectMode.None)
        {
            if (fitter != null)
            {
                fitter.aspectMode = AspectRatioFitter.AspectMode.None;
            }
        }
        else
        {
            var ratio = obj.rect.width / obj.rect.height;
            fitter = fitter ?? obj.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = aspectFit;
            if (aspectFit == AspectRatioFitter.AspectMode.FitInParent)
            {
                fitter.aspectRatio = ratio;
            }
        }
    }


    public static Vector3 WorldPos2ScreenPos(Vector3 worldPos, Camera camera)
    {
        return camera.WorldToScreenPoint(worldPos);
    }

    public static Vector3 ScreenPos2ViewPos(Vector3 screenPos, Camera camera)
    {
        Vector3 viewportPos = camera.ScreenToViewportPoint(screenPos);
        return new Vector3(viewportPos.x, viewportPos.y, viewportPos.z);
    }

    public static float AngleNormalize180(float angle)
    {
        // Normalize the angle to the range [-180, 180]
        return Mathf.Repeat(angle + 180f, 360f) - 180f;
    }
}