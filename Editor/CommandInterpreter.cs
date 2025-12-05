
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if UNITY_2017_1_OR_NEWER
using UnityEngine;
#endif
using EventFramework;

namespace EventFramework.Editor
{

    /// <summary>
    /// 方法组包装器，用于处理重载方法
    /// </summary>
    public class MethodGroup
    {
        public object Target { get; }
        public MethodInfo[] Methods { get; }

        public MethodGroup(object target, MethodInfo[] methods)
        {
            Target = target;
            Methods = methods;
        }

        /// <summary>
        /// 表示 void 方法执行成功
        /// </summary>
        public static readonly object VoidResult = new object();

        /// <summary>
        /// 根据参数选择最佳匹配的方法并调用
        /// </summary>
        public object Invoke(object[] args)
        {
            MethodInfo bestMatch = FindBestMatch(args);
            if (bestMatch == null)
            {
                string argTypes = string.Join(", ", args.Select(a => a?.GetType()?.Name ?? "null"));
                return $"Error: 未找到匹配的方法重载，参数类型: ({argTypes})";
            }

            // 转换参数类型
            var parameters = bestMatch.GetParameters();
            object[] convertedArgs = new object[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                convertedArgs[i] = ConvertArg(args[i], parameters[i].ParameterType);
            }

            object result = bestMatch.Invoke(Target, convertedArgs);

            // 如果是 void 方法，返回特殊标记
            if (bestMatch.ReturnType == typeof(void))
                return VoidResult;

            return result;
        }

        private MethodInfo FindBestMatch(object[] args)
        {
            MethodInfo bestMatch = null;
            int bestScore = -1;

            foreach (var method in Methods)
            {
                var parameters = method.GetParameters();
                if (parameters.Length != args.Length)
                    continue;

                int score = 0;
                bool match = true;

                for (int i = 0; i < args.Length; i++)
                {
                    var paramType = parameters[i].ParameterType;
                    var argType = args[i]?.GetType();

                    if (argType == null)
                    {
                        if (paramType.IsValueType) { match = false; break; }
                        score += 1;
                    }
                    else if (paramType == argType)
                    {
                        score += 10; // 完全匹配
                    }
                    else if (paramType.IsAssignableFrom(argType))
                    {
                        score += 5; // 兼容类型
                    }
                    else if (IsNumericConvertible(argType, paramType))
                    {
                        score += 3; // 数值转换
                    }
                    else
                    {
                        match = false;
                        break;
                    }
                }

                if (match && score > bestScore)
                {
                    bestScore = score;
                    bestMatch = method;
                }
            }

            return bestMatch;
        }

        private bool IsNumericConvertible(Type from, Type to)
        {
            var numericTypes = new[] { typeof(int), typeof(float), typeof(double), typeof(long),
                                   typeof(short), typeof(byte), typeof(uint), typeof(ulong),
                                   typeof(ushort), typeof(sbyte), typeof(decimal) };
            return numericTypes.Contains(from) && numericTypes.Contains(to);
        }

        private object ConvertArg(object arg, Type targetType)
        {
            if (arg == null) return null;
            if (targetType.IsAssignableFrom(arg.GetType())) return arg;
            return Convert.ChangeType(arg, targetType);
        }
    }

    /// <summary>
    /// 支持临时变量存储、深层引用、委托调用、Type静态成员访问的命令解释器（修复版）
    /// </summary>
    public class CommandInterpreter
    {
        private Dictionary<string, object> variables = new Dictionary<string, object>();
        private Dictionary<string, Func<object>> presetVariables = new Dictionary<string, Func<object>>();

        /// <summary>
        /// 注册变量（如 player），可用于初始化
        /// </summary>
        public void RegisterVariable(string name, object obj)
        {
            variables[name] = obj;
        }

        /// <summary>
        /// 注册预设变量（以#开头，只读，每次访问时动态计算）
        /// </summary>
        public void RegisterPresetVariable(string name, Func<object> getter)
        {
            if (!name.StartsWith("#"))
                name = "#" + name;
            presetVariables[name] = getter;
        }

        /// <summary>
        /// 获取预设变量的值
        /// </summary>
        private bool TryGetPresetVariable(string name, out object value)
        {
            value = null;
            if (!name.StartsWith("#"))
                return false;

            if (presetVariables.TryGetValue(name, out var getter))
            {
                try
                {
                    value = getter();
                    return true;
                }
                catch (Exception ex)
                {
                    value = $"Error: 预设变量 {name} 求值失败: {ex.Message}";
                    return true; // 返回 true 以便错误消息能被传递出去
                }
            }
            return false;
        }

        /// <summary>
        /// 获取所有预设变量名
        /// </summary>
        public IEnumerable<string> GetPresetVariableNames()
        {
            return presetVariables.Keys;
        }

        /// <summary>
        /// 处理输入命令字符串
        /// </summary>
        public string Execute(string input)
        {
            input = input.Trim();
            if (string.IsNullOrEmpty(input)) return "命令为空";

            // 修复 Bug #1: 排除 ==, !=, <=, >= 等比较运算符
            int assignIdx = FindAssignmentOperator(input);
            if (assignIdx > 0)
            {
                string left = input.Substring(0, assignIdx).Trim();
                string right = input.Substring(assignIdx + 1).Trim();

                // 禁止给 # 开头的变量赋值
                string baseVar = left.Split('.')[0].Split('[')[0].Trim();
                if (baseVar.StartsWith("#"))
                    return $"Error: 预设变量 {baseVar} 是只读的，不能赋值";

                // 右侧表达式求值
                object value = EvaluateExpression(right);

                // 修复 Bug #7: 区分错误和 null 值
                if (value is string error && IsErrorMessage(error))
                    return $"Error: 赋值失败: {error}";

                // 检查左侧是否包含成员访问（点或索引）
                if (left.Contains(".") || left.Contains("["))
                {
                    // 成员赋值，如 a.r = 0 或 list[0] = value
                    return SetMemberValue(left, value);
                }
                else
                {
                    // 简单变量赋值
                    variables[left] = value;
                    return $"变量 {left} 已赋值 = {FormatValue(value)}";
                }
            }
            else
            {
                // 非赋值语句，表达式求值或委托调用
                object result = EvaluateExpression(input);
                if (result is string error && IsErrorMessage(error))
                    return error;
                if (result == MethodGroup.VoidResult)
                    return "执行成功";
                return result != null ? $"结果: {FormatValue(result)}" : "表达式执行失败";
            }
        }

        /// <summary>
        /// 设置成员的值（支持属性、字段、索引）
        /// </summary>
        private string SetMemberValue(string memberExpr, object value)
        {
            // 找到最后一个成员访问点
            int lastDotIdx = FindLastMemberAccess(memberExpr);
            int lastBracketIdx = FindLastBracketIndex(memberExpr);

            // 判断是索引赋值还是成员赋值
            if (lastBracketIdx > lastDotIdx && memberExpr.EndsWith("]"))
            {
                // 索引赋值，如 list[0] = value 或 a.list[0] = value
                string containerExpr = memberExpr.Substring(0, lastBracketIdx);
                string indexStr = memberExpr.Substring(lastBracketIdx + 1, memberExpr.Length - lastBracketIdx - 2);

                object container = EvaluateExpression(containerExpr);
                if (container is string error && IsErrorMessage(error))
                    return $"Error: 赋值失败: {error}";

                object indexObj = EvaluateExpression(indexStr);

                if (container is IList list)
                {
                    if (!(indexObj is int idx))
                    {
                        if (!int.TryParse(indexStr, out idx))
                            return $"Error: 索引格式错误: [{indexStr}]";
                    }
                    if (idx < 0 || idx >= list.Count)
                        return $"Error: 索引越界: [{idx}] (长度={list.Count})";

                    list[idx] = value;
                    return $"{containerExpr}[{idx}] 已赋值 = {FormatValue(value)}";
                }

                if (container is IDictionary dict)
                {
                    dict[indexObj] = value;
                    return $"{containerExpr}[{indexObj}] 已赋值 = {FormatValue(value)}";
                }

                return $"Error: 索引赋值失败: {containerExpr} 不是可索引类型";
            }
            else if (lastDotIdx > 0)
            {
                // 成员赋值，如 a.r = 0
                string parentExpr = memberExpr.Substring(0, lastDotIdx);
                string memberName = memberExpr.Substring(lastDotIdx + 1);

                object parent = EvaluateExpression(parentExpr);
                if (parent is string error && IsErrorMessage(error))
                    return $"Error: 赋值失败: {error}";

                if (parent == null)
                    return "Error: 赋值失败: 尝试访问 null 对象的成员";

                Type type = parent.GetType();
                // 包含 Public 和 NonPublic（私有/保护）
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                // 尝试设置属性
                var prop = type.GetProperty(memberName, flags);
                if (prop != null && prop.CanWrite)
                {
                    try
                    {
                        object convertedValue = ConvertArgument(value, prop.PropertyType);
                        prop.SetValue(parent, convertedValue);

                        // 如果是值类型，需要更新原变量
                        UpdateValueTypeVariable(parentExpr, parent);

                        return $"{memberExpr} 已赋值 = {FormatValue(value)}";
                    }
                    catch (Exception ex)
                    {
                        return $"Error: 属性赋值失败: {ex.Message}";
                    }
                }

                // 尝试设置字段
                var field = type.GetField(memberName, flags);
                if (field != null && !field.IsInitOnly)
                {
                    try
                    {
                        object convertedValue = ConvertArgument(value, field.FieldType);
                        field.SetValue(parent, convertedValue);

                        // 如果是值类型，需要更新原变量
                        UpdateValueTypeVariable(parentExpr, parent);

                        return $"{memberExpr} 已赋值 = {FormatValue(value)}";
                    }
                    catch (Exception ex)
                    {
                        return $"Error: 字段赋值失败: {ex.Message}";
                    }
                }

                return $"Error: 未找到可写的成员: {type.Name}.{memberName}";
            }

            return $"Error: 无法解析赋值目标: {memberExpr}";
        }

        /// <summary>
        /// 更新值类型变量（处理结构体成员赋值）
        /// </summary>
        private void UpdateValueTypeVariable(string expr, object value)
        {
            // 如果表达式是简单变量名，直接更新
            if (!expr.Contains(".") && !expr.Contains("[") && variables.ContainsKey(expr))
            {
                variables[expr] = value;
            }
            // 如果是嵌套的成员访问，递归更新
            else if (expr.Contains("."))
            {
                int lastDotIdx = FindLastMemberAccess(expr);
                if (lastDotIdx > 0)
                {
                    string parentExpr = expr.Substring(0, lastDotIdx);
                    string memberName = expr.Substring(lastDotIdx + 1);

                    object parent = EvaluateExpression(parentExpr);
                    if (parent != null && !(parent is string error && IsErrorMessage(error)))
                    {
                        Type type = parent.GetType();
                        var prop = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public);
                        if (prop != null && prop.CanWrite)
                        {
                            prop.SetValue(parent, value);
                            UpdateValueTypeVariable(parentExpr, parent);
                        }
                        else
                        {
                            var field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public);
                            if (field != null)
                            {
                                field.SetValue(parent, value);
                                UpdateValueTypeVariable(parentExpr, parent);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 查找最后一个成员访问点的位置（不在括号或字符串内）
        /// </summary>
        private int FindLastMemberAccess(string expr)
        {
            int depth = 0;
            bool inString = false;
            int lastDot = -1;

            for (int i = 0; i < expr.Length; i++)
            {
                char c = expr[i];

                if (c == '"')
                    inString = !inString;
                else if (!inString)
                {
                    if (c == '(' || c == '[')
                        depth++;
                    else if (c == ')' || c == ']')
                        depth--;
                    else if (c == '.' && depth == 0)
                        lastDot = i;
                }
            }

            return lastDot;
        }

        /// <summary>
        /// 查找最后一个方括号的位置（不在字符串内）
        /// </summary>
        private int FindLastBracketIndex(string expr)
        {
            bool inString = false;
            int lastBracket = -1;
            int depth = 0;

            for (int i = 0; i < expr.Length; i++)
            {
                char c = expr[i];

                if (c == '"')
                    inString = !inString;
                else if (!inString)
                {
                    if (c == '[' && depth == 0)
                        lastBracket = i;
                    if (c == '(' || c == '[')
                        depth++;
                    else if (c == ')' || c == ']')
                        depth--;
                }
            }

            return lastBracket;
        }

        /// <summary>
        /// 查找赋值运算符位置，排除比较运算符和字符串内的等号
        /// </summary>
        private int FindAssignmentOperator(string input)
        {
            bool inString = false;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (c == '"')
                {
                    inString = !inString;
                    continue;
                }

                if (inString) continue;

                if (c == '=')
                {
                    // 排除 ==
                    if (i < input.Length - 1 && input[i + 1] == '=')
                        continue;
                    // 排除 !=, <=, >=
                    if (i > 0 && (input[i - 1] == '!' || input[i - 1] == '<' || input[i - 1] == '>'))
                        continue;

                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 判断是否为错误消息
        /// </summary>
        private bool IsErrorMessage(string msg)
        {
            return msg.StartsWith("Error:");
        }

        /// <summary>
        /// 格式化输出值
        /// </summary>
        private string FormatValue(object value)
        {
            if (value == null) return "null";

            // 处理集合类型
            if (value is IEnumerable enumerable && !(value is string))
            {
                var items = new List<string>();
                int count = 0;
                foreach (var item in enumerable)
                {
                    if (count >= 10) break;
                    items.Add(item?.ToString() ?? "null");
                    count++;
                }

                string preview = string.Join(", ", items);
                if (value is ICollection collection && collection.Count > 10)
                    preview += $"... (共 {collection.Count} 项)";

                return $"[{preview}]";
            }

            return value.ToString();
        }

        /// <summary>
        /// 递归解析并执行表达式
        /// </summary>
        private object EvaluateExpression(string expr)
        {
            expr = expr.Trim();

            // 支持 new 关键字构造对象，如 new Vector3(1,2,3)
            if (expr.StartsWith("new "))
            {
                string constructorExpr = expr.Substring(4).Trim();
                return EvaluateConstructorCall(constructorExpr);
            }

            // 支持带参数的调用，如 func(1,2) 或 Vector3(1,2,3)
            // 注意：必须在运算符检查之前，避免 Mathf.Max(0, 1) 被误判为运算符表达式
            int callIdx = FindMethodCallStart(expr);
            if (callIdx > 0 && expr.EndsWith(")"))
            {
                string funcExpr = expr.Substring(0, callIdx).Trim();
                string argsExpr = expr.Substring(callIdx + 1, expr.Length - callIdx - 2);

                // 检查是否是类型名（构造函数调用）
                Type constructorType = FindType(funcExpr);
                if (constructorType != null)
                {
                    return CreateInstance(constructorType, argsExpr);
                }

                // 否则作为委托调用
                object funcObj = EvaluateExpression(funcExpr);

                if (funcObj is Delegate del)
                {
                    object[] args = ParseArguments(argsExpr);
                    try
                    {
                        return del.DynamicInvoke(args);
                    }
                    catch (Exception ex)
                    {
                        return $"Error: 委托调用失败: {ex.InnerException?.Message ?? ex.Message}";
                    }
                }
                else if (funcObj is MethodGroup methodGroup)
                {
                    // 处理方法组（重载方法）
                    object[] args = ParseArguments(argsExpr);
                    try
                    {
                        return methodGroup.Invoke(args);
                    }
                    catch (Exception ex)
                    {
                        return $"Error: 方法调用失败: {ex.InnerException?.Message ?? ex.Message}";
                    }
                }
                else
                {
                    return $"Error: 表达式 {funcExpr} 不是委托类型";
                }
            }

            // 处理运算符表达式
            object operatorResult = TryEvaluateOperator(expr);
            if (operatorResult != null || IsOperatorExpression(expr))
            {
                return operatorResult;
            }

            // 深层引用解析（如 player.state_comp.states[0]）
            return ResolveDeepReference(expr);
        }

        /// <summary>
        /// 查找方法调用的左括号位置（排除运算符中的括号）
        /// </summary>
        private int FindMethodCallStart(string expr)
        {
            // 表达式必须以 ) 结尾
            if (!expr.EndsWith(")"))
                return -1;

            int depth = 0;
            bool inString = false;

            // 从后往前找与最后一个 ) 配对的 (
            for (int i = expr.Length - 1; i >= 0; i--)
            {
                char c = expr[i];

                if (c == '"') inString = !inString;
                if (inString) continue;

                if (c == ')')
                {
                    depth++;
                }
                else if (c == '(')
                {
                    depth--;
                    if (depth == 0)
                    {
                        // 找到了与最后一个 ) 配对的 (
                        // 检查前面是否是标识符（方法调用）
                        if (i > 0)
                        {
                            char prev = expr[i - 1];
                            if (char.IsLetterOrDigit(prev) || prev == '_' || prev == '>' || prev == ']')
                                return i;
                        }
                        return -1; // 是运算符括号，如 (1 + 2)
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// 处理 new TypeName(args) 或 new TypeName[length] 形式的构造函数调用
        /// </summary>
        private object EvaluateConstructorCall(string constructorExpr)
        {
            // 检查是否是数组创建语法，如 new int[5] 或 new Vector3[10]
            int bracketIdx = constructorExpr.IndexOf('[');
            if (bracketIdx > 0 && constructorExpr.EndsWith("]"))
            {
                return CreateArray(constructorExpr, bracketIdx);
            }

            int parenIdx = constructorExpr.IndexOf('(');

            // new Vector3 (无括号，使用默认构造函数)
            if (parenIdx < 0)
            {
                Type type = FindType(constructorExpr.Trim());
                if (type == null)
                    return $"Error: 未找到类型: {constructorExpr}";

                return CreateInstance(type, "");
            }

            // new Vector3(1,2,3)
            if (!constructorExpr.EndsWith(")"))
                return $"Error: 构造函数语法错误: {constructorExpr}";

            string typeName = constructorExpr.Substring(0, parenIdx).Trim();
            string argsExpr = constructorExpr.Substring(parenIdx + 1, constructorExpr.Length - parenIdx - 2);

            Type constructorType = FindType(typeName);
            if (constructorType == null)
                return $"Error: 未找到类型: {typeName}";

            return CreateInstance(constructorType, argsExpr);
        }

        /// <summary>
        /// 创建数组，如 new int[5] 或 new Vector3[10]
        /// </summary>
        private object CreateArray(string constructorExpr, int bracketIdx)
        {
            string typeName = constructorExpr.Substring(0, bracketIdx).Trim();
            string lengthStr = constructorExpr.Substring(bracketIdx + 1, constructorExpr.Length - bracketIdx - 2).Trim();

            // 查找元素类型
            Type elementType = FindType(typeName);
            if (elementType == null)
            {
                // 尝试解析基本类型别名
                elementType = GetPrimitiveType(typeName);
            }

            if (elementType == null)
                return $"Error: 未找到类型: {typeName}";

            // 解析数组长度
            object lengthObj = EvaluateExpression(lengthStr);
            int length;

            if (lengthObj is int len)
            {
                length = len;
            }
            else if (!int.TryParse(lengthStr, out length))
            {
                return $"Error: 数组长度格式错误: [{lengthStr}]";
            }

            if (length < 0)
                return $"Error: 数组长度不能为负数: {length}";

            try
            {
                return Array.CreateInstance(elementType, length);
            }
            catch (Exception ex)
            {
                return $"Error: 创建数组失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 获取C#基本类型别名对应的Type
        /// </summary>
        private Type GetPrimitiveType(string typeName)
        {
            switch (typeName.ToLower())
            {
                case "int": return typeof(int);
                case "float": return typeof(float);
                case "double": return typeof(double);
                case "bool": return typeof(bool);
                case "string": return typeof(string);
                case "byte": return typeof(byte);
                case "sbyte": return typeof(sbyte);
                case "short": return typeof(short);
                case "ushort": return typeof(ushort);
                case "uint": return typeof(uint);
                case "long": return typeof(long);
                case "ulong": return typeof(ulong);
                case "char": return typeof(char);
                case "decimal": return typeof(decimal);
                case "object": return typeof(object);
                default: return null;
            }
        }

        /// <summary>
        /// 创建类型实例
        /// </summary>
        private object CreateInstance(Type type, string argsExpr)
        {
            object[] args = ParseArguments(argsExpr);

            try
            {
                // 查找匹配的构造函数
                var constructors = type.GetConstructors();

                foreach (var ctor in constructors)
                {
                    var parameters = ctor.GetParameters();

                    // 参数数量匹配
                    if (parameters.Length != args.Length)
                        continue;

                    // 尝试转换参数类型
                    object[] convertedArgs = new object[args.Length];
                    bool match = true;

                    for (int i = 0; i < args.Length; i++)
                    {
                        try
                        {
                            convertedArgs[i] = ConvertArgument(args[i], parameters[i].ParameterType);
                        }
                        catch
                        {
                            match = false;
                            break;
                        }
                    }

                    if (match)
                    {
                        return ctor.Invoke(convertedArgs);
                    }
                }

                // 如果没有参数，尝试使用 Activator
                if (args.Length == 0)
                {
                    return Activator.CreateInstance(type);
                }

                return $"Error: 未找到匹配的构造函数: {type.Name}({string.Join(", ", args.Select(a => a?.GetType()?.Name ?? "null"))})";
            }
            catch (Exception ex)
            {
                return $"Error: 创建实例失败: {ex.InnerException?.Message ?? ex.Message}";
            }
        }

        /// <summary>
        /// 转换参数到目标类型
        /// </summary>
        private object ConvertArgument(object arg, Type targetType)
        {
            if (arg == null)
            {
                if (targetType.IsValueType)
                    throw new InvalidCastException("Cannot convert null to value type");
                return null;
            }

            Type argType = arg.GetType();

            // 类型完全匹配
            if (targetType.IsAssignableFrom(argType))
                return arg;

            // 数值类型转换
            if (IsNumericType(argType) && IsNumericType(targetType))
            {
                return Convert.ChangeType(arg, targetType);
            }

            // 使用 Convert
            return Convert.ChangeType(arg, targetType);
        }

        /// <summary>
        /// 判断是否为数值类型
        /// </summary>
        private bool IsNumericType(Type type)
        {
            if (type == null) return false;
            return type == typeof(int) || type == typeof(float) || type == typeof(double) ||
                   type == typeof(long) || type == typeof(short) || type == typeof(byte) ||
                   type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort) ||
                   type == typeof(sbyte) || type == typeof(decimal);
        }

        /// <summary>
        /// 解析参数字符串为对象数组
        /// </summary>
        private object[] ParseArguments(string argsExpr)
        {
            if (string.IsNullOrWhiteSpace(argsExpr)) return new object[0];

            var args = SplitArguments(argsExpr);
            List<object> result = new List<object>();

            foreach (var arg in args)
            {
                string a = arg.Trim();

                if (variables.ContainsKey(a))
                {
                    result.Add(variables[a]);
                }
                // 修复 Bug #3: 优先检测浮点数
                else if (a.Contains(".") && float.TryParse(a, out float f))
                {
                    result.Add(f);
                }
                else if (int.TryParse(a, out int i))
                {
                    result.Add(i);
                }
                else if (bool.TryParse(a, out bool b))
                {
                    result.Add(b);
                }
                else if (a.StartsWith("\"") && a.EndsWith("\"") && a.Length >= 2)
                {
                    result.Add(a.Substring(1, a.Length - 2));
                }
                else
                {
                    // 尝试作为表达式求值
                    var value = EvaluateExpression(a);
                    if (value is string error && IsErrorMessage(error))
                        result.Add(a); // 如果求值失败，作为字符串
                    else
                        result.Add(value);
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// 智能分割参数（考虑括号嵌套和字符串）
        /// </summary>
        private string[] SplitArguments(string argsExpr)
        {
            List<string> args = new List<string>();
            int depth = 0;
            int start = 0;
            bool inString = false;

            for (int i = 0; i < argsExpr.Length; i++)
            {
                char c = argsExpr[i];

                if (c == '"')
                {
                    inString = !inString;
                }
                else if (!inString)
                {
                    if (c == '(' || c == '[' || c == '<')
                        depth++;
                    else if (c == ')' || c == ']' || c == '>')
                        depth--;
                    else if (c == ',' && depth == 0)
                    {
                        args.Add(argsExpr.Substring(start, i - start));
                        start = i + 1;
                    }
                }
            }

            if (start < argsExpr.Length)
                args.Add(argsExpr.Substring(start));

            return args.ToArray();
        }

        #region 运算符支持

        /// <summary>
        /// 运算符优先级（数字越大优先级越低，越后计算）
        /// </summary>
        private static readonly string[][] OperatorsByPriority = new string[][]
        {
        new[] { "||" },                          // 逻辑或（最低优先级）
        new[] { "&&" },                          // 逻辑与
        new[] { "==", "!=" },                    // 相等比较
        new[] { "<", ">", "<=", ">=" },          // 关系比较
        new[] { "+", "-" },                      // 加减
        new[] { "*", "/", "%" },                 // 乘除模（最高优先级）
        };

        /// <summary>
        /// 检查表达式是否包含运算符
        /// </summary>
        private bool IsOperatorExpression(string expr)
        {
            int depth = 0;
            bool inString = false;

            for (int i = 0; i < expr.Length; i++)
            {
                char c = expr[i];

                if (c == '"') inString = !inString;
                if (inString) continue;

                if (c == '(' || c == '[') depth++;
                else if (c == ')' || c == ']') depth--;

                // 处理泛型尖括号（只在非运算符上下文中计入depth）
                if (c == '<' && IsGenericBracket(expr, i)) depth++;
                else if (c == '>' && depth > 0) depth--;

                if (depth == 0)
                {
                    // 检查双字符运算符
                    if (i < expr.Length - 1)
                    {
                        string twoChar = expr.Substring(i, 2);
                        if (twoChar == "||" || twoChar == "&&" || twoChar == "==" ||
                            twoChar == "!=" || twoChar == "<=" || twoChar == ">=")
                            return true;
                    }

                    // 检查单字符运算符（排除负号和方法调用）
                    if (c == '+' || c == '*' || c == '/' || c == '%')
                        return true;

                    // 减号需要特殊处理，避免与负数混淆
                    if (c == '-' && i > 0)
                    {
                        // 回溯找到非空白字符
                        int prevIdx = i - 1;
                        while (prevIdx >= 0 && char.IsWhiteSpace(expr[prevIdx]))
                            prevIdx--;
                        
                        if (prevIdx >= 0)
                        {
                            char prev = expr[prevIdx];
                            if (char.IsLetterOrDigit(prev) || prev == ')' || prev == ']')
                                return true;
                        }
                    }

                    // 比较运算符（单独的 < 或 >）
                    if ((c == '<' || c == '>') && i < expr.Length - 1 && expr[i + 1] != '=' && expr[i + 1] != '<' && expr[i + 1] != '>')
                    {
                        // 排除泛型语法
                        if (!IsGenericBracket(expr, i))
                            return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 检查是否是泛型的尖括号
        /// </summary>
        private bool IsGenericBracket(string expr, int pos)
        {
            // 简单检查：如果 < 前面是字母/数字，后面最终有匹配的 >，可能是泛型
            if (pos > 0 && char.IsLetterOrDigit(expr[pos - 1]))
            {
                int depth = 1;
                for (int i = pos + 1; i < expr.Length && depth > 0; i++)
                {
                    if (expr[i] == '<') depth++;
                    else if (expr[i] == '>') depth--;
                }
                return depth == 0;
            }
            return false;
        }

        /// <summary>
        /// 尝试计算运算符表达式
        /// </summary>
        private object TryEvaluateOperator(string expr)
        {
            // 处理括号表达式
            if (expr.StartsWith("(") && expr.EndsWith(")"))
            {
                // 检查是否是完整的括号包围
                int depth = 0;
                bool isFullyWrapped = true;
                for (int i = 0; i < expr.Length - 1; i++)
                {
                    if (expr[i] == '(') depth++;
                    else if (expr[i] == ')') depth--;
                    if (depth == 0) { isFullyWrapped = false; break; }
                }
                if (isFullyWrapped)
                {
                    return EvaluateExpression(expr.Substring(1, expr.Length - 2));
                }
            }

            // 处理一元运算符 !
            if (expr.StartsWith("!") && !expr.StartsWith("!="))
            {
                object operand = EvaluateExpression(expr.Substring(1));
                if (operand is bool b)
                    return !b;
                return $"Error: ! 运算符需要布尔值";
            }

            // 按优先级从低到高查找运算符（这样先找到的运算符后计算）
            foreach (var operators in OperatorsByPriority)
            {
                int operatorPos = FindOperatorPosition(expr, operators);
                if (operatorPos > 0)
                {
                    string op = GetOperatorAt(expr, operatorPos, operators);
                    string leftExpr = expr.Substring(0, operatorPos).Trim();
                    string rightExpr = expr.Substring(operatorPos + op.Length).Trim();

                    object left = EvaluateExpression(leftExpr);
                    object right = EvaluateExpression(rightExpr);

                    // 检查是否有错误
                    if (left is string leftErr && IsErrorMessage(leftErr))
                        return leftErr;
                    if (right is string rightErr && IsErrorMessage(rightErr))
                        return rightErr;

                    return ApplyOperator(left, op, right);
                }
            }

            return null; // 不是运算符表达式
        }

        /// <summary>
        /// 在表达式中查找指定运算符的位置（从右到左，考虑括号）
        /// </summary>
        private int FindOperatorPosition(string expr, string[] operators)
        {
            int depth = 0;
            bool inString = false;

            // 从右到左扫描，保证左结合性
            for (int i = expr.Length - 1; i >= 0; i--)
            {
                char c = expr[i];

                if (c == '"') inString = !inString;
                if (inString) continue;

                if (c == ')' || c == ']') depth++;
                else if (c == '(' || c == '[') depth--;

                // 处理泛型尖括号
                if (c == '>')
                {
                    // 检查是否是泛型的结束符
                    if (i > 0 && (char.IsLetterOrDigit(expr[i - 1]) || expr[i - 1] == '>'))
                        depth++;
                }
                else if (c == '<')
                {
                    if (depth > 0 && i > 0 && char.IsLetterOrDigit(expr[i - 1]))
                        depth--;
                }

                if (depth == 0)
                {
                    foreach (var op in operators)
                    {
                        if (i >= op.Length - 1 && MatchOperator(expr, i - op.Length + 1, op))
                        {
                            int pos = i - op.Length + 1;
                            // 确保不是表达式开头（避免一元运算符误判）
                            if (pos > 0)
                            {
                                // 对于 - 运算符，需要确保前面是操作数结尾
                                if (op == "-")
                                {
                                    // 回溯找到非空白字符
                                    int prevIdx = pos - 1;
                                    while (prevIdx >= 0 && char.IsWhiteSpace(expr[prevIdx]))
                                        prevIdx--;
                                    
                                    if (prevIdx < 0)
                                        continue;
                                    
                                    char prev = expr[prevIdx];
                                    if (!char.IsLetterOrDigit(prev) && prev != ')' && prev != ']')
                                        continue;
                                }
                                return pos;
                            }
                        }
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// 检查指定位置是否匹配运算符
        /// </summary>
        private bool MatchOperator(string expr, int pos, string op)
        {
            if (pos < 0 || pos + op.Length > expr.Length)
                return false;
            return expr.Substring(pos, op.Length) == op;
        }

        /// <summary>
        /// 获取指定位置的运算符
        /// </summary>
        private string GetOperatorAt(string expr, int pos, string[] operators)
        {
            foreach (var op in operators.OrderByDescending(o => o.Length))
            {
                if (MatchOperator(expr, pos, op))
                    return op;
            }
            return operators[0];
        }

        /// <summary>
        /// 应用运算符
        /// </summary>
        private object ApplyOperator(object left, string op, object right)
        {
            // 逻辑运算符
            if (op == "&&")
            {
                if (left is bool lb && right is bool rb)
                    return lb && rb;
                return $"Error: && 运算符需要布尔值";
            }
            if (op == "||")
            {
                if (left is bool lb && right is bool rb)
                    return lb || rb;
                return $"Error: || 运算符需要布尔值";
            }

            // 相等比较
            if (op == "==")
            {
                if (left == null && right == null) return true;
                if (left == null || right == null) return false;
                return left.Equals(right);
            }
            if (op == "!=")
            {
                if (left == null && right == null) return false;
                if (left == null || right == null) return true;
                return !left.Equals(right);
            }

            // 字符串连接
            if (op == "+" && (left is string || right is string))
            {
                return (left?.ToString() ?? "null") + (right?.ToString() ?? "null");
            }

            // 数值运算
            if (IsNumericType(left?.GetType()) && IsNumericType(right?.GetType()))
            {
                double l = Convert.ToDouble(left);
                double r = Convert.ToDouble(right);

                switch (op)
                {
                    case "+": return SimplifyNumber(l + r);
                    case "-": return SimplifyNumber(l - r);
                    case "*": return SimplifyNumber(l * r);
                    case "/":
                        if (r == 0) return "Error: 除数不能为零";
                        return SimplifyNumber(l / r);
                    case "%":
                        if (r == 0) return "Error: 除数不能为零";
                        return SimplifyNumber(l % r);
                    case "<": return l < r;
                    case ">": return l > r;
                    case "<=": return l <= r;
                    case ">=": return l >= r;
                }
            }

            // 比较运算（非数值类型）
            if (left is IComparable lc && right != null)
            {
                try
                {
                    int cmp = lc.CompareTo(right);
                    switch (op)
                    {
                        case "<": return cmp < 0;
                        case ">": return cmp > 0;
                        case "<=": return cmp <= 0;
                        case ">=": return cmp >= 0;
                    }
                }
                catch { }
            }

            return $"Error: 无法对 {left?.GetType()?.Name ?? "null"} 和 {right?.GetType()?.Name ?? "null"} 执行 {op} 运算";
        }

        /// <summary>
        /// 简化数值结果（如果是整数则返回 int）
        /// </summary>
        private object SimplifyNumber(double value)
        {
            if (value == Math.Floor(value) && value >= int.MinValue && value <= int.MaxValue)
                return (int)value;
            return (float)value;
        }

        #endregion

        /// <summary>
        /// 递归解析深层引用表达式
        /// </summary>
        private object ResolveDeepReference(string expr)
        {
            expr = expr.Trim();

            // 优先解析字面量
            if (TryParseLiteral(expr, out object literal))
                return literal;

            // 修复 Bug #2: 数组索引越界检查
            int bracketIdx = FindBracketIndex(expr);
            if (bracketIdx > 0 && expr.EndsWith("]"))
            {
                string before = expr.Substring(0, bracketIdx);
                string indexStr = expr.Substring(bracketIdx + 1, expr.Length - bracketIdx - 2);
                object container = ResolveDeepReference(before);

                // 支持变量索引
                object indexObj = EvaluateExpression(indexStr);

                if (!(indexObj is int idx))
                {
                    if (!int.TryParse(indexStr, out idx))
                    {
                        return $"Error: 索引格式错误: [{indexStr}]";
                    }
                }

                if (container is IList list)
                {
                    if (idx < 0 || idx >= list.Count)
                    {
                        return $"Error: 索引越界: [{idx}] (长度={list.Count})";
                    }
                    return list[idx];
                }

                // 支持字典索引
                if (container is IDictionary dict)
                {
                    if (dict.Contains(indexObj))
                        return dict[indexObj];
                    return $"Error: 字典中不存在键: {indexObj}";
                }

                return $"Error: 索引访问失败: {expr} 不是可索引类型";
            }

            // 按点分割，逐层解析
            var parts = SplitByDot(expr);
            object current = null;

            // 第一层：变量、预设变量或类型（可能带索引，如 vectors[0]）
            string first = parts[0];

            // 检查第一部分是否带索引（如 vectors[0]）
            int firstBracketIdx = FindBracketIndex(first);
            if (firstBracketIdx > 0)
            {
                // 带索引的情况，递归解析
                current = ResolveDeepReference(first);
                if (current is string error && IsErrorMessage(error))
                    return error;
            }
            // 优先检查预设变量（以#开头）
            else if (first.StartsWith("#"))
            {
                if (TryGetPresetVariable(first, out object presetValue))
                {
                    current = presetValue;
                }
                else
                {
                    return $"Error: 未找到预设变量: {first}";
                }
            }
            else if (variables.ContainsKey(first))
            {
                current = variables[first];
            }
            else
            {
                // Type类型支持静态成员访问
                Type type = FindType(first);
                if (type != null)
                    current = type;
                else
                    return $"Error: 未找到变量或类型: {first}";
            }

            // 后续层：字段/属性/方法
            for (int i = 1; i < parts.Length; i++)
            {
                string part = parts[i];

                // Type类型静态成员
                if (current is Type t)
                {
                    current = ResolveStaticMember(t, part);
                    if (current is string error && IsErrorMessage(error))
                        return error;
                }
                else
                {
                    current = ResolveInstanceMember(current, part);
                    if (current is string error && IsErrorMessage(error))
                        return error;
                }
            }

            return current;
        }

        /// <summary>
        /// 查找方括号索引位置（不在字符串内）
        /// </summary>
        private int FindBracketIndex(string expr)
        {
            bool inString = false;
            for (int i = 0; i < expr.Length; i++)
            {
                if (expr[i] == '"')
                    inString = !inString;
                else if (expr[i] == '[' && !inString)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// 尝试解析字面量（数字、布尔、字符串、null）
        /// </summary>
        private bool TryParseLiteral(string expr, out object result)
        {
            result = null;

            // null
            if (expr == "null")
            {
                result = null;
                return true;
            }

            // 布尔
            if (expr == "true")
            {
                result = true;
                return true;
            }
            if (expr == "false")
            {
                result = false;
                return true;
            }

            // 字符串字面量
            if (expr.StartsWith("\"") && expr.EndsWith("\"") && expr.Length >= 2)
            {
                result = expr.Substring(1, expr.Length - 2);
                return true;
            }

            // 浮点数（包含小数点或f后缀）
            if (expr.Contains(".") || expr.EndsWith("f") || expr.EndsWith("F"))
            {
                string numStr = expr.TrimEnd('f', 'F');
                if (float.TryParse(numStr, out float f))
                {
                    result = f;
                    return true;
                }
                if (double.TryParse(numStr, out double d))
                {
                    result = d;
                    return true;
                }
            }

            // 整数
            if (int.TryParse(expr, out int i))
            {
                result = i;
                return true;
            }

            // 长整数
            if (expr.EndsWith("L") || expr.EndsWith("l"))
            {
                if (long.TryParse(expr.TrimEnd('L', 'l'), out long l))
                {
                    result = l;
                    return true;
                }
            }

            // 不是字面量
            return false;
        }

        /// <summary>
        /// 按点分割表达式（不分割字符串和方法调用内的点）
        /// </summary>
        private string[] SplitByDot(string expr)
        {
            List<string> parts = new List<string>();
            int depth = 0;
            int start = 0;
            bool inString = false;

            for (int i = 0; i < expr.Length; i++)
            {
                char c = expr[i];

                if (c == '"')
                    inString = !inString;
                else if (!inString)
                {
                    if (c == '(' || c == '[')
                        depth++;
                    else if (c == ')' || c == ']')
                        depth--;
                    else if (c == '.' && depth == 0)
                    {
                        parts.Add(expr.Substring(start, i - start));
                        start = i + 1;
                    }
                }
            }

            if (start < expr.Length)
                parts.Add(expr.Substring(start));

            return parts.ToArray();
        }

        /// <summary>
        /// 查找类型（支持简单名称、完全限定名称和泛型类型）
        /// </summary>
        private Type FindType(string typeName)
        {
            // 检查是否是泛型类型，如 List<int> 或 Dictionary<string, int>
            if (typeName.Contains("<") && typeName.EndsWith(">"))
            {
                return ParseGenericType(typeName);
            }

            // 先检查基本类型别名
            Type primitiveType = GetPrimitiveType(typeName);
            if (primitiveType != null) return primitiveType;

            Type type = Type.GetType(typeName);
            if (type != null) return type;

            // 在所有程序集中查找
            type = AppDomain.CurrentDomain.GetAssemblies()
                   .SelectMany(a =>
                   {
                       try { return a.GetTypes(); }
                       catch { return new Type[0]; }
                   })
                   .FirstOrDefault(t => t.Name == typeName || t.FullName == typeName);

            return type;
        }

        /// <summary>
        /// 解析泛型类型，如 List<int>、Dictionary<string, int>
        /// </summary>
        private Type ParseGenericType(string typeName)
        {
            int angleBracketStart = typeName.IndexOf('<');
            if (angleBracketStart < 0) return null;

            string genericTypeName = typeName.Substring(0, angleBracketStart).Trim();
            string typeArgsStr = typeName.Substring(angleBracketStart + 1, typeName.Length - angleBracketStart - 2);

            // 解析泛型参数
            var typeArgs = SplitGenericArguments(typeArgsStr);

            // 查找泛型类型定义，如 List`1 或 Dictionary`2
            string genericDefName = genericTypeName + "`" + typeArgs.Length;

            Type genericDef = FindGenericTypeDefinition(genericTypeName, typeArgs.Length);
            if (genericDef == null)
                return null;

            // 解析每个类型参数
            Type[] typeArgTypes = new Type[typeArgs.Length];
            for (int i = 0; i < typeArgs.Length; i++)
            {
                typeArgTypes[i] = FindType(typeArgs[i].Trim());
                if (typeArgTypes[i] == null)
                    return null;
            }

            try
            {
                return genericDef.MakeGenericType(typeArgTypes);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// 分割泛型参数（考虑嵌套泛型）
        /// </summary>
        private string[] SplitGenericArguments(string argsStr)
        {
            List<string> args = new List<string>();
            int depth = 0;
            int start = 0;

            for (int i = 0; i < argsStr.Length; i++)
            {
                char c = argsStr[i];

                if (c == '<')
                    depth++;
                else if (c == '>')
                    depth--;
                else if (c == ',' && depth == 0)
                {
                    args.Add(argsStr.Substring(start, i - start).Trim());
                    start = i + 1;
                }
            }

            if (start < argsStr.Length)
                args.Add(argsStr.Substring(start).Trim());

            return args.ToArray();
        }

        /// <summary>
        /// 查找泛型类型定义
        /// </summary>
        private Type FindGenericTypeDefinition(string baseName, int typeParamCount)
        {
            // 常用泛型类型映射
            var commonGenerics = new Dictionary<string, Type>
        {
            { "List", typeof(List<>) },
            { "Dictionary", typeof(Dictionary<,>) },
            { "HashSet", typeof(HashSet<>) },
            { "Queue", typeof(Queue<>) },
            { "Stack", typeof(Stack<>) },
            { "LinkedList", typeof(LinkedList<>) },
            { "SortedList", typeof(SortedList<,>) },
            { "SortedDictionary", typeof(SortedDictionary<,>) },
            { "KeyValuePair", typeof(KeyValuePair<,>) },
            { "Tuple", GetTupleType(typeParamCount) },
            { "Nullable", typeof(Nullable<>) },
            { "Action", GetActionType(typeParamCount) },
            { "Func", GetFuncType(typeParamCount) },
        };

            if (commonGenerics.TryGetValue(baseName, out Type commonType))
            {
                // 验证类型参数数量匹配
                if (commonType != null && commonType.GetGenericArguments().Length == typeParamCount)
                    return commonType;
            }

            // 在所有程序集中查找
            string fullGenericName = baseName + "`" + typeParamCount;

            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return new Type[0]; }
                })
                .FirstOrDefault(t =>
                    t.IsGenericTypeDefinition &&
                    (t.Name == fullGenericName ||
                     t.Name == baseName + "`" + typeParamCount ||
                     (t.Name.StartsWith(baseName) && t.GetGenericArguments().Length == typeParamCount)));
        }

        /// <summary>
        /// 获取对应参数数量的 Tuple 类型
        /// </summary>
        private Type GetTupleType(int paramCount)
        {
            switch (paramCount)
            {
                case 1: return typeof(Tuple<>);
                case 2: return typeof(Tuple<,>);
                case 3: return typeof(Tuple<,,>);
                case 4: return typeof(Tuple<,,,>);
                case 5: return typeof(Tuple<,,,,>);
                case 6: return typeof(Tuple<,,,,,>);
                case 7: return typeof(Tuple<,,,,,,>);
                default: return null;
            }
        }

        /// <summary>
        /// 获取对应参数数量的 Action 类型
        /// </summary>
        private Type GetActionType(int paramCount)
        {
            switch (paramCount)
            {
                case 0: return typeof(Action);
                case 1: return typeof(Action<>);
                case 2: return typeof(Action<,>);
                case 3: return typeof(Action<,,>);
                case 4: return typeof(Action<,,,>);
                default: return null;
            }
        }

        /// <summary>
        /// 获取对应参数数量的 Func 类型
        /// </summary>
        private Type GetFuncType(int paramCount)
        {
            switch (paramCount)
            {
                case 1: return typeof(Func<>);
                case 2: return typeof(Func<,>);
                case 3: return typeof(Func<,,>);
                case 4: return typeof(Func<,,,>);
                case 5: return typeof(Func<,,,,>);
                default: return null;
            }
        }

        /// <summary>
        /// 解析静态成员
        /// </summary>
        private object ResolveStaticMember(Type t, string memberName)
        {
            // 包含 Public 和 NonPublic（私有/保护）
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            // 静态属性
            var prop = t.GetProperty(memberName, flags);
            if (prop != null)
                return prop.GetValue(null);

            // 静态字段
            var field = t.GetField(memberName, flags);
            if (field != null)
                return field.GetValue(null);

            // 静态方法（可能有多个重载，返回包装器）
            var methods = t.GetMethods(flags).Where(m => m.Name == memberName).ToArray();
            if (methods.Length > 0)
            {
                // 返回一个方法组包装器
                return new MethodGroup(null, methods);
            }

            return $"Error: 未找到类型静态成员: {t.Name}.{memberName}";
        }

        /// <summary>
        /// 解析实例成员
        /// </summary>
        private object ResolveInstanceMember(object obj, string memberName)
        {
            if (obj == null)
                return "Error: 尝试访问 null 对象的成员";

            Type type = obj.GetType();
            // 包含 Public 和 NonPublic（私有/保护）
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            // 检查是否是方法调用（带括号，如 Trim() 或 Substring(0, 5)）
            int parenIdx = memberName.IndexOf('(');
            if (parenIdx > 0 && memberName.EndsWith(")"))
            {
                string methodName = memberName.Substring(0, parenIdx);
                string argsStr = memberName.Substring(parenIdx + 1, memberName.Length - parenIdx - 2);
                
                var methods = type.GetMethods(flags).Where(m => m.Name == methodName).ToArray();
                if (methods.Length > 0)
                {
                    object[] args = string.IsNullOrEmpty(argsStr) ? new object[0] : ParseArguments(argsStr);
                    var methodGroup = new MethodGroup(obj, methods);
                    try
                    {
                        return methodGroup.Invoke(args);
                    }
                    catch (Exception ex)
                    {
                        return $"Error: 方法调用失败: {ex.InnerException?.Message ?? ex.Message}";
                    }
                }
                return $"Error: 未找到方法: {type.Name}.{methodName}";
            }

            // 实例属性
            var prop = type.GetProperty(memberName, flags);
            if (prop != null)
            {
                try
                {
                    return prop.GetValue(obj);
                }
                catch (Exception ex)
                {
                    return $"Error: 属性访问失败: {ex.Message}";
                }
            }

            // 实例字段
            var field = type.GetField(memberName, flags);
            if (field != null)
            {
                try
                {
                    return field.GetValue(obj);
                }
                catch (Exception ex)
                {
                    return $"Error: 字段访问失败: {ex.Message}";
                }
            }

            // 实例方法（可能有多个重载，返回包装器）
            var methods2 = type.GetMethods(flags).Where(m => m.Name == memberName).ToArray();
            if (methods2.Length > 0)
            {
                return new MethodGroup(obj, methods2);
            }

            return $"Error: 未找到成员: {type.Name}.{memberName}";
        }

        /// <summary>
        /// 获取所有已注册的变量名
        /// </summary>
        public IEnumerable<string> GetVariableNames()
        {
            return variables.Keys;
        }

        /// <summary>
        /// 获取变量值
        /// </summary>
        public object GetVariable(string name)
        {
            return variables.ContainsKey(name) ? variables[name] : null;
        }

        /// <summary>
        /// 清空所有变量
        /// </summary>
        public void ClearVariables()
        {
            variables.Clear();
        }

        /// <summary>
        /// 直接求值表达式并返回原始结果（用于测试）
        /// </summary>
        /// <param name="expr">表达式字符串</param>
        /// <returns>求值结果对象，错误时返回错误消息字符串</returns>
        public object Evaluate(string expr)
        {
            return EvaluateExpression(expr);
        }

        /// <summary>
        /// 检查一个值是否是错误消息（用于测试）
        /// </summary>
        public bool IsError(object value)
        {
            return value is string str && IsErrorMessage(str);
        }
    }

} // namespace EventFramework.Editor