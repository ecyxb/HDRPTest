using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UIElements;
using static SerializableCallback.Arg;

namespace EventFramework
{    
    #region 接口定义

    /// <summary>命令参数基础接口</summary>
    public interface ICommandArg
    {
        bool IsFunctor { get; }
        object GetRawValue();
        string Format();
    }

    /// <summary>可调用类型</summary>
    public interface IFunctor
    {
        int Invoke(CommandInterpreterRulerV2 ruler, out ICommandArg result, params ICommandArg[] args);
    }

    /// <summary>可数值运算类型</summary>
    public interface INumeric
    {
        double ToDouble();
        long ToLong();
        bool IsInteger { get; }
    }

    /// <summary>可字符串操作类型</summary>
    public interface IStringArg
    {
        string GetString();
    }



    /// <summary>可成员访问类型</summary>
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

    /// <summary>可索引类型</summary>
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
        protected Dictionary<string, CommandInterpreter_TypeArg> cacheTypes = new Dictionary<string, CommandInterpreter_TypeArg>();

        public virtual Dictionary<Type, int> GetNumericTypesIdx => new Dictionary<Type, int>()
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
        private Dictionary<Type, int> numericTypesIdxCache = null;
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

        public virtual Func<object, CommandInterpreter_NumericArg>[] GetNumericTypesFuncs => new Func<object, CommandInterpreter_NumericArg>[]
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
        private Func<object, CommandInterpreter_NumericArg>[] numericTypesFuncsCache = null;
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
        // 从 A转到B转换评分，
        // 要求A如果可能为负数，则一定是负数，
        // 比如1000000是uint,而不是int，所以int对
        //public static int[][] ChangeNumericTypeScore = new int[][]
        //{
        //    //        sbyte  byte   short  ushort  int  uint  long  float  double
        //    new int[] {1000,   -1,   990,   990,   980,  980,  970,  900,   900}, // sbyte
        //    new int[] {-1,   1000,   990,   990,   980,  980,  970,  900,   900}, // byte
        //    new int[] {-1,     -1,  1000,   995,   990,  990,  980,  900,   900}, // short
        //    new int[] {-1,     -1,   -1,   1000,   990,  990,  980,  900,   900}, // ushort
        //    new int[] {-1,     -1,   -1,    -1,   1000,  -1,   990,  900,   900}, // int
        //    new int[] {-1,     -1,   -1,    -1,    -1,  1000,  990,  900,   900}, // uint
        //    new int[] {-1,     -1,   -1,    -1,    -1,   -1,  1000,  900,   900}, // long
        //    new int[] {-1,     -1,   -1,    -1,    -1,   -1,   -1,  1000,   990}, // float
        //    new int[] {-1,     -1,   -1,    -1,    -1,   -1,   -1,   990,   1000},// double 高精度转换不考虑溢出
        //};

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
        /// 根据类型和命令参数，计算匹配分数，-1表示不匹配。返回的正数是减分制，越低分越好
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
            // 数值类型之间最粗略的必须匹配
            if (isNumericParam != isNumericArg)
            {
                return -1;
            }
            if (isNumericArg)
            {
                //如果都是数值类型
                INumeric numeric = (INumeric)commandArg;
                if (numeric.IsInteger)
                {
                    //如果arg是整数
                    if (isFloatParam)
                    {
                        //整形转浮点，可以通过，但优先级低
                        return 1000;
                    }
                    else
                    {
                        //整形转整形，检查范围
                        var limit = maxIntLimits[paramTypeIdx];
                        long argValue = numeric.ToLong();
                        if (argValue < limit[0] || argValue > limit[1])
                        {
                            //溢出，不能转换
                            return -1;
                        }
                        //只要没有溢出，认为所有整型的优先级相同，再区分会很复杂且意义不大
                        return 0;
                    }
                }
                else
                {
                    //如果arg是浮点
                    if (isFloatParam)
                    {
                        //浮点转浮点，一定转换
                        return 0;
                    }
                    else
                    {
                        //浮点转整形，是不允许的
                        return -1;
                    }
                }
            }
            else
            {
                object rawValue = commandArg.GetRawValue();
                // 非数值类型
                if (rawValue == null)
                {
                    if (paramType.IsValueType)
                    {
                        //值类型参数不匹配
                        return -1;
                    }
                    else
                    {
                        //引用类型参数，认为匹配，不用加分
                        return 0;
                    }

                }
                else
                {
                    Type argType = rawValue.GetType();
                    if (paramType == argType)
                    {
                        return 0; // 完全匹配
                    }
                    else if (paramType.IsAssignableFrom(argType))
                    {
                        return 500; // 可赋值匹配
                    }
                    else
                    {
                        return -1;
                    }
                }
            }

        }

        /// <summary>
        /// 查找最佳匹配的方法，不考虑可变参数，考虑默认参数
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
                // 如果参数数量多于传入参数，一定是不行的
                if (args.Length > parameters.Length) continue;
                int score = 0;
                bool match = true;

                for (int i = args.Length; i < parameters.Length; i++)
                {
                    // 如果多出来的参数没有默认值，一定是不行的
                    if (!parameters[i].HasDefaultValue)
                    {
                        match = false;
                        break;
                    }
                    //用默认参数，认为是比完全匹配差一点
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
                    //减分制，分数越高越好
                    //这是为了让参数少的时候能尽可能调用参数少的方法
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
                Type genericType = _ParseGenericType(typeName);
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
        private Type _ParseGenericType(string typeName)
        {
            int bracketStart = typeName.IndexOf('<');
            if (bracketStart < 0) return null;

            string baseName = typeName.Substring(0, bracketStart).Trim();
            string argsStr = typeName.Substring(bracketStart + 1, typeName.Length - bracketStart - 2);

            var typeArgs = _SplitGenericArguments(argsStr);
            Type genericDef = _FindGenericTypeDefinition(baseName, typeArgs.Length);
            if (genericDef == null) return null;

            Type[] argTypes = new Type[typeArgs.Length];
            for (int i = 0; i < typeArgs.Length; i++)
            {
                argTypes[i] = FindRawType(typeArgs[i].Trim());
                if (argTypes[i] == null) return null;
            }

            try { return genericDef.MakeGenericType(argTypes); }
            catch { return null; }
        }
        private string[] _SplitGenericArguments(string argsStr)
        {
            var args = new List<string>();
            int depth = 0, start = 0;

            for (int i = 0; i < argsStr.Length; i++)
            {
                char c = argsStr[i];
                if (c == '<') depth++;
                else if (c == '>') depth--;
                else if (c == ',' && depth == 0)
                {
                    args.Add(argsStr.Substring(start, i - start).Trim());
                    start = i + 1;
                }
            }
            if (start < argsStr.Length) args.Add(argsStr.Substring(start).Trim());
            return args.ToArray();
        }

        private Type _FindGenericTypeDefinition(string baseName, int typeParamCount)
        {
            var commonGenerics = new Dictionary<string, Type>
            {
                { "List", typeof(List<>) },
                { "Dictionary", typeof(Dictionary<,>) },
                { "HashSet", typeof(HashSet<>) },
            };

            if (commonGenerics.TryGetValue(baseName, out Type common) &&
                common.GetGenericArguments().Length == typeParamCount)
                return common;

            string fullName = baseName + "`" + typeParamCount;
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                .FirstOrDefault(t => t.IsGenericTypeDefinition &&
                    (t.Name == fullName || t.GetGenericArguments().Length == typeParamCount && t.Name.StartsWith(baseName)));
        }

        public static object ConvertArg(object arg, Type targetType)
        {
            if (arg == null) return null;
            if (targetType.IsAssignableFrom(arg.GetType())) return arg;
            return Convert.ChangeType(arg, targetType);
        }
        public static object[] ConvertArgsWitdhDefaults(ICommandArg[] args, ParameterInfo[] parameters)
        {
            object[] convertedArgs = new object[parameters.Length];
            int i = 0;
            for (; i < args.Length; i++)
            {
                convertedArgs[i] = ConvertArg(args[i].GetRawValue(), parameters[i].ParameterType);
            }
            for (; i < parameters.Length; i++)
            {
                convertedArgs[i] = parameters[i].DefaultValue;
            }
            return convertedArgs;
        }

    }
}