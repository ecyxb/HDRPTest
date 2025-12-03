
using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_2017_1_OR_NEWER
using UnityEngine;
using UnityEditor;
#endif
using EventFramework;

namespace EventFramework.Editor
{

    /// <summary>
    /// CommandInterpreter 单元测试
    /// 在 Unity 编辑器中运行: 菜单 -> Tools -> Run CommandInterpreter Tests
    /// </summary>
    public static class CommandInterpreterTests
    {
        private static int passCount = 0;
        private static int failCount = 0;
        private static List<string> failedTests = new List<string>();

        [MenuItem("Tools/Run CommandInterpreter Tests")]
        public static void RunAllTests()
        {
            passCount = 0;
            failCount = 0;
            failedTests.Clear();

            EOHelper.Log("========== CommandInterpreter 单元测试开始 ==========");

            // 1. 字面量解析测试
            TestLiterals();

            // 2. 变量测试
            TestVariables();

            // 3. 预设变量测试
            TestPresetVariables();

            // 4. 算术运算符测试
            TestArithmeticOperators();

            // 5. 比较运算符测试
            TestComparisonOperators();

            // 6. 逻辑运算符测试
            TestLogicalOperators();

            // 7. 运算符优先级测试
            TestOperatorPrecedence();

            // 8. 构造函数调用测试
            TestConstructors();

            // 9. 数组创建和访问测试
            TestArrays();

            // 10. 成员访问测试
            TestMemberAccess();

            // 11. 方法调用测试
            TestMethodCalls();

            // 12. 方法重载测试
            TestMethodOverloads();

            // 13. 静态成员访问测试
            TestStaticMembers();

            // 14. 泛型类型测试
            TestGenerics();

            // 15. 字符串操作测试
            TestStringOperations();

            // 16. 赋值测试
            TestAssignment();

            // 17. 成员赋值测试
            TestMemberAssignment();

            // 18. 索引赋值测试
            TestIndexAssignment();

            // 19. 错误处理测试
            TestErrorHandling();

            // 20. 复杂表达式测试
            TestComplexExpressions();

            // 21. 私有成员访问测试
            TestPrivateMemberAccess();

            // 22. 嵌套调用测试
            TestNestedCalls();

            // 汇总
            EOHelper.Log("========== 测试结果 ==========");
            EOHelper.Log($"通过: {passCount}, 失败: {failCount}");

            if (failedTests.Count > 0)
            {
                EOHelper.LogError("失败的测试:");
                foreach (var test in failedTests)
                {
                    EOHelper.LogError($"  - {test}");
                }
            }
            else
            {
                EOHelper.Log("<color=green>所有测试通过!</color>");
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
            else
                pass = expected.Equals(actual);

            if (pass)
            {
                passCount++;
                EOHelper.Log($"<color=green>[PASS]</color> {testName}");
            }
            else
            {
                failCount++;
                failedTests.Add(testName);
                EOHelper.LogError($"<color=red>[FAIL]</color> {testName}: 期望 {expected ?? "null"} ({expected?.GetType()?.Name ?? "null"}), 实际 {actual ?? "null"} ({actual?.GetType()?.Name ?? "null"})");
            }
        }

        private static void AssertTrue(bool condition, string testName)
        {
            if (condition)
            {
                passCount++;
                EOHelper.Log($"<color=green>[PASS]</color> {testName}");
            }
            else
            {
                failCount++;
                failedTests.Add(testName);
                EOHelper.LogError($"<color=red>[FAIL]</color> {testName}: 条件不满足");
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
                EOHelper.Log($"<color=green>[PASS]</color> {testName}");
            }
            else
            {
                failCount++;
                failedTests.Add(testName);
                EOHelper.LogError($"<color=red>[FAIL]</color> {testName}: 对象为 null");
            }
        }

        private static void AssertNull(object obj, string testName)
        {
            if (obj == null)
            {
                passCount++;
                EOHelper.Log($"<color=green>[PASS]</color> {testName}");
            }
            else
            {
                failCount++;
                failedTests.Add(testName);
                EOHelper.LogError($"<color=red>[FAIL]</color> {testName}: 对象不为 null，实际值: {obj}");
            }
        }

        private static void AssertIsError(CommandInterpreter interp, object result, string testName)
        {
            if (interp.IsError(result))
            {
                passCount++;
                EOHelper.Log($"<color=green>[PASS]</color> {testName}: 错误消息 = {result}");
            }
            else
            {
                failCount++;
                failedTests.Add(testName);
                EOHelper.LogError($"<color=red>[FAIL]</color> {testName}: 期望错误，但得到 {result}");
            }
        }

        private static void AssertNotError(CommandInterpreter interp, object result, string testName)
        {
            if (!interp.IsError(result))
            {
                passCount++;
                EOHelper.Log($"<color=green>[PASS]</color> {testName}");
            }
            else
            {
                failCount++;
                failedTests.Add(testName);
                EOHelper.LogError($"<color=red>[FAIL]</color> {testName}: 意外错误 = {result}");
            }
        }

        private static void AssertType<T>(object obj, string testName)
        {
            if (obj is T)
            {
                passCount++;
                EOHelper.Log($"<color=green>[PASS]</color> {testName}");
            }
            else
            {
                failCount++;
                failedTests.Add(testName);
                EOHelper.LogError($"<color=red>[FAIL]</color> {testName}: 期望类型 {typeof(T).Name}, 实际类型 {obj?.GetType()?.Name ?? "null"}");
            }
        }

        private static void AssertContains(string haystack, string needle, string testName)
        {
            if (haystack != null && haystack.Contains(needle))
            {
                passCount++;
                EOHelper.Log($"<color=green>[PASS]</color> {testName}");
            }
            else
            {
                failCount++;
                failedTests.Add(testName);
                EOHelper.LogError($"<color=red>[FAIL]</color> {testName}: '{haystack}' 不包含 '{needle}'");
            }
        }

        #endregion

        #region 1. 字面量解析测试

        private static void TestLiterals()
        {
            EOHelper.Log("--- 1. 字面量解析测试 ---");
            var interp = new CommandInterpreter();

            // 整数
            AssertEqual(42, interp.Evaluate("42"), "整数字面量");
            AssertEqual(0, interp.Evaluate("0"), "零");
            AssertEqual(-5, interp.Evaluate("-5"), "负整数");

            // 浮点数
            var floatResult = interp.Evaluate("3.14");
            AssertTrue(floatResult is float && Mathf.Approximately((float)floatResult, 3.14f), "浮点数字面量");

            floatResult = interp.Evaluate("2.5f");
            AssertTrue(floatResult is float && Mathf.Approximately((float)floatResult, 2.5f), "带f后缀的浮点数");

            // 布尔
            AssertEqual(true, interp.Evaluate("true"), "布尔 true");
            AssertEqual(false, interp.Evaluate("false"), "布尔 false");

            // 字符串
            AssertEqual("hello", interp.Evaluate("\"hello\""), "字符串字面量");
            AssertEqual("", interp.Evaluate("\"\""), "空字符串");
            AssertEqual("hello world", interp.Evaluate("\"hello world\""), "带空格的字符串");

            // null
            AssertNull(interp.Evaluate("null"), "null 字面量");
        }

        #endregion

        #region 2. 变量测试

        private static void TestVariables()
        {
            EOHelper.Log("--- 2. 变量测试 ---");
            var interp = new CommandInterpreter();

            // 注册变量
            interp.RegisterVariable("myInt", 100);
            interp.RegisterVariable("myFloat", 3.14f);
            interp.RegisterVariable("myString", "test");
            interp.RegisterVariable("myBool", true);
            interp.RegisterVariable("myNull", null);

            // 读取变量
            AssertEqual(100, interp.Evaluate("myInt"), "读取整数变量");
            AssertTrue(interp.Evaluate("myFloat") is float f && Mathf.Approximately(f, 3.14f), "读取浮点变量");
            AssertEqual("test", interp.Evaluate("myString"), "读取字符串变量");
            AssertEqual(true, interp.Evaluate("myBool"), "读取布尔变量");
            AssertNull(interp.Evaluate("myNull"), "读取 null 变量");

            // 通过 Execute 赋值
            string result = interp.Execute("x = 42");
            AssertContains(result, "已赋值", "赋值返回消息");
            AssertEqual(42, interp.GetVariable("x"), "GetVariable 获取赋值后的变量");

            // 获取变量名列表
            var names = new List<string>(interp.GetVariableNames());
            AssertTrue(names.Contains("myInt"), "变量名列表包含 myInt");
            AssertTrue(names.Contains("x"), "变量名列表包含 x");

            // 清空变量
            interp.ClearVariables();
            AssertNull(interp.GetVariable("myInt"), "清空后变量为 null");
        }

        #endregion

        #region 3. 预设变量测试

        private static void TestPresetVariables()
        {
            EOHelper.Log("--- 3. 预设变量测试 ---");
            var interp = new CommandInterpreter();

            int counter = 0;
            interp.RegisterPresetVariable("#counter", () => ++counter);
            interp.RegisterPresetVariable("#pi", () => 3.14159f);
            interp.RegisterPresetVariable("noHash", () => "auto-added"); // 自动添加 #

            // 每次访问动态计算
            AssertEqual(1, interp.Evaluate("#counter"), "预设变量第一次访问");
            AssertEqual(2, interp.Evaluate("#counter"), "预设变量第二次访问（动态计算）");
            AssertEqual(3, interp.Evaluate("#counter"), "预设变量第三次访问");

            // 预设变量只读
            string result = interp.Execute("#counter = 100");
            AssertContains(result, "只读", "预设变量赋值应失败");

            // 自动添加 # 前缀
            AssertEqual("auto-added", interp.Evaluate("#noHash"), "自动添加#前缀的预设变量");

            // 获取预设变量名列表
            var names = new List<string>(interp.GetPresetVariableNames());
            AssertTrue(names.Contains("#counter"), "预设变量名列表包含 #counter");
            AssertTrue(names.Contains("#noHash"), "预设变量名列表包含 #noHash");

            // 预设变量异常处理
            interp.RegisterPresetVariable("#error", () => throw new Exception("测试异常"));
            var errorResult = interp.Evaluate("#error");
            AssertTrue(interp.IsError(errorResult), "预设变量异常应返回错误");
            AssertContains(errorResult as string, "测试异常", "错误消息应包含异常信息");
        }

        #endregion

        #region 4. 算术运算符测试

        private static void TestArithmeticOperators()
        {
            EOHelper.Log("--- 4. 算术运算符测试 ---");
            var interp = new CommandInterpreter();

            // 基本运算
            AssertEqual(7, interp.Evaluate("3 + 4"), "加法");
            AssertEqual(5, interp.Evaluate("10 - 5"), "减法");
            AssertEqual(12, interp.Evaluate("3 * 4"), "乘法");
            AssertEqual(5, interp.Evaluate("15 / 3"), "除法");
            AssertEqual(1, interp.Evaluate("7 % 3"), "取模");

            // 浮点运算
            var result = interp.Evaluate("3.5 + 1.5");
            AssertTrue(result is float f1 && Mathf.Approximately(f1, 5.0f), "浮点加法");

            result = interp.Evaluate("10.0 / 4.0");
            AssertTrue(result is float f2 && Mathf.Approximately(f2, 2.5f), "浮点除法");

            // 混合类型运算
            result = interp.Evaluate("5 + 2.5");
            AssertTrue(result is float f3 && Mathf.Approximately(f3, 7.5f), "整数+浮点");

            // 除零错误
            var divZero = interp.Evaluate("10 / 0");
            AssertTrue(interp.IsError(divZero), "除零应返回错误");

            // 负数运算
            AssertEqual(-2, interp.Evaluate("3 + -5"), "加负数");
            AssertEqual(8, interp.Evaluate("3 - -5"), "减负数");
        }

        #endregion

        #region 5. 比较运算符测试

        private static void TestComparisonOperators()
        {
            EOHelper.Log("--- 5. 比较运算符测试 ---");
            var interp = new CommandInterpreter();

            // 相等比较
            AssertEqual(true, interp.Evaluate("5 == 5"), "相等 true");
            AssertEqual(false, interp.Evaluate("5 == 3"), "相等 false");
            AssertEqual(true, interp.Evaluate("5 != 3"), "不等 true");
            AssertEqual(false, interp.Evaluate("5 != 5"), "不等 false");

            // 大小比较
            AssertEqual(true, interp.Evaluate("5 > 3"), "大于 true");
            AssertEqual(false, interp.Evaluate("3 > 5"), "大于 false");
            AssertEqual(true, interp.Evaluate("3 < 5"), "小于 true");
            AssertEqual(false, interp.Evaluate("5 < 3"), "小于 false");
            AssertEqual(true, interp.Evaluate("5 >= 5"), "大于等于 true");
            AssertEqual(true, interp.Evaluate("5 >= 3"), "大于等于 true (>)");
            AssertEqual(true, interp.Evaluate("5 <= 5"), "小于等于 true");
            AssertEqual(true, interp.Evaluate("3 <= 5"), "小于等于 true (<)");

            // null 比较
            AssertEqual(true, interp.Evaluate("null == null"), "null == null");
            interp.RegisterVariable("obj", new object());
            AssertEqual(false, interp.Evaluate("obj == null"), "对象 == null");
            AssertEqual(true, interp.Evaluate("obj != null"), "对象 != null");

            // 字符串比较
            AssertEqual(true, interp.Evaluate("\"abc\" == \"abc\""), "字符串相等");
            AssertEqual(false, interp.Evaluate("\"abc\" == \"def\""), "字符串不相等");
        }

        #endregion

        #region 6. 逻辑运算符测试

        private static void TestLogicalOperators()
        {
            EOHelper.Log("--- 6. 逻辑运算符测试 ---");
            var interp = new CommandInterpreter();

            // AND
            AssertEqual(true, interp.Evaluate("true && true"), "true && true");
            AssertEqual(false, interp.Evaluate("true && false"), "true && false");
            AssertEqual(false, interp.Evaluate("false && true"), "false && true");
            AssertEqual(false, interp.Evaluate("false && false"), "false && false");

            // OR
            AssertEqual(true, interp.Evaluate("true || true"), "true || true");
            AssertEqual(true, interp.Evaluate("true || false"), "true || false");
            AssertEqual(true, interp.Evaluate("false || true"), "false || true");
            AssertEqual(false, interp.Evaluate("false || false"), "false || false");

            // NOT
            AssertEqual(false, interp.Evaluate("!true"), "!true");
            AssertEqual(true, interp.Evaluate("!false"), "!false");

            // 组合
            AssertEqual(true, interp.Evaluate("!false && true"), "!false && true");
            AssertEqual(false, interp.Evaluate("!true || false"), "!true || false");
        }

        #endregion

        #region 7. 运算符优先级测试

        private static void TestOperatorPrecedence()
        {
            EOHelper.Log("--- 7. 运算符优先级测试 ---");
            var interp = new CommandInterpreter();

            // 乘除优先于加减
            AssertEqual(14, interp.Evaluate("2 + 3 * 4"), "2 + 3 * 4 = 14");
            AssertEqual(10, interp.Evaluate("20 - 10 / 2"), "20 - 10 / 2 = 15");
            AssertEqual(11, interp.Evaluate("2 * 3 + 5"), "2 * 3 + 5 = 11");

            // 括号优先
            AssertEqual(20, interp.Evaluate("(2 + 3) * 4"), "(2 + 3) * 4 = 20");
            AssertEqual(5, interp.Evaluate("(20 - 10) / 2"), "(20 - 10) / 2 = 5");

            // 比较运算符优先于逻辑运算符
            AssertEqual(true, interp.Evaluate("3 > 2 && 5 > 4"), "3 > 2 && 5 > 4");
            AssertEqual(true, interp.Evaluate("3 < 2 || 5 > 4"), "3 < 2 || 5 > 4");

            // && 优先于 ||
            AssertEqual(true, interp.Evaluate("true || false && false"), "true || false && false = true");
            AssertEqual(false, interp.Evaluate("(true || false) && false"), "(true || false) && false = false");

            // 复杂表达式
            AssertEqual(true, interp.Evaluate("1 + 2 == 3"), "1 + 2 == 3");
            AssertEqual(true, interp.Evaluate("2 * 3 > 5"), "2 * 3 > 5");
        }

        #endregion

        #region 8. 构造函数调用测试

        private static void TestConstructors()
        {
            EOHelper.Log("--- 8. 构造函数调用测试 ---");
            var interp = new CommandInterpreter();

            // Vector3 构造
            var v3 = interp.Evaluate("new Vector3(1, 2, 3)");
            AssertType<Vector3>(v3, "new Vector3 类型");
            AssertEqual(new Vector3(1, 2, 3), v3, "new Vector3(1, 2, 3)");

            // Vector2 构造
            var v2 = interp.Evaluate("new Vector2(1.5, 2.5)");
            AssertType<Vector2>(v2, "new Vector2 类型");

            // Color 构造
            var color = interp.Evaluate("new Color(1, 0, 0, 1)");
            AssertType<Color>(color, "new Color 类型");
            AssertEqual(Color.red, color, "new Color(1, 0, 0, 1) = red");

            // 默认构造函数
            var v3Default = interp.Evaluate("new Vector3");
            AssertType<Vector3>(v3Default, "new Vector3 默认构造");
            AssertEqual(Vector3.zero, v3Default, "Vector3 默认值");

            // 无参数括号
            var v3Empty = interp.Evaluate("new Vector3()");
            AssertType<Vector3>(v3Empty, "new Vector3() 类型");

            // List 构造
            var list = interp.Evaluate("new List<int>()");
            AssertType<List<int>>(list, "new List<int>() 类型");
        }

        #endregion

        #region 9. 数组创建和访问测试

        private static void TestArrays()
        {
            EOHelper.Log("--- 9. 数组创建和访问测试 ---");
            var interp = new CommandInterpreter();

            // 创建数组
            var intArray = interp.Evaluate("new int[5]");
            AssertType<int[]>(intArray, "new int[5] 类型");
            AssertEqual(5, ((int[])intArray).Length, "int[5] 长度");

            var floatArray = interp.Evaluate("new float[3]");
            AssertType<float[]>(floatArray, "new float[3] 类型");

            var stringArray = interp.Evaluate("new string[10]");
            AssertType<string[]>(stringArray, "new string[10] 类型");

            // 数组索引访问
            interp.RegisterVariable("arr", new int[] { 10, 20, 30, 40, 50 });
            AssertEqual(10, interp.Evaluate("arr[0]"), "arr[0]");
            AssertEqual(30, interp.Evaluate("arr[2]"), "arr[2]");
            AssertEqual(50, interp.Evaluate("arr[4]"), "arr[4]");

            // 变量索引
            interp.Execute("idx = 2");
            AssertEqual(30, interp.Evaluate("arr[idx]"), "arr[idx] 其中 idx=2");

            // 索引越界
            var outOfBounds = interp.Evaluate("arr[10]");
            AssertTrue(interp.IsError(outOfBounds), "索引越界应返回错误");

            // 负数索引
            var negativeIdx = interp.Evaluate("arr[-1]");
            AssertTrue(interp.IsError(negativeIdx), "负数索引应返回错误");

            // List 索引
            var list = new List<string> { "a", "b", "c" };
            interp.RegisterVariable("list", list);
            AssertEqual("b", interp.Evaluate("list[1]"), "list[1]");
        }

        #endregion

        #region 10. 成员访问测试

        private static void TestMemberAccess()
        {
            EOHelper.Log("--- 10. 成员访问测试 ---");
            var interp = new CommandInterpreter();

            // Vector3 成员访问
            interp.RegisterVariable("v", new Vector3(1, 2, 3));
            var x = interp.Evaluate("v.x");
            AssertTrue(x is float fx && Mathf.Approximately(fx, 1f), "v.x");
            var y = interp.Evaluate("v.y");
            AssertTrue(y is float fy && Mathf.Approximately(fy, 2f), "v.y");

            // 属性访问
            AssertType<float>(interp.Evaluate("v.magnitude"), "v.magnitude 类型");

            // 深层访问
            interp.RegisterVariable("v2", new Vector3(3, 4, 0));
            var normalized = interp.Evaluate("v2.normalized");
            AssertType<Vector3>(normalized, "v2.normalized 类型");

            // null 成员访问
            interp.RegisterVariable("nullObj", null);
            var nullAccess = interp.Evaluate("nullObj.x");
            AssertTrue(interp.IsError(nullAccess), "null 对象成员访问应返回错误");

            // 不存在的成员
            var noMember = interp.Evaluate("v.notExist");
            AssertTrue(interp.IsError(noMember), "不存在的成员应返回错误");
        }

        #endregion

        #region 11. 方法调用测试

        private static void TestMethodCalls()
        {
            EOHelper.Log("--- 11. 方法调用测试 ---");
            var interp = new CommandInterpreter();

            // 字符串方法
            interp.RegisterVariable("str", "Hello World");
            AssertEqual("hello world", interp.Evaluate("str.ToLower()"), "str.ToLower()");
            AssertEqual("HELLO WORLD", interp.Evaluate("str.ToUpper()"), "str.ToUpper()");
            AssertEqual(11, interp.Evaluate("str.Length"), "str.Length");
            AssertEqual(true, interp.Evaluate("str.Contains(\"World\")"), "str.Contains(\"World\")");
            AssertEqual(false, interp.Evaluate("str.Contains(\"xyz\")"), "str.Contains(\"xyz\")");
            AssertEqual("Hello", interp.Evaluate("str.Substring(0, 5)"), "str.Substring(0, 5)");

            // List 方法
            var list = new List<int> { 1, 2, 3 };
            interp.RegisterVariable("list", list);
            AssertEqual(3, interp.Evaluate("list.Count"), "list.Count");
            AssertEqual(true, interp.Evaluate("list.Contains(2)"), "list.Contains(2)");

            // void 方法
            string result = interp.Execute("list.Add(4)");
            AssertContains(result, "执行成功", "void 方法返回执行成功");
            AssertEqual(4, list.Count, "Add 后 Count 增加");

            // Vector3 方法
            interp.RegisterVariable("v1", new Vector3(1, 0, 0));
            interp.RegisterVariable("v2", new Vector3(0, 1, 0));
        }

        #endregion

        #region 12. 方法重载测试

        private static void TestMethodOverloads()
        {
            EOHelper.Log("--- 12. 方法重载测试 ---");
            var interp = new CommandInterpreter();

            // Mathf 静态方法（多种重载）
            AssertEqual(5, interp.Evaluate("Mathf.Max(3, 5)"), "Mathf.Max(3, 5)");
            AssertEqual(1, interp.Evaluate("Mathf.Min(1, 5)"), "Mathf.Min(1, 5)");
            AssertEqual(4, interp.Evaluate("Mathf.Abs(-4)"), "Mathf.Abs(-4)");

            // 浮点重载
            var sqrtResult = interp.Evaluate("Mathf.Sqrt(16)");
            AssertTrue(sqrtResult is float sf && Mathf.Approximately(sf, 4f), "Mathf.Sqrt(16)");

            // Clamp
            AssertEqual(5, interp.Evaluate("Mathf.Clamp(3, 5, 10)"), "Mathf.Clamp(3, 5, 10)");
            AssertEqual(10, interp.Evaluate("Mathf.Clamp(15, 5, 10)"), "Mathf.Clamp(15, 5, 10)");
            AssertEqual(7, interp.Evaluate("Mathf.Clamp(7, 5, 10)"), "Mathf.Clamp(7, 5, 10)");

            // 字符串方法重载
            interp.RegisterVariable("s", "Hello");
            AssertEqual("He", interp.Evaluate("s.Substring(0, 2)"), "Substring(0, 2)");
            AssertEqual("llo", interp.Evaluate("s.Substring(2)"), "Substring(2)");
        }

        #endregion

        #region 13. 静态成员访问测试

        private static void TestStaticMembers()
        {
            EOHelper.Log("--- 13. 静态成员访问测试 ---");
            var interp = new CommandInterpreter();

            // Vector3 静态属性
            AssertEqual(Vector3.zero, interp.Evaluate("Vector3.zero"), "Vector3.zero");
            AssertEqual(Vector3.one, interp.Evaluate("Vector3.one"), "Vector3.one");
            AssertEqual(Vector3.up, interp.Evaluate("Vector3.up"), "Vector3.up");
            AssertEqual(Vector3.forward, interp.Evaluate("Vector3.forward"), "Vector3.forward");

            // Color 静态属性
            AssertEqual(Color.red, interp.Evaluate("Color.red"), "Color.red");
            AssertEqual(Color.blue, interp.Evaluate("Color.blue"), "Color.blue");

            // Mathf 静态字段
            var pi = interp.Evaluate("Mathf.PI");
            AssertTrue(pi is float piF && Mathf.Approximately(piF, Mathf.PI), "Mathf.PI");

            // Time 静态属性（编辑器模式下可能返回0）
            var time = interp.Evaluate("Time.time");
            AssertType<float>(time, "Time.time 类型");

            // 静态方法
            var lerp = interp.Evaluate("Mathf.Lerp(0, 10, 0.5)");
            AssertTrue(lerp is float lf && Mathf.Approximately(lf, 5f), "Mathf.Lerp(0, 10, 0.5)");
        }

        #endregion

        #region 14. 泛型类型测试

        private static void TestGenerics()
        {
            EOHelper.Log("--- 14. 泛型类型测试 ---");
            var interp = new CommandInterpreter();

            // List<T>
            var intList = interp.Evaluate("new List<int>()");
            AssertType<List<int>>(intList, "List<int>");

            var stringList = interp.Evaluate("new List<string>()");
            AssertType<List<string>>(stringList, "List<string>");

            // Dictionary<K,V>
            var dict = interp.Evaluate("new Dictionary<string, int>()");
            AssertType<Dictionary<string, int>>(dict, "Dictionary<string, int>");

            // HashSet<T>
            var hashSet = interp.Evaluate("new HashSet<float>()");
            AssertType<HashSet<float>>(hashSet, "HashSet<float>");

            // Queue<T>
            var queue = interp.Evaluate("new Queue<string>()");
            AssertType<Queue<string>>(queue, "Queue<string>");

            // Stack<T>
            var stack = interp.Evaluate("new Stack<int>()");
            AssertType<Stack<int>>(stack, "Stack<int>");

            // 嵌套泛型
            var nestedList = interp.Evaluate("new List<List<int>>()");
            AssertType<List<List<int>>>(nestedList, "List<List<int>>");
        }

        #endregion

        #region 15. 字符串操作测试

        private static void TestStringOperations()
        {
            EOHelper.Log("--- 15. 字符串操作测试 ---");
            var interp = new CommandInterpreter();

            // 字符串连接
            AssertEqual("HelloWorld", interp.Evaluate("\"Hello\" + \"World\""), "字符串连接");
            AssertEqual("Value: 42", interp.Evaluate("\"Value: \" + 42"), "字符串 + 数字");
            AssertEqual("true!", interp.Evaluate("true + \"!\""), "布尔 + 字符串");

            // 与 null 连接
            AssertEqual("nullValue", interp.Evaluate("null + \"Value\""), "null + 字符串");

            // 字符串比较
            AssertEqual(true, interp.Evaluate("\"abc\" == \"abc\""), "字符串相等");
            AssertEqual(true, interp.Evaluate("\"abc\" != \"def\""), "字符串不相等");
        }

        #endregion

        #region 16. 赋值测试

        private static void TestAssignment()
        {
            EOHelper.Log("--- 16. 赋值测试 ---");
            var interp = new CommandInterpreter();

            // 简单赋值
            interp.Execute("a = 10");
            AssertEqual(10, interp.GetVariable("a"), "简单赋值 a = 10");

            // 表达式赋值
            interp.Execute("b = 3 + 4");
            AssertEqual(7, interp.GetVariable("b"), "表达式赋值 b = 3 + 4");

            // 变量赋值给变量
            interp.Execute("c = a");
            AssertEqual(10, interp.GetVariable("c"), "变量赋值 c = a");

            // 对象赋值
            interp.Execute("v = new Vector3(1, 2, 3)");
            AssertType<Vector3>(interp.GetVariable("v"), "对象赋值类型");

            // 覆盖赋值
            interp.Execute("a = 20");
            AssertEqual(20, interp.GetVariable("a"), "覆盖赋值 a = 20");

            // 空字符串赋值
            interp.Execute("empty = \"\"");
            AssertEqual("", interp.GetVariable("empty"), "空字符串赋值");
        }

        #endregion

        #region 17. 成员赋值测试

        private static void TestMemberAssignment()
        {
            EOHelper.Log("--- 17. 成员赋值测试 ---");
            var interp = new CommandInterpreter();

            // 注意：Vector3 是值类型，成员赋值需要特殊处理
            // 这里测试引用类型的成员赋值

            // 测试类
            var testObj = new TestClass();
            interp.RegisterVariable("obj", testObj);

            // 字段赋值
            interp.Execute("obj.PublicField = 100");
            AssertEqual(100, testObj.PublicField, "公共字段赋值");

            // 属性赋值
            interp.Execute("obj.PublicProperty = 200");
            AssertEqual(200, testObj.PublicProperty, "公共属性赋值");

            // 嵌套对象成员赋值
            testObj.Nested = new TestClass.NestedClass();
            interp.Execute("obj.Nested.Value = 300");
            AssertEqual(300, testObj.Nested.Value, "嵌套对象成员赋值");
        }

        #endregion

        #region 18. 索引赋值测试

        private static void TestIndexAssignment()
        {
            EOHelper.Log("--- 18. 索引赋值测试 ---");
            var interp = new CommandInterpreter();

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
            AssertContains(result, "越界", "索引越界赋值应失败");
        }

        #endregion

        #region 19. 错误处理测试

        private static void TestErrorHandling()
        {
            EOHelper.Log("--- 19. 错误处理测试 ---");
            var interp = new CommandInterpreter();

            // 未找到变量
            var notFound = interp.Evaluate("notExistVar");
            AssertTrue(interp.IsError(notFound), "未找到变量应返回错误");

            // 未找到类型
            var notFoundType = interp.Evaluate("new NotExistType()");
            AssertTrue(interp.IsError(notFoundType), "未找到类型应返回错误");

            // 未找到成员
            interp.RegisterVariable("v", Vector3.zero);
            var notFoundMember = interp.Evaluate("v.notExist");
            AssertTrue(interp.IsError(notFoundMember), "未找到成员应返回错误");

            // 空命令
            string emptyResult = interp.Execute("");
            AssertContains(emptyResult, "命令为空", "空命令处理");

            // 除零
            var divZero = interp.Evaluate("10 / 0");
            AssertTrue(interp.IsError(divZero), "除零应返回错误");

            // 逻辑运算符类型错误
            var logicError = interp.Evaluate("5 && true");
            AssertTrue(interp.IsError(logicError), "逻辑运算符类型错误");

            // 方法参数不匹配
            var argMismatch = interp.Evaluate("Mathf.Max(\"string\", \"string\")");
            AssertTrue(interp.IsError(argMismatch), "方法参数不匹配应返回错误");
        }

        #endregion

        #region 20. 复杂表达式测试

        private static void TestComplexExpressions()
        {
            EOHelper.Log("--- 20. 复杂表达式测试 ---");
            var interp = new CommandInterpreter();

            // 链式方法调用
            interp.RegisterVariable("str", "  Hello World  ");
            AssertEqual("hello world", interp.Evaluate("str.Trim().ToLower()"), "链式方法调用");

            // 嵌套函数调用
            AssertEqual(5, interp.Evaluate("Mathf.Max(Mathf.Min(5, 10), 3)"), "嵌套函数调用");

            // 复杂算术
            AssertEqual(17, interp.Evaluate("(2 + 3) * 4 - 3"), "(2 + 3) * 4 - 3");
            AssertEqual(14, interp.Evaluate("2 + 3 * 4"), "2 + 3 * 4");

            // 方法调用 + 运算
            interp.RegisterVariable("v", new Vector3(3, 4, 0));
            var mag = interp.Evaluate("v.magnitude");
            AssertTrue(mag is float mf && Mathf.Approximately(mf, 5f), "v.magnitude");

            // 比较表达式
            AssertEqual(true, interp.Evaluate("Mathf.Max(3, 5) == 5"), "Mathf.Max(3, 5) == 5");

            // 变量 + 运算 + 方法
            interp.Execute("a = 10");
            interp.Execute("b = 20");
            AssertEqual(15, interp.Evaluate("(a + b) / 2"), "(a + b) / 2");
        }

        #endregion

        #region 21. 私有成员访问测试

        private static void TestPrivateMemberAccess()
        {
            EOHelper.Log("--- 21. 私有成员访问测试 ---");
            var interp = new CommandInterpreter();

            var testObj = new TestClass();
            interp.RegisterVariable("obj", testObj);

            // 私有字段访问
            var privateField = interp.Evaluate("obj.privateField");
            AssertNotError(interp, privateField, "访问私有字段不应报错");
            AssertEqual(42, privateField, "私有字段值");

            // 私有属性访问
            var privateProp = interp.Evaluate("obj.PrivateProperty");
            AssertNotError(interp, privateProp, "访问私有属性不应报错");
            AssertEqual("secret", privateProp, "私有属性值");

            // 私有方法访问
            var privateMethod = interp.Evaluate("obj.PrivateMethod()");
            AssertNotError(interp, privateMethod, "调用私有方法不应报错");
            AssertEqual("private result", privateMethod, "私有方法返回值");

            // 保护成员访问
            var protectedField = interp.Evaluate("obj.protectedField");
            AssertNotError(interp, protectedField, "访问保护字段不应报错");
        }

        #endregion

        #region 22. 嵌套调用测试

        private static void TestNestedCalls()
        {
            EOHelper.Log("--- 22. 嵌套调用测试 ---");
            var interp = new CommandInterpreter();

            // 嵌套构造函数
            var result = interp.Evaluate("new Vector3(Mathf.Sin(0), Mathf.Cos(0), 0)");
            AssertType<Vector3>(result, "嵌套构造函数类型");
            if (result is Vector3 v)
            {
                AssertTrue(Mathf.Approximately(v.x, 0f), "Sin(0) = 0");
                AssertTrue(Mathf.Approximately(v.y, 1f), "Cos(0) = 1");
            }

            // 方法参数中的运算
            AssertEqual(10, interp.Evaluate("Mathf.Max(5 + 3, 2 * 3)"), "Mathf.Max(5 + 3, 2 * 3)");

            // 深层成员 + 方法调用
            interp.RegisterVariable("v", new Vector3(3, 4, 0));
            var normalizedY = interp.Evaluate("v.normalized.y");
            AssertTrue(normalizedY is float ny && Mathf.Approximately(ny, 0.8f), "v.normalized.y");

            // 数组元素的成员访问
            var vectors = new Vector3[] { new Vector3(1, 0, 0), new Vector3(0, 1, 0) };
            interp.RegisterVariable("vectors", vectors);
            var vecX = interp.Evaluate("vectors[0].x");
            AssertTrue(vecX is float vx && Mathf.Approximately(vx, 1f), "vectors[0].x");
        }

        #endregion
    }

    /// <summary>
    /// 测试用的辅助类
    /// </summary>
    public class TestClass
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

} // namespace EventFramework.Editor