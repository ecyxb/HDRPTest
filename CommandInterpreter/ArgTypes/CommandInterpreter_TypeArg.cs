using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EventFramework
{
    /// <summary>类型（静态成员访问）</summary>
    public class CommandInterpreter_TypeArg : ICommandArg, IMemberAccessible, IFunctor
    {
        public Type Value { get; }
        public bool IsFunctor => true;
        public CommandInterpreter_TypeArg(Type type) => Value = type;
        public object GetRawValue() => Value;
        public string Format() => $"Type:{Value.Name}";

        public ICommandArg GetMember(string name)
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            var prop = Value.GetProperty(name, flags);
            if (prop != null)
            {
                try { return CommandArgFactory.Wrap(prop.GetValue(null)); }
                catch (Exception ex) { return CommandInterpreter_ErrorArg.Create(ErrorCodes.UnknownError, ex.Message); }
            }

            var field = Value.GetField(name, flags);
            if (field != null)
            {
                try { return CommandArgFactory.Wrap(field.GetValue(null)); }
                catch (Exception ex) { return CommandInterpreter_ErrorArg.Create(ErrorCodes.UnknownError, ex.Message); }
            }

            var methods = Value.GetMethods(flags).Where(m => m.Name == name).ToArray();
            if (methods.Length > 0) return new CommandInterpreter_MethodGroupArg(null, methods);

            return CommandInterpreter_ErrorArg.Create(ErrorCodes.MemberNotFound, $"{Value.Name}.{name}");
        }

        public bool SetMember(string name, ICommandArg value) => false;

        public IEnumerable<string> GetMemberNames()
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public;
            foreach (var prop in Value.GetProperties(flags)) yield return prop.Name;
            foreach (var field in Value.GetFields(flags)) yield return field.Name;
        }

        public int Invoke(CommandInterpreterRulerV2 ruler, out ICommandArg result, params ICommandArg[] args)
        {
            
            result = null;
            try
            {
                ConstructorInfo bestMatch = ruler.FindBestMatch(Value.GetConstructors(), args);
                if (bestMatch != null)
                {   
                    object[] convertedArgs = CommandInterpreterHelper.ConvertArgsWitdhDefaults(args, bestMatch.GetParameters());
                    result = CommandArgFactory.Wrap(bestMatch.Invoke(convertedArgs));
                }
                if (result == null && args.Length == 0)
                {
                    result = CommandArgFactory.Wrap(Activator.CreateInstance(this.Value));
                }
                if (result == null)
                {
                    result = CommandInterpreter_ErrorArg.Create(ErrorCodes.InvalidArgumentCount, $"未找到匹配的构造函数: {Value.Name}");
                    return ErrorCodes.InvalidArgumentCount;
                }
                return ErrorCodes.Success;
            }
            catch (Exception ex)
            {
                result = CommandInterpreter_ErrorArg.Create(ErrorCodes.UnknownError, ex.InnerException?.Message ?? ex.Message);
                return ErrorCodes.UnknownError;
            }
        }
        public ICommandArg InvokeConstructor(CommandInterpreterRulerV2 ruler, ICommandArg[] args)
        {
            Invoke(ruler, out ICommandArg result, args);
            return result;
        }
    }
}