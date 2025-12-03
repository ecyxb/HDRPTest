
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor.SceneManagement;


#if UNITY_2017_1_OR_NEWER
using UnityEngine;
#endif

namespace EventFramework
{

    public static class EOHelper
    {
        public static void Log(string message)
        {
#if UNITY_2017_1_OR_NEWER
            Debug.Log(message);
#else
            Console.WriteLine(message);
#endif
        }

        public static void LogWarning(string message)
        {
#if UNITY_2017_1_OR_NEWER
            Debug.LogWarning(message);
#else
            Console.WriteLine("[Warning] " + message);
#endif
        }

        public static void LogError(string message)
        {
#if UNITY_2017_1_OR_NEWER
            Debug.LogError(message);
#else
            Console.Error.WriteLine(message);
#endif
        }

        public static IEventProxy GetEventProxy<O>(O obj, bool ensureInit = false) where O : class
        {
            Type type = typeof(O);
            var attr = type.GetCustomAttribute<EventProxyAttribute>();
            if (attr == null)
            {
#if UNITY_2017_1_OR_NEWER
                Debug.LogWarning($"Event Failed because the class of {type.FullName} dont has attribute EventProxyAttribute");
#endif
                return null;
            }
            var field = attr.proxyName == null ? null : type.GetField(attr.proxyName, BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            if (field == null || !field.FieldType.IsAssignableFrom(typeof(IEventProxy)))
            {
#if UNITY_2017_1_OR_NEWER
                Debug.LogWarning($"Event Failed because the class of {type.FullName} dont has protect/public field {attr.proxyName}");
#endif
                return null;
            }
            IEventProxy proxy = (IEventProxy)field.GetValue(obj);
            if (proxy == null)
            {
                if (!ensureInit)
                {
                    return null;
                }
                else
                {
                    proxy = new EventProxy();
                    field.SetValue(obj, proxy);
                }
            }
            return proxy;
        }

        public static IEventProxy GetEventProxy(IEventProxy obj, bool ensureInit = false)
        {
            return obj;
        }


    }
    public class EventProxy : IEventProxy
    {
        private Dictionary<string, Delegate> __event_dic__ = null;
        public EventProxy()
        {
        }
        private void RegisterEventInternal(string eventName, Delegate action)
        {
            if (string.IsNullOrEmpty(eventName) || action == null)
            {
                return;
            }
            if (__event_dic__ == null)
            {
                __event_dic__ = new Dictionary<string, Delegate>();
            }

            if (!__event_dic__.TryGetValue(eventName, out Delegate existingDelegate))
            {
                __event_dic__[eventName] = action;
                return;
            }
            if (existingDelegate.GetType() != action.GetType())
            {
                EOHelper.LogError($"The event '{eventName}' was previously registered with a different parameter type.");
                return;
            }
            __event_dic__[eventName] = Delegate.Combine(existingDelegate, action);
        }
        private void UnRegisterEventInternal(string eventName, Delegate action)
        {
            if (string.IsNullOrEmpty(eventName) || action == null)
            {
                return;
            }

            if (__event_dic__ == null || !__event_dic__.TryGetValue(eventName, out Delegate existingDelegate))
            {
                return;
            }

            if (existingDelegate.GetType() != action.GetType())
            {
                return;
            }
            __event_dic__[eventName] = Delegate.Remove(existingDelegate, action);
        }

        private T InvokeEventInterval<T>(string eventName) where T : Delegate
        {
            if (__event_dic__ == null || !__event_dic__.TryGetValue(eventName, out Delegate existingDelegate))
            {
                return null;
            }
            if (existingDelegate.GetType() != typeof(T))
            {
                return null;
            }
            return (T)existingDelegate;
        }
        public void RegisterEvent(string eventName, Action action)
        => RegisterEventInternal(eventName, action);

        public void RegisterEvent<T>(string eventName, Action<T> action)
        => RegisterEventInternal(eventName, action);

        public void RegisterEvent<T1, T2>(string eventName, Action<T1, T2> action)
            => RegisterEventInternal(eventName, action);

        public void RegisterEvent<T1, T2, T3>(string eventName, Action<T1, T2, T3> action)
            => RegisterEventInternal(eventName, action);



        public void UnRegisterEvent(string eventName, Action action)
        => UnRegisterEventInternal(eventName, action);

        public void UnRegisterEvent<T>(string eventName, Action<T> action)
        => UnRegisterEventInternal(eventName, action);

        public void UnRegisterEvent<T1, T2>(string eventName, Action<T1, T2> action)
            => UnRegisterEventInternal(eventName, action);

        public void UnRegisterEvent<T1, T2, T3>(string eventName, Action<T1, T2, T3> action)
            => UnRegisterEventInternal(eventName, action);



        public void InVokeEvent(string eventName)
            => InvokeEventInterval<Action>(eventName)?.Invoke();
        public void InVokeEvent<T>(string eventName, T data)
            => InvokeEventInterval<Action<T>>(eventName)?.Invoke(data);
        public void InVokeEvent<T1, T2>(string eventName, T1 data, T2 data2)
            => InvokeEventInterval<Action<T1, T2>>(eventName)?.Invoke(data, data2);
        public void InVokeEvent<T1, T2, T3>(string eventName, T1 data, T2 data2, T3 data3)
            => InvokeEventInterval<Action<T1, T2, T3>>(eventName)?.Invoke(data, data2, data3);
    }

    public struct UnionInt64
    {
        public const byte INT = 0b001;
        public const byte FLOAT = 0b011;
        // public const byte UINT = 3;
        public const byte INT2 = 0b101;
        public const byte FLOAT2 = 0b111;
        public const byte CDICT = 0b1001;
        public const byte IS_FLOAT = 0b010;
        public const byte IS_2D = 0b100;

        public long data;
        public byte type;

        public static implicit operator long(UnionInt64 s)
        {
            return s.type == INT ? s.data : 0;
        }

        public static implicit operator int(UnionInt64 s)
        {
            return (int)(long)s;
        }

        public static implicit operator double(UnionInt64 s)
        {
            return s.type == FLOAT ? BitConverter.Int64BitsToDouble(s.data) : (long)s;
        }
        public static implicit operator float(UnionInt64 s)
        {
            return (float)(double)s;
        }
        public static implicit operator CustomDictionary(UnionInt64 s)
        {
            if (s.type == CDICT)
            {
                return CustomDictionaryMgr.Instance[(int)s.data]; //0表示null
            }
            return null;
        }

        public static implicit operator UnionInt64(double b)
        {
            UnionInt64 instance;
            instance.type = FLOAT;
            instance.data = BitConverter.DoubleToInt64Bits(b);
            return instance;
        }
        public static implicit operator UnionInt64(long b)
        {
            UnionInt64 instance;
            instance.type = INT;
            instance.data = b;
            return instance;
        }
        public static implicit operator UnionInt64(CustomDictionary cdict)
        {
            UnionInt64 instance;
            instance.type = CDICT;
            instance.data = cdict == null ? 0 : (long)cdict.__with_parent_pool_id__;
            return instance;
        }


#if UNITY_2017_1_OR_NEWER
        public static implicit operator Vector2(UnionInt64 s)
        {
            if (s.type == FLOAT2)
            {
                return new Vector2(BitConverter.Int32BitsToSingle((int)(s.data >> 32)), BitConverter.Int32BitsToSingle((int)s.data));
            }
            else if (s.type == INT2)
            {
                return new Vector2((int)(s.data >> 32), (int)s.data);
            }
            else
            {
                return Vector2.zero;
            }
        }

        public static implicit operator Vector2Int(UnionInt64 s)
        {
            return s.type == INT2 ? new Vector2Int((int)(s.data >> 32), (int)s.data) : Vector2Int.zero;
        }

        public static implicit operator UnionInt64(Vector2Int b)
        {
            UnionInt64 instance;
            instance.type = INT2;
            instance.data = ((long)b.x << 32) | (uint)b.y;
            return instance;
        }
        public static implicit operator UnionInt64(Vector2 b)
        {
            UnionInt64 instance;
            instance.type = FLOAT2;
            instance.data = ((long)BitConverter.SingleToInt32Bits(b.x) << 32) | (uint)BitConverter.SingleToInt32Bits(b.y);
            return instance;
        }
#endif
        public static UnionInt64 GetDefault(byte type)
        {
            switch (type)
            {
                case INT:
                    return (UnionInt64)0;
                case FLOAT:
                    return (UnionInt64)(double)0;
#if UNITY_2017_1_OR_NEWER
                case INT2:
                    return (UnionInt64)Vector2Int.zero;
                case FLOAT2:
                    return (UnionInt64)Vector2.zero;
#endif
                case CDICT:
                    UnionInt64 instance;
                    instance.type = CDICT;
                    instance.data = 0;
                    return instance;

                default:
                    return new UnionInt64();
            }
        }

        public static UnionInt64 CastAvatarType(UnionInt64 original, UnionInt64 target)
        {
            if (original.type == target.type)
                return target;
            if ((original.type & IS_2D) != (target.type & IS_2D))
            {
                return target;
            }
            if (original.type == FLOAT && target.type == INT)
            {
                return (double)target;
            }
#if UNITY_2017_1_OR_NEWER
            if (original.type == FLOAT2 && target.type == INT2)
            {
                return (Vector2)target;
            }
#endif
            return target;
        }

        public override int GetHashCode()
        {
            return data.GetHashCode();
        }

    }
    public class EventDictionary<T> : Dictionary<string, T>
    {
        protected Dictionary<string, Delegate> __event_dic__ = new Dictionary<string, Delegate>();

        public EventDictionary(Dictionary<string, T> data) : base(data) { }
        public EventDictionary() { }
        public new T this[string fieldName]
        {
            get => base[fieldName];
            set
            {
                if (base.TryGetValue(fieldName, out T oldValue))
                {
                    if (object.Equals(oldValue, value))
                        return;
                }
                else
                {
                    oldValue = default;
                }
                base[fieldName] = value;
                if (!__event_dic__.TryGetValue(fieldName, out Delegate existingDelegate) || existingDelegate == null)
                {
                    return;
                }
                ((Action<T, T>)existingDelegate).Invoke(oldValue, value);
            }
        }
        public void RegisterProp(string fieldName, Action<T, T> action)
        {
            if (string.IsNullOrEmpty(fieldName) || action == null)
            {
                return;
            }

            if (!__event_dic__.TryGetValue(fieldName, out Delegate existingDelegate))
            {
                __event_dic__[fieldName] = action;
                return;
            }
            __event_dic__[fieldName] = Delegate.Combine(existingDelegate, action);
        }

        public void UnRegisterProp(string fieldName, Action<T, T> action)
        {
            if (string.IsNullOrEmpty(fieldName) || action == null)
            {
                return;
            }
            if (!__event_dic__.TryGetValue(fieldName, out Delegate existingDelegate))
            {
                return;
            }
            __event_dic__[fieldName] = Delegate.Remove(existingDelegate, action);
        }
    }
    public class CustomDictionary : IObjectPoolUser<CustomDictionary>, IIDObject<int>
    {
        public System.UInt64 __with_parent_pool_id__ { get; private set; } = 0;// 高32位存储父pool的ID，低32位存储自身ID;

        public enum DelegateArgType : byte
        {
            Int = 0, UInt = 1, Float = 2, Double = 3, Long = 4, ULong = 5, Int2 = 6, Float2 = 7, CDICT = 14, ERROR = 15,
        }
        public static readonly Dictionary<string, DelegateArgType> DelegateArgTypeMap = new Dictionary<string, DelegateArgType>
        {
            { typeof(int).Name, DelegateArgType.Int },
            { typeof(uint).Name, DelegateArgType.UInt },
            { typeof(float).Name, DelegateArgType.Float },
            { typeof(double).Name, DelegateArgType.Double },
            { typeof(long).Name, DelegateArgType.Long },
            { typeof(ulong).Name, DelegateArgType.ULong },
            { typeof(CustomDictionary).Name, DelegateArgType.CDICT },
#if UNITY_2017_1_OR_NEWER
            { typeof(Vector2Int).Name, DelegateArgType.Int2 },
            { typeof(Vector2).Name, DelegateArgType.Float2 }
#endif
        };

        protected bool m_fixedSlot = false;
        protected Dictionary<string, UnionInt64> m_data = null;
        public bool IsValid => m_data != null;
        public CustomDictionary()
        {
            // m_data为Null，表示未初始化，不占用额外的空间
            CustomDictionaryMgr.Instance.GenIdForCustomDictionary(this, SetID);
            m_fixedSlot = false;
        }
        public CustomDictionary(Dictionary<string, UnionInt64> data, bool fixedSlot = true)
        {
            CustomDictionaryMgr.Instance.GenIdForCustomDictionary(this, SetID);
            m_data = new Dictionary<string, UnionInt64>();
            foreach (var kvp in data)
            {
                m_data[kvp.Key] = SetAsParent(kvp.Value);
            }
            m_fixedSlot = fixedSlot;
            
        }
        public CustomDictionary(Dictionary<string, byte> slots, bool fixedSlot = true)
        {

            CustomDictionaryMgr.Instance.GenIdForCustomDictionary(this, SetID);
            m_data = new Dictionary<string, UnionInt64>();
            foreach (var slot in slots)
            {
                m_data[slot.Key] = UnionInt64.GetDefault(slot.Value);
            }
            m_fixedSlot = fixedSlot;
        }

        public Dictionary<string, List<ValueTuple<DelegateArgType, Delegate>>> __prop_event_dic__ = new Dictionary<string, List<ValueTuple<DelegateArgType, Delegate>>>();

        private UnionInt64 SetAsParent(UnionInt64 value)
        {
            if (value.type == UnionInt64.CDICT)
            {
                CustomDictionary cdict = (CustomDictionary)value;
                if (cdict != null)
                {
                    cdict.SetParentID(__with_parent_pool_id__);
                    value = (UnionInt64)cdict;
                }
                else
                {
                    value = UnionInt64.GetDefault(value.type);
                }
            }
            return value;
        }
        private UnionInt64 SetAsParent(UnionInt64 value, UnionInt64 oldValue)
        {
            if (value.type == UnionInt64.CDICT)
            {
                ((CustomDictionary)oldValue)?.SetParentID(0);
                CustomDictionary cdict = (CustomDictionary)value;
                if (cdict != null)
                {
                    cdict.SetParentID(__with_parent_pool_id__);
                    value = (UnionInt64)cdict;
                }
                else
                {
                    value = UnionInt64.GetDefault(value.type);
                }
                
            }
            return value;
        }

        protected UnionInt64 this[string fieldName]
        {
            get => m_data[fieldName];
            set
            {
                if (value.type == UnionInt64.CDICT && (int)((ulong)value.data >> 32) != 0)
                {
                    EOHelper.LogError("This CustomDictionary already has a parent.");
                    return;
                }

                if (!m_data.TryGetValue(fieldName, out UnionInt64 oldValue))
                {
                    if (m_fixedSlot)
                    {
                        EOHelper.LogError($"Field '{fieldName}' does not exist in fixed slot dictionary.");
                        return;
                    }
                    m_data[fieldName] = SetAsParent(value);
                    return; //新加字段不触发事件
                }
                else
                {
                    value = UnionInt64.CastAvatarType(oldValue, value);
                    if (oldValue.type != value.type)
                    {
                        EOHelper.LogError($"Type mismatch for field '{fieldName}': old type {oldValue.type}, new type {value.type}");
                        return;
                    }
                    if (oldValue.data == value.data)
                        return;
                    m_data[fieldName] = SetAsParent(value, oldValue);
                }

                if (!__prop_event_dic__.TryGetValue(fieldName, out var existingDelegate) || existingDelegate == null)
                    return;

                foreach (var dele in existingDelegate)
                {
                    switch (dele.Item1)
                    {
                        case DelegateArgType.Float:
                            ((Action<float, float>)dele.Item2).Invoke((float)oldValue, (float)value);
                            break;
                        case DelegateArgType.Int:
                            ((Action<int, int>)dele.Item2).Invoke((int)oldValue, (int)value);
                            break;
#if UNITY_2017_1_OR_NEWER
                        case DelegateArgType.Float2:
                            ((Action<Vector2, Vector2>)dele.Item2).Invoke((Vector2)oldValue, (Vector2)value);
                            break;
                        case DelegateArgType.Int2:
                            ((Action<Vector2Int, Vector2Int>)dele.Item2).Invoke((Vector2Int)oldValue, (Vector2Int)value);
                            break;
#endif
                        case DelegateArgType.UInt:
                            ((Action<uint, uint>)dele.Item2).Invoke((uint)oldValue, (uint)value);
                            break;
                        case DelegateArgType.Long:
                            ((Action<long, long>)dele.Item2).Invoke((long)oldValue, (long)value);
                            break;
                        case DelegateArgType.Double:
                            ((Action<double, double>)dele.Item2).Invoke((double)oldValue, (double)value);
                            break;
                        case DelegateArgType.ULong:
                            ((Action<ulong, ulong>)dele.Item2).Invoke((ulong)oldValue, (ulong)value);
                            break;
                        case DelegateArgType.CDICT:
                            ((Action<CustomDictionary, CustomDictionary>)dele.Item2).Invoke((CustomDictionary)oldValue, (CustomDictionary)value);
                            break;
                    }
                }
            }
        }
        public void GetValue(string fieldName, out int obj)
        {
            obj = (int)this[fieldName];
        }
        public void GetValue(string fieldName, out float obj)
        {
            obj = (float)this[fieldName];
        }
        public void GetValue(string fieldName, out double obj)
        {
            obj = this[fieldName];
        }
        public void GetValue(string fieldName, out long obj)
        {
            obj = this[fieldName];
        }
#if UNITY_2017_1_OR_NEWER
        public void GetValue(string fieldName, out Vector2Int obj)
        {
            obj = this[fieldName];
        }
        public void GetValue(string fieldName, out Vector2 obj)
        {
            obj = this[fieldName];
        }
#endif
        public void SetValue(string fieldName, long obj)
        {
            this[fieldName] = (UnionInt64)obj;
        }
        public void SetValue(string fieldName, double obj)
        {
            this[fieldName] = (UnionInt64)obj;
        }
#if UNITY_2017_1_OR_NEWER
        public void SetValue(string fieldName, Vector2Int obj)
        {
            this[fieldName] = (UnionInt64)obj;
        }
        public void SetValue(string fieldName, Vector2 obj)
        {
            this[fieldName] = (UnionInt64)obj;
        }
#endif

        private DelegateArgType GetGenericsType(Type type)
        {
            return DelegateArgTypeMap.TryGetValue(type.Name, out var argType) ? argType : DelegateArgType.ERROR;
        }

        public void RegisterProp<T>(string fieldName, Action<T, T> action)
        {
            DelegateArgType argType = GetGenericsType(typeof(T));
            if (argType == DelegateArgType.ERROR)
            {
                EOHelper.LogError($"Unsupported type '{typeof(T).Name}' for event registration.");
                return;
            }

            if (m_fixedSlot && !m_data.ContainsKey(fieldName))
            {
                EOHelper.LogError($"Field '{fieldName}' does not exist in fixed slot dictionary.");
                return;
            }
            __prop_event_dic__.TryGetValue(fieldName, out var existingDelegate);
            if (existingDelegate == null)
            {
                __prop_event_dic__[fieldName] = new List<ValueTuple<DelegateArgType, Delegate>>();
            }
            __prop_event_dic__[fieldName].Add(new ValueTuple<DelegateArgType, Delegate>(argType, action));
        }

        public void UnRegisterProp<T>(string fieldName, Action<T, T> action)
        {
            DelegateArgType argType = GetGenericsType(typeof(T));
            if (argType == DelegateArgType.ERROR)
            {
                EOHelper.LogError($"Unsupported type '{typeof(T).Name}' for event unregistration.");
                return;
            }
            if (!__prop_event_dic__.TryGetValue(fieldName, out var existingDelegate))
                return;
            if (existingDelegate == null)
                return;
            var idx = existingDelegate.FindLastIndex(tuple => tuple.Item1 == argType && (Action<T, T>)tuple.Item2 == action);
            if (idx >= 0)
            {
                existingDelegate.RemoveAt(idx);
            }
        }

        public IEnumerable<CustomDictionary> AllPoolObjects()
        {
            foreach (var value in m_data.Values)
            {
                if (value.type == UnionInt64.CDICT)
                {
                    var cdict = (CustomDictionary)value;
                    if (cdict != null)
                    {
                        yield return cdict;
                    }
                }
            }
        }

        public int GetID()
        {
            return (int)(__with_parent_pool_id__ & 0xFFFFFFFF); // 低32位存储ID
        }

        public string GetIDString()
        {
            return __with_parent_pool_id__.ToString();
        }

        private void SetID(int id)
        {
            __with_parent_pool_id__ = (__with_parent_pool_id__ & 0xFFFFFFFF00000000) | (uint)id;
        }
        public int GetParentID()
        {
            return (int)(__with_parent_pool_id__ >> 32); // 高32位存储父pool的ID
        }

        public CustomDictionary GetParentDict()
        {
            return CustomDictionaryMgr.Instance[GetParentID()];
        }
        // 禁止由子dict直接修改父dict的ID
        private void SetParentID(System.UInt64 parentID)
        {
            __with_parent_pool_id__ = (__with_parent_pool_id__ & 0xFFFFFFFF) | (parentID << 32);
        }
        public void StartRecursiveClear()
        {
            if (GetParentDict() != null)
            {
                return; // 有父节点的dict不允许Clear
            }
            RecursiveClear();
        }

        public void RecursiveClear()
        {
            // 防止重复清理（环检测）
            if (!IsValid)
            {
                return; // 已经被清理过，避免死循环
            }
            
            // 清理所有子dict的父ID
            var tmpdata = m_data;
            m_data = null; // 立即标记为无效，防止环导致的递归
            
            foreach (var value in tmpdata.Values)
            {
                if (value.type == UnionInt64.CDICT)
                {
                    ((CustomDictionary)value)?.RecursiveClear();
                }
            }
            
            CustomDictionaryMgr.Instance.ClearIDForCustomDictionary(this);
            __with_parent_pool_id__ = 0;
        }


    }



    public class WithEventCustomDictionary : CustomDictionary, IEventProxy
    {
        private Dictionary<string, Delegate> __event_dic__ = null;
        public WithEventCustomDictionary(Dictionary<string, UnionInt64> data, bool fixedSlot = true) : base(data, fixedSlot)
        {
        }

        public WithEventCustomDictionary(Dictionary<string, byte> slots, bool fixedSlot = true) : base(slots, fixedSlot){
            
        }

        private void RegisterEventInternal(string eventName, Delegate action)
        {
            if (string.IsNullOrEmpty(eventName) || action == null)
            {
                return;
            }
            if (__event_dic__ == null)
            {
                __event_dic__ = new Dictionary<string, Delegate>();
            }

            if (!__event_dic__.TryGetValue(eventName, out Delegate existingDelegate))
            {
                __event_dic__[eventName] = action;
                return;
            }
            if (existingDelegate.GetType() != action.GetType())
            {
                EOHelper.LogError($"The event '{eventName}' was previously registered with a different parameter type.");
                return;
            }
            __event_dic__[eventName] = Delegate.Combine(existingDelegate, action);
        }
        private void UnRegisterEventInternal(string eventName, Delegate action)
        {
            if (string.IsNullOrEmpty(eventName) || action == null)
            {
                return;
            }

            if (__event_dic__ == null || !__event_dic__.TryGetValue(eventName, out Delegate existingDelegate))
            {
                return;
            }

            if (existingDelegate.GetType() != action.GetType())
            {
                return;
            }
            __event_dic__[eventName] = Delegate.Remove(existingDelegate, action);
        }

        private T InvokeEventInterval<T>(string eventName) where T : Delegate
        {
            if (__event_dic__ == null || !__event_dic__.TryGetValue(eventName, out Delegate existingDelegate))
            {
                return null;
            }
            if (existingDelegate.GetType() != typeof(T))
            {
                return null;
            }
            return (T)existingDelegate;
        }
        public void RegisterEvent(string eventName, Action action)
        => RegisterEventInternal(eventName, action);

        public void RegisterEvent<T>(string eventName, Action<T> action)
        => RegisterEventInternal(eventName, action);

        public void RegisterEvent<T1, T2>(string eventName, Action<T1, T2> action)
            => RegisterEventInternal(eventName, action);

        public void RegisterEvent<T1, T2, T3>(string eventName, Action<T1, T2, T3> action)
            => RegisterEventInternal(eventName, action);



        public void UnRegisterEvent(string eventName, Action action)
        => UnRegisterEventInternal(eventName, action);

        public void UnRegisterEvent<T>(string eventName, Action<T> action)
        => UnRegisterEventInternal(eventName, action);

        public void UnRegisterEvent<T1, T2>(string eventName, Action<T1, T2> action)
            => UnRegisterEventInternal(eventName, action);

        public void UnRegisterEvent<T1, T2, T3>(string eventName, Action<T1, T2, T3> action)
            => UnRegisterEventInternal(eventName, action);



        public void InVokeEvent(string eventName)
            => InvokeEventInterval<Action>(eventName)?.Invoke();
        public void InVokeEvent<T>(string eventName, T data)
            => InvokeEventInterval<Action<T>>(eventName)?.Invoke(data);
        public void InVokeEvent<T1, T2>(string eventName, T1 data, T2 data2)
            => InvokeEventInterval<Action<T1, T2>>(eventName)?.Invoke(data, data2);
        public void InVokeEvent<T1, T2, T3>(string eventName, T1 data, T2 data2, T3 data3)
            => InvokeEventInterval<Action<T1, T2, T3>>(eventName)?.Invoke(data, data2, data3);
    }
    public class EventObject : WithEventCustomDictionary
    {
        public bool NeedUpdate { get; set; } = false;
        public bool NeedFixedUpdate { get; set; } = false;
        public bool NeedLateUpdate { get; set; } = false;
        public GameObject gameobject { get; protected set; }

        public void BindGameObject(GameObject go)
        {
            this.gameobject = go;
        }
        protected EventObject(Dictionary<string, UnionInt64> data, bool fixedSlot = true) : base(data, fixedSlot)
        {
            this.gameobject = null;
        }

        public T AddComponent<T>() where T : Component
        {
            return gameobject?.AddComponent<T>();
        }
        public T GetComponent<T>() where T : Component
        {
            return gameobject?.GetComponent<T>();
        }
        public Transform transform => gameobject?.transform;
        public static implicit operator bool(EventObject o)
        {
            return o != null;
        }
        public IEnumerable<EventCompBase> AllEventComps()
        {
            foreach (var value in m_data.Values)
            {
                if (value.type == UnionInt64.CDICT)
                {
                    var cdict = (CustomDictionary)value;
                    if (cdict is EventCompBase eventComp)
                    {
                        yield return eventComp;
                    }
                }
            }
        }
        protected virtual void OnStart()
        {
        }
        protected virtual void OnDestroy()
        {
        }
        public void __on_start__()
        {
            OnStart();
            foreach (var comp in AllEventComps())
            {
                comp.CompStart();
            }
        }
        public void __on_destroy__()
        {
            foreach (var comp in AllEventComps())
            {
                comp.CompDestroy();
            }
            OnDestroy();
        }

        public virtual void Update()
        {

        }
        public virtual void FixedUpdate()
        {

        }
        public virtual void LateUpdate()
        {

        }

    }
    public class EventCompBase : WithEventCustomDictionary
    {
        public EventObject Owner => GetParentDict() as EventObject;

        protected EventCompBase(Dictionary<string, UnionInt64> data, bool fixedSlot = true) : base(data, fixedSlot)
        {

        }
        protected EventCompBase(Dictionary<string, byte> slots, bool fixedSlot = true) : base(slots, fixedSlot)
        {

        }

        public virtual void CompStart()
        {

        }

        public virtual void CompDestroy()
        {
            // Override this method to handle component destruction
        }
        

    }
    public class CustomDictionaryMgr
    {
        // TODO：在空闲时间销毁没有父节点的CustomDictionary，防止内存无限增长
        public static CustomDictionaryMgr Instance = new CustomDictionaryMgr(200);
        protected Stack<int> _freeIds = new Stack<int>();
        protected List<CustomDictionary> _Pool = null;

        private HashSet<IObjectPoolUser<CustomDictionary>> _returningCustomDictionaries = new HashSet<IObjectPoolUser<CustomDictionary>>();
        public CustomDictionaryMgr(int initFreeSize = 100)
        {
            _Pool = new List<CustomDictionary>(initFreeSize + 1)
            {
                null // 保留0号位不使用
            };
            _freeIds = new Stack<int>(initFreeSize);
        }
        public void GenIdForCustomDictionary(CustomDictionary obj, Action<int> SetIDAction = null)
        {
            if (_freeIds.Count > 0)
            {
                int id = _freeIds.Pop();
                SetIDAction?.Invoke(id);
                _Pool[id] = obj;
            }
            else
            {
                int id = _Pool.Count;
                SetIDAction?.Invoke(id);
                _Pool.Add(obj);
            }
        }
        public CustomDictionary this[int id]
        {
            get
            {
                if (id <= 0 || id >= _Pool.Count)
                {
                    return null;
                }
                return _Pool[id];
            }
        }

        public void ClearIDForCustomDictionary(CustomDictionary obj)
        {
            int id = obj.GetID();
            if (id <= 0 || id >= _Pool.Count)
            {
                return;
            }
            if (_Pool[id] == null)
            {
                return;
            }
            _Pool[id] = null;
            _freeIds.Push(id);
        }

    }
    
}
