using System;
using System.Collections.Generic;

namespace EventFramework
{

    /// <summary>ÊýÖµ</summary>
    public class CommandInterpreter_NumericArg : ICommandArg, INumeric, IMemberAccessible
    {
        private readonly long _intValue;
        private readonly double _floatValue;
        public bool IsInteger { get; }
        public bool IsFunctor => false;

        private CommandInterpreter_NumericArg(long value) { _intValue = value; _floatValue = value; IsInteger = true; }
        private CommandInterpreter_NumericArg(double value) { _floatValue = value; _intValue = (long)value; IsInteger = false; }

        public static CommandInterpreter_NumericArg FromInt(long value) => new CommandInterpreter_NumericArg(value);
        public static CommandInterpreter_NumericArg FromFloat(double value) => new CommandInterpreter_NumericArg(value);

        public double ToDouble() => IsInteger ? _intValue : _floatValue;
        public long ToLong() => _intValue;
        public object GetRawValue() => IsInteger ? (object)_intValue : _floatValue;
        public string Format() => IsInteger ? _intValue.ToString() : _floatValue.ToString("G");

        public ICommandArg GetMember(string name)
        {
            object raw = GetRawValue();
            return MemberAccessHelper.GetMember(raw, raw.GetType(), name);
        }
        public bool SetMember(string name, ICommandArg value) => false;
        public IEnumerable<string> GetMemberNames()
        {
            Type type = IsInteger ? typeof(long) : typeof(double);
            return MemberAccessHelper.GetMemberNames(type);
        }

        public static ICommandArg Add(INumeric a, INumeric b) =>
            a.IsInteger && b.IsInteger ? FromInt(a.ToLong() + b.ToLong()) : FromFloat(a.ToDouble() + b.ToDouble());
        public static ICommandArg Subtract(INumeric a, INumeric b) =>
            a.IsInteger && b.IsInteger ? FromInt(a.ToLong() - b.ToLong()) : FromFloat(a.ToDouble() - b.ToDouble());
        public static ICommandArg Multiply(INumeric a, INumeric b) =>
            a.IsInteger && b.IsInteger ? FromInt(a.ToLong() * b.ToLong()) : FromFloat(a.ToDouble() * b.ToDouble());
        public static ICommandArg Divide(INumeric a, INumeric b)
        {
            if (a.IsInteger && b.IsInteger)
            {
                long bVal = b.ToLong();
                return bVal == 0 ? CommandInterpreter_ErrorArg.Create(ErrorCodes.DivideByZero) : FromInt(a.ToLong() / bVal);
            }
            else { 
                double bVal = b.ToDouble();
                return bVal == 0 ? CommandInterpreter_ErrorArg.Create(ErrorCodes.DivideByZero) : FromFloat(a.ToDouble() / bVal);
            }
        }
        public static ICommandArg Modulo(INumeric a, INumeric b)
        {
            if (a.IsInteger && b.IsInteger)
            {
                long bVal = b.ToLong();
                return bVal == 0 ? CommandInterpreter_ErrorArg.Create(ErrorCodes.DivideByZero) : FromInt(a.ToLong() % bVal);
            }
            else
            {
                double bVal = b.ToDouble();
                return bVal == 0 ? CommandInterpreter_ErrorArg.Create(ErrorCodes.DivideByZero) : FromFloat(a.ToDouble() % bVal);
            }
        }
        public static CommandInterpreter_BoolArg Compare(INumeric a, string op, INumeric b)
        {
            double av = a.ToDouble(), bv = b.ToDouble();
            bool result;
            switch (op)
            {
                case "<": result = av < bv; break;
                case ">": result = av > bv; break;
                case "<=": result = av <= bv; break;
                case ">=": result = av >= bv; break;
                case "==": result = Math.Abs(av - bv) < double.Epsilon; break;
                case "!=": result = Math.Abs(av - bv) >= double.Epsilon; break;
                default: result = false; break;
            }
            return CommandInterpreter_BoolArg.From(result);
        }


    }

}