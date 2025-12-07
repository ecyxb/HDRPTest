using System;
using System.Collections.Generic;

namespace EventFramework
{
    /// <summary>字符串</summary>
    public class CommandInterpreter_StringArg : ICommandArg, IStringArg, IIndexable, IMemberAccessible
    {
        public string Value { get; }
        public bool IsFunctor => false;
        public CommandInterpreter_StringArg(string value) => Value = value ?? string.Empty;
        public string GetString() => Value;
        public object GetRawValue() => Value;
        public string Format() => $"\"{Value}\"";

        public int Count => Value.Length;

        public Type KeyType => typeof(int);

        public Type ValType => typeof(char);

        public Type[] GenericTypes => new Type[]{KeyType, ValType};

        public ICommandArg GetAt(ICommandArg index)
        {
            if (!(index is INumeric num))
                return CommandInterpreter_ErrorArg.Create(ErrorCodes.InvalidArgumentType, "字符串索引必须是整数");
            int idx = (int)num.ToLong();
            if (idx < 0 || idx >= Value.Length)
                return CommandInterpreter_ErrorArg.Create(ErrorCodes.IndexOutOfRange, $"[{idx}] (长度={Value.Length})");
            return new CommandInterpreter_StringArg(Value[idx].ToString());
        }
        public bool SetAt(ICommandArg index, ICommandArg value) => false;

        public ICommandArg GetMember(string name) => MemberAccessHelper.GetMember(Value, typeof(string), name);
        public bool SetMember(string name, ICommandArg value) => false;
        public IEnumerable<string> GetMemberNames() => MemberAccessHelper.GetMemberNames(typeof(string));
    }

}