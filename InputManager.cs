using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public enum InputEventType
{
    // 会持续存储的类型
    Started = 1 << InputActionPhase.Started,
    Performed = 1 << InputActionPhase.Performed,
    Canceled = 1 << InputActionPhase.Canceled,
    Ended = 1 << (InputActionPhase.Canceled + 1),
    // 临时判断的类型
    Clicked = 1 << (InputActionPhase.Canceled + 2),

    Deactivated = Canceled | Ended,
    Actived = Started | Performed,

    ALL = Started | Performed | Canceled | Ended,
}

public class InputActionArgs
{
    public InputAction.CallbackContext context;
    public InputEventType eventType;
    public string name;

    public T ReadValue<T>() where T : struct
    {
        if (eventType == InputEventType.Ended)
        {
            return default;
        }
        return context.ReadValue<T>();
    }
    public static InputActionArgs EndInputArgs = new InputActionArgs
    {
        // 防止生成无用end args
        context = default,
        eventType = InputEventType.Ended,
        name = string.Empty
    };
    public static InputActionArgs GetEndInputArgs(string name)
    {
        EndInputArgs.name = name;
        return EndInputArgs;
    }
}


public class InputFuncData
{
    public bool actived;
    public bool startListend;
    public Func<InputActionArgs, bool> cb;
    public GameObject responseObject;
    public InputEventType listenType;
    public Func<bool> responseFunc;


    public Func<InputActionArgs, bool> GetCallback()
    {
        return cb;
    }

    public bool HandleEvent(InputActionArgs args)
    {
        if (cb == null)
        {
            return false;
        }
        if (args.eventType == InputEventType.Ended || !responseObject.activeInHierarchy || (responseFunc != null && !responseFunc.Invoke()))
        {
            // 事件已经在外围被吞掉而停止了。
            Deactive(args.name);
            return false;
        }
        
        bool listenClick = listenType.HasFlag(InputEventType.Clicked);
        bool isClicked = args.eventType == InputEventType.Canceled && startListend; //判断这次会不会触发click
        bool swallow = false;

        // 必须要接收started, performed则保持，cancel和ended则取消
        startListend = listenClick && (args.eventType == InputEventType.Started || (startListend && args.eventType == InputEventType.Performed));
        
        // 基础事件类型
        if (listenType.HasFlag(args.eventType))
        {
            // Started， Performed，Canceled
            swallow = cb.Invoke(args);
            actived = args.eventType != InputEventType.Canceled;
        }
        // 特殊事件类型，每个按钮自己处理
        if (isClicked && listenClick)
        {
            var tempArgs = new InputActionArgs
            {
                context = args.context,
                eventType = InputEventType.Clicked,
                name = args.name
            };
            cb.Invoke(tempArgs);
        }
        return swallow;
    }
    public void Deactive(string name)
    {
        if (actived && listenType.HasFlag(InputEventType.Ended))
        {
            cb.Invoke(InputActionArgs.GetEndInputArgs(name));
        }
        actived = false;
        startListend = false;
    }

    public InputFuncData(Func<InputActionArgs, bool> cb, GameObject responseObject, InputEventType listenType, Func<bool> responseFunc = null)
    {
        this.cb = cb;
        this.responseObject = responseObject;
        this.listenType = listenType;
        this.responseFunc = responseFunc;
        this.actived = false;
    }

}



public class InputManager : MonoBehaviour
{
    private PlayerInput m_playerInput;
    private Dictionary<string, List<InputFuncData>> m_actions = new Dictionary<string, List<InputFuncData>>();
    private Dictionary<string, InputActionArgs> m_lastInputArgs = new Dictionary<string, InputActionArgs>();
    public bool IsCurrentDeviceMouse
    {
        get
        {
            return m_playerInput.currentControlScheme == "KeyboardMouse";
        }
    }


    void Awake()
    {
        m_playerInput = gameObject.AddComponent<PlayerInput>();
        m_playerInput.actions = Resources.Load<InputActionAsset>("TakePhotoInputAction");
        m_playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
        m_playerInput.onActionTriggered += OnActionTriggered;
        ActiveActionMap("TakePhoto");
    }

    public void ActiveActionMap(string actionMapName)
    {
        if (string.IsNullOrEmpty(actionMapName))
        {
            Debug.LogWarning("ActiveActionMap failed: actionMapName is null or empty.");
            return;
        }
        m_playerInput.SwitchCurrentActionMap(actionMapName);

    }

    public void RegisterInput(string actionName, InputEventType listenType, Func<InputActionArgs, bool> func, GameObject responseObject, Func<bool> responseFunc = null)
    {
        // swallow all： 即使InputEventType没对应，也吞掉这个键位的事件
        if (string.IsNullOrEmpty(actionName) || func == null || responseObject == null)
        {
            Debug.LogWarning("RegisterInput failed: actionName, action or responseObject is null.");
            return;
        }

        if (!m_actions.TryGetValue(actionName, out List<InputFuncData> actions))
        {
            actions = new List<InputFuncData>();
            m_actions.Add(actionName, actions);
        }
        actions.Add(new InputFuncData(func, responseObject, listenType, responseFunc));
    }
    public void UnRegisterInput(string actionName, Func<InputActionArgs, bool> func)
    {
        if (string.IsNullOrEmpty(actionName) || func == null)
        {
            Debug.LogWarning("UnRegisterInput failed: actionName or action is null.");
            return;
        }
        if (!m_actions.TryGetValue(actionName, out List<InputFuncData> queue))
        {
            Debug.LogWarning($"UnRegisterInput failed: actionName {actionName} not found.");
            return;
        }
        if (queue == null || queue.Count == 0)
            return;

        for (int i = queue.Count - 1; i >= 0; i--)
        {
            var item = queue[i];
            if (item.GetCallback().Equals(func))
            {
                item.Deactive(actionName);
                queue.RemoveAt(i);
                break;
            }
        }
    }
    void HandleInputEvent(List<InputFuncData> argsList, InputActionArgs lastArgs)
    {
        if (argsList == null || argsList.Count == 0)
            return;

        InputEventType originEventType = lastArgs.eventType;
        if (originEventType == InputEventType.Ended)
        {
            // 如果已经处理结束了跳过
            return;
        }

        int idx = argsList.Count;
        while (idx > 0)
        {
            idx--;
            // 这次event被swallow了，之后的ui只能处理ended
            bool swallow = argsList[idx].HandleEvent(lastArgs);
            lastArgs.eventType = !swallow ? lastArgs.eventType : InputEventType.Ended;
        }
        // 如果是取消事件，设置为Ended（认为所有监听者在canceled/ended应该被处理完了）
        lastArgs.eventType = originEventType == InputEventType.Canceled ? InputEventType.Ended : lastArgs.eventType;

    }
    void HandleAllInputEvents()
    {
        foreach (var actions in m_actions)
        {
            HandleInputEvent(actions.Value, EnsureInputActionArgs(actions.Key));
        }
    }

    void Update()
    {
        HandleAllInputEvents();
    }

    public InputActionArgs EnsureInputActionArgs(string actionName)
    {
        if (!m_lastInputArgs.TryGetValue(actionName, out InputActionArgs args))
        {
            args = new InputActionArgs
            {
                context = default,
                eventType = InputEventType.Ended,
                name = actionName
            };
            m_lastInputArgs.Add(actionName, args);
        }
        return args;
    }

    public void OnActionTriggered(InputAction.CallbackContext context)
    {
        if (context.action == null || string.IsNullOrEmpty(context.action.name))
            return;
        if (m_lastInputArgs.TryGetValue(context.action.name, out InputActionArgs lastArgs))
        {
            lastArgs.context = context;
            lastArgs.eventType = (InputEventType)(1 << (int)context.phase);
        }
        else
        {
            lastArgs = new InputActionArgs
            {
                context = context,
                eventType = (InputEventType)(1 << (int)context.phase),
                name = context.action.name
            };
            m_lastInputArgs.Add(context.action.name, lastArgs);
        }

        // Start事件需要立即触发，防止在同一帧内被perofrmed干掉
        if (lastArgs.eventType == InputEventType.Started)
        {
            HandleInputEvent(m_actions.GetValueOrDefault(context.action.name), lastArgs);
        }
    }


    public InputEventType GetCurrentEventType(string actionName)
    {
        if (m_lastInputArgs.TryGetValue(actionName, out InputActionArgs lastArgs))
        {
            return lastArgs.eventType;
        }
        return InputEventType.Ended;
    }
}

