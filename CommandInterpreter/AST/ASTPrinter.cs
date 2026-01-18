using System;
using System.Text;

namespace EventFramework.AST
{
    /// <summary>
    /// AST 打印器 - 用于调试和查看 AST 结构
    /// </summary>
    public class ASTPrinter : IASTVisitor<string>
    {
      private int _indent = 0;
        private const string IndentStr = "  ";

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
            string genericStr = node.GenericTypeArgs?.Count > 0 
      ? $"<{string.Join(", ", node.GenericTypeArgs)}>" 
      : "";
            sb.AppendLine($"{GetIndent()}New: {node.TypeName}{genericStr}");
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
    }
}
