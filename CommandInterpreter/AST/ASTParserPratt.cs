using System;
using System.Collections.Generic;

namespace EventFramework.AST
{
    /// <summary>
    /// 基于 Pratt Parser（优先级攀爬）的语法分析器
    /// 核心思想：每个运算符有左绑定力(lbp)和右绑定力(rbp)
    /// 通过比较绑定力来决定运算符的结合性和优先级
    /// </summary>
    public class ASTParserPratt
    {
        #region 绑定力定义

        /// <summary>
        /// 运算符优先级（绑定力）
        /// 数值越大，优先级越高
        /// </summary>
        private enum BindingPower
        {
            None = 0,
            Assignment = 10,    // =
            Or = 20,            // ||
            And = 30,   // &&
            Equality = 40,      // == !=
            Comparison = 50,    // < > <= >=
            Additive = 60,    // + -
            Multiplicative = 70, // * / %
            Unary = 80,// ! - (前缀)
            Call = 90,          // () [] .
            Primary = 100,      // 字面量、标识符
        }

        #endregion

        #region 字段

        private List<Token> _tokens;
        private int _current;

        #endregion

        #region 公共接口

        public ASTParserPratt()
        {
        }

        /// <summary>
        /// 解析单个表达式或语句
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
                    Advance();
                    continue;
                }

                statements.Add(ParseStatement());

                if (Check(ETokenType.Semicolon))
                    Advance();
            }

            return statements;
        }

        #endregion

        #region 语句解析

        private ASTNode ParseStatement()
        {
            int savedPosition = _current;

            // 解析左侧表达式
            ASTNode left = ParseExpression((int)BindingPower.None);

            // 检查是否是赋值语句
            if (Check(ETokenType.Equal))
            {
                Advance();
                ASTNode value = ParseExpression((int)BindingPower.None);
                return new AssignmentNode(left, value, savedPosition);
            }

            return left;
        }

        #endregion

        #region Pratt Parser 核心

        /// <summary>
        /// Pratt Parser 核心：解析表达式
        /// </summary>
        /// <param name="minBp">最小绑定力，只处理绑定力大于此值的运算符</param>
        private ASTNode ParseExpression(int minBp)
        {
            // 1. 解析前缀表达式（包括字面量、标识符、一元运算符等）
            ASTNode left = ParsePrefix();

            // 2. 循环处理中缀运算符
            while (!IsAtEnd())
            {
                // 获取当前运算符的左绑定力
                int lbp = GetInfixLeftBindingPower(Current().Type);

                // 如果当前运算符的绑定力小于等于最小绑定力，停止
                if (lbp <= minBp)
                    break;

                // 处理中缀表达式
                left = ParseInfix(left, lbp);
            }

            return left;
        }

        /// <summary>
        /// 解析前缀表达式（null denotation / nud）
        /// </summary>
        private ASTNode ParsePrefix()
        {
            Token token = Current();
            int pos = token.Position;

            switch (token.Type)
            {
                // === 字面量 ===
                case ETokenType.Integer:
                    Advance();
                    if (long.TryParse(token.Value, out long longVal))
                        return new IntegerLiteralNode(longVal, pos);
                    return new ErrorNode($"Invalid integer: {token.Value}", pos);

                case ETokenType.Float:
                    Advance();
                    if (double.TryParse(token.Value, out double doubleVal))
                        return new FloatLiteralNode(doubleVal, pos);
                    return new ErrorNode($"Invalid float: {token.Value}", pos);

                case ETokenType.String:
                    Advance();
                    return new StringLiteralNode(token.Value, pos);

                case ETokenType.True:
                    Advance();
                    return new BoolLiteralNode(true, pos);

                case ETokenType.False:
                    Advance();
                    return new BoolLiteralNode(false, pos);

                case ETokenType.Null:
                    Advance();
                    return new NullLiteralNode(pos);

                // === 标识符 ===
                case ETokenType.Identifier:
                    Advance();
                    return new IdentifierNode(token.Value, pos);

                // === 一元运算符 ===
                case ETokenType.Not:
                    Advance();
                    {
                        int rbp = GetPrefixRightBindingPower(ETokenType.Not);
                        ASTNode operand = ParseExpression(rbp);
                        return new UnaryExpressionNode("!", operand, pos);
                    }

                case ETokenType.Minus:
                    Advance();
                    {
                        int rbp = GetPrefixRightBindingPower(ETokenType.Minus);
                        ASTNode operand = ParseExpression(rbp);
                        return new UnaryExpressionNode("-", operand, pos);
                    }

                // === 括号表达式 ===
                case ETokenType.LeftParen:
                    Advance();
                    {
                        ASTNode expr = ParseExpression((int)BindingPower.None);
                        Expect(ETokenType.RightParen, "Expected ')'");
                        return expr;
                    }

                // === new 表达式 ===
                case ETokenType.New:
                    return ParseNewExpression();

                default:
                    return new ErrorNode($"Unexpected token: {token.Type}", pos);
            }
        }

        /// <summary>
        /// 解析中缀表达式（left denotation / led）
        /// </summary>
        /// <param name="left">左操作数</param>
        /// <param name="lbp">当前运算符的左绑定力</param>
        private ASTNode ParseInfix(ASTNode left, int lbp)
        {
            Token token = Current();
            int pos = token.Position;

            switch (token.Type)
            {
                // === 二元运算符 ===
                case ETokenType.Plus:
                case ETokenType.Minus:
                case ETokenType.Star:
                case ETokenType.Slash:
                case ETokenType.Percent:
                case ETokenType.EqualEqual:
                case ETokenType.NotEqual:
                case ETokenType.And:
                case ETokenType.Or:
                    {
                        string op = Advance().Value;
                        int rbp = GetInfixRightBindingPower(token.Type);
                        ASTNode right = ParseExpression(rbp);
                        return new BinaryExpressionNode(left, op, right, pos);
                    }

                // === 比较运算符（需要处理泛型歧义）===
                case ETokenType.Less:
                case ETokenType.Greater:
                case ETokenType.LessEqual:
                case ETokenType.GreaterEqual:
                    {
                        // 检查是否可能是泛型语法
                        if (token.Type == ETokenType.Less && IsLikelyGenericStart())
                        {
                            // 不是比较运算符，停止中缀解析
                            return left;
                        }

                        string op = Advance().Value;
                        int rbp = GetInfixRightBindingPower(token.Type);
                        ASTNode right = ParseExpression(rbp);
                        return new BinaryExpressionNode(left, op, right, pos);
                    }

                // === 成员访问 ===
                case ETokenType.Dot:
                    Advance();
                    return ParseMemberOrMethodCall(left);

                // === 索引访问 ===
                case ETokenType.LeftBracket:
                    Advance();
                    {
                        ASTNode index = ParseExpression((int)BindingPower.None);
                        Expect(ETokenType.RightBracket, "Expected ']'");
                        return new IndexAccessNode(left, index, pos);
                    }

                // === 函数调用 ===
                case ETokenType.LeftParen:
                    {
                        var args = ParseArgumentList();
                        return new FunctionCallNode(left, args, pos);
                    }

                default:
                    return left;
            }
        }

        #endregion

        #region 绑定力查询

        /// <summary>
        /// 获取前缀运算符的右绑定力
        /// </summary>
        private int GetPrefixRightBindingPower(ETokenType type)
        {
            switch (type)
            {
                case ETokenType.Not:
                case ETokenType.Minus:
                    return (int)BindingPower.Unary;
                default:
                    return (int)BindingPower.None;
            }
        }

        /// <summary>
        /// 获取中缀运算符的左绑定力
        /// 决定何时"吸引"左操作数
        /// </summary>
        private int GetInfixLeftBindingPower(ETokenType type)
        {
            switch (type)
            {
                case ETokenType.Or:
                    return (int)BindingPower.Or;

                case ETokenType.And:
                    return (int)BindingPower.And;

                case ETokenType.EqualEqual:
                case ETokenType.NotEqual:
                    return (int)BindingPower.Equality;

                case ETokenType.Less:
                case ETokenType.Greater:
                case ETokenType.LessEqual:
                case ETokenType.GreaterEqual:
                    return (int)BindingPower.Comparison;

                case ETokenType.Plus:
                case ETokenType.Minus:
                    return (int)BindingPower.Additive;

                case ETokenType.Star:
                case ETokenType.Slash:
                case ETokenType.Percent:
                    return (int)BindingPower.Multiplicative;

                case ETokenType.Dot:
                case ETokenType.LeftBracket:
                case ETokenType.LeftParen:
                    return (int)BindingPower.Call;

                default:
                    return (int)BindingPower.None;
            }
        }

        /// <summary>
        /// 获取中缀运算符的右绑定力
        /// 决定右操作数的解析范围
        /// 左结合：rbp = lbp
        /// 右结合：rbp = lbp - 1
        /// </summary>
        private int GetInfixRightBindingPower(ETokenType type)
        {
            switch (type)
            {
                // 左结合运算符
                case ETokenType.Or:
                    return (int)BindingPower.Or;

                case ETokenType.And:
                    return (int)BindingPower.And;

                case ETokenType.EqualEqual:
                case ETokenType.NotEqual:
                    return (int)BindingPower.Equality;

                case ETokenType.Less:
                case ETokenType.Greater:
                case ETokenType.LessEqual:
                case ETokenType.GreaterEqual:
                    return (int)BindingPower.Comparison;

                case ETokenType.Plus:
                case ETokenType.Minus:
                    return (int)BindingPower.Additive;

                case ETokenType.Star:
                case ETokenType.Slash:
                case ETokenType.Percent:
                    return (int)BindingPower.Multiplicative;

                default:
                    return (int)BindingPower.None;
            }
        }

        #endregion

        #region 辅助解析方法

        /// <summary>
        /// 解析成员访问或方法调用
        /// </summary>
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

        /// <summary>
        /// 解析 new 表达式
        /// </summary>
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
                Advance();
                ASTNode size = ParseExpression((int)BindingPower.None);
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

        /// <summary>
        /// 解析参数列表
        /// </summary>
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

                    args.Add(ParseExpression((int)BindingPower.None));
                } while (Check(ETokenType.Comma));
            }

            Expect(ETokenType.RightParen, "Expected ')'");
            return args;
        }

        /// <summary>
        /// 解析泛型参数
        /// </summary>
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
                    Advance();
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
        /// 检查当前 &lt; 是否可能是泛型开始而非比较运算符
        /// 使用更严格的启发式判断
        /// </summary>
        private bool IsLikelyGenericStart()
        {
            if (!Check(ETokenType.Less))
                return false;

            int saved = _current;
            Advance(); // 跳过 <

            // < 后面必须是标识符才可能是泛型
            if (!Check(ETokenType.Identifier))
            {
                _current = saved;
                return false;
            }

            // 尝试扫描到匹配的 >，检查整体结构是否像泛型
            int depth = 1;
            Advance(); // 跳过标识符

            while (!IsAtEnd() && depth > 0)
            {
                if (Check(ETokenType.Less))
                {
                    depth++;
                    Advance();
                }
                else if (Check(ETokenType.Greater))
                {
                    depth--;
                    if (depth == 0)
                    {
                        Advance(); // 跳过最后的 >
                                   // 泛型后面通常跟 ( ) [ ] . , > 或语句结束
                        bool looksLikeGeneric = Check(ETokenType.LeftParen) ||
                       Check(ETokenType.RightParen) ||
                   Check(ETokenType.LeftBracket) ||
                          Check(ETokenType.Dot) ||
                         Check(ETokenType.Comma) ||
                          Check(ETokenType.Greater) ||
                         Check(ETokenType.Semicolon) ||
                      Check(ETokenType.EOF);
                        _current = saved;
                        return looksLikeGeneric;
                    }
                    else
                    {
                        Advance();
                    }
                }
                else if (Check(ETokenType.Identifier) || Check(ETokenType.Comma))
                {
                    // 泛型参数中允许的 token
                    Advance();
                }
                else
                {
                    // 遇到其他 token（如运算符 + - * 等），不像泛型
                    _current = saved;
                    return false;
                }
            }

            _current = saved;
            return false;
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

        private Token Expect(ETokenType type, string errorMessage)
        {
            if (Check(type))
                return Advance();

            return new Token(ETokenType.Error, errorMessage, Current().Position, 0);
        }

        #endregion
    }
}
