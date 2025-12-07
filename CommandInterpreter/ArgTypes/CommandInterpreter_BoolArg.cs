using System.Collections.Generic;

namespace EventFramework
{
    /// <summary>²¼¶ûÖµ</summary>
    public class CommandInterpreter_BoolArg : ICommandArg, IMemberAccessible
    {
        public static readonly CommandInterpreter_BoolArg True = new CommandInterpreter_BoolArg(true);
        public static readonly CommandInterpreter_BoolArg False = new CommandInterpreter_BoolArg(false);
        public bool Value { get; }
        public bool IsFunctor => false;
        private CommandInterpreter_BoolArg(bool value) => Value = value;
        public static CommandInterpreter_BoolArg From(bool value) => value ? True : False;
        public object GetRawValue() => Value;
        public string Format() => Value ? "true" : "false";

        public ICommandArg GetMember(string name) => MemberAccessHelper.GetMember(Value, typeof(bool), name);
        public bool SetMember(string name, ICommandArg value) => false;
        public IEnumerable<string> GetMemberNames() => MemberAccessHelper.GetMemberNames(typeof(bool));
    }
}