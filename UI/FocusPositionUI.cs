
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class FocusPositionUI : UICommon
{
    protected override string[] SHORTCUT_OBJECTS => new string[]
    {
        "FocusTargetImageFakeOrigin",
    };

    // Start is called before the first frame update
    [SerializeField]
    private RectTransform leftSide;
    [SerializeField]
    private RectTransform rightSide;
    [SerializeField]
    private RectTransform topSide;
    [SerializeField]
    private RectTransform bottomSide;
    [Space(10)]
    [SerializeField]
    private RectTransform centerPoint;

    [SerializeField]
    private float SideWidth = 5f;
    // [SerializeField]
    // private Vector2 blockSize = new Vector2(100, 100);

# if UNITY_EDITOR
    [Space(10)]
    [SerializeField]
    private RectTransform meterRect;
    [SerializeField]
    private RectTransform validMeterRect;
#endif
    [SerializeField]
    private Vector2Int MeterPixelSize = new Vector2Int(1000, 1000); //需要初始化的
    [SerializeField]
    private Vector2Int MeterBlockNum = new Vector2Int(1, 1);
    [SerializeField]
    private Vector2Int BlockPixelSize = new Vector2Int(100, 100);
    [Space(10)]
    [SerializeField]
    private Vector2Int BlockIdx = new Vector2Int(100, 100);
    [SerializeField]
    private Vector2Int BlockNumSize = new Vector2Int(1, 1);

    private List<FocusTargetImageUI> focusPositionRects = new List<FocusTargetImageUI>();


    // private Vector2 ValidLB
    // {
    //     get
    //     {
    //         return new Vector2(
    //             Mathf.Max((transform as RectTransform).rect.width / 2 - MeterPixelSize.x / 2, 0),
    //             Mathf.Max((transform as RectTransform).rect.height / 2 - MeterPixelSize.y / 2, 0)
    //         );
    //     }
    // }
    // private Vector2 ValidRT
    // {
    //     get
    //     {
    //         Rect rect = (transform as RectTransform).rect;
    //         return new Vector2(
    //             Mathf.Min(rect.width / 2 + MeterPixelSize.x / 2, rect.width),
    //             Mathf.Min(rect.height / 2 + MeterPixelSize.y / 2, rect.height)
    //         );
    //     }
    // }

    public void UpdateSideWith(float sideWidth)
    {
        SideWidth = sideWidth;
        leftSide.sizeDelta = new Vector2(SideWidth, leftSide.sizeDelta.y);
        rightSide.sizeDelta = new Vector2(SideWidth, rightSide.sizeDelta.y);
        topSide.sizeDelta = new Vector2(topSide.sizeDelta.x, SideWidth);
        bottomSide.sizeDelta = new Vector2(bottomSide.sizeDelta.x, SideWidth);
        // #if UNITY_EDITOR
        //         meterRect.localPosition = Vector3.zero;
        //         meterRect.sizeDelta = new Vector2(MeterPixelSize.x, MeterPixelSize.y);
        //         validMeterRect.localPosition = Vector3.zero;
        //         validMeterRect.sizeDelta = new Vector2(ValidRT.x - ValidLB.x, ValidRT.y - ValidLB.y);
        // #endif
    }
    // public void SetBlockLT(Vector2Int blockNumSize, Vector2Int blockIdx, Vector2Int meterBlockNum)
    // {
    //     centerPoint.pivot = new Vector2(0, 0);
    //     centerPoint.anchorMax = new Vector2(0, 0);
    //     centerPoint.anchorMin = new Vector2(0, 0);
    //     MeterBlockNum = new Vector2Int(
    //         Math.Min(meterBlockNum.x, (int)(ValidRT.x - ValidLB.x) / BlockPixelSize.x),
    //         Math.Min(meterBlockNum.y, (int)(ValidRT.y - ValidLB.y) / BlockPixelSize.y)
    //     );

    //     BlockIdx = new Vector2Int(
    //         Mathf.Clamp(blockIdx.x, 0, MeterBlockNum.x - 1),
    //         Mathf.Clamp(blockIdx.y, 0, MeterBlockNum.y - 1)
    //     );
    //     BlockNumSize = new Vector2Int(
    //         Mathf.Clamp(blockNumSize.x, 1, MeterBlockNum.x - BlockIdx.x),
    //         Mathf.Clamp(blockNumSize.y, 1, MeterBlockNum.y - BlockIdx.y)
    //     );
    //     centerPoint.anchoredPosition = new Vector2(
    //         BlockIdx.x * BlockPixelSize.x + ValidLB.x,
    //         BlockIdx.y * BlockPixelSize.y + ValidLB.y
    //     );

    //     centerPoint.sizeDelta = new Vector2(BlockPixelSize.x * BlockNumSize.x, BlockPixelSize.y * BlockNumSize.y);
    // }
    public void SetBlock_Center(Vector2Int blockNumSize, Vector2Int blockIdx)
    {
        //blockIdx在这里是中心点偏移
        //但多个Block组成的中心点依旧需要从blockIdx左下开始偏移
        centerPoint.pivot = new Vector2(0.5f, 0.5f);
        centerPoint.anchorMax = new Vector2(0.5f, 0.5f);
        centerPoint.anchorMin = new Vector2(0.5f, 0.5f);

        BlockIdx = new Vector2Int(
            Mathf.Clamp(blockIdx.x, -MeterBlockNum.x / 2, MeterBlockNum.x / 2),
            Mathf.Clamp(blockIdx.y, -MeterBlockNum.y / 2, MeterBlockNum.y / 2)
        );
        BlockNumSize = new Vector2Int(
            Mathf.Clamp(blockNumSize.x, 1, MeterBlockNum.x / 2 - BlockIdx.x + 1),
            Mathf.Clamp(blockNumSize.y, 1, MeterBlockNum.y / 2 - BlockIdx.y + 1)
        );
        centerPoint.anchoredPosition = new Vector2(
            (BlockIdx.x + (BlockNumSize.x - 1) / 2.0f) * BlockPixelSize.x,
            (BlockIdx.y + (BlockNumSize.y - 1) / 2.0f) * BlockPixelSize.y
        );
        centerPoint.sizeDelta = new Vector2(BlockPixelSize.x * BlockNumSize.x, BlockPixelSize.y * BlockNumSize.y);
    }

    public void ClampBlockIdx_Center(Vector2Int size, out Vector2Int min, out Vector2Int max)
    {
        min = -MeterBlockNum / 2;
        max = Vector2Int.Max(min, MeterBlockNum / 2 - size + Vector2Int.one);
    }

    public Vector2 GetFocusPointScreenPos()
    {
        return G.UI.UIWorldPos2ScreenPos(centerPoint.position);
    }
    public Vector3[] GetFocusPointScreenCorner()
    {
        var corners = new Vector3[4];
        centerPoint.GetWorldCorners(corners);
        for (int i = 0; i < corners.Length; i++)
        {
            corners[i] = G.UI.UIWorldPos2ScreenPos(corners[i]);
        }
        return corners;
    }

    public void SetBlockColor(Color color)
    {
        leftSide.GetComponent<UnityEngine.UI.Image>().color = color;
        rightSide.GetComponent<UnityEngine.UI.Image>().color = color;
        topSide.GetComponent<UnityEngine.UI.Image>().color = color;
        bottomSide.GetComponent<UnityEngine.UI.Image>().color = color;
    }

    public void InitFocusData(Vector2Int meteringSize, Vector2Int blockPixelSize)
    {
        MeterPixelSize = meteringSize;
        BlockPixelSize = blockPixelSize;
        MeterBlockNum = new Vector2Int(
            (MeterPixelSize.x - BlockPixelSize.x) / BlockPixelSize.x / 2 * 2 + 1,
            (MeterPixelSize.y - BlockPixelSize.y) / BlockPixelSize.y / 2 * 2 + 1
        );
    }
    public void UpdateCircleFocusImage()
    {
        foreach (var focusPosition in focusPositionRects)
        {
            focusPosition.UpdateCircleFocusImage();
        }
    }

    public IEnumerable<CameraTargetMono> GetAllValidFocusTarget()
    {
        foreach (var focusPosition in focusPositionRects)
        {
            var target = focusPosition.currentTarget;
            if (target != null)
            {
                yield return target;
            }
        }
    }

    public void UpdateFocusTargetData()
    {
        switch (G.player.takePhotoCameraComp.AutoFocusArea)
        {
            case Const.AutoFocusArea.SPOT:
                foreach (var focusPosition in focusPositionRects)
                {
                    focusPosition.ClearTarget();
                }
                break;
            case Const.AutoFocusArea.FULL:
            case Const.AutoFocusArea.MIDDLE:
                RectTransform rt = this["FocusTargetImageFakeOrigin"];
                RectTransform fatherRT = rt.parent.GetComponent<RectTransform>();
                var corners = GetFocusPointScreenCorner();
                var allCameraTargets = G.gameManager.GetCameraTargets();
                foreach(var target in allCameraTargets)
                {
                    target?.GenerateCheckData(G.player.RenderCamera, corners[0], corners[2], rt);
                }

                // 筛选出需要被标记的目标
                for (int i = 0; i < allCameraTargets.Count; i++)
                {
                    var target = allCameraTargets[i];
                    if (target == null || !target.isInView)
                    {
                        allCameraTargets.RemoveAt(i);
                        i--; // Adjust index after removal
                        continue; // Skip null or not in view targets
                    }
                }
                //遍历一遍已有的ui，更新之前已经选中的
                List<FocusTargetImageUI> invalidFocusPositionRects = new List<FocusTargetImageUI>(focusPositionRects.Count);
                for (int i = 0; i < focusPositionRects.Count; i++)
                {
                    int idx = focusPositionRects[i].TrySelectTarget_OldTarget(allCameraTargets);
                    if (idx >= 0)
                    {
                        allCameraTargets.RemoveAt(idx);
                    }else if (idx == -2)
                    {
                        //如果之前的动画还在播放，则当做被占用
                    }
                    else
                    {
                        invalidFocusPositionRects.Add(focusPositionRects[i]);
                    }
                }
                // 将之前没选中的、已经
                while (allCameraTargets.Count > 0)
                {
                    FocusTargetImageUI focusTargetImageUI;
                    if (allCameraTargets[allCameraTargets.Count - 1] == null)
                    {
                        allCameraTargets.RemoveAt(allCameraTargets.Count - 1);
                        continue; // Skip null targets
                    }

                    if (invalidFocusPositionRects.Count == 0)
                    {
                        //如果没有可用的focusPosition，则创建一个新的
                        focusTargetImageUI = Helpers.DynamicAttach<FocusTargetImageUI>(this, "FocusTargetImageUI", fatherRT);
                        if (focusTargetImageUI == null)
                        {
                            Debug.LogError("Failed to create FocusTargetImageUI");
                            break;
                        }
                        focusPositionRects.Add(focusTargetImageUI);
                    }
                    else
                    {
                        focusTargetImageUI = invalidFocusPositionRects[invalidFocusPositionRects.Count - 1];
                        invalidFocusPositionRects.RemoveAt(invalidFocusPositionRects.Count - 1);
                    }
                    focusTargetImageUI.SetTargetAndCheckData(allCameraTargets[allCameraTargets.Count - 1]);
                    allCameraTargets.RemoveAt(allCameraTargets.Count - 1);
                }

                foreach (var focusPosition in invalidFocusPositionRects)
                {
                    focusPosition.ClearTarget();
                }
                break;
            default:
                Debug.LogWarning($"Unsupported AutoFocusArea: {G.player.takePhotoCameraComp.AutoFocusArea}");
                break;
        }
    }

    // int UpdateDrawTarget(CameraTargetMono target)
    // {
    //     if (target == null) return -2;
    //     for (int i = 0; i < cameraTargetMonos.Length; i++)
    //     {
    //         if (cameraTargetMonos[i] == target)
    //         {
    //             return i;
    //         }
    //     }
    //     for (int i = 0; i < cameraTargetMonos.Length; i++)
    //     {
    //         if (cameraTargetMonos[i] == null)
    //         {
    //             cameraTargetMonos[i] = target;
    //             return i;
    //         }
    //     }
    //     return -1;
    // }
    // public void SetCircleFocusImage(int idx, Vector2 position, float radius, float borderSize)
    // {
    //     RectTransform focusImageRect = this[$"FocusPositionP{idx}"];
    //     focusImageRect.position = position;
    //     focusImageRect.sizeDelta = new Vector2(radius * 2, radius * 2);
    //     borderSize = Mathf.Min(borderSize, radius); //确保边框大小不为0
    //     focusImageMaterial.SetFloat("_InnerRadius", (radius - borderSize) / radius);
    // }

}
// #if UNITY_EDITOR
    //     void OnValidate()
    //     {
    //         UpdateSideWith(SideWidth);
    //         SetBlockCenter(BlockNumSize, BlockIdx);
    //         SetBlockColor(focusColor);
    //     }
    // #endif

