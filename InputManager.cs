using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public enum InputEventType
{
    Started = 1 << InputActionPhase.Started,
    Performed = 1 << InputActionPhase.Performed,
    Canceled = 1 << InputActionPhase.Canceled,

    ALL = Started | Performed | Canceled,

    SWALLOW_STARTED = 1 << (InputActionPhase.Started + 3),
    SWALLOW_PERFORMED = 1 << (InputActionPhase.Performed + 3),
    SWALLOW_CANCELED = 1 << (InputActionPhase.Canceled + 3),
    SWALLOW_ALL = SWALLOW_STARTED | SWALLOW_PERFORMED | SWALLOW_CANCELED
}

public interface IInputEventData
{
    public Func<InputAction.CallbackContext, bool> GetCallback();
    public bool IsResponsable();
    public bool IsSwallowType(InputActionPhase phase);

    public bool IsEventType(InputActionPhase phase);



}

public struct InputEventObject : IInputEventData
{
    public Func<InputAction.CallbackContext, bool> cb;
    public GameObject responseObject;
    public InputEventType eventType;
    public Func<bool> responseFunc;

    public Func<InputAction.CallbackContext, bool> GetCallback()
    {
        return cb;
    }

    public bool IsResponsable()
    {
        return responseObject.activeInHierarchy && (responseFunc == null || responseFunc.Invoke());
    }

    public bool IsSwallowType(InputActionPhase phase)
    {
        return eventType.HasFlag((InputEventType)(1 << ((int)phase + 3)));
    }

    public bool IsEventType(InputActionPhase phase)
    {
        return eventType.HasFlag((InputEventType)(1 << (int)phase));
    }
    public InputEventObject(Func<InputAction.CallbackContext, bool> cb, GameObject responseObject, InputEventType eventType, Func<bool> responseFunc = null)
    {
        this.cb = cb;
        this.responseObject = responseObject;
        this.eventType = eventType;
        this.responseFunc = responseFunc;
    }

}



public class InputManager : MonoBehaviour
{
    private PlayerInput m_playerInput;
    private Dictionary<string, List<IInputEventData>> m_actions = new Dictionary<string, List<IInputEventData>>();
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

    public void RegisterInput(string actionName, InputEventType eventType, Func<InputAction.CallbackContext, bool> func, GameObject responseObject, Func<bool> responseFunc = null)
    {
        // swallow all： 即使InputEventType没对应，也吞掉这个键位的事件
        if (string.IsNullOrEmpty(actionName) || func == null || responseObject == null)
        {
            Debug.LogWarning("RegisterInput failed: actionName, action or responseObject is null.");
            return;
        }

        if (!m_actions.TryGetValue(actionName, out List<IInputEventData> actions))
        {
            actions = new List<IInputEventData>();
            m_actions.Add(actionName, actions);
        }
        actions.Add(new InputEventObject(func, responseObject, eventType, responseFunc));
    }
    public void UnRegisterInput(string actionName, Func<InputAction.CallbackContext, bool> func)
    {
        if (string.IsNullOrEmpty(actionName) || func == null)
        {
            Debug.LogWarning("UnRegisterInput failed: actionName or action is null.");
            return;
        }
        if (!m_actions.TryGetValue(actionName, out List<IInputEventData> queue))
        {
            Debug.LogWarning($"UnRegisterInput failed: actionName {actionName} not found.");
            return;
        }
        if (queue == null || queue.Count == 0)
            return;

        for (int i = queue.Count - 1; i >= 0; i--)
        {
            if (queue[i].GetCallback().Equals(func))
            {
                queue.RemoveAt(i);
                break;
            }
        }
    }
    public void OnActionTriggered(InputAction.CallbackContext context)
    {
        if (context.action == null || string.IsNullOrEmpty(context.action.name))
            return;
        if (!m_actions.TryGetValue(context.action.name, out List<IInputEventData> queue))
        {
            return;
        }
        if (queue == null)
            return;
        int idx = queue.Count;

        while (idx > 0)
        {
            idx--;
            var eventData = queue[idx];
            if (eventData.IsResponsable() == false)
            {
                continue;
            }
            bool swallowThisType = eventData.IsSwallowType(context.action.phase);
            bool isThisType = eventData.IsEventType(context.action.phase);
            if (!isThisType)
            {
                if (swallowThisType)
                {
                    break;
                }
                continue;
            }
            bool swallow = eventData.GetCallback().Invoke(context) || swallowThisType;
            if (swallow)
            {
                break;
            }
        }
    }
}

