using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace EventFramework.AST
{
    /// <summary>
    /// AST 树形节点数据，用于可视化
    /// </summary>
    public class ASTTreeNode
    {
        public string Label { get; set; }
        public string NodeType { get; set; }
        public Color Color { get; set; }
        public List<ASTTreeNode> Children { get; set; } = new List<ASTTreeNode>();
        public bool IsExpanded { get; set; } = true;

        public ASTTreeNode(string label, string nodeType, Color color)
        {
            Label = label;
            NodeType = nodeType;
            Color = color;
        }

        public void AddChild(ASTTreeNode child)
        {
            if (child != null)
                Children.Add(child);
        }

        public void AddChild(string label, ASTTreeNode child)
        {
            if (child != null)
            {
                var wrapper = new ASTTreeNode(label, "Label", Color.white);
                wrapper.AddChild(child);
                Children.Add(wrapper);
            }
        }
    }

    /// <summary>
    /// AST 打印器 - 用于调试和查看 AST 结构
    /// 支持文本输出和树形结构输出
    /// </summary>
    public class ASTPrinter : IASTVisitor<string>
    {
        private int _indent = 0;

        // 节点颜色定义
        public static readonly Color ColorInteger = Color.cyan;
        public static readonly Color ColorFloat = Color.cyan;
        public static readonly Color ColorString = Color.green;
        public static readonly Color ColorBool = Color.yellow;
        public static readonly Color ColorNull = Color.gray;
        public static readonly Color ColorIdentifier = new Color(1f, 0.6f, 0.2f);
        public static readonly Color ColorBinaryExpr = new Color(0.8f, 0.5f, 1f);
        public static readonly Color ColorUnaryExpr = new Color(0.8f, 0.5f, 1f);
        public static readonly Color ColorMemberAccess = new Color(0.5f, 0.8f, 1f);
        public static readonly Color ColorIndexAccess = new Color(0.5f, 0.8f, 1f);
        public static readonly Color ColorMethodCall = new Color(0.3f, 0.9f, 0.6f);
        public static readonly Color ColorFunctionCall = new Color(0.3f, 0.9f, 0.6f);
        public static readonly Color ColorNew = new Color(1f, 0.8f, 0.3f);
        public static readonly Color ColorAssignment = new Color(1f, 0.5f, 0.5f);
        public static readonly Color ColorError = Color.red;

        #region 文本输出

        public string Print(ASTNode node)
        {
            if (node == null)
                return "(null)";
            _indent = 0;
            return node.Accept(this);
        }

        private string GetIndent() => new string(' ', _indent * 2);

        private string Indent(Func<string> action)
        {
            _indent++;
            string result = action();
            _indent--;
            return result;
        }

        public string VisitIntegerLiteral(IntegerLiteralNode node)
        {
            return $"{GetIndent()}Integer: {node.Value}";
        }

        public string VisitFloatLiteral(FloatLiteralNode node)
        {
            return $"{GetIndent()}Float: {node.Value}";
        }

        public string VisitStringLiteral(StringLiteralNode node)
        {
            return $"{GetIndent()}String: \"{node.Value}\"";
        }

        public string VisitBoolLiteral(BoolLiteralNode node)
        {
            return $"{GetIndent()}Bool: {node.Value}";
        }

        public string VisitNullLiteral(NullLiteralNode node)
        {
            return $"{GetIndent()}Null";
        }

        public string VisitIdentifier(IdentifierNode node)
        {
            return $"{GetIndent()}Identifier: {node.Name}";
        }

        public string VisitBinaryExpression(BinaryExpressionNode node)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{GetIndent()}BinaryExpr: {node.Operator}");
            sb.AppendLine(Indent(() => node.Left.Accept(this)));
            sb.Append(Indent(() => node.Right.Accept(this)));
            return sb.ToString();
        }

        public string VisitUnaryExpression(UnaryExpressionNode node)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{GetIndent()}UnaryExpr: {node.Operator}");
            sb.Append(Indent(() => node.Operand.Accept(this)));
            return sb.ToString();
        }

        public string VisitMemberAccess(MemberAccessNode node)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{GetIndent()}MemberAccess: .{node.MemberName}");
            sb.Append(Indent(() => node.Target.Accept(this)));
            return sb.ToString();
        }

        public string VisitIndexAccess(IndexAccessNode node)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{GetIndent()}IndexAccess:");
            sb.AppendLine($"{GetIndent()}  Target:");
            sb.AppendLine(Indent(() => Indent(() => node.Target.Accept(this))));
            sb.AppendLine($"{GetIndent()}  Index:");
            sb.Append(Indent(() => Indent(() => node.Index.Accept(this))));
            return sb.ToString();
        }

        public string VisitMethodCall(MethodCallNode node)
        {
            var sb = new StringBuilder();
            string genericStr = node.GenericTypeArgs?.Count > 0
                     ? $"<{string.Join(", ", node.GenericTypeArgs)}>"
          : "";
            sb.AppendLine($"{GetIndent()}MethodCall: .{node.MethodName}{genericStr}()");
            sb.AppendLine($"{GetIndent()}  Target:");
            sb.AppendLine(Indent(() => Indent(() => node.Target.Accept(this))));
            if (node.Arguments.Count > 0)
            {
                sb.AppendLine($"{GetIndent()}  Arguments:");
                foreach (var arg in node.Arguments)
                {
                    sb.AppendLine(Indent(() => Indent(() => arg.Accept(this))));
                }
            }
            return sb.ToString().TrimEnd();
        }

        public string VisitFunctionCall(FunctionCallNode node)
        {
            var sb = new StringBuilder();
            string genericStr = node.GenericTypeArgs?.Count > 0
       ? $"<{string.Join(", ", node.GenericTypeArgs)}>"
     : "";
            sb.AppendLine($"{GetIndent()}FunctionCall{genericStr}:");
            sb.AppendLine($"{GetIndent()}  Function:");
            sb.AppendLine(Indent(() => Indent(() => node.Function.Accept(this))));
            if (node.Arguments.Count > 0)
            {
                sb.AppendLine($"{GetIndent()}  Arguments:");
                foreach (var arg in node.Arguments)
                {
                    sb.AppendLine(Indent(() => Indent(() => arg.Accept(this))));
                }
            }
            return sb.ToString().TrimEnd();
        }

        public string VisitNewExpression(NewExpressionNode node)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{GetIndent()}New: {node.TypeName}");
            if (node.Arguments.Count > 0)
            {
                sb.AppendLine($"{GetIndent()}  Arguments:");
                foreach (var arg in node.Arguments)
                {
                    sb.AppendLine(Indent(() => Indent(() => arg.Accept(this))));
                }
            }
            return sb.ToString().TrimEnd();
        }

        public string VisitNewArrayExpression(NewArrayExpressionNode node)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{GetIndent()}NewArray: {node.ElementTypeName}[]");
            sb.AppendLine($"{GetIndent()}  Size:");
            sb.Append(Indent(() => Indent(() => node.Size.Accept(this))));
            return sb.ToString();
        }

        public string VisitAssignment(AssignmentNode node)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{GetIndent()}Assignment:");
            sb.AppendLine($"{GetIndent()}  Target:");
            sb.AppendLine(Indent(() => Indent(() => node.Target.Accept(this))));
            sb.AppendLine($"{GetIndent()}  Value:");
            sb.Append(Indent(() => Indent(() => node.Value.Accept(this))));
            return sb.ToString();
        }

        public string VisitError(ErrorNode node)
        {
            return $"{GetIndent()}Error: {node.Message}";
        }

        #endregion

        #region 树形结构输出

        /// <summary>
        /// 将 AST 转换为树形节点结构
        /// </summary>
        public ASTTreeNode BuildTree(ASTNode node)
        {
            if (node == null)
                return new ASTTreeNode("(null)", "Null", ColorNull);

            return BuildTreeNode(node);
        }

        private ASTTreeNode BuildTreeNode(ASTNode node)
        {
            switch (node)
            {
                case IntegerLiteralNode intNode:
                    return new ASTTreeNode($"🔢 Integer: {intNode.Value}", "Integer", ColorInteger);

                case FloatLiteralNode floatNode:
                    return new ASTTreeNode($"🔢 Float: {floatNode.Value}", "Float", ColorFloat);

                case StringLiteralNode strNode:
                    return new ASTTreeNode($"📝 String: \"{strNode.Value}\"", "String", ColorString);

                case BoolLiteralNode boolNode:
                    return new ASTTreeNode($"✓ Bool: {boolNode.Value}", "Bool", ColorBool);

                case NullLiteralNode:
                    return new ASTTreeNode("⊘ Null", "Null", ColorNull);

                case IdentifierNode idNode:
                    return new ASTTreeNode($"📌 Identifier: {idNode.Name}", "Identifier", ColorIdentifier);

                case BinaryExpressionNode binNode:
                    {
                        var treeNode = new ASTTreeNode($"➕ BinaryExpr: {binNode.Operator}", "BinaryExpr", ColorBinaryExpr);
                        treeNode.AddChild("Left:", BuildTreeNode(binNode.Left));
                        treeNode.AddChild("Right:", BuildTreeNode(binNode.Right));
                        return treeNode;
                    }

                case UnaryExpressionNode unaryNode:
                    {
                        var treeNode = new ASTTreeNode($"➖ UnaryExpr: {unaryNode.Operator}", "UnaryExpr", ColorUnaryExpr);
                        treeNode.AddChild(BuildTreeNode(unaryNode.Operand));
                        return treeNode;
                    }

                case MemberAccessNode memberNode:
                    {
                        var treeNode = new ASTTreeNode($"🔗 MemberAccess: .{memberNode.MemberName}", "MemberAccess", ColorMemberAccess);
                        treeNode.AddChild("Target:", BuildTreeNode(memberNode.Target));
                        return treeNode;
                    }

                case IndexAccessNode indexNode:
                    {
                        var treeNode = new ASTTreeNode("📑 IndexAccess", "IndexAccess", ColorIndexAccess);
                        treeNode.AddChild("Target:", BuildTreeNode(indexNode.Target));
                        treeNode.AddChild("Index:", BuildTreeNode(indexNode.Index));
                        return treeNode;
                    }

                case MethodCallNode methodNode:
                    {
                        string genericStr = methodNode.GenericTypeArgs?.Count > 0
                         ? $"<{string.Join(", ", methodNode.GenericTypeArgs)}>"
                                : "";
                        var treeNode = new ASTTreeNode($"📞 MethodCall: .{methodNode.MethodName}{genericStr}()", "MethodCall", ColorMethodCall);
                        treeNode.AddChild("Target:", BuildTreeNode(methodNode.Target));
                        if (methodNode.Arguments.Count > 0)
                        {
                            var argsNode = new ASTTreeNode($"Arguments ({methodNode.Arguments.Count})", "Arguments", Color.white);
                            foreach (var arg in methodNode.Arguments)
                                argsNode.AddChild(BuildTreeNode(arg));
                            treeNode.AddChild(argsNode);
                        }
                        return treeNode;
                    }

                case FunctionCallNode funcNode:
                    {
                        string genericStr = funcNode.GenericTypeArgs?.Count > 0
                             ? $"<{string.Join(", ", funcNode.GenericTypeArgs)}>"
                            : "";
                        var treeNode = new ASTTreeNode($"📞 FunctionCall{genericStr}", "FunctionCall", ColorFunctionCall);
                        treeNode.AddChild("Function:", BuildTreeNode(funcNode.Function));
                        if (funcNode.Arguments.Count > 0)
                        {
                            var argsNode = new ASTTreeNode($"Arguments ({funcNode.Arguments.Count})", "Arguments", Color.white);
                            foreach (var arg in funcNode.Arguments)
                                argsNode.AddChild(BuildTreeNode(arg));
                            treeNode.AddChild(argsNode);
                        }
                        return treeNode;
                    }

                case NewExpressionNode newNode:
                    {
                        var treeNode = new ASTTreeNode($"🆕 New: {newNode.TypeName}", "New", ColorNew);
                        if (newNode.Arguments.Count > 0)
                        {
                            var argsNode = new ASTTreeNode($"Arguments ({newNode.Arguments.Count})", "Arguments", Color.white);
                            foreach (var arg in newNode.Arguments)
                                argsNode.AddChild(BuildTreeNode(arg));
                            treeNode.AddChild(argsNode);
                        }
                        return treeNode;
                    }

                case NewArrayExpressionNode arrayNode:
                    {
                        var treeNode = new ASTTreeNode($"🆕 NewArray: {arrayNode.ElementTypeName}[]", "NewArray", ColorNew);
                        treeNode.AddChild("Size:", BuildTreeNode(arrayNode.Size));
                        return treeNode;
                    }

                case AssignmentNode assignNode:
                    {
                        var treeNode = new ASTTreeNode("📝 Assignment", "Assignment", ColorAssignment);
                        treeNode.AddChild("Target:", BuildTreeNode(assignNode.Target));
                        treeNode.AddChild("Value:", BuildTreeNode(assignNode.Value));
                        return treeNode;
                    }

                case ErrorNode errorNode:
                    return new ASTTreeNode($"❌ Error: {errorNode.Message}", "Error", ColorError);

                default:
                    return new ASTTreeNode($"❓ Unknown: {node.GetType().Name}", "Unknown", Color.gray);
            }
        }

        #endregion
    }
}
