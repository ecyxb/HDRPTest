using System;
using System.Linq;

namespace EventFramework
{
    /// <summary>Î¯ÍÐ</summary>
    public class CommandInterpreter_DelegateArg : ICommandArg, IFunctor
    {
        public Delegate Value { get; }
        public bool IsFunctor => true;
        public CommandInterpreter_DelegateArg(Delegate del) => Value = del;
        public object GetRawValue() => Value;
        public string Format() => $"Delegate:{Value.Method.Name}";

        public int Invoke(CommandInterpreterRulerV2 ruler, out ICommandArg result, params ICommandArg[] args)
        {
            object[] rawArgs = args.Select(a => a.GetRawValue()).ToArray();
            try
            {
                object ret = Value.DynamicInvoke(rawArgs);
                result = Value.Method.ReturnType == typeof(void) ? CommandInterpreter_VoidArg.Instance : CommandArgFactory.Wrap(ret);
                return ErrorCodes.Success;
            }
            catch (Exception ex)
            {
                result = CommandInterpreter_ErrorArg.Create(ErrorCodes.UnknownError, ex.InnerException?.Message ?? ex.Message);
                return ErrorCodes.UnknownError;
            }
        }
    }
}