
namespace EventFramework
{
    /// <summary>¿ÕÖµ</summary>
    public class CommandInterpreter_NullArg : ICommandArg
    {
        public static readonly CommandInterpreter_NullArg Instance = new CommandInterpreter_NullArg();
        public bool IsFunctor => false;
        public object GetRawValue() => null;
        public string Format() => "null";
    }
}