using System;

namespace EventFramework
{
    #region À©Õ¹·½·¨

    public static class CommandArgExtensions
    {
        public static bool CanNumeric(this ICommandArg arg) => arg is INumeric;
        public static bool CanString(this ICommandArg arg) => arg is IStringArg;
        public static bool IsIndexable(this ICommandArg arg) => arg is IIndexable;
        public static bool HasMembers(this ICommandArg arg) => arg is IMemberAccessible;
        public static bool IsError(this ICommandArg arg) => arg is CommandInterpreter_ErrorArg;
    }

    #endregion

    #region ´íÎóÂë

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
        void RegisterPresetFunc(string name, Type cls, string funcname);
    }
}