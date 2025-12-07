using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

#if UNITY_2017_1_OR_NEWER
using UnityEngine;
using UnityEditor;
#endif
using EventFramework;

namespace EventFramework
{
    /// <summary>
    /// CommandInterpreterV2 单元测试
    /// 在 Unity 编辑器中运行: 菜单 -> Tools -> Run CommandInterpreterV2 Tests
    /// </summary>
    public static class CommandInterpreterTestsV2
    {
#if UNITY_2017_1_OR_NEWER
        public static Action<string> ErrorHandler = Debug.LogError;
        public static Action<string> LogHandler = Debug.Log;
#else
        public static Action<string> ErrorHandler = Console.WriteLine;
public static Action<string> LogHandler = Console.WriteLine;
#endif
        private static int passCount = 0;
        private static int failCount = 0;
        private static List<string> failedTests = new List<string>();

        [MenuItem("Tools/Run CommandInterpreterV2 Tests")]
        public static void RunAllTests()
        {
            passCount = 0;
            failCount = 0;
            failedTests.Clear();

            LogHandler("========== CommandInterpreterV2 单元测试开始 ==========");

            // 1. ICommandArg 类型测试
            TestCommandArgTypes();

            // 2. 字面量解析测试
            TestLiterals();

            // 3. 变量测试
            TestVariables();

            // 4. 预设变量测试
            TestPresetVariables();

            // 5. 算术运算符测试
            TestArithmeticOperators();

            // 6. 比较运算符测试
            TestComparisonOperators();

            // 7. 逻辑运算符测试
            TestLogicalOperators();

            // 8. 运算符优先级测试
            TestOperatorPrecedence();

            // 9. 构造函数调用测试
            TestConstructors();

            // 10. 数组创建和访问测试
            TestArrays();

            // 11. 成员访问测试
            TestMemberAccess();

            // 12. 方法调用测试
            TestMethodCalls();

            // 13. 方法重载测试
            TestMethodOverloads();

            // 14. 静态成员访问测试
            TestStaticMembers();

            // 15. 泛型类型测试
            TestGenerics();

            // 16. 字符串操作测试
            TestStringOperations();

            // 17. 赋值测试
            TestAssignment();

            // 18. 成员赋值测试
            TestMemberAssignment();

            // 19. 索引赋值测试
            TestIndexAssignment();

            // 20. 错误处理测试
            TestErrorHandling();

            // 21. 复杂表达式测试
            TestComplexExpressions();

            // 22. 私有成员访问测试
            TestPrivateMemberAccess();

            // 23. 嵌套调用测试
            TestNestedCalls();

            // 24. ICommandArg 接口能力测试
            TestInterfaceCapabilities();

            // 汇总
            LogHandler("========== 测试结果 ==========");
            LogHandler($"通过: {passCount}, 失败: {failCount}");

            if (failedTests.Count > 0)
            {
                ErrorHandler("失败的测试:");
                foreach (var test in failedTests)
                {
                    ErrorHandler($"  - {test}");
                }
            }
            else
            {
                LogHandler("<color=green>所有测试通过!</color>");
            }
        }

        #region 测试辅助方法

        private static void AssertEqual(object expected, object actual, string testName)
        {
            bool pass;
            if (expected == null && actual == null)
                pass = true;
            else if (expected == null || actual == null)
                pass = false;
            else if (expected is float ef && actual is float af)
                pass = Mathf.Approximately(ef, af);
            else if (expected is double ed && actual is double ad)
                pass = Math.Abs(ed - ad) < 0.0001;
            else if (expected is long el && actual is long al)
                pass = el == al;
            else if (expected is int ei && actual is long al2)
                pass = ei == al2;
            else if (expected is long el2 && actual is int ai)
                pass = el2 == ai;
            else
                pass = expected.Equals(actual);

            if (pass)
            {
                passCount++;
                LogHandler($"<color=green>[PASS]</color> {testName}");
            }
            else
            {
                failCount++;
                failedTests.Add(testName);
                ErrorHandler($"<color=red>[FAIL]</color> {testName}: 期望 {expected ?? "null"} ({expected?.GetType()?.Name ?? "null"}), 实际 {actual ?? "null"} ({actual?.GetType()?.Name ?? "null"})");
            }
        }

        private static void AssertTrue(bool condition, string testName)
        {
            if (condition)
            {
                passCount++;
                LogHandler($"<color=green>[PASS]</color> {testName}");
            }
            else
            {
                failCount++;
                failedTests.Add(testName);
                ErrorHandler($"<color=red>[FAIL]</color> {testName}: 条件不满足");
            }
        }

        private static void AssertFalse(bool condition, string testName)
        {
            AssertTrue(!condition, testName);
        }

        private static void AssertNotNull(object obj, string testName)
        {
            if (obj != null)
            {
                passCount++;
                LogHandler($"<color=green>[PASS]</color> {testName}");
            }
            else
            {
                failCount++;
                failedTests.Add(testName);
                ErrorHandler($"<color=red>[FAIL]</color> {testName}: 对象为 null");
            }
        }

        private static void AssertNull(object obj, string testName)
        {
            if (obj == null)
            {
                passCount++;
                LogHandler($"<color=green>[PASS]</color> {testName}");
            }
            else
            {
                failCount++;
                failedTests.Add(testName);
                ErrorHandler($"<color=red>[FAIL]</color> {testName}: 对象不为 null，实际值: {obj}");
            }
        }

        private static void AssertIsError(ICommandArg result, string testName)
        {
            if (result.IsError())
            {
                passCount++;
                LogHandler($"<color=green>[PASS]</color> {testName}: 错误消息 = {result.Format()}");
            }
            else
            {
                failCount++;
                failedTests.Add(testName);
                ErrorHandler($"<color=red>[FAIL]</color> {testName}: 期望错误，但得到 {result.Format()}");
            }
        }

        private static void AssertNotError(ICommandArg result, string testName)
        {
            if (!result.IsError())
            {
                passCount++;
                LogHandler($"<color=green>[PASS]</color> {testName}");
            }
            else
            {
                failCount++;
                failedTests.Add(testName);
                ErrorHandler($"<color=red>[FAIL]</color> {testName}: 意外错误 = {result.Format()}");
            }
        }

        private static void AssertArgType<T>(ICommandArg arg, string testName) where T : ICommandArg
        {
            if (arg is T)
            {
                passCount++;
                LogHandler($"<color=green>[PASS]</color> {testName}"); 
            }
            else
            {
                failCount++;
                failedTests.Add(testName);
                ErrorHandler($"<color=red>[FAIL]</color> {testName}: 期望 ICommandArg 类型 {typeof(T).Name}, 实际类型 {arg?.GetType()?.Name ?? "null"}");
            }
        }

        private static void AssertRawType<T>(ICommandArg arg, string testName)
        {
            var raw = arg.GetRawValue();
            if (raw is T)
            {
                passCount++;
                LogHandler($"<color=green>[PASS]</color> {testName}");
            }
            else
            {
                failCount++;
                failedTests.Add(testName);
                ErrorHandler($"<color=red>[FAIL]</color> {testName}: 期望原始类型 {typeof(T).Name}, 实际类型 {raw?.GetType()?.Name ?? "null"}");
            }
        }

        private static void AssertContains(string haystack, string needle, string testName)
        {
            if (haystack != null && haystack.Contains(needle))
            {
                passCount++;
                LogHandler($"<color=green>[PASS]</color> {testName}");
            }
            else
            {
                failCount++;
                failedTests.Add(testName);
                ErrorHandler($"<color=red>[FAIL]</color> {testName}: '{haystack}' 不包含 '{needle}'");
            }
        }

        #endregion

        #region 1. ICommandArg 类型测试

        private static void TestCommandArgTypes()
        {
            LogHandler("--- 1. ICommandArg 类型测试 ---");
            var interp = new CommandInterpreterV2();

            // NullArg
            var nullArg = interp.Evaluate("null");
            AssertArgType<CommandInterpreter_NullArg>(nullArg, "null 返回 NullArg");
            AssertNull(nullArg.GetRawValue(), "NullArg.GetRawValue() = null");

            // BoolArg
            var trueArg = interp.Evaluate("true");
            AssertArgType<CommandInterpreter_BoolArg>(trueArg, "true 返回 BoolArg");
            AssertEqual(true, trueArg.GetRawValue(), "BoolArg true 值");

            var falseArg = interp.Evaluate("false");
            AssertArgType<CommandInterpreter_BoolArg>(falseArg, "false 返回 BoolArg");
            AssertEqual(false, falseArg.GetRawValue(), "BoolArg false 值");

            // NumericArg (整数)
            var intArg = interp.Evaluate("42");
            AssertArgType<CommandInterpreter_NumericArg>(intArg, "42 返回 NumericArg");
            AssertTrue(((CommandInterpreter_NumericArg)intArg).IsInteger, "42 是整数");
            AssertEqual(42L, intArg.GetRawValue(), "NumericArg 整数值");

            // NumericArg (浮点数)
            var floatArg = interp.Evaluate("3.14");
            AssertArgType<CommandInterpreter_NumericArg>(floatArg, "3.14 返回 NumericArg");
            AssertFalse(((CommandInterpreter_NumericArg)floatArg).IsInteger, "3.14 不是整数");

            // StringArg
            var strArg = interp.Evaluate("\"hello\"");
            AssertArgType<CommandInterpreter_StringArg>(strArg, "\"hello\" 返回 StringArg");
            AssertEqual("hello", strArg.GetRawValue(), "StringArg 值");

            // ErrorArg
            var errArg = interp.Evaluate("notExistVar");
            AssertArgType<CommandInterpreter_ErrorArg>(errArg, "不存在的变量返回 ErrorArg");
            AssertTrue(errArg.IsError(), "ErrorArg.IsError() = true");
        }

        #endregion

        #region 2. 字面量解析测试

        private static void TestLiterals()
        {
            LogHandler("--- 2. 字面量解析测试 ---");
            var interp = new CommandInterpreterV2();

            // 整数
            AssertEqual(42L, interp.Evaluate("42").GetRawValue(), "整数字面量");
            AssertEqual(0L, interp.Evaluate("0").GetRawValue(), "零");

            // 长整数
            var longArg = interp.Evaluate("9999999999L");
            AssertArgType<CommandInterpreter_NumericArg>(longArg, "长整数类型");

            // 浮点数
            var floatResult = interp.Evaluate("3.14");
            AssertTrue(floatResult is CommandInterpreter_NumericArg nf && !nf.IsInteger, "浮点数字面量");

            var fSuffix = interp.Evaluate("2.5f");
            AssertTrue(fSuffix is CommandInterpreter_NumericArg nf2 && !nf2.IsInteger, "带f后缀的浮点数");

            // 布尔
            AssertEqual(true, interp.Evaluate("true").GetRawValue(), "布尔 true");
            AssertEqual(false, interp.Evaluate("false").GetRawValue(), "布尔 false");

            // 字符串
            AssertEqual("hello", interp.Evaluate("\"hello\"").GetRawValue(), "字符串字面量");
            AssertEqual("", interp.Evaluate("\"\"").GetRawValue(), "空字符串");
            AssertEqual("hello world", interp.Evaluate("\"hello world\"").GetRawValue(), "带空格的字符串");

            // null
            AssertNull(interp.Evaluate("null").GetRawValue(), "null 字面量");
        }

        #endregion

        #region 3. 变量测试

        private static void TestVariables()
        {
            LogHandler("--- 3. 变量测试 ---");
            var interp = new CommandInterpreterV2();

            // 注册变量
            interp.RegisterVariable("myInt", 100);
            interp.RegisterVariable("myFloat", 3.14f);
            interp.RegisterVariable("myString", "test");
            interp.RegisterVariable("myBool", true);
            interp.RegisterVariable("myNull", null);

            // 读取变量 - V2 返回 ICommandArg
            AssertEqual(100L, interp.Evaluate("myInt").GetRawValue(), "读取整数变量");
            AssertEqual("test", interp.Evaluate("myString").GetRawValue(), "读取字符串变量");
            AssertEqual(true, interp.Evaluate("myBool").GetRawValue(), "读取布尔变量");
            AssertNull(interp.Evaluate("myNull").GetRawValue(), "读取 null 变量");

            // 通过 Execute 赋值
            string result = interp.Execute("x = 42");
            AssertContains(result, "已赋值", "赋值返回消息");
            AssertEqual(42L, interp.GetVariable("x"), "GetVariable 获取赋值后的变量");

            // 获取变量名列表
            var names = new List<string>(interp.GetVariableNames());
            AssertTrue(names.Contains("myInt"), "变量名列表包含 myInt");
            AssertTrue(names.Contains("x"), "变量名列表包含 x");

            // 清空变量
            interp.ClearVariables();
            var cleared = interp.GetVariable("myInt");
            AssertTrue(cleared == null, "清空后变量为 NullArg");
        }

        #endregion

        #region 4. 预设变量测试

        private static void TestPresetVariables()
        {
            LogHandler("--- 4. 预设变量测试 ---");
            var interp = new CommandInterpreterV2();

            int counter = 0;
            interp.RegisterPresetVariable("#counter", () => ++counter);
            interp.RegisterPresetVariable("#pi", () => 3.14159f);
            interp.RegisterPresetVariable("noHash", () => "auto-added");

            // 每次访问动态计算
            AssertEqual(1L, interp.Evaluate("#counter").GetRawValue(), "预设变量第一次访问");
            AssertEqual(2L, interp.Evaluate("#counter").GetRawValue(), "预设变量第二次访问（动态计算）");
            AssertEqual(3L, interp.Evaluate("#counter").GetRawValue(), "预设变量第三次访问");

            // 预设变量只读
            string result = interp.Execute("#counter = 100");
            AssertContains(result, "只读", "预设变量赋值应失败");

            // 自动添加 # 前缀
            AssertEqual("auto-added", interp.Evaluate("#noHash").GetRawValue(), "自动添加#前缀的预设变量");

            // 获取预设变量名列表
            var names = new List<string>(interp.GetPresetVariableNames());
            AssertTrue(names.Contains("#counter"), "预设变量名列表包含 #counter");
            AssertTrue(names.Contains("#noHash"), "预设变量名列表包含 #noHash");
        }

        #endregion

        #region 5. 算术运算符测试

        private static void TestArithmeticOperators()
        {
            LogHandler("--- 5. 算术运算符测试 ---");
            var interp = new CommandInterpreterV2();

            // 基本运算 - V2 整数运算返回 long
            AssertEqual(7L, interp.Evaluate("3 + 4").GetRawValue(), "加法");
            AssertEqual(5L, interp.Evaluate("10 - 5").GetRawValue(), "减法");
            AssertEqual(12L, interp.Evaluate("3 * 4").GetRawValue(), "乘法");
            AssertEqual(1L, interp.Evaluate("7 % 3").GetRawValue(), "取模");

            // 除法总是返回浮点数
            var divResult = interp.Evaluate("15 / 3");
            AssertTrue(divResult is CommandInterpreter_NumericArg, "除法返回 NumericArg");

            // 浮点运算
            var floatAdd = interp.Evaluate("3.5 + 1.5");
            AssertNotError(floatAdd, "浮点加法");

            // 除零错误
            var divZero = interp.Evaluate("10 / 0");
            AssertIsError(divZero, "除零应返回错误");

            // 取模除零
            var modZero = interp.Evaluate("10 % 0");
            AssertIsError(modZero, "取模除零应返回错误");

            // 一元负号运算符
            AssertEqual(-5L, interp.Evaluate("-5").GetRawValue(), "负数字面量 -5");
            AssertEqual(-3.14, interp.Evaluate("-3.14").GetRawValue(), "负数浮点字面量 -3.14");

            interp.RegisterVariable("x", 10);
            AssertEqual(-10L, interp.Evaluate("-x").GetRawValue(), "一元负号 -x");
            
            interp.RegisterVariable("y", 3.5f);
            var negY = interp.Evaluate("-y");
            AssertNotError(negY, "一元负号 -y 不应报错");
    
            // 负号与表达式
            AssertEqual(-7L, interp.Evaluate("-(3 + 4)").GetRawValue(), "一元负号 -(3 + 4)");
            AssertEqual(15L, interp.Evaluate("5 - -10").GetRawValue(), "双负号 5 - -10");
            AssertEqual(-15L, interp.Evaluate("-5 * 3").GetRawValue(), "负数乘法 -5 * 3");
        }

        #endregion

        #region 6. 比较运算符测试

        private static void TestComparisonOperators()
        {
            LogHandler("--- 6. 比较运算符测试 ---");
            var interp = new CommandInterpreterV2();

            // 相等比较 - V2 返回 BoolArg
            AssertEqual(true, interp.Evaluate("5 == 5").GetRawValue(), "相等 true");
            AssertEqual(false, interp.Evaluate("5 == 3").GetRawValue(), "相等 false");
            AssertEqual(true, interp.Evaluate("5 != 3").GetRawValue(), "不等 true");
            AssertEqual(false, interp.Evaluate("5 != 5").GetRawValue(), "不等 false");

            // 大小比较
            AssertEqual(true, interp.Evaluate("5 > 3").GetRawValue(), "大于 true");
            AssertEqual(false, interp.Evaluate("3 > 5").GetRawValue(), "大于 false");
            AssertEqual(true, interp.Evaluate("3 < 5").GetRawValue(), "小于 true");
            AssertEqual(false, interp.Evaluate("5 < 3").GetRawValue(), "小于 false");
            AssertEqual(true, interp.Evaluate("5 >= 5").GetRawValue(), "大于等于 true");
            AssertEqual(true, interp.Evaluate("5 <= 5").GetRawValue(), "小于等于 true");

            // null 比较
            AssertEqual(true, interp.Evaluate("null == null").GetRawValue(), "null == null");
            interp.RegisterVariable("obj", new object());
            AssertEqual(false, interp.Evaluate("obj == null").GetRawValue(), "对象 == null");
            AssertEqual(true, interp.Evaluate("obj != null").GetRawValue(), "对象 != null");

            // 字符串比较
            AssertEqual(true, interp.Evaluate("\"abc\" == \"abc\"").GetRawValue(), "字符串相等");
            AssertEqual(false, interp.Evaluate("\"abc\" == \"def\"").GetRawValue(), "字符串不相等");
        }

        #endregion

        #region 7. 逻辑运算符测试

        private static void TestLogicalOperators()
        {
            LogHandler("--- 7. 逻辑运算符测试 ---");
            var interp = new CommandInterpreterV2();

            // AND
            AssertEqual(true, interp.Evaluate("true && true").GetRawValue(), "true && true");
            AssertEqual(false, interp.Evaluate("true && false").GetRawValue(), "true && false");
            AssertEqual(false, interp.Evaluate("false && true").GetRawValue(), "false && true");
            AssertEqual(false, interp.Evaluate("false && false").GetRawValue(), "false && false");

            // OR
            AssertEqual(true, interp.Evaluate("true || true").GetRawValue(), "true || true");
            AssertEqual(true, interp.Evaluate("true || false").GetRawValue(), "true || false");
            AssertEqual(true, interp.Evaluate("false || true").GetRawValue(), "false || true");
            AssertEqual(false, interp.Evaluate("false || false").GetRawValue(), "false || false");

            // NOT
            AssertEqual(false, interp.Evaluate("!true").GetRawValue(), "!true");
            AssertEqual(true, interp.Evaluate("!false").GetRawValue(), "!false");

            // 组合
            AssertEqual(true, interp.Evaluate("!false && true").GetRawValue(), "!false && true");
            AssertEqual(false, interp.Evaluate("!true || false").GetRawValue(), "!true || false");
        }

        #endregion

        #region 8. 运算符优先级测试

        private static void TestOperatorPrecedence()
        {
            LogHandler("--- 8. 运算符优先级测试 ---");
            var interp = new CommandInterpreterV2();

            // 乘除优先于加减
            AssertEqual(14L, interp.Evaluate("2 + 3 * 4").GetRawValue(), "2 + 3 * 4 = 14");
            AssertEqual(11L, interp.Evaluate("2 * 3 + 5").GetRawValue(), "2 * 3 + 5 = 11");

            // 括号优先
            AssertEqual(20L, interp.Evaluate("(2 + 3) * 4").GetRawValue(), "(2 + 3) * 4 = 20");

            // 比较运算符优先于逻辑运算符
            AssertEqual(true, interp.Evaluate("3 > 2 && 5 > 4").GetRawValue(), "3 > 2 && 5 > 4");
            AssertEqual(true, interp.Evaluate("3 < 2 || 5 > 4").GetRawValue(), "3 < 2 || 5 > 4");

            // && 优先于 ||
            AssertEqual(true, interp.Evaluate("true || false && false").GetRawValue(), "true || false && false = true");
            AssertEqual(false, interp.Evaluate("(true || false) && false").GetRawValue(), "(true || false) && false = false");

            // 复杂表达式
            AssertEqual(true, interp.Evaluate("1 + 2 == 3").GetRawValue(), "1 + 2 == 3");
            AssertEqual(true, interp.Evaluate("2 * 3 > 5").GetRawValue(), "2 * 3 > 5");
        }

        #endregion

        #region 9. 构造函数调用测试

        private static void TestConstructors()
        {
            LogHandler("--- 9. 构造函数调用测试 ---");
            var interp = new CommandInterpreterV2();

            // Vector3 构造
            var v3 = interp.Evaluate("new Vector3(1, 2, 3)");
            AssertNotError(v3, "new Vector3 不应报错");
            AssertRawType<Vector3>(v3, "new Vector3 类型");
            AssertEqual(new Vector3(1, 2, 3), v3.GetRawValue(), "new Vector3(1, 2, 3)");

            // Vector2 构造
            var v2 = interp.Evaluate("new Vector2(1.5, 2.5)");
            AssertRawType<Vector2>(v2, "new Vector2 类型");

            // Color 构造
            var color = interp.Evaluate("new Color(1, 0, 0, 1)");
            AssertRawType<Color>(color, "new Color 类型");
            AssertEqual(Color.red, color.GetRawValue(), "new Color(1, 0, 0, 1) = red");

            // 默认构造函数
            var v3Default = interp.Evaluate("new Vector3");
            AssertRawType<Vector3>(v3Default, "new Vector3 默认构造");
            AssertEqual(Vector3.zero, v3Default.GetRawValue(), "Vector3 默认值");

            // 无参数括号
            var v3Empty = interp.Evaluate("new Vector3()");
            AssertRawType<Vector3>(v3Empty, "new Vector3() 类型");

            // List 构造
            var list = interp.Evaluate("new List<int>()");
            AssertRawType<List<int>>(list, "new List<int>() 类型");
        }

        #endregion

        #region 10. 数组创建和访问测试

        private static void TestArrays()
        {
            LogHandler("--- 10. 数组创建和访问测试 ---");
            var interp = new CommandInterpreterV2();

            // 创建数组
            var intArray = interp.Evaluate("new int[5]");
            AssertRawType<int[]>(intArray, "new int[5] 类型");
            AssertEqual(5, ((int[])intArray.GetRawValue()).Length, "int[5] 长度");

            var floatArray = interp.Evaluate("new float[3]");
            AssertRawType<float[]>(floatArray, "new float[3] 类型");

            // 数组索引访问
            interp.RegisterVariable("arr", new int[] { 10, 20, 30, 40, 50 });
            AssertEqual(10, interp.Evaluate("arr[0]").GetRawValue(), "arr[0]");
            AssertEqual(30, interp.Evaluate("arr[2]").GetRawValue(), "arr[2]");
            AssertEqual(50, interp.Evaluate("arr[4]").GetRawValue(), "arr[4]");

            // 变量索引
            interp.Execute("idx = 2");
            AssertEqual(30, interp.Evaluate("arr[idx]").GetRawValue(), "arr[idx] 其中 idx=2");

            // 索引越界
            var outOfBounds = interp.Evaluate("arr[10]");
            AssertIsError(outOfBounds, "索引越界应返回错误");

            // 负数索引
            var negativeIdx = interp.Evaluate("arr[-1]");
            AssertIsError(negativeIdx, "负数索引应返回错误");

            // List 索引
            var list = new List<string> { "a", "b", "c" };
            interp.RegisterVariable("list", list);
            AssertEqual("b", interp.Evaluate("list[1]").GetRawValue(), "list[1]");
        }

        #endregion

        #region 11. 成员访问测试

        private static void TestMemberAccess()
        {
            LogHandler("--- 11. 成员访问测试 ---");
            var interp = new CommandInterpreterV2();

            // Vector3 成员访问
            interp.RegisterVariable("v", new Vector3(1, 2, 3));
            var x = interp.Evaluate("v.x");
            AssertNotError(x, "v.x 不应报错");

            // 属性访问
            var mag = interp.Evaluate("v.magnitude");
            AssertNotError(mag, "v.magnitude 不应报错");

            // 深层访问
            interp.RegisterVariable("v2", new Vector3(3, 4, 0));
            var normalized = interp.Evaluate("v2.normalized");
            AssertRawType<Vector3>(normalized, "v2.normalized 类型");

            // null 成员访问
            interp.RegisterVariable("nullObj", null);
            var nullAccess = interp.Evaluate("nullObj.x");
            AssertIsError(nullAccess, "null 对象成员访问应返回错误");

            // 不存在的成员
            var noMember = interp.Evaluate("v.notExist");
            AssertIsError(noMember, "不存在的成员应返回错误");
        }

        #endregion

        #region 12. 方法调用测试

        private static void TestMethodCalls()
        {
            LogHandler("--- 12. 方法调用测试 ---");
            var interp = new CommandInterpreterV2();

            // 字符串方法
            interp.RegisterVariable("str", "Hello World");
            AssertEqual("hello world", interp.Evaluate("str.ToLower()").GetRawValue(), "str.ToLower()");
            AssertEqual("HELLO WORLD", interp.Evaluate("str.ToUpper()").GetRawValue(), "str.ToUpper()");
            AssertEqual(11, interp.Evaluate("str.Length").GetRawValue(), "str.Length");
            AssertEqual(true, interp.Evaluate("str.Contains(\"World\")").GetRawValue(), "str.Contains(\"World\")");
            AssertEqual(false, interp.Evaluate("str.Contains(\"xyz\")").GetRawValue(), "str.Contains(\"xyz\")");
            AssertEqual("Hello", interp.Evaluate("str.Substring(0, 5)").GetRawValue(), "str.Substring(0, 5)");

            // List 方法
            var list = new List<int> { 1, 2, 3 };
            interp.RegisterVariable("list", list);
            AssertEqual(3, interp.Evaluate("list.Count").GetRawValue(), "list.Count");
            AssertEqual(true, interp.Evaluate("list.Contains(2)").GetRawValue(), "list.Contains(2)");

            // void 方法
            string result = interp.Execute("list.Add(4)");
            AssertContains(result, "执行成功", "void 方法返回执行成功");
            AssertEqual(4, list.Count, "Add 后 Count 增加");
        }

        #endregion

        #region 13. 方法重载测试

        private static void TestMethodOverloads()
        {
            LogHandler("--- 13. 方法重载测试 ---");
            var interp = new CommandInterpreterV2();

            // Mathf 静态方法（多种重载）
            AssertEqual(5, interp.Evaluate("Mathf.Max(3, 5)").GetRawValue(), "Mathf.Max(3, 5)");
            AssertEqual(1, interp.Evaluate("Mathf.Min(1, 5)").GetRawValue(), "Mathf.Min(1, 5)");
            AssertEqual(4, interp.Evaluate("Mathf.Abs(-4)").GetRawValue(), "Mathf.Abs(-4)");

            // Clamp
            AssertEqual(5, interp.Evaluate("Mathf.Clamp(3, 5, 10)").GetRawValue(), "Mathf.Clamp(3, 5, 10)");
            AssertEqual(10, interp.Evaluate("Mathf.Clamp(15, 5, 10)").GetRawValue(), "Mathf.Clamp(15, 5, 10)");
            AssertEqual(7, interp.Evaluate("Mathf.Clamp(7, 5, 10)").GetRawValue(), "Mathf.Clamp(7, 5, 10)");

            // 字符串方法重载
            interp.RegisterVariable("s", "Hello");
            AssertEqual("He", interp.Evaluate("s.Substring(0, 2)").GetRawValue(), "Substring(0, 2)");
            AssertEqual("llo", interp.Evaluate("s.Substring(2)").GetRawValue(), "Substring(2)");
        }

        #endregion

        #region 14. 静态成员访问测试

        private static void TestStaticMembers()
        {
            LogHandler("--- 14. 静态成员访问测试 ---");
            var interp = new CommandInterpreterV2();

            // Vector3 静态属性
            AssertEqual(Vector3.zero, interp.Evaluate("Vector3.zero").GetRawValue(), "Vector3.zero");
            AssertEqual(Vector3.one, interp.Evaluate("Vector3.one").GetRawValue(), "Vector3.one");
            AssertEqual(Vector3.up, interp.Evaluate("Vector3.up").GetRawValue(), "Vector3.up");
            AssertEqual(Vector3.forward, interp.Evaluate("Vector3.forward").GetRawValue(), "Vector3.forward");

            // Color 静态属性
            AssertEqual(Color.red, interp.Evaluate("Color.red").GetRawValue(), "Color.red");
            AssertEqual(Color.blue, interp.Evaluate("Color.blue").GetRawValue(), "Color.blue");

            // Mathf 静态字段
            var pi = interp.Evaluate("Mathf.PI");
            AssertNotError(pi, "Mathf.PI 不应报错");

            // 静态方法
            var lerp = interp.Evaluate("Mathf.Lerp(0, 10, 0.5)");
            AssertNotError(lerp, "Mathf.Lerp 不应报错");
        }

        #endregion

        #region 15. 泛型类型测试

        private static void TestGenerics()
        {
            LogHandler("--- 15. 泛型类型测试 ---");
            var interp = new CommandInterpreterV2();

            // List<T>
            var intList = interp.Evaluate("new List<int>()");
            AssertRawType<List<int>>(intList, "List<int>");

            var stringList = interp.Evaluate("new List<string>()");
            AssertRawType<List<string>>(stringList, "List<string>");

            // Dictionary<K,V>
            var dict = interp.Evaluate("new Dictionary<string, int>()");
            AssertRawType<Dictionary<string, int>>(dict, "Dictionary<string, int>");

            // HashSet<T>
            var hashSet = interp.Evaluate("new HashSet<float>()");
            AssertRawType<HashSet<float>>(hashSet, "HashSet<float>");
        }

        #endregion

        #region 16. 字符串操作测试

        private static void TestStringOperations()
        {
            LogHandler("--- 16. 字符串操作测试 ---");
            var interp = new CommandInterpreterV2();

            // 字符串连接
            AssertEqual("HelloWorld", interp.Evaluate("\"Hello\" + \"World\"").GetRawValue(), "字符串连接");
            AssertEqual("Value: 42", interp.Evaluate("\"Value: \" + 42").GetRawValue(), "字符串 + 数字");

            // 字符串索引 (IIndexable)
            var str = interp.Evaluate("\"Hello\"");
            AssertTrue(str.IsIndexable(), "字符串是 IIndexable");
            var charAt = interp.Evaluate("\"Hello\"[1]");
            AssertEqual("e", charAt.GetRawValue(), "字符串索引访问");
        }

        #endregion

        #region 17. 赋值测试

        private static void TestAssignment()
        {
            LogHandler("--- 17. 赋值测试 ---");
            var interp = new CommandInterpreterV2();

            // 简单赋值
            interp.Execute("a = 10");
            AssertEqual(10L, interp.GetVariable("a"), "简单赋值 a = 10");

            // 表达式赋值
            interp.Execute("b = 3 + 4");
            AssertEqual(7L, interp.GetVariable("b"), "表达式赋值 b = 3 + 4");

            // 变量赋值给变量
            interp.Execute("c = a");
            AssertEqual(10L, interp.GetVariable("c"), "变量赋值 c = a");

            // 对象赋值
            interp.Execute("v = new Vector3(1, 2, 3)");
            AssertEqual(typeof(Vector3), interp.GetVariable("v").GetType(), "对象赋值类型");

            // 覆盖赋值
            interp.Execute("a = 20");
            AssertEqual(20L, interp.GetVariable("a"), "覆盖赋值 a = 20");

            // 空字符串赋值
            interp.Execute("empty = \"\"");
            AssertEqual("", interp.GetVariable("empty"), "空字符串赋值");
        }

        #endregion

        #region 18. 成员赋值测试

        private static void TestMemberAssignment()
        {
            LogHandler("--- 18. 成员赋值测试 ---");
            var interp = new CommandInterpreterV2();

            // 测试类
            var testObj = new TestClassV2();
            interp.RegisterVariable("obj", testObj);

            // 字段赋值
            interp.Execute("obj.PublicField = 100");
            AssertEqual(100, testObj.PublicField, "公共字段赋值");

            // 属性赋值
            interp.Execute("obj.PublicProperty = 200");
            AssertEqual(200, testObj.PublicProperty, "公共属性赋值");

            // 嵌套对象成员赋值
            testObj.Nested = new TestClassV2.NestedClass();
            interp.Execute("obj.Nested.Value = 300");
            AssertEqual(300, testObj.Nested.Value, "嵌套对象成员赋值");
        }

        #endregion

        #region 19. 索引赋值测试

        private static void TestIndexAssignment()
        {
            LogHandler("--- 19. 索引赋值测试 ---");
            var interp = new CommandInterpreterV2();

            // 数组索引赋值
            var arr = new int[] { 1, 2, 3, 4, 5 };
            interp.RegisterVariable("arr", arr);
            interp.Execute("arr[0] = 100");
            AssertEqual(100, arr[0], "数组索引赋值 arr[0] = 100");

            interp.Execute("arr[4] = 500");
            AssertEqual(500, arr[4], "数组索引赋值 arr[4] = 500");

            // List 索引赋值
            var list = new List<string> { "a", "b", "c" };
            interp.RegisterVariable("list", list);
            interp.Execute("list[1] = \"modified\"");
            AssertEqual("modified", list[1], "List 索引赋值");

            // Dictionary 索引赋值
            var dict = new Dictionary<string, int> { { "key1", 1 }, { "key2", 2 } };
            interp.RegisterVariable("dict", dict);
            interp.Execute("dict[\"key1\"] = 100");
            AssertEqual(100, dict["key1"], "Dictionary 索引赋值");

            // 添加新键
            interp.Execute("dict[\"key3\"] = 300");
            AssertEqual(300, dict["key3"], "Dictionary 添加新键");

            // 索引越界赋值
            string result = interp.Execute("arr[10] = 999");
            AssertContains(result, "Error", "索引越界赋值应失败");
        }

        #endregion

        #region 20. 错误处理测试

        private static void TestErrorHandling()
        {
            LogHandler("--- 20. 错误处理测试 ---");
            var interp = new CommandInterpreterV2();

            // 未找到变量
            var notFound = interp.Evaluate("notExistVar");
            AssertIsError(notFound, "未找到变量应返回 ErrorArg");

            // 未找到类型
            var notFoundType = interp.Evaluate("new NotExistType()");
            AssertIsError(notFoundType, "未找到类型应返回 ErrorArg");

            // 未找到成员
            interp.RegisterVariable("v", Vector3.zero);
            var notFoundMember = interp.Evaluate("v.notExist");
            AssertIsError(notFoundMember, "未找到成员应返回 ErrorArg");

            // 空命令
            string emptyResult = interp.Execute("");
            AssertContains(emptyResult, "命令为空", "空命令处理");

            // 除零
            var divZero = interp.Evaluate("10 / 0");
            AssertIsError(divZero, "除零应返回 ErrorArg");
            AssertTrue(((CommandInterpreter_ErrorArg)divZero).ErrorCode == ErrorCodes.DivideByZero, "除零错误码正确");

            // 逻辑运算符类型错误
            var logicError = interp.Evaluate("5 && true");
            AssertIsError(logicError, "逻辑运算符类型错误应返回 ErrorArg");
        }

        #endregion

        #region 21. 复杂表达式测试

        private static void TestComplexExpressions()
        {
            LogHandler("--- 21. 复杂表达式测试 ---");
            var interp = new CommandInterpreterV2();

            // 链式方法调用
            interp.RegisterVariable("str", "  Hello World  ");
            AssertEqual("hello world", interp.Evaluate("str.Trim().ToLower()").GetRawValue(), "链式方法调用");

            // 嵌套函数调用
            AssertEqual(5, interp.Evaluate("Mathf.Max(Mathf.Min(5, 10), 3)").GetRawValue(), "嵌套函数调用");

            // 复杂算术
            AssertEqual(17L, interp.Evaluate("(2 + 3) * 4 - 3").GetRawValue(), "(2 + 3) * 4 - 3");
            AssertEqual(14L, interp.Evaluate("2 + 3 * 4").GetRawValue(), "2 + 3 * 4");

            // 比较表达式
            AssertEqual(true, interp.Evaluate("Mathf.Max(3, 5) == 5").GetRawValue(), "Mathf.Max(3, 5) == 5");

            // 变量 + 运算 + 方法
            interp.Execute("a = 10");
            interp.Execute("b = 20");
            var halfSum = interp.Evaluate("(a + b) / 2");
            AssertNotError(halfSum, "(a + b) / 2 不应报错");
        }

        #endregion

        #region 22. 私有成员访问测试

        private static void TestPrivateMemberAccess()
        {
            LogHandler("--- 22. 私有成员访问测试 ---");
            var interp = new CommandInterpreterV2();

            var testObj = new TestClassV2();
            interp.RegisterVariable("obj", testObj);

            // 私有字段访问
            var privateField = interp.Evaluate("obj.privateField");
            AssertNotError(privateField, "访问私有字段不应报错");
            AssertEqual(42, privateField.GetRawValue(), "私有字段值");

            // 私有属性访问
            var privateProp = interp.Evaluate("obj.PrivateProperty");
            AssertNotError(privateProp, "访问私有属性不应报错");
            AssertEqual("secret", privateProp.GetRawValue(), "私有属性值");

            // 私有方法访问
            var privateMethod = interp.Evaluate("obj.PrivateMethod()");
            AssertNotError(privateMethod, "调用私有方法不应报错");
            AssertEqual("private result", privateMethod.GetRawValue(), "私有方法返回值");

            // 保护成员访问
            var protectedField = interp.Evaluate("obj.protectedField");
            AssertNotError(protectedField, "访问保护字段不应报错");
        }

        #endregion

        #region 23. 嵌套调用测试

        private static void TestNestedCalls()
        {
            LogHandler("--- 23. 嵌套调用测试 ---");
            var interp = new CommandInterpreterV2();

            // 嵌套构造函数
            var result = interp.Evaluate("new Vector3(Mathf.Sin(0), Mathf.Cos(0), 0)");
            AssertRawType<Vector3>(result, "嵌套构造函数类型");

            // 方法参数中的运算
            AssertEqual(8, interp.Evaluate("Mathf.Max(5 + 3, 2 * 3)").GetRawValue(), "Mathf.Max(5 + 3, 2 * 3)");

            // 深层成员 + 方法调用
            interp.RegisterVariable("v", new Vector3(3, 4, 0));
            var normalizedY = interp.Evaluate("v.normalized.y");
            AssertNotError(normalizedY, "v.normalized.y 不应报错");

            // 数组元素的成员访问
            var vectors = new Vector3[] { new Vector3(1, 0, 0), new Vector3(0, 1, 0) };
            interp.RegisterVariable("vectors", vectors);
            var vecX = interp.Evaluate("vectors[0].x");
            AssertNotError(vecX, "vectors[0].x 不应报错");
        }

        #endregion

        #region 24. ICommandArg 接口能力测试

        private static void TestInterfaceCapabilities()
        {
            LogHandler("--- 24. ICommandArg 接口能力测试 ---");
            var interp = new CommandInterpreterV2();

            // INumeric 测试
            var numArg = interp.Evaluate("42");
            AssertTrue(numArg.CanNumeric(), "NumericArg.CanNumeric() = true");
            AssertTrue(numArg is INumeric, "NumericArg implements INumeric");

            // IStringArg 测试
            var strArg = interp.Evaluate("\"test\"");
            AssertTrue(strArg.CanString(), "StringArg.CanString() = true");
            AssertTrue(strArg is IStringArg, "StringArg implements IStringArg");

            // IIndexable 测试
            interp.RegisterVariable("list", new List<int> { 1, 2, 3 });
            var listArg = interp.Evaluate("list");
            AssertTrue(listArg.IsIndexable(), "ListArg.IsIndexable() = true");
            AssertTrue(listArg is IIndexable, "ListArg implements IIndexable");

            // StringArg 也是 IIndexable
            AssertTrue(strArg.IsIndexable(), "StringArg.IsIndexable() = true");

            // IMemberAccessible 测试
            interp.RegisterVariable("obj", new TestClassV2());
            var objArg = interp.Evaluate("obj");
            AssertTrue(objArg.HasMembers(), "ObjectArg.HasMembers() = true");
            AssertTrue(objArg is IMemberAccessible, "ObjectArg implements IMemberAccessible");

            // IFunctor 测试
            var typeArg = interp.Evaluate("Vector3");
            AssertTrue(typeArg.IsFunctor, "TypeArg.IsFunctor = true");
            AssertTrue(typeArg is IFunctor, "TypeArg implements IFunctor");

            // MethodGroupArg 测试
            var methodArg = interp.Evaluate("obj.PrivateMethod");
            AssertTrue(methodArg.IsFunctor, "MethodGroupArg.IsFunctor = true");
            AssertTrue(methodArg is IFunctor, "MethodGroupArg implements IFunctor");

            // 通过 IFunctor 调用
            if (typeArg is IFunctor functor)
            {
                ICommandArg[] args = { CommandInterpreter_NumericArg.FromInt(1), CommandInterpreter_NumericArg.FromInt(2), CommandInterpreter_NumericArg.FromInt(3) };
                int code = functor.Invoke(interp.Ruler, out ICommandArg result, args);
                AssertEqual(ErrorCodes.Success, code, "IFunctor.Invoke 成功");
                AssertRawType<Vector3>(result, "IFunctor.Invoke 返回 Vector3");
            }

            // ErrorArg 测试
            var errArg = interp.Evaluate("notExist");
            AssertTrue(errArg.IsError(), "ErrorArg.IsError() = true");
            AssertFalse(errArg.IsFunctor, "ErrorArg.IsFunctor = false");
            AssertFalse(errArg.CanNumeric(), "ErrorArg.CanNumeric() = false");
        }

        #endregion
    }

    /// <summary>
    /// V2 测试用的辅助类
    /// </summary>
    public class TestClassV2
    {
        public int PublicField = 10;
        public int PublicProperty { get; set; } = 20;

        private int privateField = 42;
        private string PrivateProperty { get; set; } = "secret";
        protected int protectedField = 100;

        public NestedClass Nested;

        private string PrivateMethod()
        {
            return "private result";
        }

        public class NestedClass
        {
            public int Value { get; set; }
        }
    }

} // namespace EventFramework
