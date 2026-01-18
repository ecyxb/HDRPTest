using System;
using System.Collections.Generic;

namespace EventFramework.AST
{
    /// <summary>
    /// 语法分析器 - 将 Token 流构建为 AST
    /// 使用递归下降解析器，支持运算符优先级
    /// </summary>
    public class ASTParser
    {
        private List<Token> _tokens;
        private int _current;

        public ASTParser()
        {
        }

        /// <summary>
        /// 解析表达式或语句
        /// </summary>
        public ASTNode Parse(string input)
        {
            var lexer = new ASTLexer(input);
            _tokens = lexer.Tokenize();
            _current = 0;

            if (Check(ETokenType.Error))
            {
                return new ErrorNode(_tokens[_current].Value, _tokens[_current].Position);
            }

            return ParseStatement();
        }

        /// <summary>
        /// 解析多条语句
        /// </summary>
        public List<ASTNode> ParseMultiple(string input)
        {
            var lexer = new ASTLexer(input);
            _tokens = lexer.Tokenize();
            _current = 0;

            var statements = new List<ASTNode>();

            while (!IsAtEnd())
            {
                if (Check(ETokenType.Error))
                {
                    statements.Add(new ErrorNode(_tokens[_current].Value, _tokens[_current].Position));
                    break;
                }

                if (Check(ETokenType.Semicolon))
                {
                    Advance(); // 跳过空语句
                    continue;
                }

                statements.Add(ParseStatement());

                // 可选的分号
                if (Check(ETokenType.Semicolon))
                    Advance();
            }

            return statements;
        }

        #region 语句解析

        private ASTNode ParseStatement()
        {
            // 检查是否是赋值语句
            int savedPosition = _current;
            ASTNode left = ParseExpression();

            if (Check(ETokenType.Equal))
            {
                Advance(); // 消费 =
                ASTNode value = ParseExpression();
                return new AssignmentNode(left, value, savedPosition);
            }

            return left;
        }

        #endregion

        #region 表达式解析 - 运算符优先级

        private ASTNode ParseExpression()
        {
            return ParseOrExpression();
        }

        // 优先级最低: ||
        private ASTNode ParseOrExpression()
        {
            ASTNode left = ParseAndExpression();

            while (Check(ETokenType.Or))
            {
                int pos = Current().Position;
                Advance();
                ASTNode right = ParseAndExpression();
                left = new BinaryExpressionNode(left, "||", right, pos);
            }

            return left;
        }

        // &&
        private ASTNode ParseAndExpression()
        {
            ASTNode left = ParseEqualityExpression();

            while (Check(ETokenType.And))
            {
                int pos = Current().Position;
                Advance();
                ASTNode right = ParseEqualityExpression();
                left = new BinaryExpressionNode(left, "&&", right, pos);
            }

            return left;
        }

        // == !=
        private ASTNode ParseEqualityExpression()
        {
            ASTNode left = ParseComparisonExpression();

            while (Check(ETokenType.EqualEqual) || Check(ETokenType.NotEqual))
            {
                int pos = Current().Position;
                string op = Advance().Value;
                ASTNode right = ParseComparisonExpression();
                left = new BinaryExpressionNode(left, op, right, pos);
            }

            return left;
        }

        // < > <= >=
        private ASTNode ParseComparisonExpression()
        {
            ASTNode left = ParseAdditiveExpression();

            while (Check(ETokenType.Less) || Check(ETokenType.Greater) ||
           Check(ETokenType.LessEqual) || Check(ETokenType.GreaterEqual))
            {
                // 检查是否可能是泛型语法
                if (Check(ETokenType.Less) && IsLikelyGenericStart())
                    break;

                int pos = Current().Position;
                string op = Advance().Value;
                ASTNode right = ParseAdditiveExpression();
                left = new BinaryExpressionNode(left, op, right, pos);
            }

            return left;
        }

        // + -
        private ASTNode ParseAdditiveExpression()
        {
            ASTNode left = ParseMultiplicativeExpression();

            while (Check(ETokenType.Plus) || Check(ETokenType.Minus))
            {
                int pos = Current().Position;
                string op = Advance().Value;
                ASTNode right = ParseMultiplicativeExpression();
                left = new BinaryExpressionNode(left, op, right, pos);
            }

            return left;
        }

        // * / %
        private ASTNode ParseMultiplicativeExpression()
        {
            ASTNode left = ParseUnaryExpression();

            while (Check(ETokenType.Star) || Check(ETokenType.Slash) || Check(ETokenType.Percent))
            {
                int pos = Current().Position;
                string op = Advance().Value;
                ASTNode right = ParseUnaryExpression();
                left = new BinaryExpressionNode(left, op, right, pos);
            }

            return left;
        }

        // ! -
        private ASTNode ParseUnaryExpression()
        {
            if (Check(ETokenType.Not))
            {
                int pos = Current().Position;
                Advance();
                ASTNode operand = ParseUnaryExpression();
                return new UnaryExpressionNode("!", operand, pos);
            }

            if (Check(ETokenType.Minus))
            {
                int pos = Current().Position;
                Advance();
                ASTNode operand = ParseUnaryExpression();
                return new UnaryExpressionNode("-", operand, pos);
            }

            return ParsePostfixExpression();
        }

        // 后缀表达式: 成员访问、索引访问、方法调用
        private ASTNode ParsePostfixExpression()
        {
            ASTNode expr = ParsePrimaryExpression();

            while (true)
            {
                if (Check(ETokenType.Dot))
                {
                    Advance(); // 消费 .
                    expr = ParseMemberOrMethodCall(expr);
                }
                else if (Check(ETokenType.LeftBracket))
                {
                    int pos = Current().Position;
                    Advance(); // 消费 [
                    ASTNode index = ParseExpression();
                    Expect(ETokenType.RightBracket, "Expected ']'");
                    expr = new IndexAccessNode(expr, index, pos);
                }
                else if (Check(ETokenType.LeftParen))
                {
                    // 函数调用
                    int pos = Current().Position;
                    var args = ParseArgumentList();
                    expr = new FunctionCallNode(expr, args, pos);
                }
                else
                {
                    break;
                }
            }

            return expr;
        }

        private ASTNode ParseMemberOrMethodCall(ASTNode target)
        {
            int pos = Current().Position;

            if (!Check(ETokenType.Identifier))
            {
                return new ErrorNode("Expected identifier after '.'", pos);
            }

            string name = Advance().Value;
            List<string> genericArgs = null;

            // 检查泛型参数
            if (Check(ETokenType.Less) && IsLikelyGenericStart())
            {
                genericArgs = ParseGenericArguments();
            }

            // 检查是否是方法调用
            if (Check(ETokenType.LeftParen))
            {
                var args = ParseArgumentList();
                var methodCall = new MethodCallNode(target, name, args, pos);
                if (genericArgs != null)
                    methodCall.GenericTypeArgs = genericArgs;
                return methodCall;
            }

            // 普通成员访问
            return new MemberAccessNode(target, name, pos);
        }

        #endregion

        #region 基础表达式

        private ASTNode ParsePrimaryExpression()
        {
            int pos = Current().Position;

            // new 表达式
            if (Check(ETokenType.New))
            {
                return ParseNewExpression();
            }

            // 字面量
            if (Check(ETokenType.Integer))
            {
                string value = Advance().Value;
                if (long.TryParse(value, out long longVal))
                    return new IntegerLiteralNode(longVal, pos);
                return new ErrorNode($"Invalid integer: {value}", pos);
            }

            if (Check(ETokenType.Float))
            {
                string value = Advance().Value;
                if (double.TryParse(value, out double doubleVal))
                    return new FloatLiteralNode(doubleVal, pos);
                return new ErrorNode($"Invalid float: {value}", pos);
            }

            if (Check(ETokenType.String))
            {
                return new StringLiteralNode(Advance().Value, pos);
            }

            if (Check(ETokenType.True))
            {
                Advance();
                return new BoolLiteralNode(true, pos);
            }

            if (Check(ETokenType.False))
            {
                Advance();
                return new BoolLiteralNode(false, pos);
            }

            if (Check(ETokenType.Null))
            {
                Advance();
                return new NullLiteralNode(pos);
            }

            // 标识符
            if (Check(ETokenType.Identifier))
            {
                return new IdentifierNode(Advance().Value, pos);
            }

            // 括号表达式
            if (Check(ETokenType.LeftParen))
            {
                Advance();
                ASTNode expr = ParseExpression();
                Expect(ETokenType.RightParen, "Expected ')'");
                return expr;
            }

            return new ErrorNode($"Unexpected token: {Current().Type}", pos);
        }

        private ASTNode ParseNewExpression()
        {
            int pos = Current().Position;
            Advance(); // 消费 new

            if (!Check(ETokenType.Identifier))
            {
                return new ErrorNode("Expected type name after 'new'", pos);
            }

            string typeName = Advance().Value;
            List<string> genericArgs = null;

            // 检查泛型参数
            if (Check(ETokenType.Less) && IsLikelyGenericStart())
            {
                genericArgs = ParseGenericArguments();
                if (genericArgs != null && genericArgs.Count > 0)
                {
                    typeName += "<" + string.Join(", ", genericArgs) + ">";
                }
            }

            // 数组构造: new Type[size]
            if (Check(ETokenType.LeftBracket))
            {
                Advance(); // 消费 [
                ASTNode size = ParseExpression();
                Expect(ETokenType.RightBracket, "Expected ']'");
                return new NewArrayExpressionNode(typeName, size, pos);
            }

            // 普通构造: new Type(args)
            if (Check(ETokenType.LeftParen))
            {
                var args = ParseArgumentList();
                var newExpr = new NewExpressionNode(typeName, args, pos);
                if (genericArgs != null)
                    newExpr.GenericTypeArgs = genericArgs;
                return newExpr;
            }

            // 无参构造: new Type
            return new NewExpressionNode(typeName, new List<ASTNode>(), pos);
        }

        #endregion

        #region 辅助解析方法

        private List<ASTNode> ParseArgumentList()
        {
            var args = new List<ASTNode>();
            Advance(); // 消费 (

            if (!Check(ETokenType.RightParen))
            {
                do
                {
                    if (Check(ETokenType.Comma))
                        Advance();

                    args.Add(ParseExpression());
                } while (Check(ETokenType.Comma));
            }

            Expect(ETokenType.RightParen, "Expected ')'");
            return args;
        }

        private List<string> ParseGenericArguments()
        {
            var args = new List<string>();

            if (!Check(ETokenType.Less))
                return args;

            Advance(); // 消费 <

            int depth = 1;
            var currentArg = new System.Text.StringBuilder();

            while (!IsAtEnd() && depth > 0)
            {
                if (Check(ETokenType.Less))
                {
                    depth++;
                    currentArg.Append("<");
                    Advance();
                }
                else if (Check(ETokenType.Greater))
                {
                    depth--;
                    if (depth > 0)
                    {
                        currentArg.Append(">");
                    }
                    Advance(); // 始终消费 >，包括最后一个
                }
                else if (Check(ETokenType.Comma) && depth == 1)
                {
                    args.Add(currentArg.ToString().Trim());
                    currentArg.Clear();
                    Advance();
                }
                else
                {
                    currentArg.Append(Current().Value);
                    Advance();
                }
            }

            if (currentArg.Length > 0)
            {
                args.Add(currentArg.ToString().Trim());
            }

            return args;
        }

        /// <summary>
        /// 检查当前 < 是否可能是泛型开始而非比较运算符
        /// </summary>
        private bool IsLikelyGenericStart()
        {
            if (!Check(ETokenType.Less))
                return false;

            // 简单启发式: 检查 < 后面是否是标识符
            int saved = _current;
            Advance(); // 跳过 <

            bool isGeneric = Check(ETokenType.Identifier);

            _current = saved;
            return isGeneric;
        }

        #endregion

        #region Token 操作

        private bool IsAtEnd() => Check(ETokenType.EOF);

        private Token Current() => _tokens[_current];

        private Token Previous() => _tokens[_current - 1];

        private bool Check(ETokenType type) => !(_current >= _tokens.Count) && _tokens[_current].Type == type;

        private Token Advance()
        {
            if (!IsAtEnd())
                _current++;
            return Previous();
        }

        private bool Match(params ETokenType[] types)
        {
            foreach (var type in types)
            {
                if (Check(type))
                {
                    Advance();
                    return true;
                }
            }
            return false;
        }

        private Token Expect(ETokenType type, string errorMessage)
        {
            if (Check(type))
                return Advance();

            // 创建一个错误 token 但不中断解析
            return new Token(ETokenType.Error, errorMessage, Current().Position, 0);
        }

        #endregion
    }
}
