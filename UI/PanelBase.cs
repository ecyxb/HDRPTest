using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public abstract class UICommon
{
    public RectTransform transform { get; protected set; }
    public GameObject gameObject => transform.gameObject;
    protected UICommon parent;

    protected static Dictionary<string, string> __shortcuts__ = null;
    protected virtual Dictionary<string, string> ShortCutsCache => null;
    protected virtual string[] SHORTCUT_OBJECTS => null;
    protected HashSet<UICommon> m_Children = null;
    private bool isRegistered = false;

    public UICommon()
    {
    }
    public virtual bool IsActive
    {
        get
        {
            return gameObject.activeInHierarchy && gameObject.activeSelf;
        }
    }
    public virtual void Destroy()
    {
        try
        {
            __unregister_input__();
            OnUnload();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error destroying UICommon: {e.Message}");
        }

        foreach (var child in m_Children ?? Enumerable.Empty<UICommon>())
        {
            child.Destroy();
        }
        transform = null;
        parent = null;
    }
    protected void __on_load__()
    {
        UICommon.InitUICommonShortcuts(this);
        OnLoad();
    }

    public virtual void Show()
    {
        gameObject.SetActive(true);
        __register_input__();
    }
    public virtual void Hide()
    {
        gameObject.SetActive(false);
        __unregister_input__();
    }


    private void __register_input__()
    {
        if (isRegistered)
        {
            return;
        }
        isRegistered = true;
        Register();

    }
    private void __unregister_input__()
    {
        if (!isRegistered)
        {
            return;
        }
        isRegistered = false;
        UnRegister();
    }

    public virtual RectTransform this[string uiName]
    {
        get
        {
            string fullname = ShortCutsCache?.GetValueOrDefault(uiName, null) ?? uiName;
            return transform.Find(fullname) as RectTransform;
        }
    }


    protected virtual void OnUnload() { }
    protected virtual void OnLoad() { }
    protected virtual void Register() { }
    protected virtual void UnRegister() { }


    protected T AttachUI<T>(string prefabPath, string attachPosName, AspectRatioFitter.AspectMode aspectFit = AspectRatioFitter.AspectMode.None) where T : UICommon, new()
    {
        var attachPos = this[attachPosName] ?? transform.Find(attachPosName) as RectTransform;
        if (attachPos == null)
        {
            Debug.LogError($"Attach position {attachPosName} not found in parent {gameObject.name}.");
            return null;
        }
        var uiInstance = AttachUI<T>(this, prefabPath, attachPos, aspectFit: aspectFit);
        uiInstance.gameObject.name = prefabPath;
        return uiInstance;
    }
    public static T AttachUI<T>(UICommon parent, string prefabPath, RectTransform attachPos, AspectRatioFitter.AspectMode aspectFit = AspectRatioFitter.AspectMode.None) where T : UICommon, new()
    {
        GameObject prefab = Helpers.FindUIPrefab(prefabPath);
        if (prefab == null)
        {
            return null;
        }
        return DynamicAttach<T>(parent, prefab, attachPos, aspectFit);
    }
    public static T DynamicAttach<T>(UICommon parent, GameObject prefabGameObject, RectTransform attachPos, AspectRatioFitter.AspectMode aspectFit = AspectRatioFitter.AspectMode.None) where T : UICommon, new()
    {
        T uiCommon = new T();
        RectTransform rectTransform = GameObject.Instantiate(prefabGameObject, attachPos)?.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError($"Failed to instantiate UI prefab {prefabGameObject.name} at attach position {attachPos.name}.");
            return null;
        }
        Helpers.EnsureAspectRatioFitter(rectTransform, aspectFit);
        uiCommon.parent = parent;
        parent.m_Children ??= new HashSet<UICommon>();
        parent.m_Children.Add(uiCommon);
        uiCommon.transform = rectTransform;
        uiCommon.__on_load__();
        return uiCommon;
    }
    protected static void InitUICommonShortcuts(UICommon uiCommon)
    {
        var shortcuts = uiCommon.SHORTCUT_OBJECTS?.Where(s => !string.IsNullOrEmpty(s)).ToArray();
        var cache = uiCommon.ShortCutsCache;
        if (cache != null && cache.Count == 0 && shortcuts != null && shortcuts.Length > 0)
        {
            Stack<RectTransform> stack = new Stack<RectTransform>();
            StringBuilder sb = new StringBuilder();
            stack.Push(uiCommon.transform);

            while (stack.Count > 0 && cache.Count < shortcuts.Length)
            {
                RectTransform rect = stack.Pop();
                if (rect == null)
                {
                    continue;
                }
                if (!string.IsNullOrEmpty(rect.name) && shortcuts.Contains(rect.name))
                    cache[rect.name] = Helpers.GetGameObjectPath(rect, uiCommon.transform, sb);
                foreach (RectTransform child in rect)
                {
                    stack.Push(child);
                }
            }
        }
    }
}
public class EmptyUICommon : UICommon { }

public class PanelBase : UICommon
{
    // Start is called before the first frame update
    public virtual CanvasType CanvasType { get; } = CanvasType.PanelBase;

    public static T InitPanel<T>(string panelName, Dictionary<int, Canvas> uiCanvases) where T : PanelBase, new()
    {
        GameObject prefab = Helpers.FindUIPrefab(panelName);
        if (prefab == null)
        {
            return null;
        }
        RectTransform rectTransform = GameObject.Instantiate(prefab)?.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError($"Failed to instantiate UI prefab {panelName}.");
            return null;
        }

        T panel = new T();
        var canvasType = panel.CanvasType;
        if (!uiCanvases.TryGetValue((int)canvasType, out var existingCanvas))
        {
            GameObject canvasObject = GameObject.Instantiate(Resources.Load<GameObject>("UIPrefabs/Canvas" + canvasType));
            existingCanvas = canvasObject.GetComponent<Canvas>();
            existingCanvas.sortingOrder = (int)panel.CanvasType;
            uiCanvases.Add((int)canvasType, existingCanvas);
        }
        rectTransform.SetParent(existingCanvas.transform, false);
        panel.transform = rectTransform;
        panel.parent = null; // Reset parent to null for new panels
        panel.__on_load__();
        return panel;
    }

    public override void Destroy()
    {
        RectTransform root = transform;
        base.Destroy();
        if (root != null)
        {
            GameObject.Destroy(root.gameObject);
        }
    }

}
