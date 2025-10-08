using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerData : MonoBehaviour
{
    protected CameraData cameraData;
    protected LensData lensData;
    protected PlugData equippedPlugData;
    protected float focalLength = 35;

    public int GetMaxFocalLength()
    {
        return Math.Max(lensData.minFocalLenth, lensData.maxFocalLenth);
    }
    public int GetMinFocalLength()
    {
        return lensData.minFocalLenth;
    }
    public bool IsFixedFocalLength()
    {
        return lensData.maxFocalLenth == lensData.minFocalLenth;
    }
    public float GetMaxAperture()
    {
        return lensData.maxAperture;
    }
    public float GetFocalLength()
    {
        return focalLength;
    }
    public float ChangeFocalLength(float delta)
    {
        focalLength = Mathf.Clamp(focalLength + delta, GetMinFocalLength(), GetMaxFocalLength());
        return GetFocalLength();
    }
}
