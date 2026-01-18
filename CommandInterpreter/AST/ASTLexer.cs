using System;
using System.Collections.Generic;
using System.Text;

namespace EventFramework.AST
{
    /// <summary>
    /// 词法分析器 - 将输入字符串转换为 Token 流
    /// </summary>
    public class ASTLexer
    {
        private readonly string _input;
        private int _position;
        private readonly List<Token> _tokens;

        public ASTLexer(string input)
        {
            _input = input ?? string.Empty;
            _position = 0;
            _tokens = new List<Token>();
        }

        public List<Token> Tokenize()
        {
            _tokens.Clear();
            _position = 0;

            while (!IsAtEnd())
            {
                SkipWhitespace();
                if (IsAtEnd()) break;

                Token token = ScanToken();
                _tokens.Add(token);

                if (token.Type == ETokenType.Error)
                    break;
            }

            _tokens.Add(Token.EOF(_position));
            return _tokens;
        }

        private Token ScanToken()
        {
            int start = _position;
            char c = Advance();

            // 单字符 token
            switch (c)
            {
                case '(': return MakeToken(ETokenType.LeftParen, "(", start);
                case ')': return MakeToken(ETokenType.RightParen, ")", start);
                case '[': return MakeToken(ETokenType.LeftBracket, "[", start);
                case ']': return MakeToken(ETokenType.RightBracket, "]", start);
                case '{': return MakeToken(ETokenType.LeftBrace, "{", start);
                case '}': return MakeToken(ETokenType.RightBrace, "}", start);
                case ',': return MakeToken(ETokenType.Comma, ",", start);
                case ';': return MakeToken(ETokenType.Semicolon, ";", start);
                case '.': return MakeToken(ETokenType.Dot, ".", start);
                case '+': return MakeToken(ETokenType.Plus, "+", start);
                case '-': return MakeToken(ETokenType.Minus, "-", start);
                case '*': return MakeToken(ETokenType.Star, "*", start);
                case '/': return MakeToken(ETokenType.Slash, "/", start);
                case '%': return MakeToken(ETokenType.Percent, "%", start);

                case '=':
                    if (Match('=')) return MakeToken(ETokenType.EqualEqual, "==", start);
                    return MakeToken(ETokenType.Equal, "=", start);

                case '!':
                    if (Match('=')) return MakeToken(ETokenType.NotEqual, "!=", start);
                    return MakeToken(ETokenType.Not, "!", start);

                case '<':
                    if (Match('=')) return MakeToken(ETokenType.LessEqual, "<=", start);
                    // < 可能是比较运算符或泛型开始，由 Parser 决定
                    return MakeToken(ETokenType.Less, "<", start);

                case '>':
                    if (Match('=')) return MakeToken(ETokenType.GreaterEqual, ">=", start);
                    return MakeToken(ETokenType.Greater, ">", start);

                case '&':
                    if (Match('&')) return MakeToken(ETokenType.And, "&&", start);
                    return Token.Error("Unexpected '&', expected '&&'", start);

                case '|':
                    if (Match('|')) return MakeToken(ETokenType.Or, "||", start);
                    return Token.Error("Unexpected '|', expected '||'", start);

                case '"':
                    return ScanString(start);

                default:
                    if (IsDigit(c))
                        return ScanNumber(start);
                    if (IsIdentifierStart(c))
                        return ScanIdentifier(start);

                    return Token.Error($"Unexpected character '{c}'", start);
            }
        }

        private Token ScanString(int start)
        {
            var sb = new StringBuilder();
            while (!IsAtEnd() && Peek() != '"')
            {
                char c = Advance();
                if (c == '\\' && !IsAtEnd())
                {
                    char escaped = Advance();
                    switch (escaped)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 't': sb.Append('\t'); break;
                        case 'r': sb.Append('\r'); break;
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        default: sb.Append(escaped); break;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }

            if (IsAtEnd())
                return Token.Error("Unterminated string", start);

            Advance(); // 消费闭合的 "
            return MakeToken(ETokenType.String, sb.ToString(), start);
        }

        private Token ScanNumber(int start)
        {
            // 回退到数字开始位置
            _position = start;

            bool isFloat = false;
            var sb = new StringBuilder();

            // 整数部分
            while (!IsAtEnd() && IsDigit(Peek()))
            {
                sb.Append(Advance());
            }

            // 小数部分
            if (!IsAtEnd() && Peek() == '.' && !IsAtEnd() && _position + 1 < _input.Length && IsDigit(_input[_position + 1]))
            {
                isFloat = true;
                sb.Append(Advance()); // 消费 '.'
                while (!IsAtEnd() && IsDigit(Peek()))
                {
                    sb.Append(Advance());
                }
            }

            // 浮点后缀 f/F
            if (!IsAtEnd() && (Peek() == 'f' || Peek() == 'F'))
            {
                isFloat = true;
                Advance(); // 消费 f/F
            }
            // 长整型后缀 L/l
            else if (!IsAtEnd() && (Peek() == 'L' || Peek() == 'l'))
            {
                Advance(); // 消费 L/l
            }

            return MakeToken(isFloat ? ETokenType.Float : ETokenType.Integer, sb.ToString(), start);
        }

        private Token ScanIdentifier(int start)
        {
            _position = start;
            var sb = new StringBuilder();

            while (!IsAtEnd() && IsIdentifierChar(Peek()))
            {
                sb.Append(Advance());
            }

            string value = sb.ToString();

            // 检查关键字
            switch (value)
            {
                case "true": return MakeToken(ETokenType.True, value, start);
                case "false": return MakeToken(ETokenType.False, value, start);
                case "null": return MakeToken(ETokenType.Null, value, start);
                case "new": return MakeToken(ETokenType.New, value, start);
                default: return MakeToken(ETokenType.Identifier, value, start);
            }
        }

        #region 辅助方法

        private bool IsAtEnd() => _position >= _input.Length;

        private char Peek() => IsAtEnd() ? '\0' : _input[_position];

        private char PeekNext() => _position + 1 >= _input.Length ? '\0' : _input[_position + 1];

        private char Advance() => _input[_position++];

        private bool Match(char expected)
        {
            if (IsAtEnd() || _input[_position] != expected) return false;
            _position++;
            return true;
        }

        private void SkipWhitespace()
        {
            while (!IsAtEnd() && char.IsWhiteSpace(Peek()))
                Advance();
        }

        private bool IsDigit(char c) => c >= '0' && c <= '9';

        private bool IsIdentifierStart(char c) => char.IsLetter(c) || c == '_' || c == '#';

        private bool IsIdentifierChar(char c) => char.IsLetterOrDigit(c) || c == '_' || c == '#';

        private Token MakeToken(ETokenType type, string value, int start)
        {
            return new Token(type, value, start, _position - start);
        }

        #endregion
    }
}
