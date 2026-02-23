using System;
using System.Collections.Generic;

namespace EventFramework.AST
{
    /// <summary>
    /// AST 执行器 - 遍历 AST 并执行，返回 ICommandArg
    /// </summary>
    public class ASTEvaluator : IASTVisitor<ICommandArg>
    {
        private readonly CommandInterpreterRuler _ruler;
        private readonly Dictionary<string, ICommandArg> _variables;
        private readonly Dictionary<string, Func<ICommandArg>> _presetVariables;

        public ASTEvaluator(
    CommandInterpreterRuler ruler,
          Dictionary<string, ICommandArg> variables,
    Dictionary<string, Func<ICommandArg>> presetVariables)
        {
            _ruler = ruler;
            _variables = variables;
            _presetVariables = presetVariables;
        }

        public ICommandArg Evaluate(ASTNode node)
        {
            if (node == null)
                return CommandInterpreter_ErrorArg.Create(ErrorCodes.ParseError, "Null AST node");

            return node.Accept(this);
        }

        #region 字面量访问

        public ICommandArg VisitIntegerLiteral(IntegerLiteralNode node)
        {
            return CommandInterpreter_NumericArg.FromInt(node.Value);
        }

        public ICommandArg VisitFloatLiteral(FloatLiteralNode node)
        {
            return CommandInterpreter_NumericArg.FromFloat(node.Value);
        }

        public ICommandArg VisitStringLiteral(StringLiteralNode node)
        {
            return new CommandInterpreter_StringArg(node.Value);
        }

        public ICommandArg VisitBoolLiteral(BoolLiteralNode node)
        {
            return CommandInterpreter_BoolArg.From(node.Value);
        }

        public ICommandArg VisitNullLiteral(NullLiteralNode node)
        {
            return CommandInterpreter_NullArg.Instance;
        }

        #endregion

        #region 表达式访问

        public ICommandArg VisitIdentifier(IdentifierNode node)
        {
            string name = node.Name;

            // 预设变量 (以 # 开头)
            if (name.StartsWith("#"))
            {
                if (_presetVariables.TryGetValue(name, out var getter))
                {
                    try
                    {
                        return getter();
                    }
                    catch (Exception ex)
                    {
                        return CommandInterpreter_ErrorArg.Create(ErrorCodes.UnknownError, ex.Message);
                    }
                }
                return CommandInterpreter_ErrorArg.Create(ErrorCodes.MemberNotFound, $"预设变量 {name}");
            }

            // 普通变量
            if (_variables.TryGetValue(name, out var variable))
            {
                return variable;
            }

            // 尝试作为类型名
            var typeArg = _ruler.FindType(name);
            if (typeArg != null)
            {
                return typeArg;
            }

            return CommandInterpreter_ErrorArg.Create(ErrorCodes.MemberNotFound, $"变量或类型 {name}");
        }

        public ICommandArg VisitBinaryExpression(BinaryExpressionNode node)
        {
            string op = node.Operator;

            // 短路求值
            if (op == "&&")
            {
                ICommandArg left = Evaluate(node.Left);
                if (left is CommandInterpreter_BoolArg lb && !lb.Value)
                    return CommandInterpreter_BoolArg.False;

                ICommandArg right = Evaluate(node.Right);
                if (left is CommandInterpreter_BoolArg lb2 && right is CommandInterpreter_BoolArg rb)
                    return CommandInterpreter_BoolArg.From(lb2.Value && rb.Value);

                return CommandInterpreter_ErrorArg.Create(ErrorCodes.InvalidArgumentType, "&& 运算符需要布尔值");
            }

            if (op == "||")
            {
                ICommandArg left = Evaluate(node.Left);
                if (left is CommandInterpreter_BoolArg lb && lb.Value)
                    return CommandInterpreter_BoolArg.True;

                ICommandArg right = Evaluate(node.Right);
                if (left is CommandInterpreter_BoolArg lb2 && right is CommandInterpreter_BoolArg rb)
                    return CommandInterpreter_BoolArg.From(lb2.Value || rb.Value);

                return CommandInterpreter_ErrorArg.Create(ErrorCodes.InvalidArgumentType, "|| 运算符需要布尔值");
            }

            // 普通二元运算
            ICommandArg leftVal = Evaluate(node.Left);
            if (leftVal.IsError()) return leftVal;

            ICommandArg rightVal = Evaluate(node.Right);
            if (rightVal.IsError()) return rightVal;

            return ApplyBinaryOperator(leftVal, op, rightVal);
        }

        private ICommandArg ApplyBinaryOperator(ICommandArg left, string op, ICommandArg right)
        {
            // 相等运算符
            if (op == "==" || op == "!=")
            {
                bool eq = Equals(left.GetRawValue(), right.GetRawValue());
                return CommandInterpreter_BoolArg.From(op == "==" ? eq : !eq);
            }

            // 字符串连接
            if (op == "+" && (left is CommandInterpreter_StringArg || right is CommandInterpreter_StringArg))
            {
                return new CommandInterpreter_StringArg(
                         (left.GetRawValue()?.ToString() ?? "null") +
                  (right.GetRawValue()?.ToString() ?? "null"));
            }

            // 数值运算
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

            // 尝试运算符重载
            ICommandArg overloadResult = TryInvokeOperatorOverload(left, op, right);
            if (overloadResult != null)
                return overloadResult;

            return CommandInterpreter_ErrorArg.Create(ErrorCodes.InvalidArgumentType,
              $"无法对 {left.GetType().Name} 和 {right.GetType().Name} 执行 {op} 运算");
        }

        private ICommandArg TryInvokeOperatorOverload(ICommandArg left, string op, ICommandArg right)
        {
            if (!_ruler.OperatorMethodNames.TryGetValue(op, out string methodName))
                return null;

            object leftRaw = left.GetRawValue();
            object rightRaw = right.GetRawValue();

            if (leftRaw == null || rightRaw == null)
                return null;

            var leftTypeArg = new CommandInterpreter_TypeArg(leftRaw.GetType());
            var rightTypeArg = new CommandInterpreter_TypeArg(rightRaw.GetType());

            ICommandArg leftMethod = leftTypeArg.GetMember(methodName);
            if (!leftMethod.IsError() && leftMethod.IsFunctor && leftMethod is IFunctor leftFunctor)
            {
                ICommandArg[] args = new ICommandArg[] { left, right };
                leftFunctor.Invoke(_ruler, out ICommandArg result, args);
                if (!result.IsError())
                    return result;
            }

            ICommandArg rightMethod = rightTypeArg.GetMember(methodName);
            if (!rightMethod.IsError() && rightMethod.IsFunctor && rightMethod is IFunctor rightFunctor)
            {
                ICommandArg[] args = new ICommandArg[] { left, right };
                rightFunctor.Invoke(_ruler, out ICommandArg result, args);
                if (!result.IsError())
                    return result;
            }

            return null;
        }

        public ICommandArg VisitUnaryExpression(UnaryExpressionNode node)
        {
            ICommandArg operand = Evaluate(node.Operand);
            if (operand.IsError())
                return operand;

            switch (node.Operator)
            {
                case "!":
                    if (operand is CommandInterpreter_BoolArg b)
                        return CommandInterpreter_BoolArg.From(!b.Value);
                    return CommandInterpreter_ErrorArg.Create(ErrorCodes.InvalidArgumentType, "! 运算符需要布尔值");

                case "-":
                    if (operand is INumeric num)
                    {
                        if (num.IsInteger)
                            return CommandInterpreter_NumericArg.FromInt(-num.ToLong());
                        else
                            return CommandInterpreter_NumericArg.FromFloat(-num.ToDouble());
                    }
                    return CommandInterpreter_ErrorArg.Create(ErrorCodes.InvalidArgumentType, "- 运算符需要数值");

                default:
                    return CommandInterpreter_ErrorArg.Create(ErrorCodes.InvalidArgumentType, $"未知的一元运算符: {node.Operator}");
            }
        }

        public ICommandArg VisitMemberAccess(MemberAccessNode node)
        {
            ICommandArg target = Evaluate(node.Target);
            if (target.IsError())
                return target;

            if (target is IMemberAccessible accessible)
            {
                return accessible.GetMember(node.MemberName);
            }

            return CommandInterpreter_ErrorArg.Create(ErrorCodes.InvalidArgumentType,
       $"{node.Target} 不支持成员访问");
        }

        public ICommandArg VisitIndexAccess(IndexAccessNode node)
        {
            ICommandArg target = Evaluate(node.Target);
            if (target.IsError())
                return target;

            ICommandArg index = Evaluate(node.Index);
            if (index.IsError())
                return index;

            if (target is IIndexable indexable)
            {
                return indexable.GetAt(index);
            }

            return CommandInterpreter_ErrorArg.Create(ErrorCodes.InvalidArgumentType,
                   $"对象不是可索引类型");
        }

        public ICommandArg VisitMethodCall(MethodCallNode node)
        {
            ICommandArg target = Evaluate(node.Target);
            if (target.IsError())
                return target;

            if (!(target is IMemberAccessible accessible))
            {
                return CommandInterpreter_ErrorArg.Create(ErrorCodes.InvalidArgumentType,
        $"目标不支持成员访问");
            }

            ICommandArg method = accessible.GetMember(node.MethodName);
            if (method.IsError())
                return method;

            // 处理泛型方法
            if (node.GenericTypeArgs != null && node.GenericTypeArgs.Count > 0 &&
 method is CommandInterpreter_MethodGroupArg methodGroup)
            {
                Type[] genericTypes = ParseGenericTypes(node.GenericTypeArgs);
                if (genericTypes == null)
                {
                    return CommandInterpreter_ErrorArg.Create(ErrorCodes.TypeNotFound,
                     $"无法解析泛型类型参数");
                }
                method = methodGroup.WithGenericTypes(genericTypes);
            }

            if (method.IsFunctor && method is IFunctor functor)
            {
                ICommandArg[] args = EvaluateArguments(node.Arguments);
                functor.Invoke(_ruler, out ICommandArg result, args);
                return result;
            }

            return CommandInterpreter_ErrorArg.Create(ErrorCodes.NotCallable, node.MethodName);
        }

        public ICommandArg VisitFunctionCall(FunctionCallNode node)
        {
            // 先检查是否是类型构造函数调用
            if (node.Function is IdentifierNode idNode)
            {
                CommandInterpreter_TypeArg type = _ruler.FindType(idNode.Name);
                if (type != null)
                {
                    ICommandArg[] args = EvaluateArguments(node.Arguments);
                    return type.InvokeConstructor(_ruler, args);
                }
            }

            ICommandArg func = Evaluate(node.Function);
            if (func.IsError())
                return func;

            // 处理泛型
            if (node.GenericTypeArgs != null && node.GenericTypeArgs.Count > 0 &&
        func is CommandInterpreter_MethodGroupArg methodGroup)
            {
                Type[] genericTypes = ParseGenericTypes(node.GenericTypeArgs);
                if (genericTypes == null)
                {
                    return CommandInterpreter_ErrorArg.Create(ErrorCodes.TypeNotFound,
                                $"无法解析泛型类型参数");
                }
                func = methodGroup.WithGenericTypes(genericTypes);
            }

            if (func.IsFunctor && func is IFunctor functor)
            {
                ICommandArg[] args = EvaluateArguments(node.Arguments);
                functor.Invoke(_ruler, out ICommandArg result, args);
                return result;
            }

            return CommandInterpreter_ErrorArg.Create(ErrorCodes.NotCallable, "对象不可调用");
        }

        public ICommandArg VisitNewExpression(NewExpressionNode node)
        {
            string typeName = node.TypeName;

            // 处理泛型类型 - 只有当 TypeName 不包含泛型信息时才添加
            // Parser 已经将泛型参数拼接到 TypeName 中了
            if (node.GenericTypeArgs != null && node.GenericTypeArgs.Count > 0 && !typeName.Contains("<"))
            {
                typeName += "<" + string.Join(", ", node.GenericTypeArgs) + ">";
            }

            CommandInterpreter_TypeArg type = _ruler.FindType(typeName);
            if (type == null)
            {
                return CommandInterpreter_ErrorArg.Create(ErrorCodes.TypeNotFound, typeName);
            }

            ICommandArg[] args = EvaluateArguments(node.Arguments);
            return type.InvokeConstructor(_ruler, args);
        }

        public ICommandArg VisitNewArrayExpression(NewArrayExpressionNode node)
        {
            Type elementType = _ruler.FindRawType(node.ElementTypeName);
            if (elementType == null)
            {
                return CommandInterpreter_ErrorArg.Create(ErrorCodes.TypeNotFound, node.ElementTypeName);
            }

            ICommandArg sizeArg = Evaluate(node.Size);
            if (sizeArg.IsError())
                return sizeArg;

            if (!(sizeArg is INumeric num))
            {
                return CommandInterpreter_ErrorArg.Create(ErrorCodes.InvalidArgumentType, "数组长度必须是整数");
            }

            int length = (int)num.ToLong();
            if (length < 0)
            {
                return CommandInterpreter_ErrorArg.Create(ErrorCodes.InvalidArgumentType, "数组长度不能为负数");
            }

            return CommandArgFactory.Wrap(Array.CreateInstance(elementType, length));
        }

        public ICommandArg VisitAssignment(AssignmentNode node)
        {
            ICommandArg value = Evaluate(node.Value);
            if (value.IsError())
                return value;

            return AssignValue(node.Target, value);
        }

        private ICommandArg AssignValue(ASTNode target, ICommandArg value)
        {
            // 简单变量赋值
            if (target is IdentifierNode idNode)
            {
                string name = idNode.Name;
                if (name.StartsWith("#"))
                {
                    return CommandInterpreter_ErrorArg.Create(ErrorCodes.InvalidArgumentType,
              $"预设变量 {name} 是只读的");
                }
                _variables[name] = value;
                return value;
            }

            // 成员赋值
            if (target is MemberAccessNode memberNode)
            {
                ICommandArg targetObj = Evaluate(memberNode.Target);
                if (targetObj.IsError())
                    return targetObj;

                if (targetObj is IMemberAccessible accessible)
                {
                    if (accessible.SetMember(memberNode.MemberName, value))
                    {
                        // 更新值类型变量
                        UpdateValueTypeVariable(memberNode.Target, targetObj);
                        return value;
                    }
                    return CommandInterpreter_ErrorArg.Create(ErrorCodes.MemberNotFound,
               $"无法设置成员 {memberNode.MemberName}");
                }
                return CommandInterpreter_ErrorArg.Create(ErrorCodes.InvalidArgumentType,
                       $"目标不支持成员赋值");
            }

            // 索引赋值
            if (target is IndexAccessNode indexNode)
            {
                ICommandArg container = Evaluate(indexNode.Target);
                if (container.IsError())
                    return container;

                ICommandArg index = Evaluate(indexNode.Index);
                if (index.IsError())
                    return index;

                if (container is IIndexable indexable)
                {
                    if (indexable.SetAt(index, value))
                        return value;
                    return CommandInterpreter_ErrorArg.Create(ErrorCodes.IndexOutOfRange, "索引赋值失败");
                }
                return CommandInterpreter_ErrorArg.Create(ErrorCodes.InvalidArgumentType,
            $"对象不是可索引类型");
            }

            return CommandInterpreter_ErrorArg.Create(ErrorCodes.InvalidArgumentType,
                $"无法赋值到目标");
        }

        private void UpdateValueTypeVariable(ASTNode target, ICommandArg value)
        {
            if (target is IdentifierNode idNode && _variables.ContainsKey(idNode.Name))
            {
                _variables[idNode.Name] = value;
            }
        }

        public ICommandArg VisitError(ErrorNode node)
        {
            return CommandInterpreter_ErrorArg.Create(ErrorCodes.ParseError, node.Message);
        }

        #endregion

        #region 辅助方法

        private ICommandArg[] EvaluateArguments(List<ASTNode> argNodes)
        {
            if (argNodes == null || argNodes.Count == 0)
                return Array.Empty<ICommandArg>();

            var args = new ICommandArg[argNodes.Count];
            for (int i = 0; i < argNodes.Count; i++)
            {
                args[i] = Evaluate(argNodes[i]);
            }
            return args;
        }

        private Type[] ParseGenericTypes(List<string> typeNames)
        {
            if (typeNames == null || typeNames.Count == 0)
                return null;

            var types = new Type[typeNames.Count];
            for (int i = 0; i < typeNames.Count; i++)
            {
                types[i] = _ruler.FindRawType(typeNames[i]);
                if (types[i] == null)
                    return null;
            }
            return types;
        }

        #endregion
    }
}
