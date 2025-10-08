
#define EVENT_OBJ_IN_UNITY_PROJECT

using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;

#if EVENT_OBJ_IN_UNITY_PROJECT
using UnityEngine;
#endif

namespace EventFramework
{

    public static class EOHelper
    {
        public static void LogError(string message)
        {
#if EVENT_OBJ_IN_UNITY_PROJECT
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
#if EVENT_OBJ_IN_UNITY_PROJECT
                Debug.LogWarning($"Event Failed because the class of {type.FullName} dont has attribute EventProxyAttribute");
#endif
                return null;
            }
            var field = attr.proxyName == null ? null : type.GetField(attr.proxyName, BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            if (field == null || !field.FieldType.IsAssignableFrom(typeof(IEventProxy)))
            {
#if EVENT_OBJ_IN_UNITY_PROJECT
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
                return CustomDictionaryPool.Instance[(int)s.data];
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
            instance.data = (long)cdict.__with_parent_pool_id__;
            return instance;
        }


#if EVENT_OBJ_IN_UNITY_PROJECT
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
#if EVENT_OBJ_IN_UNITY_PROJECT
                case INT2:
                    return (UnionInt64)Vector2Int.zero;
                case FLOAT2:
                    return (UnionInt64)Vector2.zero;
#endif
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
#if EVENT_OBJ_IN_UNITY_PROJECT
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
#if EVENT_OBJ_IN_UNITY_PROJECT
            { typeof(Vector2Int).Name, DelegateArgType.Int2 },
            { typeof(Vector2).Name, DelegateArgType.Float2 }
#endif
        };

        protected bool m_fixedSlot = true;
        protected Dictionary<string, UnionInt64> m_data = null;
        public bool IsValid => m_data != null;
        public CustomDictionary()
        {
            // m_data为Null，表示未初始化，不占用额外的空间
            m_fixedSlot = false;
        }
        public void InitDictionaryData(Dictionary<string, UnionInt64> data, bool fixedSlot = true)
        {
            m_data = new Dictionary<string, UnionInt64>(data);
            m_fixedSlot = fixedSlot;
        }
        public void InitDictionaryData(Dictionary<string, byte> slots, bool fixedSlot = true)
        {
            m_data = new Dictionary<string, UnionInt64>();
            foreach (var slot in slots)
            {
                m_data[slot.Key] = UnionInt64.GetDefault(slot.Value);
            }
            m_fixedSlot = fixedSlot;
        }

        public CustomDictionary(Dictionary<string, UnionInt64> data, bool fixedSlot = true) : base()
        {
            InitDictionaryData(data, fixedSlot);
        }
        public CustomDictionary(Dictionary<string, byte> slots, bool fixedSlot = true) : base()
        {
            InitDictionaryData(slots, fixedSlot);
        }
        public Dictionary<string, List<ValueTuple<DelegateArgType, Delegate>>> __prop_event_dic__ = new Dictionary<string, List<ValueTuple<DelegateArgType, Delegate>>>();
        
        protected UnionInt64 this[string fieldName]
        {
            get => m_data[fieldName];
            set
            {
                if (!m_data.TryGetValue(fieldName, out UnionInt64 oldValue))
                {
                    if (m_fixedSlot)
                    {
                        EOHelper.LogError($"Field '{fieldName}' does not exist in fixed slot dictionary.");
                        return;
                    }
                    m_data[fieldName] = value;
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
                    if (value.type == UnionInt64.CDICT)
                    {
                        if (value.data != 0)
                        {
                            int newDictOldParentId = (int)((ulong)value.data >> 32);
                            if (newDictOldParentId != 0 && newDictOldParentId != GetID())
                            {
                                EOHelper.LogError("This CustomDictionary already has a parent.");
                                return;
                            }
                        }
                        ((CustomDictionary)oldValue)?.SetParentID(0);
                        CustomDictionary cdict = (CustomDictionary)value;
                        if (cdict == null)
                        {
                            value = UnionInt64.GetDefault(value.type);
                        }
                        else
                        {
                            cdict.SetParentID(__with_parent_pool_id__);
                            value = (UnionInt64)cdict;
                        }
                    }
                    m_data[fieldName] = value;
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
#if EVENT_OBJ_IN_UNITY_PROJECT
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
#if EVENT_OBJ_IN_UNITY_PROJECT
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
#if EVENT_OBJ_IN_UNITY_PROJECT
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

        public void SetID(int id)
        {
            __with_parent_pool_id__ = (__with_parent_pool_id__ & 0xFFFFFFFF00000000) | (uint)id;
        }
        public int GetParentID()
        {
            return (int)(__with_parent_pool_id__ >> 32); // 高32位存储父pool的ID
        }
        public CustomDictionary GetParentDict()
        {
            return CustomDictionaryPool.Instance[GetParentID()];
        }
        // 禁止由子dict直接修改父dict的ID
        private void SetParentID(System.UInt64 parentID)
        {
            __with_parent_pool_id__ = (__with_parent_pool_id__ & 0xFFFFFFFF) | (parentID << 32);
        }
        public void TryClear()
        {
            if (GetParentDict() != null)
            {
                return; // 有父节点的dict不允许Clear
            }
            // 清理所有子dict的父ID
            foreach (var value in m_data.Values)
            {
                if (value.type == UnionInt64.CDICT)
                {
                    ((CustomDictionary)value)?.SetParentID(0);
                }
            }
            CustomDictionaryPool.Instance.UserReturn(this);
            m_data = null;
        }
        

    }


    public class EventObject
    {

    }
    public class EventCompBase : CustomDictionary, IEventProxy
    {
        private Dictionary<string, Delegate> __event_dic__ = null;

        public EventCompBase(Dictionary<string, UnionInt64> data, bool fixedSlot = true) : base(data, fixedSlot)
        {
        }
        public EventCompBase(Dictionary<string, byte> slots, bool fixedSlot = true) : base(slots, fixedSlot)
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

    public class CustomDictionaryPool : Array_IDObjectPool<CustomDictionary>
    {
        // TODO：在空闲时间销毁没有父节点的CustomDictionary，防止内存无限增长
        public static CustomDictionaryPool Instance = new CustomDictionaryPool(200);
        private Dictionary<string, UnionInt64> tmpdata;
        private Dictionary<string, byte> tmpslots;
        private bool tmpfixedSlot;

        private HashSet<IObjectPoolUser<CustomDictionary>> _returningCustomDictionaries = new HashSet<IObjectPoolUser<CustomDictionary>>();
        public CustomDictionaryPool(int initFreeSize = 100) : base(initFreeSize, CustomDictionaryCreateFunc, CustomDictionaryReturnFunc, OnGetCustomDictionary)
        {
        }
        public CustomDictionary GetCustomDictionary(Dictionary<string, UnionInt64> data, bool fixedSlot = true, IObjectPoolUser<CustomDictionary> user = null)
        {
            tmpdata = data;
            tmpfixedSlot = fixedSlot;
            tmpslots = null;
            var dict = Get(user);
            tmpdata = null;
            return dict;
        }
        public CustomDictionary GetCustomDictionary(Dictionary<string, byte> slots, bool fixedSlot = true, IObjectPoolUser<CustomDictionary> user = null)
        {
            tmpdata = null;
            tmpfixedSlot = fixedSlot;
            tmpslots = slots;
            var dict = Get(user);
            tmpdata = null;
            return dict;
        }

        private static bool OnGetCustomDictionary(CustomDictionary obj)
        {
            if (obj.IsValid)
            {
                return false;
            }
            if (Instance.tmpdata != null)
            {
                obj.InitDictionaryData(Instance.tmpdata, Instance.tmpfixedSlot);
                return true;
            }
            obj.InitDictionaryData(Instance.tmpslots, Instance.tmpfixedSlot);
            return true;
        }

        private static CustomDictionary CustomDictionaryCreateFunc()
        {
            return new CustomDictionary();
        }
        private static bool CustomDictionaryReturnFunc(CustomDictionary obj)
        {
            obj.TryClear();
            return true;
        }
        
        public override void UserReturn(IObjectPoolUser<CustomDictionary> user)
        {
            if (user == null)
            {
                return;
            }
            if (_returningCustomDictionaries.Contains(user))
            {
                EOHelper.LogError($"CustomDictionaryPool: Detected recursive return of CustomDictionary, check circle.");
                return;
            }
            _returningCustomDictionaries.Add(user);
#if EventFrameWork_DEBUG
            _users.Remove(user);
            Return(user.AllPoolObjects());
#else
            Return(user.AllPoolObjects());
#endif
            _returningCustomDictionaries.Remove(user);
        }

    }

}
