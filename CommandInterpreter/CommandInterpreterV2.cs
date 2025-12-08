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

    public interface ICanRegisterPresetCommand
    {
        void RegisterPresetVariable(string name, Func<object> obj);
        void RegisterPresetFunc(string name, object func);
    }

    #region 解释器 V2

    /// <summary>
    /// 命令解释器 V2 - 使用 ICommandArg 作为变量存储类型
    /// </summary>
    public class CommandInterpreterV2 : ICanRegisterPresetCommand
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
        public void RegisterPresetFunc(string name, object func)
        {
            if (!name.StartsWith("#")) name = "#" + name;
            _presetVariables[name] = () => CommandArgFactory.Wrap(func);
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
                if (target.StartsWith("#"))
                    return $"Error: 预设变量 {target} 是只读的，不能赋值";
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

        #endregion
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
                    // 检查是否是泛型方法调用 Method<T1, T2>(args)
                    int genericStart = part.IndexOf('<');
                    int parenIdx = part.IndexOf('(');

                    if (genericStart > 0 && parenIdx > genericStart && part.EndsWith(")"))
                    {
                        // 泛型方法调用
                        string methodName = part.Substring(0, genericStart);
                        int genericEnd = FindMatchingGenericClose(part, genericStart);
                        if (genericEnd < 0 || genericEnd >= parenIdx)
                        {
                            return CommandInterpreter_ErrorArg.Create(ErrorCodes.ParseError, $"无效的泛型方法语法: {part}");
                        }

                        string genericArgsStr = part.Substring(genericStart + 1, genericEnd - genericStart - 1);
                        string argsStr = part.Substring(parenIdx + 1, part.Length - parenIdx - 2);

                        // 解析泛型类型参数
                        Type[] genericTypes = ParseGenericTypeArguments(genericArgsStr);
                        if (genericTypes == null || genericTypes.Length == 0)
                        {
                            return CommandInterpreter_ErrorArg.Create(ErrorCodes.TypeNotFound, $"无法解析泛型类型参数: {genericArgsStr}");
                        }

                        // 获取方法组
                        ICommandArg method = accessible.GetMember(methodName);
                        if (method.IsError()) return method;

                        if (method is CommandInterpreter_MethodGroupArg methodGroup)
                        {
                            // 创建带有泛型类型参数的方法组
                            var genericMethodGroup = methodGroup.WithGenericTypes(genericTypes);
                            ICommandArg[] args = ParseArguments(argsStr);
                            genericMethodGroup.Invoke(Ruler, out current, args);
                            if (current.IsError()) return current;
                        }
                        else
                        {
                            return CommandInterpreter_ErrorArg.Create(ErrorCodes.NotCallable, $"{methodName} 不是方法");
                        }
                    }
                    else if (parenIdx > 0 && part.EndsWith(")"))
                    {
                        // 普通方法调用
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
                        else
                            return CommandInterpreter_ErrorArg.Create(ErrorCodes.NotCallable, methodName);
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

        /// <summary>
        /// 查找匹配的泛型参数结束位置 '>'
        /// </summary>
        private int FindMatchingGenericClose(string expr, int start)
        {
            if (start < 0 || expr[start] != '<') return -1;

            int depth = 1;
            for (int i = start + 1; i < expr.Length; i++)
            {
                char c = expr[i];
                if (c == '<') depth++;
                else if (c == '>')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }
            return -1;
        }



        /// <summary>
        /// 从位置 end 的 > 查找匹配的 <
        /// </summary>
        private int FindMatchingOpenAngle(string expr, int end)
        {
            if (end < 0 || expr[end] != '>') return -1;

            int depth = 1;
            for (int i = end - 1; i >= 0; i--)
            {
                char c = expr[i];
                if (c == '>') depth++;
                else if (c == '<')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 检查位置 i 处的 > 是否可能是泛型的结束符
        /// </summary>
        private bool IsLikelyGenericClose(string expr, int i)
        {
            // 如果 > 后面紧跟 ( ，很可能是泛型方法调用如 Method<T>(
            if (i + 1 < expr.Length && expr[i + 1] == '(')
                return true;

            // 如果 > 后面是 > ，可能是嵌套泛型如 List<List<int>>
            if (i + 1 < expr.Length && expr[i + 1] == '>')
                return true;

            // 如果 > 后面是 , ，可能是多参数泛型如 Dictionary<K, V>
            if (i + 1 < expr.Length && expr[i + 1] == ',')
                return true;

            // 如果 > 是表达式末尾，检查整个表达式是否像泛型
            if (i == expr.Length - 1)
            {
                // 检查是否有匹配的 <，且 < 前面是标识符
                int openAngle = FindMatchingOpenAngle(expr, i);
                if (openAngle > 0 && IsIdentifierChar(expr[openAngle - 1]))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 检查位置处的 < 或 > 是否是泛型语法的一部分
        /// </summary>
        private bool IsPartOfGenericSyntax(string expr, int pos)
        {
            char c = expr[pos];

            if (c == '<')
            {
                // 检查 < 前面是否是标识符（方法名或类型名）
                if (pos > 0 && IsIdentifierChar(expr[pos - 1]))
                {
                    // 检查是否有匹配的 >
                    int closeAngle = FindMatchingGenericClose(expr, pos);
                    if (closeAngle > pos)
                    {
                        // 检查 > 后面是否是 ( ，表示泛型方法调用
                        if (closeAngle + 1 < expr.Length && expr[closeAngle + 1] == '(')
                            return true;
                        // 或者 > 是表达式末尾（泛型类型引用）
                        if (closeAngle == expr.Length - 1)
                            return true;
                    }
                }
            }
            else if (c == '>')
            {
                // 检查是否有匹配的 <
                int openAngle = FindMatchingOpenAngle(expr, pos);
                if (openAngle >= 0 && openAngle < pos)
                {
                    // 检查 < 前面是否是标识符
                    if (openAngle > 0 && IsIdentifierChar(expr[openAngle - 1]))
                        return true;
                }
            }

            return false;
        }

        private bool IsIdentifierChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }


        /// <summary>
        /// 解析泛型类型参数字符串
        /// </summary>
        private Type[] ParseGenericTypeArguments(string genericArgsStr)
        {
            var typeNames = CommandInterpreterHelper.SplitGenericArguments(genericArgsStr);
            var types = new List<Type>();

            foreach (var typeName in typeNames)
            {
                Type type = Ruler.FindRawType(typeName.Trim());
                if (type == null) return null;
                types.Add(type);
            }

            return types.ToArray();
        }

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

            // 处理相等运算符
            if (op == "==" || op == "!=")
            {
                bool eq = Equals(left.GetRawValue(), right.GetRawValue());
                return CommandInterpreter_BoolArg.From(op == "==" ? eq : !eq);
            }

            // 处理字符串链接的情况
            if (op == "+" && (left is CommandInterpreter_StringArg || right is CommandInterpreter_StringArg))
                return new CommandInterpreter_StringArg((left.GetRawValue()?.ToString() ?? "null") + (right.GetRawValue()?.ToString() ?? "null"));

            //处理内置数值运算，主要针对整形和浮点型
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

            // 尝试调用运算符重载
            ICommandArg overloadResult = TryInvokeOperatorOverload(left, op, right);
            if (overloadResult != null) return overloadResult;

            return CommandInterpreter_ErrorArg.Create(ErrorCodes.InvalidArgumentType,
                $"无法对 {left.GetType().Name} 和 {right.GetType().Name} 执行 {op} 运算");
        }



        /// <summary>
        /// 尝试调用运算符重载方法
        /// </summary>
        private ICommandArg TryInvokeOperatorOverload(ICommandArg left, string op, ICommandArg right)
        {
            if (!Ruler.OperatorMethodNames.TryGetValue(op, out string methodName))
                return null;

            object leftRaw = left.GetRawValue();
            object rightRaw = right.GetRawValue();
            ICommandArg result = null;

            if (leftRaw == null || rightRaw == null) return null;

            var leftTypeArg = new CommandInterpreter_TypeArg(leftRaw.GetType());
            var rightTypeArg = new CommandInterpreter_TypeArg(rightRaw.GetType());

            ICommandArg leftMethod = leftTypeArg.GetMember(methodName);
            if (!leftMethod.IsError() && leftMethod.IsFunctor && leftMethod is IFunctor leftFunctor)
            {
                ICommandArg[] args = new ICommandArg[] { left, right };
                leftFunctor.Invoke(Ruler, out result, args);
                if (!result.IsError()) return result;
            }

            ICommandArg rightMethod = rightTypeArg.GetMember(methodName);
            if (!rightMethod.IsError() && rightMethod.IsFunctor && rightMethod is IFunctor rightFunctor)
            {
                ICommandArg[] args = new ICommandArg[] { left, right };
                rightFunctor.Invoke(Ruler, out result, args);
                if (!result.IsError()) return result;
            }
            return null;
        }


        private int FindOperatorPosition(string expr, string[] operators)
        {
            int depth = 0;
            int angleDepth = 0; // 跟踪泛型括号深度
            bool inString = false;

            for (int i = expr.Length - 1; i >= 0; i--)
            {
                char c = expr[i];
                if (c == '"') inString = !inString;
                if (inString) continue;

                if (c == ')' || c == ']') depth++;
                else if (c == '(' || c == '[') depth--;
                else if (c == '>')
                {
                    // 检查是否是泛型的结束符（后面紧跟 '(' 或者在已有的泛型上下文中）
                    if (IsLikelyGenericClose(expr, i))
                        angleDepth++;
                }
                else if (c == '<')
                {
                    // 如果有未匹配的 >，这是泛型开始符
                    if (angleDepth > 0)
                        angleDepth--;
                }

                if (depth == 0 && angleDepth == 0)
                {
                    foreach (var op in operators)
                    {
                        if (i >= op.Length - 1 && MatchOperator(expr, i - op.Length + 1, op))
                        {
                            int pos = i - op.Length + 1;
                            if (pos > 0)
                            {
                                // 对于 < 和 > 运算符，额外检查是否是泛型语法
                                if ((op == "<" || op == ">") && IsPartOfGenericSyntax(expr, pos))
                                    continue;

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