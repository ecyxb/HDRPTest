using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EventFramework
{
    /// <summary>方法组，支持泛型方法</summary>
    public class CommandInterpreter_MethodGroupArg : ICommandArg, IFunctor, IGenericArg
    {
        public object Target { get; }
        public MethodInfo[] Methods { get; }
        public bool IsFunctor => true;

        /// <summary>
        /// 指定的泛型类型参数（用于泛型方法调用）
        /// </summary>
        public Type[] GenericTypes { get; private set; }


        public CommandInterpreter_MethodGroupArg(object target, MethodInfo[] methods)
        {
            Target = target;
            Methods = methods;
            GenericTypes = null;
        }

        public object GetRawValue() => Methods;
        public string Format() => $"Method:{Methods[0].Name}({Methods.Length} overloads)";

        /// <summary>
        /// 创建带有指定泛型类型参数的方法组副本
        /// </summary>
        public CommandInterpreter_MethodGroupArg WithGenericTypes(Type[] typeArgs)
        {
            return new CommandInterpreter_MethodGroupArg(Target, Methods)
            {
                GenericTypes = typeArgs
            };
        }

        public int Invoke(CommandInterpreterRulerV2 ruler, out ICommandArg result, params ICommandArg[] args)
        {
            // 如果指定了泛型类型参数，先尝试构造泛型方法
            if (GenericTypes != null && GenericTypes.Length > 0)
            {
                return InvokeGenericMethod(ruler, out result, args);
            }

            // 非泛型方法或自动推断泛型参数
            MethodInfo bestMatch = ruler.FindBestMatch(Methods, args);

            if (bestMatch == null)
            {
                // 尝试查找泛型方法并自动推断类型参数
                var genericResult = TryInvokeWithTypeInference(ruler, args, out result);
                if (genericResult != ErrorCodes.InvalidArgumentType)
                {
                    return genericResult;
                }

                result = CommandInterpreter_ErrorArg.Create(ErrorCodes.InvalidArgumentType, "未找到匹配的方法重载");
                return ErrorCodes.InvalidArgumentType;
            }
            try
            {
                object[] convertedArgs = CommandInterpreterHelper.ConvertArgsWitdhDefaults(args, bestMatch.GetParameters());
                object ret = bestMatch.Invoke(Target, convertedArgs);
                result = bestMatch.ReturnType == typeof(void) ? CommandInterpreter_VoidArg.Instance : CommandArgFactory.Wrap(ret);
                return ErrorCodes.Success;
            }
            catch (Exception ex)
            {
                result = CommandInterpreter_ErrorArg.Create(ErrorCodes.UnknownError, ex.InnerException?.Message ?? ex.Message);
                return ErrorCodes.UnknownError;
            }
        }

        /// <summary>
        /// 调用带有显式泛型类型参数的方法
        /// </summary>
        private int InvokeGenericMethod(CommandInterpreterRulerV2 ruler, out ICommandArg result, ICommandArg[] args)
        {
            // 筛选出泛型方法
            var genericMethods = Methods.Where(m => m.IsGenericMethodDefinition).ToArray();

            if (genericMethods.Length == 0)
            {
                result = CommandInterpreter_ErrorArg.Create(ErrorCodes.InvalidArgumentType,
                    $"方法 {Methods[0].Name} 不是泛型方法");
                return ErrorCodes.InvalidArgumentType;
            }

            // 尝试找到匹配的泛型方法
            foreach (var genericMethod in genericMethods)
            {
                var genericParams = genericMethod.GetGenericArguments();
                if (genericParams.Length != GenericTypes.Length)
                    continue;

                try
                {
                    // 构造具体的泛型方法
                    MethodInfo constructedMethod = genericMethod.MakeGenericMethod(GenericTypes);

                    // 检查参数是否匹配
                    var parameters = constructedMethod.GetParameters();
                    if (!IsArgsCompatible(ruler, parameters, args))
                        continue;

                    // 调用方法
                    object[] convertedArgs = CommandInterpreterHelper.ConvertArgsWitdhDefaults(args, parameters);
                    object ret = constructedMethod.Invoke(Target, convertedArgs);
                    result = constructedMethod.ReturnType == typeof(void)
                        ? CommandInterpreter_VoidArg.Instance
                        : CommandArgFactory.Wrap(ret);
                    return ErrorCodes.Success;
                }
                catch (ArgumentException)
                {
                    // 泛型约束不满足，尝试下一个
                    continue;
                }
                catch (Exception ex)
                {
                    result = CommandInterpreter_ErrorArg.Create(ErrorCodes.UnknownError,
                        ex.InnerException?.Message ?? ex.Message);
                    return ErrorCodes.UnknownError;
                }
            }

            result = CommandInterpreter_ErrorArg.Create(ErrorCodes.InvalidArgumentType,
                $"未找到匹配的泛型方法 {Methods[0].Name}<{string.Join(", ", GenericTypes.Select(t => t.Name))}>");
            return ErrorCodes.InvalidArgumentType;
        }

        /// <summary>
        /// 尝试通过类型推断调用泛型方法
        /// </summary>
        private int TryInvokeWithTypeInference(CommandInterpreterRulerV2 ruler, ICommandArg[] args, out ICommandArg result)
        {
            result = null;

            var genericMethods = Methods.Where(m => m.IsGenericMethodDefinition).ToArray();
            if (genericMethods.Length == 0)
            {
                return ErrorCodes.InvalidArgumentType;
            }

            foreach (var genericMethod in genericMethods)
            {
                var inferredTypes = TryInferGenericTypes(genericMethod, args);
                if (inferredTypes == null)
                    continue;

                try
                {
                    MethodInfo constructedMethod = genericMethod.MakeGenericMethod(inferredTypes);
                    var parameters = constructedMethod.GetParameters();

                    if (!IsArgsCompatible(ruler, parameters, args))
                        continue;

                    object[] convertedArgs = CommandInterpreterHelper.ConvertArgsWitdhDefaults(args, parameters);
                    object ret = constructedMethod.Invoke(Target, convertedArgs);
                    result = constructedMethod.ReturnType == typeof(void)
                        ? CommandInterpreter_VoidArg.Instance
                        : CommandArgFactory.Wrap(ret);
                    return ErrorCodes.Success;
                }
                catch
                {
                    continue;
                }
            }

            return ErrorCodes.InvalidArgumentType;
        }

        /// <summary>
        /// 尝试从参数推断泛型类型
        /// </summary>
        private Type[] TryInferGenericTypes(MethodInfo genericMethod, ICommandArg[] args)
        {
            var genericParams = genericMethod.GetGenericArguments();
            var methodParams = genericMethod.GetParameters();
            var inferredTypes = new Type[genericParams.Length];

            // 参数数量检查
            int requiredParams = methodParams.Count(p => !p.HasDefaultValue);
            if (args.Length < requiredParams || args.Length > methodParams.Length)
                return null;

            // 尝试从每个参数推断类型
            for (int i = 0; i < args.Length && i < methodParams.Length; i++)
            {
                var paramType = methodParams[i].ParameterType;
                var argValue = args[i].GetRawValue();

                if (argValue == null)
                    continue;

                var argType = argValue.GetType();

                // 如果参数类型是泛型参数
                if (paramType.IsGenericParameter)
                {
                    int genericIndex = Array.IndexOf(genericParams, paramType);
                    if (genericIndex >= 0)
                    {
                        if (inferredTypes[genericIndex] == null)
                            inferredTypes[genericIndex] = argType;
                        else if (inferredTypes[genericIndex] != argType)
                            return null; // 类型冲突
                    }
                }
                // 如果参数类型包含泛型参数（如 IEnumerable<T>）
                else if (paramType.ContainsGenericParameters)
                {
                    var inferred = InferFromGenericType(paramType, argType, genericParams);
                    if (inferred != null)
                    {
                        for (int j = 0; j < inferred.Length; j++)
                        {
                            if (inferred[j] != null)
                            {
                                if (inferredTypes[j] == null)
                                    inferredTypes[j] = inferred[j];
                                else if (inferredTypes[j] != inferred[j])
                                    return null;
                            }
                        }
                    }
                }
            }

            // 检查是否所有泛型参数都已推断
            if (inferredTypes.Any(t => t == null))
                return null;

            return inferredTypes;
        }

        /// <summary>
        /// 从泛型类型中推断类型参数
        /// </summary>
        private Type[] InferFromGenericType(Type paramType, Type argType, Type[] genericParams)
        {
            var result = new Type[genericParams.Length];

            if (!paramType.IsGenericType)
                return result;

            var paramGenericDef = paramType.GetGenericTypeDefinition();
            var paramGenericArgs = paramType.GetGenericArguments();

            // 查找 argType 实现的匹配接口或基类
            Type matchingType = null;

            if (argType.IsGenericType && argType.GetGenericTypeDefinition() == paramGenericDef)
            {
                matchingType = argType;
            }
            else
            {
                // 检查接口
                foreach (var iface in argType.GetInterfaces())
                {
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == paramGenericDef)
                    {
                        matchingType = iface;
                        break;
                    }
                }

                // 检查基类
                if (matchingType == null)
                {
                    var baseType = argType.BaseType;
                    while (baseType != null)
                    {
                        if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == paramGenericDef)
                        {
                            matchingType = baseType;
                            break;
                        }
                        baseType = baseType.BaseType;
                    }
                }
            }

            if (matchingType != null)
            {
                var matchingArgs = matchingType.GetGenericArguments();
                for (int i = 0; i < paramGenericArgs.Length && i < matchingArgs.Length; i++)
                {
                    if (paramGenericArgs[i].IsGenericParameter)
                    {
                        int idx = Array.IndexOf(genericParams, paramGenericArgs[i]);
                        if (idx >= 0)
                            result[idx] = matchingArgs[i];
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 检查参数是否兼容
        /// </summary>
        private bool IsArgsCompatible(CommandInterpreterRulerV2 ruler, ParameterInfo[] parameters, ICommandArg[] args)
        {
            int requiredParams = parameters.Count(p => !p.HasDefaultValue);
            if (args.Length < requiredParams || args.Length > parameters.Length)
                return false;

            for (int i = 0; i < args.Length; i++)
            {
                if (ruler.GetMatchScore(parameters[i].ParameterType, args[i]) < 0)
                    return false;
            }

            return true;
        }
    }
}