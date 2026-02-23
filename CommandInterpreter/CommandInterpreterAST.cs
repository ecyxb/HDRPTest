using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EventFramework.AST;
using UnityEngine;

namespace EventFramework
{
    /// <summary>
    /// 基于 AST 的命令解释器
    /// 先将命令解析为 AST，再执行
    /// </summary>
    public class CommandInterpreterAST : ICanRegisterPresetCommand
    {
        private readonly Dictionary<string, ICommandArg> _variables = new Dictionary<string, ICommandArg>();
        private readonly Dictionary<string, Func<ICommandArg>> _presetVariables = new Dictionary<string, Func<ICommandArg>>();

        public CommandInterpreterRuler Ruler { get; private set; } = new CommandInterpreterRuler();

        public readonly ASTParser _parser = new ASTParser();

        #region 公共 API

        /// <summary>
        /// 注册变量
        /// </summary>
        public void RegisterVariable(string name, object obj)
        {
            _variables[name] = CommandArgFactory.Wrap(obj);
        }

        /// <summary>
        /// 注册预设变量 (以 # 开头，只读)
        /// </summary>
        public void RegisterPresetVariable(string name, Func<object> getter)
        {
            if (!name.StartsWith("#")) name = "#" + name;
            _presetVariables[name] = () => CommandArgFactory.Wrap(getter());
        }

        /// <summary>
        /// 注册预设函数
        /// </summary>
        public void RegisterPresetFunc(string name, object func)
        {
            if (!name.StartsWith("#")) name = "#" + name;
            _presetVariables[name] = () => CommandArgFactory.Wrap(func);
        }

        /// <summary>
        /// 注册预设静态方法
        /// </summary>
        public void RegisterPresetFunc(string name, Type type, string funcname)
        {
            if (!name.StartsWith("#")) name = "#" + name;
            if (type == null) return;

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic)
     .Where(m => m.Name == funcname).ToArray();
            if (methods.Length == 0) return;

            _presetVariables[name] = () => new CommandInterpreter_MethodGroupArg(type, methods);
        }

        /// <summary>
        /// 执行命令字符串
        /// </summary>
        public string Execute(string input)
        {
            input = input?.Trim();
            if (string.IsNullOrEmpty(input)) return "命令为空";
            // 解析为 AST
            var statements = _parser.ParseMultiple(input);

            if (statements.Count == 0)
                return "命令为空";

            var results = new List<string>();
            var evaluator = new ASTEvaluator(Ruler, _variables, _presetVariables);

            foreach (var stmt in statements)
            {
                ICommandArg result = evaluator.Evaluate(stmt);

                if (result.IsError())
                {
                    results.Add(result.Format());
                    results.Add("(后续语句未执行)");
                    break;
                }

                if (stmt is AssignmentNode assignment)
                {
                    string targetName = GetAssignmentTargetName(assignment.Target);
                    results.Add($"变量 {targetName} 已赋值 = {result.Format()}");
                }
                else if (result is CommandInterpreter_VoidArg)
                {
                    results.Add("执行成功");
                }
                else
                {
                    results.Add($"结果: {result.Format()}");
                }
            }

            return string.Join("\n", results);
        }

        ///// <summary>
        ///// 解析并返回 AST (用于调试或高级用途)
        ///// </summary>
        //public ASTNode Parse(string input)
        //{
        //    return _parser.Parse(input);
        //}

        ///// <summary>
        ///// 解析多条语句并返回 AST 列表
        ///// </summary>
        //public List<ASTNode> ParseMultiple(string input)
        //{
        //    return _parser.ParseMultiple(input);
        //}

        /// <summary>
        /// 执行 AST
        /// </summary>
        protected ICommandArg Evaluate(ASTNode node)
        {
            var evaluator = new ASTEvaluator(Ruler, _variables, _presetVariables);
            return evaluator.Evaluate(node);
        }

        /// <summary>
        /// 执行表达式并返回结果
        /// </summary>
        public ICommandArg Evaluate(string expr)
        {
            ASTNode node = _parser.Parse(expr?.Trim() ?? string.Empty);
            return Evaluate(node);
        }

        /// <summary>
        /// 获取变量值
        /// </summary>
        public object GetVariable(string name)
        {
            if (_variables.TryGetValue(name, out var value))
            {
                return value.GetRawValue();
            }
            return null;
        }

        /// <summary>
        /// 获取所有变量名
        /// </summary>
        public IEnumerable<string> GetVariableNames() => _variables.Keys;

        /// <summary>
        /// 获取所有预设变量名
        /// </summary>
        public IEnumerable<string> GetPresetVariableNames() => _presetVariables.Keys;

        /// <summary>
        /// 清除所有变量
        /// </summary>
        public void ClearVariables() => _variables.Clear();

        #endregion

        #region 辅助方法

        private string GetAssignmentTargetName(ASTNode target)
        {
            if (target is IdentifierNode idNode)
                return idNode.Name;

            if (target is MemberAccessNode memberNode)
                return GetAssignmentTargetName(memberNode.Target) + "." + memberNode.MemberName;

            if (target is IndexAccessNode indexNode)
                return GetAssignmentTargetName(indexNode.Target) + "[...]";

            return "unknown";
        }

        #endregion
    }
}
