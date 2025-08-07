using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum CanvasType
{
    PanelScreenOutput = -1,
    PanelBase = 1,
    PanelMain = 2,

}
public class UIManager : MonoBehaviour
{
    // Start is called before the first frame update

    private Dictionary<int, Canvas> uiCanvases = new Dictionary<int, Canvas>();
    private Dictionary<string, PanelBase> __panels__ = new Dictionary<string, PanelBase>();
    private Action<float> __late_updates__;

    public PanelBase this[string uiName]
    {
        get
        {
            return this.__panels__?.GetValueOrDefault(uiName, null);
        }
    }
    public T EnsurePanel<T>(string panelName) where T : PanelBase, new()
    {
        PanelBase old_pnl = this[panelName];
        if(old_pnl != null)
        {
            return old_pnl as T;
        }
        var pnl = PanelBase.InitPanel<T>(panelName, uiCanvases);
        if (pnl != null)
        {
            pnl.transform.name = panelName;
            __panels__[panelName] = pnl;
            pnl.Show();
        }
        return pnl;
    }
    public void DestroyPanel(string panelName)
    {
        if (__panels__.TryGetValue(panelName, out var panel))
        {
            panel.Destroy();
            __panels__.Remove(panelName);
        }
    }

    public Vector2 UIWorldPos2ScreenPos(Vector2 position)
    {
        return RectTransformUtility.WorldToScreenPoint(null, position);
    }
    public Vector3 ScreenPos2UIWorldPos(Vector2 position, RectTransform rectTransform)
    {
        return RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, position, null, out Vector3 worldPos) ? worldPos : Vector3.zero;
    }
    void Awake()
    {
        __late_updates__ = new Action<float>(SelfLateUpdate);
    }
    void OnDestroy()
    {
        foreach (var panel in __panels__)
        {
            if (panel.Value != null)
            {
                panel.Value.Destroy();
            }
        }
        __panels__.Clear();
    }

    void LateUpdate()
    {
        __late_updates__?.Invoke(Time.deltaTime);
    }

    void SelfLateUpdate(float dt)
    {
        
    }
    public void RegisterLateUpdate(Action<float> action)
    {
        if (action == null) return;
        __late_updates__ += action;
    }
    public void UnregisterLateUpdate(Action<float> action)
    {
        if (action == null) return;
        __late_updates__ -= action;
    }
}
