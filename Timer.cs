using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TimerUpdateMode
{
    Update = 0,
    FixedUpdate = 1,
    LateUpdate = 2,

    UnScaledUpdate = 3,
    UnScaledFixedUpdate = 4,
    UnScaledLateUpdate = 5,
    
    MAX = 6
}

public class Timer
{
    class TimerAction
    {
        public float deltaTime;
        public int repeateCount;
        public Action action;
        // public TimerUpdateMode mode;



        public TimerAction(float deltaTime, Action action, TimerUpdateMode mode, int repeateCount)
        {
            this.deltaTime = deltaTime;
            this.repeateCount = repeateCount;
            this.action = action;
            // this.mode = mode;
        }
    }

    private uint m_nextId = 1;
    private List<(float, uint)>[] m_timer2Id = null;
    private Dictionary<uint, TimerAction> m_Action = new Dictionary<uint, TimerAction>();

    public uint RegisterTimer(float time, Action action, TimerUpdateMode mode = TimerUpdateMode.Update, int repeateCount = 1)
    {
        TimerAction timerAction = new TimerAction(time, action, mode, repeateCount);
        uint id = m_nextId++;
        m_Action.Add(id, timerAction);
        m_timer2Id[(int)mode].Add((mode <= TimerUpdateMode.LateUpdate ? Time.time + time : Time.unscaledTime + time, id));
        return id;
    }
    public void UnRegisterTimer(uint id)
    {
        if (m_Action.ContainsKey(id))
        {
            m_Action.Remove(id);
        }
    }


    void UpdateTimer2ID(List<(float, uint)> data, float time)
    {
        while (data.Count > 0 && data[0].Item1 <= time)
        {
            if (data[0].Item1 > time)
            {
                break; // If the next timer is in the future, exit
            }
            uint id = data[0].Item2;
            data.RemoveAt(0);
            if (m_Action.TryGetValue(id, out TimerAction timerAction) && timerAction != null)
            {
                timerAction.action.Invoke();
                if (!m_Action.ContainsKey(id))
                {
                    continue; // If the action was removed during invocation, exit early
                }
                if (timerAction.repeateCount > 1 || timerAction.repeateCount < 0)
                {
                    timerAction.repeateCount = timerAction.repeateCount > 0 ? timerAction.repeateCount - 1 : timerAction.repeateCount;
                    data.Add((time + timerAction.deltaTime, id)); // Re-register for next time
                }
                else
                {
                    m_Action.Remove(id);
                }
            }
        }
    }

    public void Awake()
    {

        m_timer2Id = new List<(float, uint)>[(int)TimerUpdateMode.MAX];
        for (int i = 0; i < m_timer2Id.Length; i++)
        {
            m_timer2Id[i] = new List<(float, uint)>();
        }
    }

    public void Update()
    {
        UpdateTimer2ID(m_timer2Id[(int)TimerUpdateMode.Update], Time.time);
        UpdateTimer2ID(m_timer2Id[(int)TimerUpdateMode.UnScaledUpdate], Time.unscaledTime);
    }
    public void FixedUpdate()
    {
        for (int i = 0; i < m_timer2Id.Length; i++)
        {
            m_timer2Id[i].Sort((a, b) => a.Item1.CompareTo(b.Item1));
        }
        UpdateTimer2ID(m_timer2Id[(int)TimerUpdateMode.FixedUpdate], Time.time);
        UpdateTimer2ID(m_timer2Id[(int)TimerUpdateMode.UnScaledFixedUpdate], Time.unscaledTime);
    }
    public void LateUpdate()
    {
        UpdateTimer2ID(m_timer2Id[(int)TimerUpdateMode.LateUpdate], Time.time);
        UpdateTimer2ID(m_timer2Id[(int)TimerUpdateMode.UnScaledLateUpdate], Time.unscaledTime);
    }

}
