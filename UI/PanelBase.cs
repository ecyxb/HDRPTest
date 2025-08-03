using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;

public struct BindUICommonArgs
{
    public UnityEngine.UI.AspectRatioFitter.AspectMode aspectFit;
    public string uiName;
    public string attachPosName;
    public string objName; // 如果不填，则使用uiName
    public BindUICommonArgs(string uiName, string attachPosName, string objName = null, UnityEngine.UI.AspectRatioFitter.AspectMode aspectFit = UnityEngine.UI.AspectRatioFitter.AspectMode.None)
    {
        this.uiName = uiName;
        this.attachPosName = attachPosName;
        this.objName = objName;
        this.aspectFit = aspectFit;
    }
    public string ObjectName
    {
        get => string.IsNullOrEmpty(objName) ? uiName : objName;
    }

}
[Serializable]
public struct UICommonSerializeData
{
    public string objName;
    public string objPath;
}
public class UICommon : MonoBehaviour, ISerializationCallbackReceiver
{
    public virtual bool IsActive
    {
        get
        {
            return gameObject.activeInHierarchy && gameObject.activeSelf;
        }
    }
    void OnDestroy()
    {
        __unregister_input__();
        OnUnload();
    }
    void Awake()
    {
        var sobjs = SHORTCUT_OBJECTS;
        if (sobjs != null && sobjs.Length > 0)
        {
            var shortcuts = new HashSet<string>(sobjs);
            Stack<RectTransform> stack = new Stack<RectTransform>();
            __objshortcuts__ = new Dictionary<string, RectTransform>(shortcuts.Count);
            foreach (var child in transform)
            {
                stack.Push(child as RectTransform);
            }

            while (stack.Count > 0 && __objshortcuts__.Count < shortcuts.Count)
            {
                RectTransform rect = stack.Pop();
                if (rect == null)
                {
                    continue;
                }
                if (!string.IsNullOrEmpty(rect.name) && shortcuts.Contains(rect.name))
                    __objshortcuts__[rect.name] = rect;

                if (!rect.GetComponent<UICommon>()) //子对象的UICOMOON自己统计
                {
                    foreach (RectTransform child in rect)
                    {
                        stack.Push(child);
                    }
                }
            }
        }
        OnInit();
    }
    public void __on_load__()
    {
        OnLoad();
    }

    [SerializeField]
    private List<UICommonSerializeData> __serializeData__ = null;
    private Dictionary<string, UICommon> __originals__ = null;
    protected Dictionary<string, RectTransform> __objshortcuts__ = null;
    private bool isRegistered = false;

    protected UICommon AttachUI(BindUICommonArgs args)
    {
        __originals__ = __originals__ ?? new Dictionary<string, UICommon>();
        var attachPos = this[args.attachPosName] ?? this.transform.Find(args.attachPosName) as RectTransform;
        if (attachPos == null)
        {
            Debug.LogError($"Attach position {args.attachPosName} not found in parent {this.name}.");
            return null;
        }
        var uiInstance = Helpers.DynamicAttach(this, args.uiName, attachPos, aspectFit: args.aspectFit);
        uiInstance.name = args.ObjectName;
        if (uiInstance != null)
        {
            __originals__[args.ObjectName] = uiInstance;
        }
        return uiInstance;
    }

    public void Show()
    {
        gameObject.SetActive(true);
        __register_input__();
    }
    public void Hide()
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
    public void __auto_attach__()
    {
        var bindClips = UICOMMON_BIND;
        if (bindClips != null && bindClips.Count > 0)
        {
            foreach (var clip in bindClips)
            {
                AttachUI(clip);
            }
        }
        // var originals = UICOMMON_PREFABS;
        // if (originals != null && originals.Count > 0)
        // {
        //     if (__originals__ == null)
        //     {
        //         __originals__ = new Dictionary<string, string>();
        //     }

        //     foreach (var original in originals)
        //     {
        //         var uiPrefab = Helpers.FindUIPrefab(original.Item1);
        //         var originalName = string.IsNullOrEmpty(original.Item2) ? original.Item1 : original.Item2;
        //         if (uiPrefab != null)
        //         {
        //             __originals__[originalName] = uiPrefab.GetComponent<UICommon>();
        //         }
        //     }
        // }

    }

    public virtual RectTransform this[string uiName]
    {
        get
        {
            return this.__objshortcuts__?.GetValueOrDefault(uiName, null);
        }
    }

    public virtual T GetUICommon<T>(string uiName) where T : UICommon
    {
        if (__originals__ != null && __originals__.ContainsKey(uiName))
        {
            return __originals__[uiName] as T;
        }
        return null;
    }

    protected virtual string[] SHORTCUT_OBJECTS
    {
        get
        {
            return null;
        }
    }

    protected virtual List<BindUICommonArgs> UICOMMON_BIND
    {
        get
        {
            return null;
        }
    }


    protected virtual void OnUnload()
    {

    }
    protected virtual void OnLoad()
    {

    }
    protected virtual void OnInit()
    {
        
    }

    protected virtual void Register()
    {
        
    }
    protected virtual void UnRegister()
    {
        
    }

    public void OnBeforeSerialize()
    {
        if (__originals__ == null || __originals__.Count == 0)
        {
            return; // Nothing to serialize
        }
        if (__serializeData__ == null)
        {
            __serializeData__ = new List<UICommonSerializeData>();
        }
        else
        {
            __serializeData__.Clear();
        }
        foreach (var original in __originals__)
        {
            string path = Helpers.GetPathBetweenFatherAndChild(this.gameObject, original.Value.gameObject);
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }
            __serializeData__.Add(new UICommonSerializeData
            {
                objName = original.Key,
                objPath = path
            });
        }
    }

    public void OnAfterDeserialize()
    {
        if (__serializeData__ == null || __serializeData__.Count == 0)
        {
            __serializeData__ = null;
            return;
        }
        if (__originals__ == null)
        {
            __originals__ = new Dictionary<string, UICommon>();
        }

        foreach (var data in __serializeData__)
        {
            var attachUI = this.transform.Find(data.objPath)?.GetComponent<UICommon>();
            if (attachUI != null)
            {
                __originals__[data.objName] = attachUI;
            }
        }
        __serializeData__ = null;
    }
}

public class PanelBase : UICommon
{
    // Start is called before the first frame update
    public virtual CanvasType CanvasType { get; } = CanvasType.PanelBase;

}
