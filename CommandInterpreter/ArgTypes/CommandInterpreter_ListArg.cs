using System;
using System.Collections;
using System.Collections.Generic;

namespace EventFramework
{

    /// <summary>列表</summary>
    public class CommandInterpreter_ListArg : ICommandArg, IIndexable, IMemberAccessible
    {
        private readonly IList _list;
        private readonly Type eType = null;
        public Type[] GenericTypes => new Type[] { typeof(int), eType };
        public Type KeyType => typeof(int);
        public Type ValType => eType;
        public bool IsFunctor => false;

        public CommandInterpreter_ListArg(IList list) {
            _list = list;
            Type listType = _list.GetType();
            // 处理数组
            if (listType.IsArray)
                eType = listType.GetElementType();

            // 处理泛型 List<T>
            if (listType.IsGenericType)
            {
                var genericArgs = listType.GetGenericArguments();
                if (genericArgs.Length > 0)
                    eType = genericArgs[0];
            }
            
        }
        public object GetRawValue() => _list;

        public string Format()
        {
            var items = new List<string>();
            int count = 0;
            foreach (var item in _list)
            {
                if (count >= 10) break;
                items.Add(CommandArgFactory.Wrap(item).Format());
                count++;
            }
            string preview = string.Join(", ", items);
            if (_list.Count > 10) preview += $"... (共 {_list.Count} 项)";
            return $"[{preview}]";
        }

        public int Count => _list.Count;
        public ICommandArg GetAt(ICommandArg index)
        {
            if (!(index is INumeric num))
                return CommandInterpreter_ErrorArg.Create(ErrorCodes.InvalidArgumentType, "列表索引必须是整数");
            int idx = (int)num.ToLong();
            if (idx < 0 || idx >= _list.Count)
                return CommandInterpreter_ErrorArg.Create(ErrorCodes.IndexOutOfRange, $"[{idx}] (长度={_list.Count})");
            return CommandArgFactory.Wrap(_list[idx]);
        }
        public bool SetAt(ICommandArg index, ICommandArg value)
        {
            if (!(index is INumeric num)) return false;
            int idx = (int)num.ToLong();
            if (idx < 0 || idx >= _list.Count) return false;

            object rawValue = value.GetRawValue();

            // 获取列表元素类型并进行类型转换
            if (ValType != null && rawValue != null)
            {
                try
                {
                    rawValue = CommandInterpreterRulerV2.ConvertArg(rawValue, ValType);
                }
                catch
                {
                    return false;
                }
            }

            _list[idx] = rawValue;
            return true;
        }
        public ICommandArg GetMember(string name) => MemberAccessHelper.GetMember(_list, _list.GetType(), name);
        public bool SetMember(string name, ICommandArg value) => MemberAccessHelper.SetMember(_list, _list.GetType(), name, value);
        public IEnumerable<string> GetMemberNames() => MemberAccessHelper.GetMemberNames(_list.GetType());
    }

}