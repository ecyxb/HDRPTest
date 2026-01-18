using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EventFramework
{
    /// <summary>
    /// 把任意对象包装为 ICommandArg 的工厂类
    /// </summary>
    public static class CommandArgFactory
    {
        public static ICommandArg Wrap(object value)
        {
            if (value == null) return CommandInterpreter_NullArg.Instance;
            if (value is ICommandArg arg) return arg;

            if (value is bool b) return CommandInterpreter_BoolArg.From(b);
            if (value is string s) return new CommandInterpreter_StringArg(s);
            if (value is int i) return CommandInterpreter_NumericArg.FromInt(i);
            if (value is long l) return CommandInterpreter_NumericArg.FromInt(l);
            if (value is float f) return CommandInterpreter_NumericArg.FromFloat(f);
            if (value is double d) return CommandInterpreter_NumericArg.FromFloat(d);
            if (value is short sh) return CommandInterpreter_NumericArg.FromInt(sh);
            if (value is byte by) return CommandInterpreter_NumericArg.FromInt(by);
            if (value is Delegate del) return new CommandInterpreter_DelegateArg(del);
            if (value is Type t) return new CommandInterpreter_TypeArg(t);
            if (value is IDictionary dict) return new CommandInterpreter_DictArg(dict);
            if (value is IList list) return new CommandInterpreter_ListArg(list);

            return new CommandInterpreter_ObjectArg(value);
        }

        public static ICommandArg ParseLiteral(string expr)
        {
            if (expr == "null") return CommandInterpreter_NullArg.Instance;
            if (expr == "true") return CommandInterpreter_BoolArg.True;
            if (expr == "false") return CommandInterpreter_BoolArg.False;

            if (expr.StartsWith("\"") && expr.EndsWith("\"") && expr.Length >= 2)
                return new CommandInterpreter_StringArg(expr.Substring(1, expr.Length - 2));

            // 支持负数浮点数字面量
            if (expr.Contains(".") || expr.EndsWith("f") || expr.EndsWith("F"))
            {
                string numStr = expr.TrimEnd('f', 'F');
                if (double.TryParse(numStr, out double dVal)) return CommandInterpreter_NumericArg.FromFloat(dVal);
            }

            // 支持负数整数字面量
            if (int.TryParse(expr, out int iVal)) return CommandInterpreter_NumericArg.FromInt(iVal);
            if (long.TryParse(expr, out long lVal2)) return CommandInterpreter_NumericArg.FromInt(lVal2);
            if ((expr.EndsWith("L") || expr.EndsWith("l")) && long.TryParse(expr.TrimEnd('L', 'l'), out long lVal))
                return CommandInterpreter_NumericArg.FromInt(lVal);

            return null;
        }
    }


    /// <summary>成员访问辅助类，提供通用的成员访问实现</summary>
    public static class MemberAccessHelper
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public static ICommandArg GetMember(object target, Type type, string name, bool isStatic = false)
        {
            if (target == null && !isStatic) return CommandInterpreter_ErrorArg.Create(ErrorCodes.NullReference);
            var flags = isStatic ? StaticFlags : InstanceFlags;

            // 检查是否是泛型方法调用 (如 TestGenericMethod<int>)
            int genericStart = name.IndexOf('<');
            string baseName = name;
            Type[] genericTypeArgs = null;

            if (genericStart > 0 && name.EndsWith(">"))
            {
                baseName = name.Substring(0, genericStart);
                string genericArgsStr = name.Substring(genericStart + 1, name.Length - genericStart - 2);
                genericTypeArgs = ParseGenericTypeArgs(genericArgsStr);
            }

            var prop = type.GetProperty(baseName, flags);
            if (prop != null && genericTypeArgs == null)
            {
                try { return CommandArgFactory.Wrap(prop.GetValue(target)); }
                catch (Exception ex) { return CommandInterpreter_ErrorArg.Create(ErrorCodes.UnknownError, ex.Message); }
            }

            var field = type.GetField(baseName, flags);
            if (field != null && genericTypeArgs == null)
            {
                try { return CommandArgFactory.Wrap(field.GetValue(target)); }
                catch (Exception ex) { return CommandInterpreter_ErrorArg.Create(ErrorCodes.UnknownError, ex.Message); }
            }

            var methods = type.GetMethods(flags).Where(m => m.Name == baseName).ToArray();
            if (methods.Length > 0)
            {
                var methodGroup = new CommandInterpreter_MethodGroupArg(target, methods);
                // 如果有泛型类型参数，返回带有泛型参数的方法组
                if (genericTypeArgs != null && genericTypeArgs.Length > 0)
                {
                    return methodGroup.WithGenericTypes(genericTypeArgs);
                }
                return methodGroup;
            }

            return CommandInterpreter_ErrorArg.Create(ErrorCodes.MemberNotFound, $"{type.Name}.{name}");
        }

        /// <summary>
        /// 解析泛型类型参数字符串
        /// </summary>
        private static Type[] ParseGenericTypeArgs(string genericArgsStr)
        {
            var typeNames = CommandInterpreterHelper.SplitGenericArguments(genericArgsStr);
            var types = new List<Type>();

            foreach (var typeName in typeNames)
            {
                Type t = FindType(typeName.Trim());
                if (t == null) return null;
                types.Add(t);
            }

            return types.ToArray();
        }

        /// <summary>
        /// 简单的类型查找
        /// </summary>
        private static Type FindType(string typeName)
        {
            // 基本类型
            switch (typeName.ToLower())
            {
                case "int": return typeof(int);
                case "float": return typeof(float);
                case "double": return typeof(double);
                case "bool": return typeof(bool);
                case "string": return typeof(string);
                case "byte": return typeof(byte);
                case "long": return typeof(long);
                case "short": return typeof(short);
                case "char": return typeof(char);
                case "object": return typeof(object);
            }

            // 尝试直接查找
            Type t = Type.GetType(typeName);
            if (t != null) return t;

            // 在所有程序集中查找
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                .FirstOrDefault(type => type.Name == typeName || type.FullName == typeName);
        }

        public static bool SetMember(object target, Type type, string name, ICommandArg value)
        {
            if (target == null) return false;
            object rawValue = value.GetRawValue();

            var prop = type.GetProperty(name, InstanceFlags);
            if (prop != null && prop.CanWrite)
            {
                try { prop.SetValue(target, ConvertValue(rawValue, prop.PropertyType)); return true; }
                catch { return false; }
            }

            var field = type.GetField(name, InstanceFlags);
            if (field != null && !field.IsInitOnly)
            {
                try { field.SetValue(target, ConvertValue(rawValue, field.FieldType)); return true; }
                catch { return false; }
            }
            return false;
        }

        public static IEnumerable<string> GetMemberNames(Type type)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            foreach (var prop in type.GetProperties(flags)) yield return prop.Name;
            foreach (var field in type.GetFields(flags)) yield return field.Name;
        }

        private static object ConvertValue(object value, Type targetType)
        {
            if (value == null) return null;
            if (targetType.IsAssignableFrom(value.GetType())) return value;
            return Convert.ChangeType(value, targetType);
        }
    }

    public static class CommandInterpreterHelper
    {
        public const int UDP_BROADCAST_PORT = 11451;
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

        public static Type ParseGenericType(string typeName, Func<string, Type> FindRawType)
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