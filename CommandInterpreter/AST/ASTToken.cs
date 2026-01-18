using System;

namespace EventFramework.AST
{
    /// <summary>
    /// Token 类型枚举
    /// </summary>
    public enum ETokenType
    {
        // 字面量
        Integer,        // 整数: 123, -456
        Float,  // 浮点数: 1.5, -3.14f
        String,      // 字符串: "hello"
        True,           // true
        False,  // false
        Null,           // null

        // 标识符和关键字
        Identifier,   // 变量名、方法名等
        New,            // new 关键字

        // 运算符
        Plus,  // +
        Minus,          // -
        Star,       // *
        Slash,   // /
        Percent,        // %
        Equal,       // =
        EqualEqual,     // ==
        NotEqual,  // !=
        Less,  // <
        Greater,        // >
        LessEqual,      // <=
        GreaterEqual,   // >=
        And,            // &&
        Or,   // ||
        Not,            // !
        Dot,            // .
        Comma,          // ,
        Semicolon,      // ;

        // 括号
        LeftParen,      // (
        RightParen,     // )
        LeftBracket,    // [
        RightBracket,   // ]
        LeftBrace,      // {
        RightBrace,     // }
        LeftAngle,      // < (泛型)
        RightAngle,     // > (泛型)

        // 特殊
        EOF,            // 结束
        Error,          // 错误
    }

    /// <summary>
    /// Token 结构
    /// </summary>
    public struct Token
    {
        public ETokenType Type;
        public string Value;
        public int Position;
        public int Length;

        public Token(ETokenType type, string value, int position, int length)
        {
            Type = type;
            Value = value;
            Position = position;
            Length = length;
        }

        public override string ToString()
        {
            return $"[{Type}: '{Value}' at {Position}]";
        }

        public static Token EOF(int position) => new Token(ETokenType.EOF, "", position, 0);
        public static Token Error(string message, int position) => new Token(ETokenType.Error, message, position, 0);
    }
}
