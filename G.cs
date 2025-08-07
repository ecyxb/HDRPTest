using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using UnityEngine.Events;
using System;
using UnityEngine.InputSystem;

public enum LocalizationLanguage
{
    English,
    Chinese,
    Japanese
}
public static class ObjectExtensions
{
    public static void Assert(this object obj, bool condition, string str)
    {
        if (!condition)
            throw new Exception(obj.ToString() + str);
    }
    public static void Print(this object obj, string str)
    {
        Debug.Log(str);
    }
    public static uint AddTimer(this object obj, float time, Action action, TimerUpdateMode mode = TimerUpdateMode.Update, int repeateCount = 1)
    {
        return G.Timer.RegisterTimer(time, action, mode, repeateCount);
    }
    public static void CancelTimer(this object obj, uint timerIdx)
    {
        G.Timer?.UnRegisterTimer(timerIdx);
    }
    public static int EnumToInt(this System.Enum e)
    {
        return e.GetHashCode();
    }
    
}

public sealed class G
{
    public static LocalizationLanguage CurrentLanguage { get; } = LocalizationLanguage.Chinese;
    public static PrimaryPlayer player { get; set; }
    public static GameManager gameManager { get; set; }

    private static InputManager m_inputMgr;
    public static InputManager InputMgr
    {
        get
        {
            if (gameManager == null)
                return null;
            if (m_inputMgr == null)
                m_inputMgr = gameManager.GetComponent<InputManager>();
            return m_inputMgr;
        }
    }
    public static Timer Timer
    {
        get
        {
            if (gameManager == null)
                return null;
            return gameManager.Timer;
        }
    }

    private static UIManager m_uiMgr;
    public static UIManager UI
    {
        get
        {
            if (gameManager == null)
                return null;
            if (m_uiMgr == null)
                m_uiMgr = gameManager.GetComponent<UIManager>();
            return m_uiMgr;
        }
    }

    private G()
    {

    }
    public static readonly G Instance = new G();


    public static EventObject.IEventProxy GetEventProxy<O>(O obj, bool ensureInit = false) where O : class
    {
        Type type = typeof(O);
        var attr = type.GetCustomAttribute<EventObject.EventObjectAttribute>();
        if (attr == null)
        {
            Debug.LogWarning($"Event Failed because the class of {type.FullName} dont has attribute EventObject.EventObjectAttribute");
            return null;
        }
        var field = attr.proxyName == null ? null : type.GetField(attr.proxyName, BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        if (field == null || !field.FieldType.IsAssignableFrom(typeof(EventObject.IEventProxy)))
        {
            Debug.LogWarning($"Event Failed because the class of {type.FullName} dont has protect/public field {attr.proxyName}");
            return null;
        }
        EventObject.IEventProxy proxy = (EventObject.IEventProxy)field.GetValue(obj);
        if (proxy == null)
        {
            if (!ensureInit)
            {
                return null;
            }
            else
            {
                proxy = new EventObject.EventProxy();
                field.SetValue(obj, proxy);
            }
        }
        return proxy;
    }

    public static EventObject.IEventProxy GetEventProxy(EventObject.IEventProxy obj, bool ensureInit = false)
    {
        return obj;
    }

    public static void RegisterEvent<O>(O obj, string eventName, Action func) where O : class
        => GetEventProxy(obj, ensureInit: true)?.RegisterEvent(eventName, func);
    public static void RegisterEvent<O, T>(O obj, string eventName, Action<T> func) where O : class
        => GetEventProxy(obj, ensureInit: true)?.RegisterEvent(eventName, func);
    public static void RegisterEvent<O, T1, T2>(O obj, string eventName, Action<T1, T2> func) where O : class
        => GetEventProxy(obj, ensureInit: true)?.RegisterEvent(eventName, func);
    public static void RegisterEvent<O, T1, T2, T3>(O obj, string eventName, Action<T1, T2, T3> func) where O : class
        => GetEventProxy(obj, ensureInit: true)?.RegisterEvent(eventName, func);
    public static void RegisterProp<O>(O obj, string eventName, Action func) where O : class
        => GetEventProxy(obj, ensureInit: true)?.RegisterEvent(eventName, func);



    public static void UnRegisterEvent<O>(O obj, string eventName, Action func) where O : class
        => GetEventProxy(obj, ensureInit: false)?.UnRegisterEvent(eventName, func);
    public static void UnRegisterEvent<O, T>(O obj, string eventName, Action<T> func) where O : class
        => GetEventProxy(obj, ensureInit: false)?.UnRegisterEvent(eventName, func);
    public static void UnRegisterEvent<O, T1, T2>(O obj, string eventName, Action<T1, T2> func) where O : class
        => GetEventProxy(obj, ensureInit: false)?.UnRegisterEvent(eventName, func);
    public static void UnRegisterEvent<O, T1, T2, T3>(O obj, string eventName, Action<T1, T2, T3> func) where O : class
        => GetEventProxy(obj, ensureInit: false)?.UnRegisterEvent(eventName, func);


    public static void InVokeEvent<O>(O obj, string eventName) where O : class
        => GetEventProxy(obj, ensureInit: false)?.InVokeEvent(eventName);
    public static void InVokeEvent<O, T>(O obj, string eventName, T data) where O : class
        => GetEventProxy(obj, ensureInit: false)?.InVokeEvent(eventName, data);
    public static void InVokeEvent<O, T1, T2>(O obj, string eventName, T1 data, T2 data2) where O : class
        => GetEventProxy(obj, ensureInit: false)?.InVokeEvent(eventName, data, data2);
    public static void InVokeEvent<O, T1, T2, T3>(O obj, string eventName, T1 data, T2 data2, T3 data3) where O : class
        => GetEventProxy(obj, ensureInit: false)?.InVokeEvent(eventName, data, data2, data3);

    public static uint RegisterTimer(float time, Action action, TimerUpdateMode mode = TimerUpdateMode.Update, int repeateCount = 1)
    {
        if (Timer == null)
        {
            return 0;
        }
        return Timer.RegisterTimer(time, action, mode, repeateCount);
    }
    public static void UnRegisterTimer(uint id)
    {
        if (Timer == null)
        {
            return;
        }
        Timer.UnRegisterTimer(id);
    }

    public static void RegisterInput(string actionName, InputEventType listenType, Func<InputActionArgs, bool> func, GameObject responseObject, Func<bool> responseFunc = null)
    {
        if (InputMgr == null)
        {
            return;
        }
        InputMgr.RegisterInput(actionName, listenType, func, responseObject, responseFunc: responseFunc);
    }
    public static void UnRegisterInput(string actionName, Func<InputActionArgs, bool> func)
    {
        if (InputMgr == null)
        {
            return;
        }
        InputMgr.UnRegisterInput(actionName, func);
    }

    public static Sprite LoadSprite(string path, int idx=0)
    {
        var sprites = Resources.LoadAll<Sprite>(path);
        return sprites != null && sprites.Length > idx ? sprites[idx] : null;
    }
}