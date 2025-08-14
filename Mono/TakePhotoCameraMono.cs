using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Linq;
using Const;
using Unity.VisualScripting;
using UnityEngine.Rendering.HighDefinition;



public class PhotoCalculator
{
    # region 一些常量和Get方法
    public readonly static Vector2Int GroupThreadSize = new Vector2Int(24, 24); //一个线程组的线程数量
    public readonly static Vector2Int ThreadPixelSize = new Vector2Int(4, 4); //一个线程处理的像素大小
    public static Vector2Int GroupPixelSize => new Vector2Int(ThreadPixelSize.x * GroupThreadSize.x, ThreadPixelSize.y * GroupThreadSize.y);
    public Vector2Int GroupSize { get; private set; } // 线程组数量
    public Vector2Int MeteringSize => GroupSize * GroupPixelSize; // 测光区域大小
    public Vector2Int CenterIdx => GroupSize / 2;
    private Vector2Int LBOffset;
    public readonly static int HistogramSize = 5; // 直方图大小

    public int BrightBufferSize => GroupSize.x * GroupSize.y;
    public int HistogramBufferSize => GroupSize.x * GroupSize.y * HistogramSize;

    public void PrepareAllShaderData(ComputeShader shader)
    {
        // 准备所有计算着色器需要的数据
        shader.SetInts("groupSize", new int[] { GroupSize.x, GroupSize.y, 1 });
        shader.SetInts("groupPixelSize", new int[] { GroupPixelSize.x, GroupPixelSize.y });
        shader.SetInts("lbOffset", new int[] { LBOffset.x, LBOffset.y });
        shader.SetInts("threadPixelSize", new int[] { ThreadPixelSize.x, ThreadPixelSize.y });
        Debug.Log($"GroupSize: {GroupSize}, groupPixelSize: {GroupPixelSize}, lbOffset: {LBOffset}, threadPixelSize: {ThreadPixelSize}");
    }
    #endregion
    int GroupID2GroupIndex(int x, int y)
    {
        //将二维组ID转换为一维组索引
        return x + y * GroupSize.x;
    }
    float GetBrightInGroup(int x, int y)
    {
        //获取指定组内的亮度数据
        int groupIndex = GroupID2GroupIndex(x, y);
        return BrightData[groupIndex];
    }
    float GetLaplacianInGroup(int x, int y)
    {
        //获取指定组内的拉普拉斯数据
        int groupIndex = GroupID2GroupIndex(x, y);
        return LaplacianData[groupIndex];
    }

    float GetHistogramInGroup(int x, int y, int histogramIndex)
    {
        //获取指定组内的直方图数据
        int groupIndex = GroupID2GroupIndex(x, y);
        return HistogramData[groupIndex + GroupSize.x * GroupSize.y * histogramIndex];
    }
    public float GetHistogram(int histogramIndex)
    {
        //获取所有组内的直方图数据
        float total = 0f;
        for (int i = 0; i < GroupSize.x; i++)
        {
            for (int j = 0; j < GroupSize.y; j++)
            {
                total += GetHistogramInGroup(i, j, histogramIndex);
            }
        }
        return total;
    }

    public float[] BrightData { get; private set; }
    public float[] LaplacianData { get; private set; }
    public uint[] HistogramData { get; private set; }

    public PhotoCalculator(Vector2Int textureSize)
    {
        GroupSize = new Vector2Int(
            (textureSize.x - GroupPixelSize.x) / GroupPixelSize.x / 2 * 2 + 1,
            (textureSize.y - GroupPixelSize.y) / GroupPixelSize.y / 2 * 2 + 1
        );
        LBOffset.x = (textureSize.x - MeteringSize.x) / 2;
        LBOffset.y = (textureSize.y - MeteringSize.y) / 2;

        BrightData = new float[BrightBufferSize];
        LaplacianData = new float[BrightBufferSize];
        HistogramData = new uint[HistogramBufferSize];
    }

    public float CalcBrightness(LightMeteringMode mode, Vector2Int focusPointID)
    {
        float totalBrightness = 0f;
        float totalWeight = 0f;
        float maxSqrDistance = 0f;
        switch (mode)
        {
            case LightMeteringMode.CenterWeighted:
                maxSqrDistance = CenterIdx.sqrMagnitude / 9;
                for (int i = 0; i < GroupSize.x; i++)
                {
                    for (int j = 0; j < GroupSize.y; j++)
                    {
                        Vector2Int posID = new Vector2Int(i, j);
                        float sqrDist = Math.Max((focusPointID - posID).sqrMagnitude, 1);
                        if (sqrDist < maxSqrDistance)
                        {
                            totalWeight += 1 / sqrDist * GroupPixelSize.x * GroupPixelSize.y;
                            totalBrightness += 1 / sqrDist * GetBrightInGroup(i, j);
                        }
                    }
                }
                return totalBrightness / totalWeight; // 返回中心加权区域的平均亮度
            case LightMeteringMode.Spot:
                focusPointID = new Vector2Int(Mathf.Clamp(focusPointID.x, 0, GroupSize.x - 1), Mathf.Clamp(focusPointID.y, 0, GroupSize.y - 1));
                totalBrightness = GetBrightInGroup(focusPointID.x, focusPointID.y);
                return totalBrightness / GroupPixelSize.x / GroupPixelSize.y; // 返回焦点区域的平均亮度
            case LightMeteringMode.Matrix:
                maxSqrDistance = CenterIdx.sqrMagnitude / 4;
                for (int i = 0; i < GroupSize.x; i++)
                {
                    for (int j = 0; j < GroupSize.y; j++)
                    {
                        Vector2Int posID = new Vector2Int(i, j);
                        float sqrDist = Math.Clamp((CenterIdx - posID).sqrMagnitude, 1, maxSqrDistance);
                        totalWeight += 1 / sqrDist * GroupPixelSize.x * GroupPixelSize.y;
                        totalBrightness += 1 / sqrDist * GetBrightInGroup(i, j);
                    }
                }
                return totalBrightness / totalWeight; // 返回中心加权区域的平均亮度
            case LightMeteringMode.Average:
                for (int i = 0; i < BrightData.Length; i++)
                {
                    totalBrightness += BrightData[i];
                }
                return totalBrightness / MeteringSize.x / MeteringSize.y; // 返回平均亮度
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }
    }
    public float CalcLaplacian(Vector2Int focusPointID)
    {
        // 计算指定焦点区域的拉普拉斯值
        return GetLaplacianInGroup(focusPointID.x, focusPointID.y);
    }
    public Vector2Int GetFocusNumSize(AutoFocusArea mode)
    {
        // 根据测光模式获取焦点区域的块数量，之后换成Focus的枚举
        switch (mode)
        {
            case AutoFocusArea.MIDDLE:
                return new Vector2Int(GroupSize.x / 2, GroupSize.y / 2);
            case AutoFocusArea.SPOT:
                return new Vector2Int(1, 1);
            case AutoFocusArea.FULL:
                return GroupSize;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }
    }

    public void ClampBlockIdx_Center(Vector2Int size, out Vector2Int min, out Vector2Int max)
    {
        min = -CenterIdx;
        max = Vector2Int.Max(min, CenterIdx - size + Vector2Int.one);
    }
}

public class TakePhotoCameraMono : MonoBehaviour
{

    [SerializeField]
    private ComputeShader cameraComputeShader;
    public Camera computeCamera { get; private set; }

    private PhotoCalculator _photoCalculator => G.player.photoCalculator;
    private RenderTexture nonCompensationRT;
    private int m_kernelHandle;
    private ComputeBuffer m_computeBrightBuffer;
    private ComputeBuffer m_laplacianBuffer;
    private ComputeBuffer m_histogramBuffer;

    private uint calcBrightnessTimer = 0;
    private List<AsyncGPUReadbackRequest> m_readbackRequests = new List<AsyncGPUReadbackRequest>(2);
    private readonly float checkDtTime = 0.15f; // 检查间隔时间

    // private uint focusCheckTimer;

    private List<float> ContrastFocusDistance = new List<float>(4);
    private List<float> ContrastFocusValue = new List<float>(4);
    private float lastCheckContrastStep = 5f;
    void Start()
    {
        computeCamera = transform.Find("ComputeCamera").GetComponent<Camera>();
        nonCompensationRT = computeCamera.targetTexture = G.player.computeCameraTexture;
        // 获取或添加HD Camera组件
        var hdComputeCamera = computeCamera.GetComponent<HDAdditionalCameraData>();
        G.gameManager.EnableVolumeForCamera(computeCamera.gameObject, new string[] { "ComputeVolume", "DefaultVolume" }, reset: true);
        G.gameManager.EnableVolumeForCamera(gameObject, new string[] { "RenderVolume", "DefaultVolume" }, reset: true);
        
        m_kernelHandle = cameraComputeShader.FindKernel("ComputeBrightMain");
        m_computeBrightBuffer = new ComputeBuffer(_photoCalculator.BrightBufferSize, sizeof(float));
        m_laplacianBuffer = new ComputeBuffer(_photoCalculator.BrightBufferSize, sizeof(float));
        m_histogramBuffer = new ComputeBuffer(_photoCalculator.HistogramBufferSize, sizeof(uint));

        cameraComputeShader.SetBuffer(m_kernelHandle, "BrightBuffer", m_computeBrightBuffer);
        cameraComputeShader.SetBuffer(m_kernelHandle, "LaplacianBuffer", m_laplacianBuffer);
        cameraComputeShader.SetBuffer(m_kernelHandle, "HistogramBuffer", m_histogramBuffer);
        cameraComputeShader.SetTexture(m_kernelHandle, "InputTexture", nonCompensationRT);
        _photoCalculator.PrepareAllShaderData(cameraComputeShader);
        // calcBrightnessTimer = G.RegisterTimer(checkDtTime, StartCalcBrightnessSync, repeateCount: -1);
        calcBrightnessTimer = G.RegisterTimer(checkDtTime, StartCalcBrightness, repeateCount: -1);
        if (G.player != null)
        {
            var takePhotoCameraComp = G.player.takePhotoCameraComp;
            takePhotoCameraComp.RegisterProp("focalLength", (Action<float, float>)OnFocalLengthChanged);
            takePhotoCameraComp.RegisterProp("focusDistance", (Action<float, float>)OnFocusDistanceChanged);
            // takePhotoCameraComp.RegisterProp("enableAutoFocus", (Action<int, int>)OnEnableAutoFocusChanged);

        }
    }

    void OnDestroy()
    {
        if (G.player != null)
        {
            var takePhotoCameraComp = G.player.takePhotoCameraComp;
            takePhotoCameraComp.UnRegisterProp("focalLength", (Action<float, float>)OnFocalLengthChanged);
            // takePhotoCameraComp.UnRegisterProp("enableAutoFocus", (Action<int, int>)OnEnableAutoFocusChanged);
            takePhotoCameraComp.UnRegisterProp("focusDistance", (Action<float, float>)OnFocusDistanceChanged);

        }
        if (calcBrightnessTimer != 0)
        {
            G.UnRegisterTimer(calcBrightnessTimer);
            calcBrightnessTimer = 0;
        }
        // if (focusCheckTimer != 0)
        // {
        //     G.UnRegisterTimer(focusCheckTimer);
        //     focusCheckTimer = 0;
        // }
        if (m_computeBrightBuffer != null)
        {
            m_computeBrightBuffer.Release();
            m_computeBrightBuffer = null;
        }
        if (m_laplacianBuffer != null)
        {
            m_laplacianBuffer.Release();
            m_laplacianBuffer = null;
        }
        if (m_histogramBuffer != null)
        {
            m_histogramBuffer.Release();
            m_histogramBuffer = null;
        }

    }
    void Update()
    {
        CalcBrightness();
        // ContrastFocus();
    }
    public void ContrastFocus()
    {
        float focusDistance;
        switch (ContrastFocusDistance.Count)
        {
            case 0:
                focusDistance = G.player.GetRenderCameraFocusDistance();
                break;
            case 1:
                focusDistance = Mathf.Max(5, ContrastFocusDistance[0] - lastCheckContrastStep);
                break;
            default:
            case 2:
                focusDistance = Mathf.Max(5, ContrastFocusDistance[0] + lastCheckContrastStep);
                break;
        }
        computeCamera.focusDistance = focusDistance;
        computeCamera.Render();
        cameraComputeShader.Dispatch(m_kernelHandle, _photoCalculator.GroupSize.x, _photoCalculator.GroupSize.y, 1);
        m_computeBrightBuffer.GetData(_photoCalculator.BrightData);
        m_laplacianBuffer.GetData(_photoCalculator.LaplacianData);
        m_histogramBuffer.GetData(_photoCalculator.HistogramData);


        ContrastFocusDistance.Add(focusDistance);
        float focusValue = G.player.takePhotoCameraComp.CalcLaplacian();
        ContrastFocusValue.Add(focusValue);

        switch (ContrastFocusDistance.Count)
        {
            case 1:
                break;
            case 2:
                break;
            default:
            case 3:
                float left = ContrastFocusValue[0] - ContrastFocusValue[1];
                float right = ContrastFocusValue[0] - ContrastFocusValue[2];
                if (left > 0 && right > 0)
                {
                    lastCheckContrastStep /= 2f;
                    lastCheckContrastStep = Mathf.Clamp(lastCheckContrastStep, 1f, 10f);
                    focusDistance = ContrastFocusDistance[0];
                    focusValue = ContrastFocusValue[0];
                    ContrastFocusDistance.Clear();
                    ContrastFocusValue.Clear();
                }
                else if (left > 0)
                {
                    lastCheckContrastStep *= 1.5f;
                    lastCheckContrastStep = Mathf.Clamp(lastCheckContrastStep, 1f, 10f);
                    focusDistance = ContrastFocusDistance[2];
                    focusValue = ContrastFocusValue[2];
                    ContrastFocusDistance.Clear();
                    ContrastFocusValue.Clear();
                }
                else if (right > 0)
                {
                    lastCheckContrastStep *= 1.5f;
                    lastCheckContrastStep = Mathf.Clamp(lastCheckContrastStep, 1f, 10f);
                    focusDistance = ContrastFocusDistance[1];
                    focusValue = ContrastFocusValue[1];
                    ContrastFocusDistance.Clear();
                    ContrastFocusValue.Clear();
                }
                else
                {
                    // lastCheckContrastStep *= 1.5f;
                    focusDistance = ContrastFocusDistance[0];
                    focusValue = ContrastFocusValue[0];
                    ContrastFocusDistance.Clear();
                    ContrastFocusValue.Clear();
                }
                Debug.Log($"Focus Distance: {focusDistance}, Focus Value: {focusValue}, Last Check Step: {lastCheckContrastStep}");
                G.player.SetRenderCameraFocusDistance(focusDistance);
                break;
        }

    }

    public void CalcBrightness()
    {
        if (m_readbackRequests.Count == 0)
        {
            return;
        }
        foreach (var request in m_readbackRequests)
        {
            if (!request.done)
            {
                return;
            }
        }
        m_readbackRequests[0].GetData<float>().ToList().CopyTo(_photoCalculator.BrightData);
        m_readbackRequests[1].GetData<float>().ToList().CopyTo(_photoCalculator.LaplacianData);
        m_readbackRequests[2].GetData<uint>().ToList().CopyTo(_photoCalculator.HistogramData);

        m_readbackRequests.Clear();
        float averageBrightness = G.player.takePhotoCameraComp.CalcBrightness();
        // Debug.Log($"Average Brightness: {averageBrightness}");
        float diffEV = (Const.PhotoConst.TaregetBrightness - averageBrightness) / Const.PhotoConst.EVToBrightness;
        if (MathF.Abs(diffEV) > 0.33f / 2)
        {
            G.player.takePhotoCameraComp.ChangedRealWantBaseEV100(diffEV);
        }

        // float laplacian = G.player.takePhotoCameraComp.CalcLaplacian();
        // Debug.Log($"Laplacian: {laplacian}");

        // Debug.Log($"Average Brightness: {averageBrightness}, Diff EV: {diffEV}");
        // for (int i = 0; i < PhotoCalculator.HistogramSize; i++)
        // {
        //     float histogramValue = m_photoCalculator.GetHistogram(i);
        //     Debug.Log($"Histogram[{i}]: {histogramValue}");
        // }

    }
    public void StartCalcBrightness()
    {
        if (m_readbackRequests.Count > 0)
        {
            return;
        }
        if (_photoCalculator == null)
        {
            return;
        }

        G.player.takePhotoCameraComp.GetComputeCameraParams(out float shutterSpeed, out float aperture, out int iso);
        computeCamera.shutterSpeed = shutterSpeed;
        computeCamera.aperture = aperture;
        computeCamera.iso = iso;
        computeCamera.Render();
        cameraComputeShader.Dispatch(m_kernelHandle, _photoCalculator.GroupSize.x, _photoCalculator.GroupSize.y, 1);
        m_readbackRequests.Add(AsyncGPUReadback.Request(m_computeBrightBuffer));
        m_readbackRequests.Add(AsyncGPUReadback.Request(m_laplacianBuffer));
        m_readbackRequests.Add(AsyncGPUReadback.Request(m_histogramBuffer));
    }

    // public void StartCalcBrightnessSync()
    // {
    //     if (m_readbackRequests.Count > 0)
    //     {
    //         return;
    //     }
    //     if (_photoCalculator == null)
    //     {
    //         return;
    //     }

    //     G.player.takePhotoCameraComp.GetComputeCameraParams(out float shutterSpeed, out float aperture, out int iso);
    //     computeCamera.shutterSpeed = shutterSpeed;
    //     computeCamera.aperture = aperture;
    //     computeCamera.iso = iso;
    //     computeCamera.Render();
    //     cameraComputeShader.Dispatch(m_kernelHandle, _photoCalculator.GroupSize.x, _photoCalculator.GroupSize.y, 1);

    //     m_computeBrightBuffer.GetData(_photoCalculator.BrightData);
    //     m_laplacianBuffer.GetData(_photoCalculator.LaplacianData);
    //     m_histogramBuffer.GetData(_photoCalculator.HistogramData);

    //     float averageBrightness = G.player.takePhotoCameraComp.CalcBrightness();
    //     // Debug.Log($"Average Brightness: {averageBrightness}");
    //     float diffEV = (Const.PhotoConst.TaregetBrightness - averageBrightness) / Const.PhotoConst.EVToBrightness;
    //     if (MathF.Abs(diffEV) > 0.33f / 2)
    //     {
    //         G.player.takePhotoCameraComp.ChangedRealWantBaseEV100(diffEV);
    //     }

    //     // float laplacian = G.player.takePhotoCameraComp.CalcLaplacian();
    // }

    public void OnFocalLengthChanged(float oldValue, float newValue)
    {
        computeCamera.focalLength = newValue;
    }
    // public void OnEnableAutoFocusChanged(int oldValue, int newValue)
    // {
    //     bool enable = newValue != 0;
    //     if (enable)
    //     {
    //         if (focusCheckTimer == 0)
    //         {
    //             focusCheckTimer = G.RegisterTimer(0.1f, TryAutoFocus, repeateCount: -1);
    //         }
    //     }
    //     else
    //     {
    //         if (focusCheckTimer != 0)
    //         {
    //             G.UnRegisterTimer(focusCheckTimer);
    //             focusCheckTimer = 0;
    //         }
    //     }
    // }
    public void OnFocusDistanceChanged(float oldValue, float newValue)
    {
        computeCamera.focusDistance = newValue;
    }
}
