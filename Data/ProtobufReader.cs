using System.Collections;
using System.Collections.Generic;
using ProtoBuf;
using System;
using System.IO;
using UnityEngine;
using System.Reflection;
using System.Linq;

namespace CData
{

    [ProtoContract]
    [ProtoInclude(100, typeof(IntValue))]
    [ProtoInclude(101, typeof(DoubleValue))]
    [ProtoInclude(102, typeof(BoolValue))]
    [ProtoInclude(103, typeof(DateTimeValue))]
    [ProtoInclude(104, typeof(StringValue))]
    public abstract class CellValue
    {
        // 基础抽象类，所有类型值的父类
        public override string ToString() => "UNKNOWN";
        public virtual int ToInteger() => 0;
        public virtual float ToFloat() => 0.0f;
        public virtual double ToDouble() => 0.0;
        public virtual bool ToBoolean() => false;
        public virtual DateTime ToDateTime() => DateTime.MinValue;
    }

    [ProtoContract]
    public class IntValue : CellValue
    {
        [ProtoMember(1)]
        public int Value { get; set; }
        public override string ToString() => Value.ToString();
        public override int ToInteger() => Value;
        public override float ToFloat() => Value;
        public override double ToDouble() => Value;
    }

    [ProtoContract]
    public class DoubleValue : CellValue
    {
        [ProtoMember(1)]
        public double Value { get; set; }
        public override string ToString() => Value.ToString();
        public override double ToDouble() => Value;
        public override float ToFloat() => (float)Value;
    }

    [ProtoContract]
    public class BoolValue : CellValue
    {
        [ProtoMember(1)]
        public bool Value { get; set; }
        public override string ToString() => Value.ToString();
        public override bool ToBoolean() => Value;
        public override int ToInteger() => Value ? 1 : 0;
    }

    [ProtoContract]
    public class DateTimeValue : CellValue
    {
        [ProtoMember(1)]
        public DateTime Value { get; set; }
        public override string ToString() => Value.ToString();
        public override DateTime ToDateTime() => Value;
    }

    [ProtoContract]
    public class StringValue : CellValue
    {
        [ProtoMember(1)]
        public string Value { get; set; }
        public override string ToString() => Value;
    }

    [ProtoContract]
    public class TypedExcelData
    {
        [ProtoMember(1)]
        public List<string> Headers { get; set; } = new List<string>();

        [ProtoMember(2)]
        public List<TypedRow> Rows { get; set; } = new List<TypedRow>();
    }

    [ProtoContract]
    public class TypedRow
    {
        [ProtoMember(1)]
        public List<CellValue> Values { get; set; } = new List<CellValue>();
    }


    public class ProtobufReader
    {

        private TypedExcelData BaseDeserialize(string className)
        {

            var filePath = Path.Combine(Application.streamingAssetsPath, "cdata", className + ".bin");
            using (FileStream fsRead = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                // 使用ProtoBuf反序列化
                return Serializer.Deserialize<TypedExcelData>(fsRead);
            }
        }

        public Dictionary<TKey, T> GenerateDictionary<TKey, T>(string className) where T : CDataBase<TKey>
        {
            var data = BaseDeserialize(className);
            if(data == null || data.Headers.Count == 0 || data.Rows.Count == 0)
            {
                Debug.LogError($"Data for {className} is empty or invalid.");
                return new Dictionary<TKey, T>();
            }
            var type = typeof(T);
            var res = new Dictionary<TKey, T>(data.Rows.Count);

            // 检查规则
            CDataConstructAttribute attr = type.GetCustomAttribute<CDataConstructAttribute>();
            if (attr == null || string.IsNullOrWhiteSpace(attr.FuncName))
            {
                Debug.LogError($"Type {type.Name} does not have CDataConstructAttribute");
                return res;
            }
            MethodInfo constructFunc = type.GetMethod(attr.FuncName, BindingFlags.Public | BindingFlags.Static);
            if (constructFunc == null)
            {
                Debug.LogError($"Method {attr.FuncName} not found in type {type.Name}");
                return res;
            }

            Dictionary<string, int> headerNameToPos = new Dictionary<string, int>(data.Headers.Count);
            res.EnsureCapacity(data.Rows.Count);
            for (int i = 0; i < data.Headers.Count; i++)
            {
                headerNameToPos[data.Headers[i]] = i;
            }

            for (int i = 0; i < data.Rows.Count; i++)
            {
                TypedRow row = data.Rows[i];
                if (row.Values.Count != data.Headers.Count)
                {
                    Debug.LogError($"Row {i} has {row.Values.Count} values but expected {data.Headers.Count}");
                    continue;
                }
                T item = (T)constructFunc.Invoke(null, new object[] { headerNameToPos, row });
                TKey key = item.GetKey();
                if (key == null)
                {
                    Debug.LogError($"Row {i} has invalid key: {key}");
                    continue;
                }
                res[key] = item;
            }
            return res;
        }

        // public Dictionary<TKey, T> GenerateDictionary<TKey, T>(string className)
        // {
        //     var data = BaseDeserialize(className);
        //     if(data == null || data.Headers.Count == 0 || data.Rows.Count == 0)
        //     {
        //         Debug.LogError($"Data for {className} is empty or invalid.");
        //         return new Dictionary<TKey, T>();
        //     }
        //     var type = typeof(T);
        //     var res = new Dictionary<TKey, T>(data.Rows.Count);

        //     // 检查规则
        //     CDataConstructAttribute attr = type.GetCustomAttribute<CDataConstructAttribute>();
        //     if (attr == null || string.IsNullOrWhiteSpace(attr.FuncName))
        //     {
        //         Debug.LogError($"Type {type.Name} does not have CDataConstructAttribute");
        //         return res;
        //     }
        //     MethodInfo constructFunc = type.GetMethod(attr.FuncName, BindingFlags.Public | BindingFlags.Static);
        //     if (constructFunc == null)
        //     {
        //         Debug.LogError($"Method {attr.FuncName} not found in type {type.Name}");
        //         return res;
        //     }
        //     FieldInfo[] keys = type.GetFields(BindingFlags.Public | BindingFlags.Instance).Where(f => f.GetCustomAttribute<CDataMainKeyAttribute>() != null).ToArray();
        //     PropertyInfo[] keyProps = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetCustomAttribute<CDataMainKeyAttribute>() != null && p.GetGetMethod() != null).ToArray();
        //     if (keys.Length == 0 && keyProps.Length == 0 || keys.Length + keyProps.Length > 1)
        //     {
        //         Debug.LogError($"Type {type.Name} must have exactly one field or property with CDataMainKeyAttribute");
        //         return res;
        //     }

        //     // 转换类型
        //     var getKeyFunc = keys.Length > 0
        //         ? (Func<T, TKey>)(d => (TKey)keys[0].GetValue(d))
        //         : (d => (TKey)keyProps[0].GetValue(d));

        //     Dictionary<string, int> headerNameToPos = new Dictionary<string, int>(data.Headers.Count);
        //     res.EnsureCapacity(data.Rows.Count);
        //     for (int i = 0; i < data.Headers.Count; i++)
        //     {
        //         headerNameToPos[data.Headers[i]] = i;
        //     }

        //     for (int i = 0; i < data.Rows.Count; i++)
        //     {
        //         TypedRow row = data.Rows[i];
        //         if (row.Values.Count != data.Headers.Count)
        //         {
        //             Debug.LogError($"Row {i} has {row.Values.Count} values but expected {data.Headers.Count}");
        //             continue;
        //         }
        //         T item = (T)constructFunc.Invoke(null, new object[] { headerNameToPos, row });
        //         TKey key = getKeyFunc(item);
        //         if (key == null || EqualityComparer<TKey>.Default.Equals(key, default))
        //         {
        //             Debug.LogError($"Row {i} has invalid key: {key}");
        //             continue;
        //         }
        //         res[key] = item;
        //     }
        //     return res;
        // }
    }

}