namespace EventFramework
{
    /// <summary>Void∑µªÿ÷µ</summary>
    public class CommandInterpreter_VoidArg : ICommandArg
    {
        public static readonly CommandInterpreter_VoidArg Instance = new CommandInterpreter_VoidArg();
        public bool IsFunctor => false;
        public object GetRawValue() => null;
        public string Format() => "void";
    }

}