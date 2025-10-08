using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EventFramework;

public class SfxComp : PrimaryPlayerCompBase
{
    public Vector3 radialBlurCenter { get; private set; } = Vector3.zero;
    public float radialBlurFactor { get{ GetValue("radialBlurFactor", out float blurAmount); return blurAmount; } }
    protected static Dictionary<string, UnionInt64> slotMap = new Dictionary<string, UnionInt64>
    {
        { "radialBlurFactor", 1f },
    };
    public SfxComp(PrimaryPlayer player) : base(player, slotMap, true)
    {

    }
    public override void CompStart()
    {
        base.CompStart();
        m_player.playerMovementComp.RegisterEvent<CameraTargetMono>("OnFocusTargetChanged", OnFocusTargetChanged);
    }

    public virtual void RefreshSfxParams_Update()
    {
        var targetMono = m_player.playerMovementComp.focusTargetMono;
        if (targetMono)
        {
            radialBlurCenter = Helpers.ScreenPos2ViewPos(targetMono.GetCenterScreenPosition(), m_player.RenderCamera);
            G.gameManager.SetRadialBlurParams("RenderVolume", 0.2f, radialBlurCenter, 0.25f, 0.25f, 6);
        }
    }


    public Vector2 GetAfterRadialBlurScreenPosition(Vector3 position)
    {
        var targetMono = m_player.playerMovementComp.focusTargetMono;
        if (!targetMono)
        {
            return position;
        }
        Vector3 center = m_player.playerMovementComp.focusTargetMono.GetCenterScreenPosition();
        return (position - center) * radialBlurFactor + center;
    }
    private void OnFocusTargetChanged(CameraTargetMono targetMono)
    {
        // 在这里处理焦点目标变化的逻辑
        if (targetMono != null)
        {
            // 如果目标不为空，启用径向模糊
            SetValue("radialBlurFactor", 1.2f);
            radialBlurCenter = Helpers.ScreenPos2ViewPos(targetMono.GetCenterScreenPosition(), m_player.RenderCamera);
            G.gameManager.SetRadialBlurParams("RenderVolume", 0.2f, radialBlurCenter, 0.25f, 0.25f, 6);
        }
        else
        {
            // 如果目标为空，禁用径向模糊
            SetValue("radialBlurFactor", 1.0f);
            G.gameManager.SetRadialBlurParams("RenderVolume", 0f, Vector2.zero, 0.25f, 0.25f, 6);
        }
    }
}
