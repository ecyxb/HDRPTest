
using System;
using System.Collections.Generic;
using System.Linq;

namespace EventFramework{
    /// <summary>对象包装</summary>
    public class CommandInterpreter_ObjectArg : ICommandArg, IMemberAccessible
    {
        public object Value { get; }
        public Type ObjectType { get; }
        public bool IsFunctor => false;

        public CommandInterpreter_ObjectArg(object value) { Value = value; ObjectType = value?.GetType(); }
        public object GetRawValue() => Value;
        public string Format() => Value?.ToString() ?? "null";

        public ICommandArg GetMember(string name) => MemberAccessHelper.GetMember(Value, ObjectType, name);
        public bool SetMember(string name, ICommandArg value) => MemberAccessHelper.SetMember(Value, ObjectType, name, value);
        public IEnumerable<string> GetMemberNames() => ObjectType != null ? MemberAccessHelper.GetMemberNames(ObjectType) : Enumerable.Empty<string>();
    }


}