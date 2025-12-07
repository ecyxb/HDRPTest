using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EventFramework
{

    #region 扩展方法

    public static class CommandArgExtensions
    {
        public static bool CanNumeric(this ICommandArg arg) => arg is INumeric;
        public static bool CanString(this ICommandArg arg) => arg is IStringArg;
        public static bool IsIndexable(this ICommandArg arg) => arg is IIndexable;
        public static bool HasMembers(this ICommandArg arg) => arg is IMemberAccessible;
        public static bool IsError(this ICommandArg arg) => arg is CommandInterpreter_ErrorArg;
    }

    #endregion

    #region 错误码

    public static class ErrorCodes
    {
        public const int Success = 0;
        public const int InvalidArgumentCount = 1;
        public const int InvalidArgumentType = 2;
        public const int DivideByZero = 3;
        public const int Overflow = 4;
        public const int IndexOutOfRange = 5;
        public const int MemberNotFound = 6;
        public const int NotCallable = 7;
        public const int NullReference = 8;
        public const int TypeNotFound = 9;
        public const int ParseError = 10;
        public const int UnknownError = 99;

        private static readonly string[] Messages =
        {
            "Success", "Invalid Argument Count", "Invalid Argument Type",
            "Divide By Zero", "Overflow", "Index Out Of Range",
            "Member Not Found", "Not Callable", "Null Reference",
            "Type Not Found", "Parse Error"
        };

        public static string GetMessage(int code) =>
            code >= 0 && code < Messages.Length ? Messages[code] : "Unknown Error";
    }

    #endregion

    #region 参数类型实现


    #endregion

    #region 成员访问辅助类

    /// <summary>成员访问辅助类，提供通用的成员访问实现</summary>
    public static class MemberAccessHelper
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static ICommandArg GetMember(object target, Type type, string name)
        {
            if (target == null) return CommandInterpreter_ErrorArg.Create(ErrorCodes.NullReference);

            var prop = type.GetProperty(name, InstanceFlags);
            if (prop != null)
            {
                try { return CommandArgFactory.Wrap(prop.GetValue(target)); }
                catch (Exception ex) { return CommandInterpreter_ErrorArg.Create(ErrorCodes.UnknownError, ex.Message); }
            }

            var field = type.GetField(name, InstanceFlags);
            if (field != null)
            {
                try { return CommandArgFactory.Wrap(field.GetValue(target)); }
                catch (Exception ex) { return CommandInterpreter_ErrorArg.Create(ErrorCodes.UnknownError, ex.Message); }
            }

            var methods = type.GetMethods(InstanceFlags).Where(m => m.Name == name).ToArray();
            if (methods.Length > 0) return new CommandInterpreter_MethodGroupArg(target, methods);

            return CommandInterpreter_ErrorArg.Create(ErrorCodes.MemberNotFound, $"{type.Name}.{name}");
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

    #endregion

    #region 工厂类

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

    #endregion

    #region 解释器 V2

    /// <summary>
    /// 命令解释器 V2 - 使用 ICommandArg 作为变量存储类型
    /// </summary>
    public class CommandInterpreterV2
    {
        private readonly Dictionary<string, ICommandArg> _variables = new Dictionary<string, ICommandArg>();
        private readonly Dictionary<string, Func<ICommandArg>> _presetVariables = new Dictionary<string, Func<ICommandArg>>();
        public CommandInterpreterRulerV2 Ruler { get; private set; } = new CommandInterpreterRulerV2();

        #region 公共 API

        public void RegisterVariable(string name, object obj) =>
            _variables[name] = CommandArgFactory.Wrap(obj);

        public void RegisterPresetVariable(string name, Func<object> getter)
        {
            if (!name.StartsWith("#")) name = "#" + name;
            _presetVariables[name] = () => CommandArgFactory.Wrap(getter());
        }

        public string Execute(string input)
        {
            input = input?.Trim();
            if (string.IsNullOrEmpty(input)) return "命令为空";

            // 多语句执行支持：用分号分割
            var statements = SplitStatements(input);
            if (statements.Length > 1)
            {
                var results = new List<string>();
                foreach (var stmt in statements)
                {
                    string trimmed = stmt.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;

                    string result = ExecuteSingle(trimmed);
                    results.Add(result);

                    // 如果遇到错误，停止执行后续语句
                    if (result.StartsWith("Error:") || result.Contains("失败"))
                    {
                        results.Add("(后续语句未执行)");
                        break;
                    }
                }
                return string.Join("\n", results);
            }

            return ExecuteSingle(input);
        }

        private string ExecuteSingle(string input)
        {
            int assignIdx = FindAssignmentOperator(input);
            if (assignIdx > 0)
            {
                string left = input.Substring(0, assignIdx).Trim();
                string right = input.Substring(assignIdx + 1).Trim();

                string baseVar = left.Split('.')[0].Split('[')[0].Trim();
                if (baseVar.StartsWith("#"))
                    return $"Error: 预设变量 {baseVar} 是只读的，不能赋值";

                ICommandArg value = Evaluate(right);
                if (value.IsError()) return $"赋值失败: {value.Format()}";

                return AssignValue(left, value);
            }
            else
            {
                ICommandArg result = Evaluate(input);
                if (result.IsError()) return result.Format();
                if (result is CommandInterpreter_VoidArg) return "执行成功";
                return $"结果: {result.Format()}";
            }
        }

        /// <summary>
        /// 分割多语句
        /// </summary>
        private string[] SplitStatements(string input)
        {
            return input.Split(';');
        }

        public ICommandArg Evaluate(string expr) => EvaluateExpression(expr?.Trim() ?? string.Empty);

        public object GetVariable(string name)
        {
            if (_variables.TryGetValue(name, out var value))
            {
                return value.GetRawValue();
            }
            return null;
        }


        public IEnumerable<string> GetVariableNames() => _variables.Keys;
        public IEnumerable<string> GetPresetVariableNames() => _presetVariables.Keys;
        public void ClearVariables() => _variables.Clear();

        #endregion

        #region 赋值处理

        private string AssignValue(string target, ICommandArg value)
        {
            if (!target.Contains(".") && !target.Contains("["))
            {
                _variables[target] = value;
                return $"变量 {target} 已赋值 = {value.Format()}";
            }

            int lastDot = FindLastMemberAccess(target);
            int lastBracket = FindLastBracketIndex(target);

            if (lastBracket > lastDot && target.EndsWith("]"))
            {
                string containerExpr = target.Substring(0, lastBracket);
                string indexStr = target.Substring(lastBracket + 1, target.Length - lastBracket - 2);

                ICommandArg container = Evaluate(containerExpr);
                if (container.IsError()) return $"赋值失败: {container.Format()}";

                ICommandArg indexArg = Evaluate(indexStr);
                if (indexArg.IsError()) return $"赋值失败: {indexArg.Format()}";

                if (container is IIndexable indexable)
                {
                    if (indexable.SetAt(indexArg, value))
                        return $"{target} 已赋值 = {value.Format()}";
                    return $"Error: 索引赋值失败";
                }
                return $"Error: {containerExpr} 不是可索引类型";
            }

            if (lastDot > 0)
            {
                string parentExpr = target.Substring(0, lastDot);
                string memberName = target.Substring(lastDot + 1);

                ICommandArg parent = Evaluate(parentExpr);
                if (parent.IsError()) return $"赋值失败: {parent.Format()}";

                if (parent is IMemberAccessible accessible)
                {
                    if (accessible.SetMember(memberName, value))
                    {
                        UpdateValueTypeVariable(parentExpr, parent);
                        return $"{target} 已赋值 = {value.Format()}";
                    }
                    return $"Error: 无法设置成员 {memberName}";
                }
                return $"Error: {parentExpr} 不支持成员访问";
            }

            return $"Error: 无法解析赋值目标: {target}";
        }

        private void UpdateValueTypeVariable(string expr, ICommandArg value)
        {
            if (!expr.Contains(".") && !expr.Contains("[") && _variables.ContainsKey(expr))
                _variables[expr] = value;
        }

        #endregion

        #region 表达式求值

        private ICommandArg EvaluateExpression(string expr)
        {
            if (string.IsNullOrEmpty(expr))
                return CommandInterpreter_ErrorArg.Create(ErrorCodes.ParseError, "空表达式");

            // new 构造函数
            if (expr.StartsWith("new "))
                return EvaluateConstructor(expr.Substring(4).Trim());

            // 方法/函数调用
            int callIdx = FindMethodCallStart(expr);
            if (callIdx > 0 && expr.EndsWith(")"))
            {
                string funcExpr = expr.Substring(0, callIdx).Trim();
                string argsExpr = expr.Substring(callIdx + 1, expr.Length - callIdx - 2);

                // 只有当 funcExpr 不包含成员访问符 '.' 时，才尝试作为类型名查找
                // 这样 "str.Trim()" 不会把 "str.Trim" 当作类型名
                if (!funcExpr.Contains("."))
                {
                    CommandInterpreter_TypeArg type = Ruler.FindType(funcExpr);
                    if (type != null) return type.InvokeConstructor(Ruler, ParseArguments(argsExpr));
                }

                ICommandArg func = EvaluateExpression(funcExpr);
                if (func.IsError()) return func;

                if (func.IsFunctor && func is IFunctor functor)
                {
                    ICommandArg[] args = ParseArguments(argsExpr);
                    functor.Invoke(Ruler, out ICommandArg result, args);
                    return result;
                }
                return CommandInterpreter_ErrorArg.Create(ErrorCodes.NotCallable, funcExpr);
            }

            // 运算符表达式
            ICommandArg opResult = TryEvaluateOperator(expr);
            if (opResult != null) return opResult;

            // 深层引用
            return ResolveReference(expr);
        }

        private ICommandArg EvaluateConstructor(string constructorExpr)
        {
            int bracketIdx = constructorExpr.IndexOf('[');
            if (bracketIdx > 0 && constructorExpr.EndsWith("]"))
            {
                string typeName = constructorExpr.Substring(0, bracketIdx).Trim();
                string lengthStr = constructorExpr.Substring(bracketIdx + 1, constructorExpr.Length - bracketIdx - 2);

                Type elementType = Ruler.FindRawType(typeName);
                if (elementType == null) return CommandInterpreter_ErrorArg.Create(ErrorCodes.TypeNotFound, typeName);

                ICommandArg lengthArg = EvaluateExpression(lengthStr);
                if (!(lengthArg is INumeric num))
                    return CommandInterpreter_ErrorArg.Create(ErrorCodes.InvalidArgumentType, "数组长度必须是整数");

                int length = (int)num.ToLong();
                if (length < 0) return CommandInterpreter_ErrorArg.Create(ErrorCodes.InvalidArgumentType, "数组长度不能为负数");

                return CommandArgFactory.Wrap(Array.CreateInstance(elementType, length));
            }

            int parenIdx = constructorExpr.IndexOf('(');
            if (parenIdx < 0)
            {
                CommandInterpreter_TypeArg type = Ruler.FindType(constructorExpr.Trim());
                if (type == null) return CommandInterpreter_ErrorArg.Create(ErrorCodes.TypeNotFound, constructorExpr);
                return type.InvokeConstructor(Ruler, Array.Empty<ICommandArg>());
            }

            if (!constructorExpr.EndsWith(")"))
                return CommandInterpreter_ErrorArg.Create(ErrorCodes.ParseError, "构造函数语法错误");

            string ctorTypeName = constructorExpr.Substring(0, parenIdx).Trim();
            string ctorArgs = constructorExpr.Substring(parenIdx + 1, constructorExpr.Length - parenIdx - 2);

            CommandInterpreter_TypeArg ctorType = Ruler.FindType(ctorTypeName);
            if (ctorType == null) return CommandInterpreter_ErrorArg.Create(ErrorCodes.TypeNotFound, ctorTypeName);
            return ctorType.InvokeConstructor(Ruler, ParseArguments(ctorArgs));
        }

        private ICommandArg ResolveReference(string expr)
        {
            ICommandArg literal = CommandArgFactory.ParseLiteral(expr);
            if (literal != null) return literal;

            int bracketIdx = FindBracketIndex(expr);
            if (bracketIdx > 0 && expr.EndsWith("]"))
            {
                string containerExpr = expr.Substring(0, bracketIdx);
                string indexStr = expr.Substring(bracketIdx + 1, expr.Length - bracketIdx - 2);

                ICommandArg container = ResolveReference(containerExpr);
                if (container.IsError()) return container;

                ICommandArg indexArg = EvaluateExpression(indexStr);
                if (indexArg.IsError()) return indexArg;

                if (container is IIndexable indexable)
                {
                    return indexable.GetAt(indexArg);
                }
                return CommandInterpreter_ErrorArg.Create(ErrorCodes.InvalidArgumentType, $"{containerExpr} 不是可索引类型");
            }

            var parts = SplitByDot(expr);
            ICommandArg current = null;

            string first = parts[0];
            int firstBracket = FindBracketIndex(first);

            if (firstBracket > 0)
            {
                current = ResolveReference(first);
                if (current.IsError()) return current;
            }
            else if (first.StartsWith("#"))
            {
                // 先检查内置函数
                if (_presetVariables.TryGetValue(first, out var getter))
                {
                    try { current = getter(); }
                    catch (Exception ex) { return CommandInterpreter_ErrorArg.Create(ErrorCodes.UnknownError, ex.Message); }
                }
                else return CommandInterpreter_ErrorArg.Create(ErrorCodes.MemberNotFound, $"预设变量 {first}");
            }
            else if (_variables.TryGetValue(first, out var variable))
            {
                current = variable;
            }
            else
            {
                current = Ruler.FindType(first);
                if (current == null) return CommandInterpreter_ErrorArg.Create(ErrorCodes.MemberNotFound, $"变量或类型 {first}");
            }

            for (int i = 1; i < parts.Length; i++)
            {
                string part = parts[i];

                if (current is IMemberAccessible accessible)
                {
                    int parenIdx = part.IndexOf('(');
                    if (parenIdx > 0 && part.EndsWith(")"))
                    {
                        string methodName = part.Substring(0, parenIdx);
                        string argsStr = part.Substring(parenIdx + 1, part.Length - parenIdx - 2);

                        ICommandArg method = accessible.GetMember(methodName);
                        if (method.IsError()) return method;

                        if (method.IsFunctor && method is IFunctor functor)
                        {
                            ICommandArg[] args = ParseArguments(argsStr);
                            functor.Invoke(Ruler, out current, args);
                            if (current.IsError()) return current;
                        }
                        else return CommandInterpreter_ErrorArg.Create(ErrorCodes.NotCallable, methodName);
                    }
                    else
                    {
                        current = accessible.GetMember(part);
                        if (current.IsError()) return current;
                    }
                }
                else return CommandInterpreter_ErrorArg.Create(ErrorCodes.InvalidArgumentType, $"{parts[i - 1]} 不支持成员访问");
            }

            return current;
        }

        #endregion

        #region 运算符处理

        private static readonly string[][] OperatorsByPriority =
        {
            new[] { "||" },
            new[] { "&&" },
            new[] { "==", "!=" },
            new[] { "<", ">", "<=", ">=" },
            new[] { "+", "-" },
            new[] { "*", "/", "%" },
        };

        private ICommandArg TryEvaluateOperator(string expr)
        {
            if (expr.StartsWith("(") && expr.EndsWith(")"))
            {
                int depth = 0;
                bool isFullyWrapped = true;
                for (int i = 0; i < expr.Length - 1; i++)
                {
                    if (expr[i] == '(') depth++;
                    else if (expr[i] == ')') depth--;
                    if (depth == 0) { isFullyWrapped = false; break; }
                }
                if (isFullyWrapped)
                    return EvaluateExpression(expr.Substring(1, expr.Length - 2));
            }

            // 处理一元运算符 !
            if (expr.StartsWith("!") && !expr.StartsWith("!="))
            {
                ICommandArg operand = EvaluateExpression(expr.Substring(1));
                if (operand is CommandInterpreter_BoolArg b) return CommandInterpreter_BoolArg.From(!b.Value);
                return CommandInterpreter_ErrorArg.Create(ErrorCodes.InvalidArgumentType, "! 运算符需要布尔值");
            }

            // 处理一元负号运算符 -
            if (expr.StartsWith("-") && expr.Length > 1)
            {
                // 先尝试解析为负数字面量
                ICommandArg literal = CommandArgFactory.ParseLiteral(expr);
                if (literal != null) return literal;

                // 否则作为一元运算符处理
                ICommandArg operand = EvaluateExpression(expr.Substring(1));
                if (operand.IsError()) return operand;
                if (operand is INumeric num)
                {
                    if (num.IsInteger)
                        return CommandInterpreter_NumericArg.FromInt(-num.ToLong());
                    else
                        return CommandInterpreter_NumericArg.FromFloat(-num.ToDouble());
                }
                return CommandInterpreter_ErrorArg.Create(ErrorCodes.InvalidArgumentType, "- 运算符需要数值");
            }

            foreach (var operators in OperatorsByPriority)
            {
                int pos = FindOperatorPosition(expr, operators);
                if (pos > 0)
                {
                    string op = GetOperatorAt(expr, pos, operators);
                    string leftExpr = expr.Substring(0, pos).Trim();
                    string rightExpr = expr.Substring(pos + op.Length).Trim();

                    ICommandArg left = EvaluateExpression(leftExpr);
                    if (left.IsError()) return left;

                    ICommandArg right = EvaluateExpression(rightExpr);
                    if (right.IsError()) return right;

                    return ApplyOperator(left, op, right);
                }
            }

            return null;
        }

        private ICommandArg ApplyOperator(ICommandArg left, string op, ICommandArg right)
        {
            if (op == "&&" || op == "||")
            {
                if (left is CommandInterpreter_BoolArg lb && right is CommandInterpreter_BoolArg rb)
                    return CommandInterpreter_BoolArg.From(op == "&&" ? lb.Value && rb.Value : lb.Value || rb.Value);
                return CommandInterpreter_ErrorArg.Create(ErrorCodes.InvalidArgumentType, $"{op} 运算符需要布尔值");
            }

            if (op == "==" || op == "!=")
            {
                bool eq = Equals(left.GetRawValue(), right.GetRawValue());
                return CommandInterpreter_BoolArg.From(op == "==" ? eq : !eq);
            }

            if (op == "+" && (left is CommandInterpreter_StringArg || right is CommandInterpreter_StringArg))
                return new CommandInterpreter_StringArg((left.GetRawValue()?.ToString() ?? "null") + (right.GetRawValue()?.ToString() ?? "null"));

            if (left.CanNumeric() && right.CanNumeric())
            {
                var ln = (INumeric)left;
                var rn = (INumeric)right;

                switch (op)
                {
                    case "+": return CommandInterpreter_NumericArg.Add(ln, rn);
                    case "-": return CommandInterpreter_NumericArg.Subtract(ln, rn);
                    case "*": return CommandInterpreter_NumericArg.Multiply(ln, rn);
                    case "/": return CommandInterpreter_NumericArg.Divide(ln, rn);
                    case "%": return CommandInterpreter_NumericArg.Modulo(ln, rn);
                    case "<":
                    case ">":
                    case "<=":
                    case ">=":
                        return CommandInterpreter_NumericArg.Compare(ln, op, rn);
                }
            }

            return CommandInterpreter_ErrorArg.Create(ErrorCodes.InvalidArgumentType,
                $"无法对 {left.GetType().Name} 和 {right.GetType().Name} 执行 {op} 运算");
        }

        private int FindOperatorPosition(string expr, string[] operators)
        {
            int depth = 0;
            bool inString = false;

            for (int i = expr.Length - 1; i >= 0; i--)
            {
                char c = expr[i];
                if (c == '"') inString = !inString;
                if (inString) continue;

                if (c == ')' || c == ']') depth++;
                else if (c == '(' || c == '[') depth--;

                if (depth == 0)
                {
                    foreach (var op in operators)
                    {
                        if (i >= op.Length - 1 && MatchOperator(expr, i - op.Length + 1, op))
                        {
                            int pos = i - op.Length + 1;
                            if (pos > 0)
                            {
                                if (op == "-")
                                {
                                    int prevIdx = pos - 1;
                                    while (prevIdx >= 0 && char.IsWhiteSpace(expr[prevIdx])) prevIdx--;
                                    if (prevIdx < 0) continue;
                                    char prev = expr[prevIdx];
                                    if (!char.IsLetterOrDigit(prev) && prev != ')' && prev != ']') continue;
                                }
                                return pos;
                            }
                        }
                    }
                }
            }
            return -1;
        }

        private bool MatchOperator(string expr, int pos, string op)
        {
            if (pos < 0 || pos + op.Length > expr.Length) return false;
            return expr.Substring(pos, op.Length) == op;
        }

        private string GetOperatorAt(string expr, int pos, string[] operators)
        {
            foreach (var op in operators.OrderByDescending(o => o.Length))
                if (MatchOperator(expr, pos, op)) return op;
            return operators[0];
        }

        #endregion

        #region 参数解析

        private ICommandArg[] ParseArguments(string argsExpr)
        {
            if (string.IsNullOrWhiteSpace(argsExpr)) return Array.Empty<ICommandArg>();

            var args = SplitArguments(argsExpr);
            return args.Select(arg => EvaluateExpression(arg.Trim())).ToArray();
        }

        private string[] SplitArguments(string argsExpr)
        {
            var args = new List<string>();
            int depth = 0, start = 0;
            bool inString = false;

            for (int i = 0; i < argsExpr.Length; i++)
            {
                char c = argsExpr[i];
                if (c == '"') inString = !inString;
                else if (!inString)
                {
                    if (c == '(' || c == '[' || c == '<') depth++;
                    else if (c == ')' || c == ']' || c == '>') depth--;
                    else if (c == ',' && depth == 0)
                    {
                        args.Add(argsExpr.Substring(start, i - start));
                        start = i + 1;
                    }
                }
            }
            if (start < argsExpr.Length) args.Add(argsExpr.Substring(start));
            return args.ToArray();
        }

        private string[] SplitByDot(string expr)
        {
            var parts = new List<string>();
            int depth = 0, start = 0;
            bool inString = false;

            for (int i = 0; i < expr.Length; i++)
            {
                char c = expr[i];
                if (c == '"') inString = !inString;
                else if (!inString)
                {
                    if (c == '(' || c == '[') depth++;
                    else if (c == ')' || c == ']') depth--;
                    else if (c == '.' && depth == 0)
                    {
                        parts.Add(expr.Substring(start, i - start));
                        start = i + 1;
                    }
                }
            }
            if (start < expr.Length) parts.Add(expr.Substring(start));
            return parts.ToArray();
        }

        #endregion

        #region 辅助方法

        private int FindAssignmentOperator(string input)
        {
            bool inString = false;
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (c == '"') { inString = !inString; continue; }
                if (inString) continue;

                if (c == '=')
                {
                    if (i < input.Length - 1 && input[i + 1] == '=') continue;
                    if (i > 0 && (input[i - 1] == '!' || input[i - 1] == '<' || input[i - 1] == '>')) continue;
                    return i;
                }
            }
            return -1;
        }

        private int FindMethodCallStart(string expr)
        {
            if (!expr.EndsWith(")")) return -1;
            int depth = 0;
            bool inString = false;

            for (int i = expr.Length - 1; i >= 0; i--)
            {
                char c = expr[i];
                if (c == '"') inString = !inString;
                if (inString) continue;

                if (c == ')') depth++;
                else if (c == '(')
                {
                    depth--;
                    if (depth == 0 && i > 0)
                    {
                        char prev = expr[i - 1];
                        if (char.IsLetterOrDigit(prev) || prev == '_' || prev == '>' || prev == ']')
                            return i;
                        return -1;
                    }
                }
            }
            return -1;
        }

        private int FindBracketIndex(string expr)
        {
            bool inString = false;
            for (int i = 0; i < expr.Length; i++)
            {
                if (expr[i] == '"') inString = !inString;
                else if (expr[i] == '[' && !inString) return i;
            }
            return -1;
        }

        private int FindLastMemberAccess(string expr)
        {
            int depth = 0, lastDot = -1;
            bool inString = false;

            for (int i = 0; i < expr.Length; i++)
            {
                char c = expr[i];
                if (c == '"') inString = !inString;
                else if (!inString)
                {
                    if (c == '(' || c == '[') depth++;
                    else if (c == ')' || c == ']') depth--;
                    else if (c == '.' && depth == 0) lastDot = i;
                }
            }
            return lastDot;
        }

        private int FindLastBracketIndex(string expr)
        {
            int lastBracket = -1, depth = 0;
            bool inString = false;

            for (int i = 0; i < expr.Length; i++)
            {
                char c = expr[i];
                if (c == '"') inString = !inString;
                else if (!inString)
                {
                    if (c == '[' && depth == 0) lastBracket = i;
                    if (c == '(' || c == '[') depth++;
                    else if (c == ')' || c == ']') depth--;
                }
            }
            return lastBracket;
        }


        #endregion
    }

    #endregion
}