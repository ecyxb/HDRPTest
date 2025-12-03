using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

/// <summary>
/// 支持临时变量存储、深层引用、委托调用、Type静态成员访问的命令解释器（修复版）
/// </summary>
public class CommandInterpreter
{
    private Dictionary<string, object> variables = new Dictionary<string, object>();

    /// <summary>
    /// 注册变量（如 player），可用于初始化
    /// </summary>
    public void RegisterVariable(string name, object obj)
    {
        variables[name] = obj;
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

            // 右侧表达式求值
            object value = EvaluateExpression(right);

            // 修复 Bug #7: 区分错误和 null 值
            if (value is string error && IsErrorMessage(error))
                return $"赋值失败: {error}";

            variables[left] = value;
            return $"变量 {left} 已赋值 = {FormatValue(value)}";
        }
        else
        {
            // 非赋值语句，表达式求值或委托调用
            object result = EvaluateExpression(input);
            if (result is string error && IsErrorMessage(error))
                return error;
            return result != null ? $"结果: {FormatValue(result)}" : "表达式执行失败";
        }
    }

    /// <summary>
    /// 查找赋值运算符位置，排除比较运算符
    /// </summary>
    private int FindAssignmentOperator(string input)
    {
        int assignIdx = input.IndexOf('=');
        if (assignIdx <= 0) return -1;

        // 排除 ==, !=, <=, >=
        if (assignIdx < input.Length - 1 && input[assignIdx + 1] == '=')
            return -1;
        if (assignIdx > 0 && (input[assignIdx - 1] == '!' || input[assignIdx - 1] == '<' || input[assignIdx - 1] == '>'))
            return -1;

        return assignIdx;
    }

    /// <summary>
    /// 判断是否为错误消息
    /// </summary>
    private bool IsErrorMessage(string msg)
    {
        return msg.StartsWith("未找到") || msg.StartsWith("索引") ||
    msg.Contains("不是委托") || msg.Contains("失败") ||
 msg.StartsWith("错误");
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

        // 支持带参数的委托调用，如 func(1,2)
        int callIdx = expr.IndexOf('(');
        if (callIdx > 0 && expr.EndsWith(")"))
        {
            string funcExpr = expr.Substring(0, callIdx).Trim();
            string argsExpr = expr.Substring(callIdx + 1, expr.Length - callIdx - 2);
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
                    return $"委托调用失败: {ex.InnerException?.Message ?? ex.Message}";
                }
            }
            else
            {
                return $"表达式 {funcExpr} 不是委托类型";
            }
        }

        // 深层引用解析（如 player.state_comp.states[0]）
        return ResolveDeepReference(expr);
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
    /// 智能分割参数（考虑括号嵌套）
    /// </summary>
    private string[] SplitArguments(string argsExpr)
    {
        List<string> args = new List<string>();
        int depth = 0;
        int start = 0;

        for (int i = 0; i < argsExpr.Length; i++)
        {
            char c = argsExpr[i];

            if (c == '(' || c == '[')
                depth++;
            else if (c == ')' || c == ']')
                depth--;
            else if (c == ',' && depth == 0)
            {
                args.Add(argsExpr.Substring(start, i - start));
                start = i + 1;
            }
        }

        if (start < argsExpr.Length)
            args.Add(argsExpr.Substring(start));

        return args.ToArray();
    }

    /// <summary>
    /// 递归解析深层引用表达式
    /// </summary>
    private object ResolveDeepReference(string expr)
    {
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
                    return $"索引格式错误: [{indexStr}]";
                }
            }

            if (container is IList list)
            {
                if (idx < 0 || idx >= list.Count)
                {
                    return $"索引越界: [{idx}] (长度={list.Count})";
                }
                return list[idx];
            }

            // 支持字典索引
            if (container is IDictionary dict)
            {
                if (dict.Contains(indexObj))
                    return dict[indexObj];
                return $"字典中不存在键: {indexObj}";
            }

            return $"索引访问失败: {expr} 不是可索引类型";
        }

        // 按点分割，逐层解析
        var parts = SplitByDot(expr);
        object current = null;

        // 第一层：变量或类型
        string first = parts[0];
        if (variables.ContainsKey(first))
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
                return $"未找到变量或类型: {first}";
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
    /// 查找类型（支持简单名称和完全限定名称）
    /// </summary>
    private Type FindType(string typeName)
    {
        Type type = Type.GetType(typeName);
        if (type != null) return type;

        // 在所有程序集中查找
        type = AppDomain.CurrentDomain.GetAssemblies()
               .SelectMany(a => {
                   try { return a.GetTypes(); }
                   catch { return new Type[0]; }
               })
               .FirstOrDefault(t => t.Name == typeName || t.FullName == typeName);

        return type;
    }

    /// <summary>
    /// 解析静态成员
    /// </summary>
    private object ResolveStaticMember(Type t, string memberName)
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.Public;

        // 静态属性
        var prop = t.GetProperty(memberName, flags);
        if (prop != null)
            return prop.GetValue(null);

        // 静态字段
        var field = t.GetField(memberName, flags);
        if (field != null)
            return field.GetValue(null);

        // 静态方法（返回委托）
        var method = t.GetMethod(memberName, flags);
        if (method != null)
        {
            return CreateDelegate(method, null);
        }

        return $"未找到类型静态成员: {t.Name}.{memberName}";
    }

    /// <summary>
    /// 解析实例成员
    /// </summary>
    private object ResolveInstanceMember(object obj, string memberName)
    {
        if (obj == null)
            return "尝试访问 null 对象的成员";

        Type type = obj.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;

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
                return $"属性访问失败: {ex.Message}";
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
                return $"字段访问失败: {ex.Message}";
            }
        }

        // 实例方法（返回委托）
        var method = type.GetMethod(memberName, flags);
        if (method != null)
        {
            return CreateDelegate(method, obj);
        }

        return $"未找到成员: {type.Name}.{memberName}";
    }

    /// <summary>
    /// 创建方法委托
    /// </summary>
    private Delegate CreateDelegate(MethodInfo method, object target)
    {
        try
        {
            var paramTypes = method.GetParameters().Select(p => p.ParameterType).ToList();
            paramTypes.Add(method.ReturnType);
            var delegateType = Expression.GetDelegateType(paramTypes.ToArray());

            // 修复 Bug #6: 直接传递 MethodInfo
            if (target == null)
                return Delegate.CreateDelegate(delegateType, method);
            else
                return Delegate.CreateDelegate(delegateType, target, method);
        }
        catch (Exception ex)
        {
            return null;
        }
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
}
