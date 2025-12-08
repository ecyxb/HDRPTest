using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EventFramework
{    
    #region �ӿڶ���

    /// <summary>������������ӿ�</summary>
    public interface ICommandArg
    {
        bool IsFunctor { get; }
        object GetRawValue();
        string Format();
    }

    /// <summary>�ɵ�������</summary>
    public interface IFunctor
    {
        int Invoke(CommandInterpreterRulerV2 ruler, out ICommandArg result, params ICommandArg[] args);
    }

    /// <summary>����ֵ��������</summary>
    public interface INumeric
    {
        double ToDouble();
        long ToLong();
        bool IsInteger { get; }
    }

    /// <summary>���ַ�����������</summary>
    public interface IStringArg
    {
        string GetString();
    }



    /// <summary>�ɳ�Ա��������</summary>
    public interface IMemberAccessible
    {
        ICommandArg GetMember(string name);
        bool SetMember(string name, ICommandArg value);
        IEnumerable<string> GetMemberNames();
    }
    public interface IGenericArg
    {
        Type[] GenericTypes { get; }
    }

    /// <summary>����������</summary>
    public interface IIndexable : IGenericArg
    {
        int Count { get; }
        ICommandArg GetAt(ICommandArg index);
        bool SetAt(ICommandArg index, ICommandArg value);
        public Type KeyType { get; }
        public Type ValType { get; }
    }
    #endregion

    public class CommandInterpreterRulerV2
    {
        /// <summary>
        /// �������Ͷ���
        /// </summary>
        protected Dictionary<string, CommandInterpreter_TypeArg> cacheTypes = new Dictionary<string, CommandInterpreter_TypeArg>();

        protected virtual Dictionary<Type, int> GetNumericTypesIdx => new Dictionary<Type, int>()
        {
            { typeof(sbyte), 0},
            { typeof(byte), 1},
            { typeof(short), 2},
            { typeof(ushort), 3},
            { typeof(int), 4},
            { typeof(uint), 5},
            { typeof(long), 6},
            { typeof(float), 7},
            { typeof(double), 8},
        };
        protected Dictionary<Type, int> numericTypesIdxCache = null;
        public Dictionary<Type, int> NumericTypesIdx
        {
            get
            {
                if (numericTypesIdxCache == null)
                {
                    numericTypesIdxCache = GetNumericTypesIdx;
                }
                return numericTypesIdxCache;
            }
        }

        protected virtual Func<object, CommandInterpreter_NumericArg>[] GetNumericTypesFuncs => new Func<object, CommandInterpreter_NumericArg>[]
        {
            obj => CommandInterpreter_NumericArg.FromInt((sbyte)obj),
            obj => CommandInterpreter_NumericArg.FromInt((byte)obj),
            obj => CommandInterpreter_NumericArg.FromInt((short)obj),
            obj => CommandInterpreter_NumericArg.FromInt((ushort)obj),
            obj => CommandInterpreter_NumericArg.FromInt((int)obj),
            obj => CommandInterpreter_NumericArg.FromInt((uint)obj),
            obj => CommandInterpreter_NumericArg.FromInt((long)obj),
            obj => CommandInterpreter_NumericArg.FromFloat((float)obj),
            obj => CommandInterpreter_NumericArg.FromFloat((double)obj),
        };
        protected Func<object, CommandInterpreter_NumericArg>[] numericTypesFuncsCache = null;
        public Func<object, CommandInterpreter_NumericArg>[] NumericTypesFuncs
        {
            get
            {
                if (numericTypesFuncsCache == null)
                {
                    numericTypesFuncsCache = GetNumericTypesFuncs;
                }
                return numericTypesFuncsCache;
            }
        }


        public static long[][] maxIntLimits = new long[][]
        {
            new long[] {sbyte.MinValue, sbyte.MaxValue },
            new long[] {byte.MinValue, byte.MaxValue },
            new long[] {short.MinValue, short.MaxValue },
            new long[] {ushort.MinValue, ushort.MaxValue },
            new long[] {int.MinValue, int.MaxValue },
            new long[] {uint.MinValue, uint.MaxValue },
            new long[] {long.MinValue, long.MaxValue },
        };


        /// <summary>
        /// ���������������ӳ��
        /// </summary>
        public virtual Dictionary<string, string> GetOperatorMethodNames => new Dictionary<string, string>
        {
            { "+", "op_Addition" },
            { "-", "op_Subtraction" },
            { "*", "op_Multiply" },
            { "/", "op_Division" },
            { "%", "op_Modulus" },
            { "==", "op_Equality" },
            { "!=", "op_Inequality" },
            { "<", "op_LessThan" },
            { ">", "op_GreaterThan" },
            { "<=", "op_LessThanOrEqual" },
            { ">=", "op_GreaterThanOrEqual" },
        };
        protected Dictionary<string, string> operatorMethodNamesCache = null;
        public Dictionary<string, string> OperatorMethodNames
        {
            get
            {
                if (operatorMethodNamesCache == null)
                {
                    operatorMethodNamesCache = GetOperatorMethodNames;
                }
                return operatorMethodNamesCache;
            }
        }


        /// <summary>
        /// �������ͺ��������������ƥ�������-1��ʾ��ƥ�䡣���ص������Ǽ����ƣ�Խ�ͷ�Խ��
        /// </summary>
        /// <param name="paramType"></param>
        /// <param name="commandArg"></param>
        /// <returns></returns>
        public int GetMatchScore(Type paramType, ICommandArg commandArg)
        {
            int paramTypeIdx = GetNumericTypesIdx.GetValueOrDefault(paramType, -1);
            bool isNumericParam = paramTypeIdx >= 0;
            bool isFloatParam = paramTypeIdx >= 7; // float or double
            bool isNumericArg = commandArg is INumeric;
            // ��ֵ����֮������Եı���ƥ��
            if (isNumericParam != isNumericArg)
            {
                return -1;
            }
            if (isNumericArg)
            {
                //���������ֵ����
                INumeric numeric = (INumeric)commandArg;
                if (numeric.IsInteger)
                {
                    //���arg������
                    if (isFloatParam)
                    {
                        //����ת���㣬����ͨ���������ȼ���
                        return 1000;
                    }
                    else
                    {
                        //����ת���Σ���鷶Χ
                        var limit = maxIntLimits[paramTypeIdx];
                        long argValue = numeric.ToLong();
                        if (argValue < limit[0] || argValue > limit[1])
                        {
                            //���������ת��
                            return -1;
                        }
                        //ֻҪû���������Ϊ�������͵����ȼ���ͬ�������ֻ�ܸ��������岻��
                        return 0;
                    }
                }
                else
                {
                    //���arg�Ǹ���
                    if (isFloatParam)
                    {
                        //����ת���㣬һ��ת��
                        return 0;
                    }
                    else
                    {
                        //����ת���Σ��ǲ�������
                        return -1;
                    }
                }
            }
            else
            {
                object rawValue = commandArg.GetRawValue();
                // ����ֵ����
                if (rawValue == null)
                {
                    if (paramType.IsValueType)
                    {
                        //ֵ���Ͳ�����ƥ��
                        return -1;
                    }
                    else
                    {
                        //�������Ͳ�������Ϊƥ�䣬���üӷ�
                        return 0;
                    }

                }
                else
                {
                    Type argType = rawValue.GetType();
                    if (paramType == argType)
                    {
                        return 0; // ��ȫƥ��
                    }
                    else if (paramType.IsAssignableFrom(argType))
                    {
                        return 500; // �ɸ�ֵƥ��
                    }
                    else
                    {
                        return -1;
                    }
                }
            }

        }

        /// <summary>
        /// �������ƥ��ķ����������ǿɱ����������Ĭ�ϲ���
        /// </summary>
        /// <param name="Methods"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public T FindBestMatch<T>(T[] Methods, ICommandArg[] args) where T: MethodBase
        {
            T bestMatch = null;
            int bestScore = -9999999;

            for (int mIdx = 0; mIdx < Methods.Length; mIdx++)
            {
                var method = Methods[mIdx];
                var parameters = method.GetParameters();
                // ��������������ڴ��������һ���ǲ��е�
                if (args.Length > parameters.Length) continue;
                int score = 0;
                bool match = true;

                for (int i = args.Length; i < parameters.Length; i++)
                {
                    // ���������Ĳ���û��Ĭ��ֵ��һ���ǲ��е�
                    if (!parameters[i].HasDefaultValue)
                    {
                        match = false;
                        break;
                    }
                    //��Ĭ�ϲ�������Ϊ�Ǳ���ȫƥ���һ��
                    score -= 1;
                }
                if (!match) continue;
                for (int i = 0; i < args.Length; i++)
                {
                    int s = GetMatchScore(parameters[i].ParameterType, args[i]);
                    if (s < 0)
                    {
                        match = false;
                        break;
                    }
                    //�����ƣ�����Խ��Խ��
                    //����Ϊ���ò����ٵ�ʱ���ܾ����ܵ��ò����ٵķ���
                    score -= s; 
                }
                if (!match) continue;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = method;
                }
            }
            return bestMatch;
        }
   

        public Type FindRawType(string typeName)
        {
            return FindType(typeName)?.Value;
        }
        public CommandInterpreter_TypeArg FindType(string typeName)
        {
            if(cacheTypes.TryGetValue(typeName, out CommandInterpreter_TypeArg cachedType))
            {
                return cachedType;
            }
            if (typeName.Contains("<") && typeName.EndsWith(">"))
            {
                Type genericType = ParseGenericType(typeName);
                if (genericType != null)
                {
                    CommandInterpreter_TypeArg typeArg = new CommandInterpreter_TypeArg(genericType);
                    cacheTypes[typeName] = typeArg;
                    return typeArg;
                }
                return null;
            }
            Type primitive = _GetPrimitiveType(typeName);
            if (primitive != null)
            {
                CommandInterpreter_TypeArg typeArg = new CommandInterpreter_TypeArg(primitive);
                cacheTypes[typeName] = typeArg;
                return typeArg;
            }

            Type rawFindType = Type.GetType(typeName);
            if (rawFindType != null)
            {
                CommandInterpreter_TypeArg typeArg = new CommandInterpreter_TypeArg(rawFindType);
                cacheTypes[typeName] = typeArg;
                return typeArg;
            }

            Type appFindType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                .FirstOrDefault(t => t.Name == typeName || t.FullName == typeName);
            if (appFindType != null)
            {
                CommandInterpreter_TypeArg typeArg = new CommandInterpreter_TypeArg(appFindType);
                cacheTypes[typeName] = typeArg;
                return typeArg;
            }
            return null;
        }
        private Type _GetPrimitiveType(string typeName)
        {
            switch (typeName.ToLower())
            {
                case "int": return typeof(int);
                case "float": return typeof(float);
                case "double": return typeof(double);
                case "bool": return typeof(bool);
                case "string": return typeof(string);
                case "byte": return typeof(byte);
                case "long": return typeof(long);
                case "short": return typeof(short);
                case "char": return typeof(char);
                case "object": return typeof(object);
                default: return null;
            }
        }
        protected virtual Type ParseGenericType(string typeName)
        {
            return CommandInterpreterHelper.ParseGenericType(typeName, FindRawType);
        }

    }
}