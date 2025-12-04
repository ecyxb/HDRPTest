using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

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
        private bool showVariables = true;
        private bool needFocusInput = false;
        
        // 单例实例（静态）
        public static CommandInterpreterWindow Instance { get; private set; }
        
        // 缓存的背景纹理
        private Texture2D outputBackgroundTex;

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

            EditorGUILayout.EndHorizontal();

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
                "支持: 变量赋值(x = expr), 属性访问(obj.prop), 索引(list[0]), 方法调用(obj.Method(args))",
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

        private void ExecuteCommand()
        {
            if (string.IsNullOrWhiteSpace(inputCommand)) return;

            string cmd = inputCommand.Trim();

            // 添加到历史
            commandHistory.Add(cmd);
            historyIndex = -1;

            // 显示输入命令
            AddOutput($"<color=cyan>> {cmd}</color>");

            // 执行命令
            string result = interpreter.Execute(cmd);

            // 显示结果（根据内容着色）
            if (result.Contains("失败") || result.Contains("错误") || result.StartsWith("未找到"))
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

            // 清空输入
            inputCommand = "";

            // 滚动到底部
            outputScrollPos = new Vector2(0, float.MaxValue);

            Repaint();
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