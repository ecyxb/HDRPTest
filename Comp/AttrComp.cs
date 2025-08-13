using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EventFramework;

public class AttrComp : EventCompBase
{
    public AttrComp(PrimaryPlayer player) : base(player, new Dictionary<string, UnionInt64>(), fixedSlot: false)
    {
        var baseAttr = DataLoader.Instance.playerBasePropertyMap[Const.StateConst.NONE];
        foreach (var attr in baseAttr.attrMap)
        {
            this[attr.Key] = attr.Value;
        }
    }

}
