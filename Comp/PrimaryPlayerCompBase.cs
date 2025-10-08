using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EventFramework;

public class PrimaryPlayerCompBase : CustomDictionary
{
    protected PrimaryPlayer m_player => (PrimaryPlayer)m_owner;
    protected MonoBehaviour m_owner;
    public virtual void CompStart()
    {

    }

    public virtual void CompDestroy()
    {
        // Override this method to handle component destruction
    }

    public PrimaryPlayerCompBase(MonoBehaviour owner, Dictionary<string, UnionInt64> data, bool fixedSlot = true) : base(data, fixedSlot)
    {
        m_owner = owner;
    }
    public PrimaryPlayerCompBase(MonoBehaviour owner, Dictionary<string, byte> slots, bool fixedSlot = true) : base(slots, fixedSlot)
    {
        m_owner = owner;
    }
}