using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace EventFramework.Editor
{

    /// <summary>
    /// 命令解释器编辑器窗口
    /// </summary>
    public class CommandInterpreterWindow : EditorWindow
    {
        private CommandInterpreter interpreter;
        private string inputCommand = "";
        private List<string> outputHistory = new List<string>();
        private List<string> commandHistory = new List<string>();
        private int historyIndex = -1;
        private Vector2 outputScrollPos;
        private Vector2 variablesScrollPos;
        private Vector2 helpScrollPos;
        private bool showVariables = true;
        private bool showHelp = true;
        private bool needFocusInput = false;
        
        // 单例实例（静态）
        public static CommandInterpreterWindow Instance { get; private set; }
        
        // 缓存的背景纹理
        private Texture2D outputBackgroundTex;
        
        // UDP 广播配置
        private const int UDP_BROADCAST_PORT = 11451;
        private UdpClient udpClient;
        
        // 广播帧号（用于指定命令在哪个逻辑帧执行）
        public int broadcastTargetFrame = 0;

        [MenuItem("Tools/命令解释器 %#T")]
        public static void ShowWindow()
        {
            var window = GetWindow<CommandInterpreterWindow>("命令解释器");
            window.minSize = new Vector2(400, 300);
        }

        private void OnEnable()
        {
            Instance = this;
            interpreter = new CommandInterpreter();

            // 注册预设变量（只读，动态计算）
            RegisterPresetVariables();

            // 自动注册一些常用变量
            RegisterDefaultVariables();

            // 监听 Play Mode 状态变化
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            Instance = null;
            // 取消监听
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            
            // 清理 UDP 客户端
            if (udpClient != null)
            {
                udpClient.Close();
                udpClient = null;
            }
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // 退出 Play Mode 时清空变量
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                interpreter.ClearVariables();
                AddOutput("<color=yellow>已退出 Play Mode，变量已清空</color>");
                Repaint();
            }
            else if (state == PlayModeStateChange.EnteredPlayMode)
            {
                interpreter.ClearVariables();
                AddOutput("<color=yellow>已进入 Play Mode，变量已清空</color>");
                Repaint();
            }
        }

        private void RegisterPresetVariables()
        {
            interpreter.RegisterPresetVariable("#sel", () => Selection.activeGameObject);
            interpreter.RegisterPresetVariable("#time", () => Time.time);
            interpreter.RegisterPresetVariable("#dt", () => Time.deltaTime);
            interpreter.RegisterPresetVariable("#cam", () => Camera.main);

            // 内置委托：打印类型信息
            interpreter.RegisterPresetVariable("#typeof", () => new System.Func<object, string>(obj =>
            {
                if (obj == null) return "null";
                var type = obj.GetType();
                return $"{type.FullName} (Assembly: {type.Assembly.GetName().Name})";
            }));
        }
        public void RegisterPresetVariable(string name, System.Func<object> func)
        {
            interpreter.RegisterPresetVariable(name, func);
        }

        private void RegisterDefaultVariables()
        {
            // 注册 Selection 相关
            if (Selection.activeGameObject != null)
            {
                interpreter.RegisterVariable("selected", Selection.activeGameObject);
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            // 工具栏
            DrawToolbar();

            // 主内容区域
            EditorGUILayout.BeginHorizontal();

            // 左侧：输出和输入
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            DrawOutputArea();
            DrawInputArea();
            EditorGUILayout.EndVertical();

            // 右侧：变量面板
            if (showVariables)
            {
                DrawVariablesPanel();
            }

            // 最右侧：帮助文档面板
            if (showHelp)
            {
                DrawHelpPanel();
            }

            EditorGUILayout.EndHorizontal();;

            EditorGUILayout.EndVertical();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("清空输出", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                outputHistory.Clear();
            }

            if (GUILayout.Button("清空变量", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                interpreter.ClearVariables();
                RegisterDefaultVariables();
            }

            if (GUILayout.Button("刷新选中", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RegisterDefaultVariables();
            }
            if (GUILayout.Button("单元测试", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                CommandInterpreterTests.RunAllTests();
            }

            GUILayout.FlexibleSpace();

            showVariables = GUILayout.Toggle(showVariables, "显示变量", EditorStyles.toolbarButton, GUILayout.Width(60));
            showHelp = GUILayout.Toggle(showHelp, "帮助", EditorStyles.toolbarButton, GUILayout.Width(40));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawOutputArea()
        {
            EditorGUILayout.LabelField("输出", EditorStyles.boldLabel);

            // 创建深色背景样式（使用缓存的纹理）
            if (outputBackgroundTex == null)
            {
                outputBackgroundTex = MakeTex(2, 2, new Color(0.15f, 0.15f, 0.15f, 1f));
            }
            
            GUIStyle scrollViewStyle = new GUIStyle();
            scrollViewStyle.normal.background = outputBackgroundTex;

            outputScrollPos = EditorGUILayout.BeginScrollView(outputScrollPos, scrollViewStyle,
                GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

            // 使用 SelectableLabel 支持复制文本
            GUIStyle outputStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                wordWrap = true,
                padding = new RectOffset(5, 5, 2, 2),
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
            };

            foreach (var line in outputHistory)
            {
                // 计算文本高度
                float height = outputStyle.CalcHeight(new GUIContent(line), EditorGUIUtility.currentViewWidth - 30);
                Rect rect = EditorGUILayout.GetControlRect(false, height);
                EditorGUI.SelectableLabel(rect, line, outputStyle);
            }

            EditorGUILayout.EndScrollView();
        }

        // 创建纯色纹理
        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void DrawInputArea()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(">", GUILayout.Width(15));

            // 检查键盘事件（在 TextField 之前处理）
            Event e = Event.current;
            bool shouldExecute = false;

            // 上下键 - 浏览历史命令
            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.UpArrow || e.keyCode == KeyCode.DownArrow))
            {
                // 先让输入框失去焦点
                GUI.FocusControl(null);

                if (commandHistory.Count > 0)
                {
                    if (e.keyCode == KeyCode.UpArrow)
                    {
                        historyIndex = Mathf.Clamp(historyIndex + 1, 0, commandHistory.Count - 1);
                        inputCommand = commandHistory[commandHistory.Count - 1 - historyIndex];
                    }
                    else if (e.keyCode == KeyCode.DownArrow)
                    {
                        historyIndex = Mathf.Max(historyIndex - 1, -1);
                        inputCommand = historyIndex >= 0 ? commandHistory[commandHistory.Count - 1 - historyIndex] : "";
                    }
                }
                e.Use();
                needFocusInput = true;
                Repaint();
            }
            // Enter 键 - 执行命令
            else if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
            {
                shouldExecute = true;
                e.Use();
            }

            // 在 Repaint 事件时处理延迟聚焦
            if (e.type == EventType.Repaint && needFocusInput)
            {
                needFocusInput = false;
                EditorGUI.FocusTextInControl("CommandInput");
            }

            GUI.SetNextControlName("CommandInput");
            inputCommand = EditorGUILayout.TextField(inputCommand, GUILayout.ExpandWidth(true));

            if (GUILayout.Button("执行", GUILayout.Width(50)) || shouldExecute)
            {
                ExecuteCommand();
                needFocusInput = true;
                Repaint(); // 触发下一帧的 Repaint 事件
            }

            EditorGUILayout.EndHorizontal();

            // 帮助提示
            EditorGUILayout.HelpBox(
                "本地执行: 变量赋值(x = expr), 属性访问(obj.prop), 索引(list[0]), 方法调用(obj.Method(args))\n" +
                "远程执行: @command 广播命令到逻辑线程 (UDP 127.0.0.1:11451)",
                MessageType.Info);
        }

        private void DrawVariablesPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(200));

            EditorGUILayout.LabelField("已注册变量", EditorStyles.boldLabel);

            variablesScrollPos = EditorGUILayout.BeginScrollView(variablesScrollPos,
                GUILayout.ExpandHeight(true));

            foreach (var name in interpreter.GetVariableNames())
            {
                EditorGUILayout.BeginHorizontal("box");

                object value = interpreter.GetVariable(name);
                string typeStr = value?.GetType().Name ?? "null";
                string valueStr = FormatVariableValue(value);

                EditorGUILayout.LabelField(name, EditorStyles.boldLabel, GUILayout.Width(80));
                EditorGUILayout.LabelField($"({typeStr})", GUILayout.Width(60));

                // 点击变量名插入到输入框
                if (GUILayout.Button("插入", GUILayout.Width(40)))
                {
                    inputCommand += name;
                    GUI.FocusControl("CommandInput");
                }

                EditorGUILayout.EndHorizontal();

                // 显示值的预览
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField(valueStr, EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndScrollView();

            // 快速注册
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("快速注册", EditorStyles.boldLabel);

            if (GUILayout.Button("注册选中对象 (selected)"))
            {
                if (Selection.activeGameObject != null)
                {
                    interpreter.RegisterVariable("selected", Selection.activeGameObject);
                    AddOutput("<color=green>已注册: selected = " + Selection.activeGameObject.name + "</color>");
                }
                else
                {
                    AddOutput("<color=yellow>未选中任何对象</color>");
                }
            }

            if (GUILayout.Button("注册选中组件"))
            {
                if (Selection.activeGameObject != null)
                {
                    var components = Selection.activeGameObject.GetComponents<Component>();
                    foreach (var comp in components)
                    {
                        string varName = comp.GetType().Name.ToLower();
                        interpreter.RegisterVariable(varName, comp);
                    }
                    AddOutput($"<color=green>已注册 {components.Length} 个组件</color>");
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawHelpPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(220));

            EditorGUILayout.LabelField("帮助文档", EditorStyles.boldLabel);

            helpScrollPos = EditorGUILayout.BeginScrollView(helpScrollPos, GUILayout.ExpandHeight(true));

            // 帮助文档样式
            GUIStyle helpStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                richText = true,
                fontSize = 11
            };

            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12
            };

            // === 内置预设变量 ===
            EditorGUILayout.LabelField("内置预设变量", headerStyle);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("<color=#88ff88>#sel</color> - 当前选中的 GameObject", helpStyle);
            EditorGUILayout.LabelField("<color=#88ff88>#time</color> - Time.time", helpStyle);
            EditorGUILayout.LabelField("<color=#88ff88>#dt</color> - Time.deltaTime", helpStyle);
            EditorGUILayout.LabelField("<color=#88ff88>#cam</color> - Camera.main", helpStyle);
            EditorGUILayout.LabelField("<color=#88ff88>#typeof(obj)</color> - 获取类型信息", helpStyle);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // === 基本语法 ===
            EditorGUILayout.LabelField("基本语法", headerStyle);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("<color=#ffff88>x = 表达式</color> - 变量赋值", helpStyle);
            EditorGUILayout.LabelField("<color=#ffff88>obj.property</color> - 属性访问", helpStyle);
            EditorGUILayout.LabelField("<color=#ffff88>obj.Method(args)</color> - 方法调用", helpStyle);
            EditorGUILayout.LabelField("<color=#ffff88>list[0]</color> - 索引访问", helpStyle);
            EditorGUILayout.LabelField("<color=#ffff88>obj.a.b.c</color> - 链式访问", helpStyle);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // === 运算符 ===
            EditorGUILayout.LabelField("运算符", headerStyle);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("<color=#88ffff>+ - * / %</color> - 算术运算", helpStyle);
            EditorGUILayout.LabelField("<color=#88ffff>== != < > <= >=</color> - 比较", helpStyle);
            EditorGUILayout.LabelField("<color=#88ffff>&& || !</color> - 逻辑运算", helpStyle);
            EditorGUILayout.LabelField("<color=#88ffff>? :</color> - 三元表达式", helpStyle);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // === 远程命令 ===
            EditorGUILayout.LabelField("远程命令", headerStyle);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("<color=#ff88ff>@command</color> - 广播到逻辑线程", helpStyle);
            EditorGUILayout.LabelField($"UDP 端口: {UDP_BROADCAST_PORT}", helpStyle);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // === 示例 ===
            EditorGUILayout.LabelField("示例", headerStyle);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("<color=#aaaaaa>#sel.name</color>", helpStyle);
            EditorGUILayout.LabelField("<color=#aaaaaa>#sel.transform.position</color>", helpStyle);
            EditorGUILayout.LabelField("<color=#aaaaaa>pos = #sel.transform.position</color>", helpStyle);
            EditorGUILayout.LabelField("<color=#aaaaaa>pos.x + 1</color>", helpStyle);
            EditorGUILayout.LabelField("<color=#aaaaaa>#cam.fieldOfView = 60</color>", helpStyle);
            EditorGUILayout.LabelField("<color=#aaaaaa>\"hello\".ToUpper()</color>", helpStyle);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void ExecuteCommand()
        {
            if (string.IsNullOrWhiteSpace(inputCommand)) return;

            string cmd = inputCommand.Trim();

            // 添加到历史
            commandHistory.Add(cmd);
            historyIndex = -1;

            // 显示输入命令
            AddOutput($"<color=cyan>> {cmd}</color>");

            // 检查是否是广播命令（以 @ 开头）
            if (cmd.StartsWith("@"))
            {
                string broadcastCmd = cmd.Substring(1); // 去掉 @ 前缀
                BroadcastToLogicThread(broadcastCmd, broadcastTargetFrame);
            }
            else
            {
                // 本地执行命令
                string result = interpreter.Execute(cmd);

                // 显示结果（根据内容着色）
                if (result.StartsWith("Error:"))
                {
                    AddOutput($"<color=red>{result}</color>");
                }
                else if (result.Contains("已赋值"))
                {
                    AddOutput($"<color=green>{result}</color>");
                }
                else
                {
                    AddOutput($"<color=white>{result}</color>");
                }
            }

            // 清空输入
            inputCommand = "";

            // 滚动到底部
            outputScrollPos = new Vector2(0, float.MaxValue);

            Repaint();
        }

        /// <summary>
        /// 通过 UDP 广播命令到逻辑线程
        /// </summary>
        /// <param name="command">命令内容</param>
        /// <param name="targetFrame">目标执行帧号</param>
        private void BroadcastToLogicThread(string command, int targetFrame)
        {
            try
            {
                if (udpClient == null)
                {
                    udpClient = new UdpClient();
                }

                // 构造数据：前4字节为帧号(int)，后续为命令字符串(UTF8)
                byte[] frameBytes = System.BitConverter.GetBytes(targetFrame);
                byte[] commandBytes = Encoding.UTF8.GetBytes(command);
                byte[] data = new byte[frameBytes.Length + commandBytes.Length];
                System.Buffer.BlockCopy(frameBytes, 0, data, 0, frameBytes.Length);
                System.Buffer.BlockCopy(commandBytes, 0, data, frameBytes.Length, commandBytes.Length);
                
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, UDP_BROADCAST_PORT);
                
                udpClient.Send(data, data.Length, endPoint);

                string frameInfo = $"(帧:{targetFrame})";
                AddOutput($"<color=yellow>[UDP] 已发送到 127.0.0.1:{UDP_BROADCAST_PORT} {frameInfo}: {command}</color>");
            }
            catch (System.Exception ex)
            {
                AddOutput($"<color=red>[UDP] 发送失败: {ex.Message}</color>");
            }
        }

        private void AddOutput(string line)
        {
            outputHistory.Add(line);

            // 限制历史记录数量
            if (outputHistory.Count > 500)
            {
                outputHistory.RemoveAt(0);
            }
        }

        private string FormatVariableValue(object value)
        {
            if (value == null) return "null";

            string str = value.ToString();
            if (str.Length > 50)
            {
                str = str.Substring(0, 47) + "...";
            }
            return str;
        }

        // 当选中对象变化时刷新
        private void OnSelectionChange()
        {
            RegisterDefaultVariables();
            Repaint();
        }
    }

} // namespace EventFramework.Editor