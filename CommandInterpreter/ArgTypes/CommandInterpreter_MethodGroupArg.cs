using System;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace EventFramework
{
    /// <summary>方法组</summary>
    public class CommandInterpreter_MethodGroupArg : ICommandArg, IFunctor
    {
        public object Target { get; }
        public MethodInfo[] Methods { get; }
        public bool IsFunctor => true;

        public CommandInterpreter_MethodGroupArg(object target, MethodInfo[] methods) { Target = target; Methods = methods; }
        public object GetRawValue() => Methods;
        public string Format() => $"Method:{Methods[0].Name}({Methods.Length} overloads)";

        public int Invoke(CommandInterpreterRulerV2 ruler, out ICommandArg result, params ICommandArg[] args)
        {

            MethodInfo bestMatch = ruler.FindBestMatch(Methods, args);

            if (bestMatch == null)
            {
                result = CommandInterpreter_ErrorArg.Create(ErrorCodes.InvalidArgumentType, "未找到匹配的方法重载");
                return ErrorCodes.InvalidArgumentType;
            }
            try
            {
                object[] convertedArgs = CommandInterpreterHelper.ConvertArgsWitdhDefaults(args, bestMatch.GetParameters());
                object ret = bestMatch.Invoke(Target, convertedArgs);
                result = bestMatch.ReturnType == typeof(void) ? CommandInterpreter_VoidArg.Instance : CommandArgFactory.Wrap(ret);
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