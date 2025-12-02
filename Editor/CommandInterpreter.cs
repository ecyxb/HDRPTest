using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

/// <summary>
/// 支持临时变量存储、深层引用、委托调用、Type静态成员访问的命令解释器
/// </summary>
public class CommandInterpreter
{
    // 临时变量存储
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

        // 赋值语句（如 var1=player.state_comp.states[0]）
        int assignIdx = input.IndexOf('=');
        if (assignIdx > 0)
        {
            string left = input.Substring(0, assignIdx).Trim();
            string right = input.Substring(assignIdx + 1).Trim();

            // 右侧表达式求值
            object value = EvaluateExpression(right);
            if (value == null) return $"赋值失败: {right} 结果为 null";

            variables[left] = value;
            return $"变量 {left} 已赋值";
        }
        else
        {
            // 非赋值语句，表达式求值或委托调用
            object result = EvaluateExpression(input);
            return result != null ? $"结果: {result}" : "表达式执行失败";
        }
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
                return del.DynamicInvoke(args);
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
        var args = argsExpr.Split(',');
        List<object> result = new List<object>();
        foreach (var arg in args)
        {
            string a = arg.Trim();
            // 支持变量引用或基本类型
            if (variables.ContainsKey(a))
                result.Add(variables[a]);
            else if (int.TryParse(a, out int i))
                result.Add(i);
            else if (float.TryParse(a, out float f))
                result.Add(f);
            else if (bool.TryParse(a, out bool b))
                result.Add(b);
            else if (a.StartsWith("\"") && a.EndsWith("\""))
                result.Add(a.Substring(1, a.Length - 2));
            else
                result.Add(a); // 默认字符串
        }
        return result.ToArray();
    }

    /// <summary>
    /// 递归解析深层引用表达式
    /// </summary>
    private object ResolveDeepReference(string expr)
    {
        // 支持数组/列表索引，如 states[0]
        int bracketIdx = expr.IndexOf('[');
        if (bracketIdx > 0 && expr.EndsWith("]"))
        {
            string before = expr.Substring(0, bracketIdx);
            string indexStr = expr.Substring(bracketIdx + 1, expr.Length - bracketIdx - 2);
            object container = ResolveDeepReference(before);
            int idx = int.TryParse(indexStr, out int i) ? i : 0;
            if (container is IList list && idx < list.Count)
                return list[idx];
            return $"索引访问失败: {expr}";
        }

        // 按点分割，逐层解析
        var parts = expr.Split('.');
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
            //Type type = Type.GetType(first);
            //if (type != null)
            //    current = type;
            //else
            //    return $"未找到变量或类型: {first}";
            Type type = Type.GetType(first);
            if (type == null)
            {
                type = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == first || t.FullName == first);
            }
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
                // 静态属性/字段
                var prop = t.GetProperty(part, BindingFlags.Static | BindingFlags.Public);
                if (prop != null)
                {
                    current = prop.GetValue(null);
                    continue;
                }
                var field = t.GetField(part, BindingFlags.Static | BindingFlags.Public);
                if (field != null)
                {
                    current = field.GetValue(null);
                    continue;
                }
                // 静态方法（返回委托）
                var method = t.GetMethod(part, BindingFlags.Static | BindingFlags.Public);
                if (method != null)
                {
                    //current = Delegate.CreateDelegate(Expression.GetDelegateType(
                    //    Array.ConvertAll(method.GetParameters(), p => p.ParameterType)
                    //    .Concat(new[] { method.ReturnType }).ToArray()), method);
                    //continue;
                    // 自动推断委托类型
                    var paramTypes = method.GetParameters().Select(p => p.ParameterType).ToList();
                    paramTypes.Add(method.ReturnType);
                    var delegateType = Expression.GetDelegateType(paramTypes.ToArray());
                    current = Delegate.CreateDelegate(delegateType, method);
                    continue;
                }
                return $"未找到类型静态成员: {part}";
            }
            else
            {
                // 实例属性/字段/方法
                var type = current.GetType();
                var prop = type.GetProperty(part, BindingFlags.Instance | BindingFlags.Public);
                if (prop != null)
                {
                    current = prop.GetValue(current);
                    continue;
                }
                var field = type.GetField(part, BindingFlags.Instance | BindingFlags.Public);
                if (field != null)
                {
                    current = field.GetValue(current);
                    continue;
                }
                var method = type.GetMethod(part, BindingFlags.Instance | BindingFlags.Public);
                if (method != null)
                {
                    current = Delegate.CreateDelegate(Expression.GetDelegateType(
                        Array.ConvertAll(method.GetParameters(), p => p.ParameterType)
                        .Concat(new[] { method.ReturnType }).ToArray()), current, method.Name);
                    continue;
                }
                return $"未找到成员: {part}";
            }
        }
        return current;
    }
}
