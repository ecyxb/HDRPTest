# CommandInterpreterV2 命令解释器文档

## 概述
CommandInterpreterV2 是一个运行时命令解释器，支持变量存储、表达式求值、方法调用等功能。支持本地执行和远程广播到逻辑线程执行。

---

## 🗂️ 文件结构

| 文件 | 说明 |
|------|------|
| `CommandInterpreterV2.cs` | 核心解释器，负责解析和执行命令 |
| `CommandInterpreterWindow.cs` | Unity 编辑器窗口 GUI |
| `CommandInterpreterProxy.cs` | UDP 接收代理，用于逻辑线程接收远程命令 |
| `CommandInterpreterTestsV2.cs` | 单元测试 |
| `ArgTypes` - 文件夹 | 各种object对应的类型 |
| `CommandInterpreterHelper.cs` | 一些帮助方法 |
| `CommandInterpreterRulerV2.cs` | 规定接口、匹配函数、匹配内置类型 |

---

## ✅ 支持的语法

### 1. 变量赋值
```csharp
x = 10                      // 整数赋值
y = 3.14                    // 浮点数赋值
name = "hello"              // 字符串赋值
flag = true                 // 布尔赋值
obj = null                  // null 赋值
```

### 2. 构造函数调用
```csharp
v = new Vector3(1, 2, 3)    // 使用 new 关键字
v = Vector3(1, 2, 3)        // 省略 new（自动识别类型）
v = new Vector3()           // 无参构造
list = new List<int>()      // 泛型类型构造
dict = new Dictionary<string, int>()  // 多泛型参数
```

### 3. 数组创建
```csharp
arr = new int[5]            // 创建整数数组
arr = new Vector3[10]       // 创建 Vector3 数组
arr = new string[3]         // 创建字符串数组
```

### 4. 成员访问
```csharp
v.x                         // 访问字段/属性
obj.transform.position      // 链式访问
list.Count                  // 访问属性
```

### 5. 成员赋值
```csharp
v.x = 10                    // 设置字段
obj.transform.position = new Vector3(0, 0, 0)
color.r = 0.5               // 结构体成员赋值（自动处理值类型）
```

### 6. 索引访问
```csharp
list[0]                     // List 索引
arr[2]                      // 数组索引
dict["key"]                 // 字典索引
obj.children[0].name        // 链式索引访问
```

### 7. 索引赋值
```csharp
list[0] = 100               // List 索引赋值
arr[2] = "hello"            // 数组索引赋值
dict["key"] = 42            // 字典索引赋值
```

### 8. 方法调用
```csharp
list.Add(1)                 // 实例方法
list.Contains(5)            // 带返回值的方法
obj.GetComponent("Camera")  // Unity 方法
str.ToUpper()               // 字符串方法
```

### 9. 静态成员访问
```csharp
Mathf.PI                    // 静态字段
Time.deltaTime              // 静态属性
Vector3.zero                // 静态属性
```

### 10. 静态方法调用
```csharp
Mathf.Max(1, 2)             // 静态方法
Mathf.Clamp(x, 0, 100)      // 带变量参数
Vector3.Distance(a, b)      // Unity 静态方法
Debug.Log("hello")          // 调试输出
```

### 11. 运算符
```csharp
// 算术运算符
1 + 2                       // 加法
10 - 3                      // 减法
4 * 5                       // 乘法
10 / 3                      // 除法
10 % 3                      // 取模

// 比较运算符
a == b                      // 相等
a != b                      // 不等
a < b                       // 小于
a > b                       // 大于
a <= b                      // 小于等于
a >= b                      // 大于等于

// 逻辑运算符
a && b                      // 逻辑与
a || b                      // 逻辑或
!flag                       // 逻辑非

// 字符串连接
"Hello " + "World"          // 字符串拼接
"Value: " + 42              // 字符串与数字拼接
```

### 12. 括号表达式
```csharp
(1 + 2) * 3                 // 改变运算优先级
((a + b) * c) / d           // 嵌套括号
```

### 13. 预设变量（只读，# 开头）
```csharp
#sel                        // 选中的对象
#time                       // Time.time
#dt                         // Time.deltaTime
#cam                        // Camera.main 
#typeof                     // 内置委托，用于获取类型信息
```

### 14. 预设变量使用示例
```csharp
#sel.name                   // 获取选中对象名称
#cam.fieldOfView            // 获取主摄像机 FOV
pos = #sel.transform.position  // 将预设变量赋值给普通变量
```

### 15. 内置委托 #typeof
```csharp
#typeof(123)                // 结果: System.Int32 (Assembly: mscorlib)
#typeof("hello")            // 结果: System.String (Assembly: mscorlib)
#typeof(#sel)               // 结果: UnityEngine.GameObject (Assembly: UnityEngine.CoreModule)
#typeof(new Vector3(1,2,3)) // 结果: UnityEngine.Vector3 (Assembly: UnityEngine.CoreModule)
#typeof(new List<int>())    // 结果: System.Collections.Generic.List`1[...]
#typeof(null)               // 结果: null
```

### 16. 私有/保护成员访问
```csharp
obj._privateField           // 访问私有字段
obj.m_internalValue         // 访问内部字段
obj.ProtectedMethod()       // 调用保护方法
Type.s_staticPrivateField   // 访问静态私有字段
obj._backingField = 10      // 设置私有字段
```

### 17. 泛型类型
```csharp
new List<int>()
new List<string>()
new Dictionary<string, int>()
new HashSet<float>()
new Queue<Vector3>()
new Stack<int>()
```

### 18. 复杂表达式

```csharp
Mathf.Max(a + b, c * 2)                    // 方法参数中使用运算符
list[Mathf.Min(i, list.Count - 1)]         // 索引中使用方法调用
obj.transform.position.magnitude           // 深层链式访问
new Vector3(Mathf.Sin(t), 0, Mathf.Cos(t)) // 构造函数参数中调用方法
```

### 19. 链式方法调用
```csharp
str.Trim().ToLower()                       // 字符串方法链
str.Trim().ToUpper().Substring(0, 3)       // 多级方法链
list[0].ToString().ToLower()               // 索引后链式调用
```

### 20. 数组元素成员访问
```csharp
vectors[0].x                               // 访问数组元素的成员
list[0].transform.position                 // 索引后深层访问
arr[i].Method()                            // 数组元素方法调用
```

### 21. 泛型方法

```csharp
#ui.Get<UIPanel>() 							// 根据类型获取UI
#ui.Get<UILogin>().binding.BtnLogin.GetComponent<RectTransform>() //也支持链式
```

### 22. 多语句执行

```csharp
x = 1; y = 2; z = 3						// 按封号分割，不允许字符串里带封号		
```







---

## 🌐 远程命令（UDP 广播）

### 基本用法
```csharp
@command                    // 以 @ 开头的命令会广播到逻辑线程
@player.health = 100        // 远程设置玩家血量
@gameManager.Pause()        // 远程调用方法
```

### 通信协议
- **端口**: 11451
- **数据格式**: `[4字节帧号(int)] + [命令字符串(UTF8)]`
- **帧号**: 0 表示立即执行，>0 表示在指定逻辑帧执行

### CommandInterpreterProxy 使用
```csharp
// 在逻辑线程初始化
var proxy = new CommandInterpreterProxy();
proxy.RegisterVariable("player", playerInstance);
proxy.Start();

// 每帧调用（在逻辑线程）
proxy.ProcessPendingCommands(currentLogicFrame);

// 关闭时
proxy.Dispose();
```

---

## ❌ 不支持的语法 (TODO)

### 1. Lambda 表达式
```csharp
// ❌ 不支持
list.Where(x => x > 0)
list.Select(x => x * 2)
Action<int> a = x => Debug.Log(x)
```

### 2. LINQ 查询语法
```csharp
// ❌ 不支持
from x in list where x > 0 select x
```

### 3. 控制流语句
```csharp
// ❌ 不支持
if (x > 0) y = 1
for (int i = 0; i < 10; i++) { }
while (true) { }
switch (x) { }
```

### 4. 变量声明带类型
```csharp
// ❌ 不支持
int x = 10
Vector3 v = new Vector3()
var list = new List<int>()
```

### 5. 复合赋值运算符
```csharp
// ❌ 不支持
x += 1
x -= 1
x *= 2
x /= 2
x++
x--
++x
--x
```

### 6. 三元运算符
```csharp
// ❌ 不支持
x > 0 ? "positive" : "negative"
```

### 7. null 合并运算符
```csharp
// ❌ 不支持
x ?? defaultValue
x?.property
x ??= defaultValue
```

### 8. typeof / is / as 运算符
```csharp
// ❌ 不支持
typeof(Vector3)
obj is GameObject
obj as Transform
```

### 9. 数组初始化器
```csharp
// ❌ 不支持
new int[] { 1, 2, 3 }
new Vector3[] { Vector3.zero, Vector3.one }
```

### 10. 对象初始化器
```csharp
// ❌ 不支持
new Person { Name = "Tom", Age = 20 }
```

### 11. 匿名类型
```csharp
// ❌ 不支持
new { Name = "Tom", Age = 20 }
```

### 12. 字符串插值
```csharp
// ❌ 不支持
$"Hello {name}"
$"Value: {x:F2}"
```

### 13. async/await
```csharp
// ❌ 不支持
await Task.Delay(1000)
async () => { }
```

### 14. throw 表达式
```csharp
// ❌ 不支持
throw new Exception("error")
```

### 15. using 语句
```csharp
// ❌ 不支持
using (var stream = File.Open(...)) { }
```

### 16. 预设变量赋值
```csharp
// ❌ 不支持（预设变量只读）
#selected = obj
```

### 17. 位运算符
```csharp
// ❌ 不支持
x & y      // 按位与
x | y      // 按位或
x ^ y      // 按位异或
~x         // 按位取反
x << 2     // 左移
x >> 2     // 右移
```

### 18. 元组
```csharp
// ❌ 不支持
(int a, int b) = (1, 2)
var tuple = (1, "hello")
```

### 19. ref / out 参数
```csharp
// ❌ 不支持
int.TryParse("123", out int result)
```

---

## 📝 注意事项

1. **字符串必须使用双引号**: `"hello"` ✅, `'hello'` ❌
2. **浮点数**: 支持 `3.14` 和 `3.14f` 两种写法
3. **大小写敏感**: 变量名和成员名区分大小写
4. **void 方法**: 调用 void 方法会返回 "执行成功"
5. **错误处理**: 错误信息以红色显示，包含 "失败"、"错误"、"未找到" 等关键词
6. **预设变量**: 以 `#` 开头，只读，每次访问时动态计算
7. **历史命令**: 使用 ↑↓ 键浏览历史命令

---

## 🔧 快捷键

| 快捷键 | 功能 |
|--------|------|
| `Ctrl+Shift+T` | 打开命令解释器窗口 |
| `Enter` | 执行命令 |
| `↑` | 上一条历史命令 |
| `↓` | 下一条历史命令 |

---

## 📌 使用示例

```csharp
// 1. 创建并操作 List
list = new List<int>()
list.Add(1)
list.Add(2)
list.Add(3)
list[0]                     // 结果: 1
list.Count                  // 结果: 3

// 2. 操作选中的 GameObject
#sel.name                   // 获取名称
#t.position                 // 获取位置
pos = #t.localPosition      // 保存到变量
pos.y                       // 访问 y 分量

// 3. 数学计算
Mathf.Sqrt(2)               // 结果: 1.414...
Mathf.Max(10, 20)           // 结果: 20
Mathf.Clamp(150, 0, 100)    // 结果: 100

// 4. 创建 Vector3
v = new Vector3(1, 2, 3)
v.magnitude                 // 结果: 3.741...
v.normalized                // 结果: (0.27, 0.53, 0.80)

// 5. 复杂表达式
1 + 2 * 3                   // 结果: 7 (先乘后加)
(1 + 2) * 3                 // 结果: 9 (括号优先)
10 > 5 && 3 < 8             // 结果: true
```
