using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 需要是结构体，因为别的地方会临时存储，我们不希望因为一个地方被修改了而产生修改
public struct CameraTargetCheckData
{
    public Vector2 centerOffset; // Offset from the camera target's position to the center point
    public float z;
    public bool isInView;
    public Const.TargetDrawUIType targetDrawUIType;
    public float[] targetDrawUIParams; // Parameters for drawing the target UI, e.g., radius for circle
    public Vector3 screenPos; // Screen position of the target

    public float GetRadius()
    {
        return targetDrawUIParams[2];
    }
    public Vector2 GetPosition()
    {
        return new Vector2(targetDrawUIParams[0], targetDrawUIParams[1]);
    }
    public float GetBorderSize()
    {
        return Mathf.Min(GetRadius(), targetDrawUIParams[3]);
    }

}

public class CameraTargetMono : MonoBehaviour
{
    [SerializeField]
    private Vector3 targetPositionOffset = new Vector3(0, 0f, 0); // Offset from the camera target's position to the center point
    [SerializeField]
    private Vector3 boxSize = new Vector3(1, 1, 1); // Size of the bounding box for the camera target
    public Vector3 BoxSize => new Vector3(boxSize.x * transform.localScale.x, boxSize.y * transform.localScale.y, boxSize.z * transform.localScale.z);
    [SerializeField]
    private bool canBeLocked = false;

    [SerializeField]
    [Space(10)]
    private List<Vector3> canFocusPositions = new List<Vector3>(); // Positions where the camera can focus

    public CameraTargetCheckData latestCheckData { get; private set; }
    public bool isInView => latestCheckData.isInView;

    public bool CanBeLocked => canBeLocked;
    // Start is called before the first frame update
    protected virtual void Start()
    {
        G.gameManager?.RegisterCameraTarget(this);

    }

    public Vector3 GetCenterPosition()
    {
        // Return the center position of the camera target
        return transform.position + transform.TransformVector(targetPositionOffset);
    }

    protected virtual void OnDrawGizmosSelected()
    {
        // Gizmos.color = Color.yellow;
        // Gizmos.DrawWireSphere(GetCenterPosition(), 0.25f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(GetCenterPosition(), BoxSize);
        Gizmos.color = Color.yellow;
        foreach (var pos in canFocusPositions)
        {
            Gizmos.DrawSphere(transform.TransformVector(pos) + GetCenterPosition(), 0.1f);
        }
    }

    public Vector2 GetCenterOffset(float radialBlurScale = 1f)
    {
        return latestCheckData.centerOffset;
    }

    public void GenerateCheckData(Camera camera, Vector2 LBClamp, Vector2 RTClamp, RectTransform rt, float minZ = 0.1f, float maxZ = 200f)
    {
        var res = new CameraTargetCheckData();
        res.screenPos = Helpers.WorldPos2ScreenPos(GetCenterPosition(), camera);
        if (res.screenPos.z <= 0 || res.screenPos.z > maxZ || res.screenPos.z < minZ)
        {
            res.isInView = false; // Object is behind the camera or too far away
        }
        else if (!(res.screenPos.x >= LBClamp.x && res.screenPos.x <= RTClamp.x && res.screenPos.y >= LBClamp.y && res.screenPos.y <= RTClamp.y))
        {
            res.isInView = false;
        }
        else
        {
            res.centerOffset = new Vector2(
                res.screenPos.x - (LBClamp.x + RTClamp.x) / 2,
                res.screenPos.y - (LBClamp.y + RTClamp.y) / 2
            );
            res.z = res.screenPos.z;
            res.targetDrawUIType = Const.TargetDrawUIType.CIRCLE;
            var worldPos = G.UI.ScreenPos2UIWorldPos(res.screenPos, rt);
            var sidePos = G.UI.ScreenPos2UIWorldPos(Helpers.WorldPos2ScreenPos(GetCenterPosition() + new Vector3(0, BoxSize.y * 0.5f, 0), camera), rt);
            //坐标、半径、粗细
            float radius = Vector2.Distance(worldPos, sidePos);
            if (radius * 2 >= RTClamp.x - LBClamp.x || radius * 2 >= RTClamp.y - LBClamp.y)
            {
                res.isInView = false; // If radius is too large, the target is not visible
            }
            else
            {
                res.targetDrawUIParams = new float[] { worldPos.x, worldPos.y, radius, 3 };
                res.isInView = true;
            }
        }
        latestCheckData = res;
    }

    public Vector3 GetCenterScreenPosition()
    {
        return latestCheckData.screenPos;
    }

    public List<Vector3> GetCanFocusPositions()
    {
        var p = new List<Vector3>(canFocusPositions.Count + 1);
        var center = GetCenterPosition();
        p.Add(center);
        foreach (var pos in canFocusPositions)
        {
            p.Add(transform.TransformVector(pos) + center);
        }
        return p;
    }
}
