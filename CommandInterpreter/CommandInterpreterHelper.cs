using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EventFramework
{
    public static class CommandInterpreterHelper
    {
        /// <summary>
        /// 将参数转换为目标类型
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        public static object ConvertArg(object arg, Type targetType)
        {
            if (arg == null) return null;
            if (targetType.IsAssignableFrom(arg.GetType())) return arg;
            return Convert.ChangeType(arg, targetType);
        }
        public static object[] ConvertArgsWitdhDefaults(ICommandArg[] args, ParameterInfo[] parameters)
        {
            object[] convertedArgs = new object[parameters.Length];
            int i = 0;
            for (; i < args.Length; i++)
            {
                convertedArgs[i] = ConvertArg(args[i].GetRawValue(), parameters[i].ParameterType);
            }
            for (; i < parameters.Length; i++)
            {
                convertedArgs[i] = parameters[i].DefaultValue;
            }
            return convertedArgs;
        }
        public static Type FindGenericTypeDefinition(string baseName, int typeParamCount)
        {
            var commonGenerics = new Dictionary<string, Type>
            {
                { "List", typeof(List<>) },
                { "Dictionary", typeof(Dictionary<,>) },
                { "HashSet", typeof(HashSet<>) },
            };

            if (commonGenerics.TryGetValue(baseName, out Type common) &&
                common.GetGenericArguments().Length == typeParamCount)
                return common;

            string fullName = baseName + "`" + typeParamCount;
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                .FirstOrDefault(t => t.IsGenericTypeDefinition &&
                    (t.Name == fullName || t.GetGenericArguments().Length == typeParamCount && t.Name.StartsWith(baseName)));
        }

        public static string[] SplitGenericArguments(string argsStr)
        {
            var args = new List<string>();
            int depth = 0, start = 0;

            for (int i = 0; i < argsStr.Length; i++)
            {
                char c = argsStr[i];
                if (c == '<') depth++;
                else if (c == '>') depth--;
                else if (c == ',' && depth == 0)
                {
                    args.Add(argsStr.Substring(start, i - start).Trim());
                    start = i + 1;
                }
            }
            if (start < argsStr.Length) args.Add(argsStr.Substring(start).Trim());
            return args.ToArray();
        }

        public static Type ParseGenericType(string typeName, Func<string,Type> FindRawType)
        {
            int bracketStart = typeName.IndexOf('<');
            if (bracketStart < 0) return null;

            string baseName = typeName.Substring(0, bracketStart).Trim();
            string argsStr = typeName.Substring(bracketStart + 1, typeName.Length - bracketStart - 2);

            var typeArgs = CommandInterpreterHelper.SplitGenericArguments(argsStr);
            Type genericDef = CommandInterpreterHelper.FindGenericTypeDefinition(baseName, typeArgs.Length);
            if (genericDef == null) return null;

            Type[] argTypes = new Type[typeArgs.Length];
            for (int i = 0; i < typeArgs.Length; i++)
            {
                argTypes[i] = FindRawType(typeArgs[i].Trim());
                if (argTypes[i] == null) return null;
            }

            try { return genericDef.MakeGenericType(argTypes); }
            catch { return null; }
        }
    }   
}