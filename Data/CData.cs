using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CData
{
    // [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    // public class CDataMainKeyAttribute : Attribute
    // {
    //     public CDataMainKeyAttribute() { }
    // }

    [AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public class CDataConstructAttribute : Attribute
    {
        public string FuncName { get; private set; }
        public CDataConstructAttribute(string funcname) { FuncName = funcname; }

    }

    public interface CDataBase<Tkey>
    {
        public Tkey GetKey();
    }



    [CDataConstruct("CreateTranslateData")]
    public struct TranslateData : CDataBase<int>
    {
        public int id;
        public string cn;
        public string en;
        public string jp;

        public int GetKey()
        {
            return id;
        }
        public static TranslateData CreateTranslateData(Dictionary<string, int> HeaderNameToPos, TypedRow row)
        {
            return new TranslateData
            {
                id = row.Values[HeaderNameToPos["id"]].ToInteger(),
                cn = row.Values[HeaderNameToPos["cn"]].ToString(),
                en = row.Values[HeaderNameToPos["en"]].ToString(),
                jp = row.Values[HeaderNameToPos["jp"]].ToString()
            };
        }


    }
    [CDataConstruct("CreateStateData")]
    public struct StateData : CDataBase<int>
    {
        public int id;
        public Const.StateOp[] rule;

        public int GetKey()
        {
            return id;
        }
        public static StateData CreateStateData(Dictionary<string, int> HeaderNameToPos, TypedRow row)
        {
            string stateName = row.Values[HeaderNameToPos["state"]].ToString();
            int stateNum = HeaderNameToPos.Count - 1;
            var stateData = new StateData
            {
                id = HeaderNameToPos[stateName] - 1,
                rule = new Const.StateOp[stateNum]
            };
            for (int i = 0; i < stateNum; i++)
            {
                stateData.rule[i] = (Const.StateOp)row.Values[i + 1].ToInteger();
            }
            return stateData;
        }
    }
    [CDataConstruct("CreatePlayerBaseProperty")]
    public struct PlayerBaseProperty : CDataBase<Const.StateConst>
    {
        public Const.StateConst state;
        public int priority;
        public Dictionary<string, EventFramework.UnionInt64> attrMap;

        public Const.StateConst GetKey()
        {
            return state;
        }
        public static PlayerBaseProperty CreatePlayerBaseProperty(Dictionary<string, int> HeaderNameToPos, TypedRow row)
        {
            Const.StateConst.TryParse(row.Values[HeaderNameToPos["state"]].ToString(), out Const.StateConst _state);
            return new PlayerBaseProperty
            {
                state = _state,
                priority = row.Values[HeaderNameToPos["priority"]].ToInteger(),
                attrMap = new Dictionary<string, EventFramework.UnionInt64>
                {
                    { "maxSpeed", (EventFramework.UnionInt64)row.Values[HeaderNameToPos["maxSpeed"]].ToDouble() },
                    { "maxSprintSpeed", (EventFramework.UnionInt64)row.Values[HeaderNameToPos["maxSprintSpeed"]].ToDouble() },
                    { "yawRotateSpeed", (EventFramework.UnionInt64)row.Values[HeaderNameToPos["yawRotateSpeed"]].ToDouble() },
                    { "pitchRotateSpeed", (EventFramework.UnionInt64)row.Values[HeaderNameToPos["pitchRotateSpeed"]].ToDouble() },
                    { "maxPitchAngle", (EventFramework.UnionInt64)row.Values[HeaderNameToPos["maxPitchAngle"]].ToDouble() },
                    { "minPitchAngle", (EventFramework.UnionInt64)row.Values[HeaderNameToPos["minPitchAngle"]].ToDouble() },
                    { "jumpHeight", (EventFramework.UnionInt64)row.Values[HeaderNameToPos["jumpHeight"]].ToDouble() },
                    { "gravity", (EventFramework.UnionInt64)row.Values[HeaderNameToPos["gravity"]].ToDouble() },
                },
            };
        }
    }

}