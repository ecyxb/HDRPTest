
using System.Collections.Generic;
using UnityEngine;
using EventObject;
using CData;

public class StateComp : EventCompBase
{
    private bool m_Modifying = false;

    private bool[] m_states;
    private List<Const.StateConst> m_stateCache = new List<Const.StateConst>(8);

    // private Const.StateOp[] m_ruleCache;
    protected static Dictionary<string, UnionInt64> slotMap = new Dictionary<string, UnionInt64>
    {


    };
    // Start is called before the first frame update
    public StateComp(PrimaryPlayer player) : base(player, slotMap)
    {
        m_states = new bool[DataLoader.Instance.stateDataMap.Count];
    }
    public bool AddState(Const.StateConst newState)
    {
        if (m_states[(int)newState])
        {
            return false;
        }
        if (m_Modifying)
        {
            Debug.LogWarning("StateComp is modifying, cannot add state now.");
            return false;
        }
        m_Modifying = true;

        bool addNew = true;
        foreach (var state in m_stateCache)
        {
            if (DataLoader.Instance.stateDataMap[(int)state].rule[(int)newState].HasFlag(Const.StateOp.DISCARD_NEW))
            {
                addNew = false;
                break;
            }
        }
        for (int i = 0; i < m_stateCache.Count; i++)
        {
            var r = DataLoader.Instance.stateDataMap[(int)m_stateCache[i]].rule[(int)newState];
            if (r == Const.StateOp.DISCARD_AND_DISCARD || (r == Const.StateOp.DISCARD_AND_GET && addNew))
            {
                Const.StateConst stateToRemove = m_stateCache[i];
                m_stateCache.RemoveAt(i);
                m_states[(int)stateToRemove] = false;
                i--;
                this.InVokeEvent("OnStateRemove", stateToRemove);
                this.InVokeEvent($"OnState{stateToRemove}Remove");
            }
        }
        if (addNew)
        {
            m_stateCache.Add(newState);
            m_states[(int)newState] = true;
            this.InVokeEvent("OnStateAdd", newState);
            this.InVokeEvent($"OnState{newState}Add");
        }
        m_Modifying = false;
        // UpdatePlayerBasePropertyID();
        return addNew;
    }

    public bool RemoveState(Const.StateConst state)
    {
        if (!m_states[(int)state])
        {
            return false; // 状态不存在
        }
        if (m_Modifying)
        {
            Debug.LogWarning("StateComp is modifying, cannot remove state now.");
            return false;
        }
        m_Modifying = true;

        m_states[(int)state] = false;
        m_stateCache.Remove(state);
        this.InVokeEvent("OnStateRemove", state);
        this.InVokeEvent($"OnState{state}Remove");
        m_Modifying = false;
        // UpdatePlayerBasePropertyID();
        return true;
    }
    public bool HasState(Const.StateConst state)
    {
        return m_states[(int)state];
    }
    public bool CanAddState(Const.StateConst newState)
    {
        if (m_states[(int)newState])
        {
            return false; // 已经存在该状态
        }
        if (m_Modifying)
        {
            return false;
        }
        foreach (var state in m_stateCache)
        {
            if (DataLoader.Instance.stateDataMap[(int)state].rule[(int)newState].HasFlag(Const.StateOp.DISCARD_NEW))
            {
                return false; // 有状态规则禁止添加新状态
            }
        }
        return true; // 可以添加该状态
    }

}
