using System;
using System.Collections.Generic;

namespace EventFramework.AST
{
    /// <summary>
    /// AST 节点类型枚举
    /// </summary>
    public enum EASTNodeType
    {
        // 字面量
        IntegerLiteral,
        FloatLiteral,
        StringLiteral,
        BoolLiteral,
        NullLiteral,

        // 表达式
        Identifier,     // 变量/标识符
        BinaryExpression,       // 二元运算: a + b
        UnaryExpression,  // 一元运算: !a, -a
        MemberAccess,   // 成员访问: a.b
        IndexAccess, // 索引访问: a[i]
        MethodCall,             // 方法调用: a.Method(args)
        FunctionCall,// 函数调用: Func(args)
        NewExpression,          // 构造: new Type(args)
        NewArrayExpression,     // 数组构造: new Type[size]

        // 语句
        Assignment,       // 赋值: a = b
        ExpressionStatement,    // 表达式语句

        // 特殊
        Error,      // 错误节点
    }

    /// <summary>
    /// AST 节点基类
    /// </summary>
    public abstract class ASTNode
    {
        public abstract EASTNodeType NodeType { get; }
        public int Position { get; set; }

        public abstract T Accept<T>(IASTVisitor<T> visitor);
    }

    /// <summary>
    /// AST 访问者接口
    /// </summary>
    public interface IASTVisitor<T>
    {
        T VisitIntegerLiteral(IntegerLiteralNode node);
        T VisitFloatLiteral(FloatLiteralNode node);
        T VisitStringLiteral(StringLiteralNode node);
        T VisitBoolLiteral(BoolLiteralNode node);
        T VisitNullLiteral(NullLiteralNode node);
        T VisitIdentifier(IdentifierNode node);
        T VisitBinaryExpression(BinaryExpressionNode node);
        T VisitUnaryExpression(UnaryExpressionNode node);
        T VisitMemberAccess(MemberAccessNode node);
        T VisitIndexAccess(IndexAccessNode node);
        T VisitMethodCall(MethodCallNode node);
        T VisitFunctionCall(FunctionCallNode node);
        T VisitNewExpression(NewExpressionNode node);
        T VisitNewArrayExpression(NewArrayExpressionNode node);
        T VisitAssignment(AssignmentNode node);
        T VisitError(ErrorNode node);
    }

    #region 字面量节点

    public class IntegerLiteralNode : ASTNode
    {
        public override EASTNodeType NodeType => EASTNodeType.IntegerLiteral;
        public long Value { get; set; }

        public IntegerLiteralNode(long value, int position = 0)
        {
            Value = value;
            Position = position;
        }

        public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitIntegerLiteral(this);
    }

    public class FloatLiteralNode : ASTNode
    {
        public override EASTNodeType NodeType => EASTNodeType.FloatLiteral;
        public double Value { get; set; }

        public FloatLiteralNode(double value, int position = 0)
        {
            Value = value;
            Position = position;
        }

        public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitFloatLiteral(this);
    }

    public class StringLiteralNode : ASTNode
    {
        public override EASTNodeType NodeType => EASTNodeType.StringLiteral;
        public string Value { get; set; }

        public StringLiteralNode(string value, int position = 0)
        {
            Value = value;
            Position = position;
        }

        public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitStringLiteral(this);
    }

    public class BoolLiteralNode : ASTNode
    {
        public override EASTNodeType NodeType => EASTNodeType.BoolLiteral;
        public bool Value { get; set; }

        public BoolLiteralNode(bool value, int position = 0)
        {
            Value = value;
            Position = position;
        }

        public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitBoolLiteral(this);
    }

    public class NullLiteralNode : ASTNode
    {
        public override EASTNodeType NodeType => EASTNodeType.NullLiteral;

        public NullLiteralNode(int position = 0)
        {
            Position = position;
        }

        public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitNullLiteral(this);
    }

    #endregion

    #region 表达式节点

    public class IdentifierNode : ASTNode
    {
        public override EASTNodeType NodeType => EASTNodeType.Identifier;
        public string Name { get; set; }

        public IdentifierNode(string name, int position = 0)
        {
            Name = name;
            Position = position;
        }

        public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitIdentifier(this);
    }

    public class BinaryExpressionNode : ASTNode
    {
        public override EASTNodeType NodeType => EASTNodeType.BinaryExpression;
        public ASTNode Left { get; set; }
        public string Operator { get; set; }
        public ASTNode Right { get; set; }

        public BinaryExpressionNode(ASTNode left, string op, ASTNode right, int position = 0)
        {
            Left = left;
            Operator = op;
            Right = right;
            Position = position;
        }

        public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitBinaryExpression(this);
    }

    public class UnaryExpressionNode : ASTNode
    {
        public override EASTNodeType NodeType => EASTNodeType.UnaryExpression;
        public string Operator { get; set; }
        public ASTNode Operand { get; set; }

        public UnaryExpressionNode(string op, ASTNode operand, int position = 0)
        {
            Operator = op;
            Operand = operand;
            Position = position;
        }

        public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitUnaryExpression(this);
    }

    public class MemberAccessNode : ASTNode
    {
        public override EASTNodeType NodeType => EASTNodeType.MemberAccess;
        public ASTNode Target { get; set; }
        public string MemberName { get; set; }

        public MemberAccessNode(ASTNode target, string memberName, int position = 0)
        {
            Target = target;
            MemberName = memberName;
            Position = position;
        }

        public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitMemberAccess(this);
    }

    public class IndexAccessNode : ASTNode
    {
        public override EASTNodeType NodeType => EASTNodeType.IndexAccess;
        public ASTNode Target { get; set; }
        public ASTNode Index { get; set; }

        public IndexAccessNode(ASTNode target, ASTNode index, int position = 0)
        {
            Target = target;
            Index = index;
            Position = position;
        }

        public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitIndexAccess(this);
    }

    public class MethodCallNode : ASTNode
    {
        public override EASTNodeType NodeType => EASTNodeType.MethodCall;
        public ASTNode Target { get; set; }
        public string MethodName { get; set; }
        public List<ASTNode> Arguments { get; set; }
        public List<string> GenericTypeArgs { get; set; }

        public MethodCallNode(ASTNode target, string methodName, List<ASTNode> arguments, int position = 0)
        {
            Target = target;
            MethodName = methodName;
            Arguments = arguments ?? new List<ASTNode>();
            GenericTypeArgs = new List<string>();
            Position = position;
        }

        public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitMethodCall(this);
    }

    public class FunctionCallNode : ASTNode
    {
        public override EASTNodeType NodeType => EASTNodeType.FunctionCall;
        public ASTNode Function { get; set; }
        public List<ASTNode> Arguments { get; set; }
        public List<string> GenericTypeArgs { get; set; }

        public FunctionCallNode(ASTNode function, List<ASTNode> arguments, int position = 0)
        {
            Function = function;
            Arguments = arguments ?? new List<ASTNode>();
            GenericTypeArgs = new List<string>();
            Position = position;
        }

        public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitFunctionCall(this);
    }

    public class NewExpressionNode : ASTNode
    {
        public override EASTNodeType NodeType => EASTNodeType.NewExpression;
        public string TypeName { get; set; }
        public List<ASTNode> Arguments { get; set; }
        public List<string> GenericTypeArgs { get; set; }

        public NewExpressionNode(string typeName, List<ASTNode> arguments, int position = 0)
        {
            TypeName = typeName;
            Arguments = arguments ?? new List<ASTNode>();
            GenericTypeArgs = new List<string>();
            Position = position;
        }

        public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitNewExpression(this);
    }

    public class NewArrayExpressionNode : ASTNode
    {
        public override EASTNodeType NodeType => EASTNodeType.NewArrayExpression;
        public string ElementTypeName { get; set; }
        public ASTNode Size { get; set; }

        public NewArrayExpressionNode(string elementTypeName, ASTNode size, int position = 0)
        {
            ElementTypeName = elementTypeName;
            Size = size;
            Position = position;
        }

        public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitNewArrayExpression(this);
    }

    #endregion

    #region 语句节点

    public class AssignmentNode : ASTNode
    {
        public override EASTNodeType NodeType => EASTNodeType.Assignment;
        public ASTNode Target { get; set; }
        public ASTNode Value { get; set; }

        public AssignmentNode(ASTNode target, ASTNode value, int position = 0)
        {
            Target = target;
            Value = value;
            Position = position;
        }

        public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitAssignment(this);
    }

    #endregion

    #region 特殊节点

    public class ErrorNode : ASTNode
    {
        public override EASTNodeType NodeType => EASTNodeType.Error;
        public string Message { get; set; }

        public ErrorNode(string message, int position = 0)
        {
            Message = message;
            Position = position;
        }

        public override T Accept<T>(IASTVisitor<T> visitor) => visitor.VisitError(this);
    }

    #endregion
}
