
namespace EventFramework
{
    /// <summary>´íÎó</summary>
    public class CommandInterpreter_ErrorArg : ICommandArg
    {
        public int ErrorCode { get; }
        public string Message { get; }
        public bool IsFunctor => false;

        public CommandInterpreter_ErrorArg(int code, string message = null)
        {
            ErrorCode = code;
            Message = message ?? ErrorCodes.GetMessage(code);
        }

        public object GetRawValue() => null;
        public string Format() => $"Error: {Message}";

        public static CommandInterpreter_ErrorArg Create(int code, string detail = null)
        {
            string msg = ErrorCodes.GetMessage(code);
            if (!string.IsNullOrEmpty(detail)) msg += ": " + detail;
            return new CommandInterpreter_ErrorArg(code, msg);
        }
    }
}