using System;
using System.Collections;
using System.Collections.Generic;

namespace EventFramework
{

    /// <summary>字典</summary>
    public class CommandInterpreter_DictArg : ICommandArg, IIndexable, IMemberAccessible
    {
        private readonly IDictionary _dict;
        public bool IsFunctor => false;
        public Type[] genricTypes;

        public Type[] GenericTypes => genricTypes;
        public Type KeyType => genricTypes[0];
        public Type ValType => genricTypes[1];
        public CommandInterpreter_DictArg(IDictionary dict)
        {
            this._dict = dict;
            var dictType = _dict.GetType();
            if (dictType.IsGenericType)
            {
                var genericArgs = dictType.GetGenericArguments();
                if (genericArgs.Length >= 2)
                    genricTypes = new Type[] { genericArgs[0], genericArgs[1] };
            }
        }
        public object GetRawValue() => _dict;

        public string Format()
        {
            var items = new List<string>();
            int count = 0;
            foreach (DictionaryEntry entry in _dict)
            {
                if (count >= 5) break;
                items.Add($"{entry.Key}: {CommandArgFactory.Wrap(entry.Value).Format()}");
                count++;
            }
            string preview = string.Join(", ", items);
            if (_dict.Count > 5) preview += $"... (共 {_dict.Count} 项)";
            return $"{{{preview}}}";
        }

        public int Count => _dict.Count;

        public ICommandArg GetAt(ICommandArg index)
        {
            object key = index.GetRawValue();
            if (KeyType != null && key != null)
            {
                try { key = CommandInterpreterRulerV2.ConvertArg(key, KeyType); }
                catch { /* 保持原样 */ }
            }

            if (_dict.Contains(key)) return CommandArgFactory.Wrap(_dict[key]);
            return CommandInterpreter_ErrorArg.Create(ErrorCodes.MemberNotFound, $"键 '{index.GetRawValue()}' 不存在");
        }

        public bool SetAt(ICommandArg index, ICommandArg value)
        {
            object key = index.GetRawValue();
            object val = value.GetRawValue();

            if (KeyType != null && key != null)
            {
                try { key = CommandInterpreterRulerV2.ConvertArg(key, KeyType); }
                catch { return false; }
            }

            if (ValType != null && val != null)
            {
                try { val = CommandInterpreterRulerV2.ConvertArg(val, ValType); }
                catch { return false; }
            }

            _dict[key] = val;
            return true;
        }

        public ICommandArg GetMember(string name) => MemberAccessHelper.GetMember(_dict, _dict.GetType(), name);
        public bool SetMember(string name, ICommandArg value) => MemberAccessHelper.SetMember(_dict, _dict.GetType(), name, value);
        public IEnumerable<string> GetMemberNames() => MemberAccessHelper.GetMemberNames(_dict.GetType());
    }


}