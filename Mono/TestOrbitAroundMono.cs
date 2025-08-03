using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestOrbitAroundMono : CameraTargetMono
{
[Header("Orbit Settings")]
    [Tooltip("The target to orbit around")]
    public Transform target;  // 要围绕的目标对象
    
    [Tooltip("Height above the target")]
    public float height = 2f;  // 目标上方的固定高度
    
    [Tooltip("Orbit radius")]
    public float radius = 3f;  // 轨道半径
    
    [Tooltip("Orbit speed in degrees per second")]
    public float speed = 30f;  // 旋转速度（度/秒）
    
    [Tooltip("Should the orbiting object face the target?")]
    public bool faceTarget = true;  // 是否始终面向目标
    
    [Header("Debug")]
    public bool drawOrbitGizmo = true;  // 是否在编辑器中绘制轨道
    
    private float currentAngle = 0f;  // 当前角度

    protected override void Start()
    {
        base.Start();
        currentAngle = Random.Range(0f, 360f);  // 随机初始角度
    }

    void FixedUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning("No target assigned for OrbitAroundTarget script!");
            return;
        }
        
        // 更新角度（基于时间）
        currentAngle += speed * Time.fixedDeltaTime;
        currentAngle %= 360f;  // 保持在0-360度范围内
        
        // 计算新位置
        Vector3 orbitPosition = CalculateOrbitPosition(currentAngle);
        
        // 设置位置
        transform.position = orbitPosition;
        
        // 如果需要，面向目标
        if (faceTarget)
        {
            transform.LookAt(target.position);
        }
    }
    
    // 计算轨道位置
    private Vector3 CalculateOrbitPosition(float angle)
    {
        // 将角度转换为弧度
        float angleInRadians = angle * Mathf.Deg2Rad;
        
        // 计算x和z坐标（水平面上的圆周运动）
        float x = Mathf.Cos(angleInRadians) * radius;
        float z = Mathf.Sin(angleInRadians) * radius;
        
        // 获取目标位置并添加上方偏移
        Vector3 targetPosition = target.position + Vector3.up * height;
        
        // 返回最终位置
        return targetPosition + new Vector3(x, 0f, z);
    }
    
    // 在编辑器中绘制轨道（仅在选中时显示）
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        if (drawOrbitGizmo && target != null)
        {
            Gizmos.color = Color.cyan;

            // 绘制轨道圆
            Vector3 center = target.position + Vector3.up * height;
            Vector3 prevPoint = center + new Vector3(radius, 0f, 0f);

            for (int i = 1; i <= 36; i++)
            {
                float angle = i * 10f;
                float angleInRadians = angle * Mathf.Deg2Rad;
                Vector3 nextPoint = center + new Vector3(
                    Mathf.Cos(angleInRadians) * radius,
                    0f,
                    Mathf.Sin(angleInRadians) * radius
                );

                Gizmos.DrawLine(prevPoint, nextPoint);
                prevPoint = nextPoint;
            }

            // 绘制到目标的线
            Gizmos.DrawLine(center, transform.position);
        }
    }
}
