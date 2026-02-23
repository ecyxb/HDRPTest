using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EventFramework.AST;
using UnityEditor;
namespace EventFramework
{
    public class MyCommandInterpreterWindow : CommandInterpreterWindow
    {
        [MenuItem("Tools/命令解释器 %#T")]
        public static void ShowWindow()
        {
            var window = GetWindow<MyCommandInterpreterWindow>("命令解释器");
            window.minSize = new Vector2(400, 300);
        }

        [MenuItem("Tools/AST 可视化")]
        public static void ShowASTViewer()
        {
            var window = GetWindow<ASTViewerWindow>("AST 可视化");
            window.minSize = new Vector2(500, 400);
        }

        [MenuItem("Tools/命令解释器单元测试")]
        public static void RunTests()
        {
            CommandInterpreterWindow.RunTest();
        }

    }

    /// <summary>
    /// AST 可视化窗口
    /// </summary>
    public class ASTViewerWindow : EditorWindow
    {
        private string inputExpression = "1 + 2 * 3";
        private string astOutput = "";
        private Vector2 scrollPos;
        private ASTParser parser = new ASTParser();
        private ASTPrinter printer = new ASTPrinter();

        // 树形可视化相关
        private ASTTreeNode rootTreeNode;
        private bool useTreeView = true;

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            // 标题
            EditorGUILayout.LabelField("AST 可视化工具", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // 输入区域
            EditorGUILayout.LabelField("输入表达式:");
            inputExpression = EditorGUILayout.TextField(inputExpression);

            EditorGUILayout.Space(5);

            // 按钮区域
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("解析 AST", GUILayout.Height(30)))
            {
                ParseExpression();
            }
            if (GUILayout.Button("清空", GUILayout.Height(30), GUILayout.Width(60)))
            {
                inputExpression = "";
                astOutput = "";
                rootTreeNode = null;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 视图切换
            useTreeView = EditorGUILayout.Toggle("树形视图", useTreeView);

            EditorGUILayout.Space(5);

            // 输出区域
            EditorGUILayout.LabelField("AST 结构:", EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));

            if (useTreeView && rootTreeNode != null)
            {
                DrawTreeNode(rootTreeNode, 0);
            }
            else
            {
                // 文本输出
                var style = new GUIStyle(EditorStyles.textArea)
                {
                    wordWrap = true,
                    richText = true
                };
                EditorGUILayout.SelectableLabel(astOutput, style, GUILayout.ExpandHeight(true));
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void ParseExpression()
        {
            if (string.IsNullOrWhiteSpace(inputExpression))
            {
                astOutput = "请输入表达式";
                rootTreeNode = null;
                return;
            }

            try
            {
                var ast = parser.Parse(inputExpression);
                astOutput = printer.Print(ast);
                rootTreeNode = printer.BuildTree(ast);
            }
            catch (System.Exception ex)
            {
                astOutput = $"解析错误: {ex.Message}";
                rootTreeNode = null;
            }
        }

        private void DrawTreeNode(ASTTreeNode node, int indent)
        {
            if (node == null) return;

            EditorGUI.indentLevel = indent;

            if (node.Children.Count > 0)
            {
                // 有子节点，显示可折叠
                var style = new GUIStyle(EditorStyles.foldout);
                style.normal.textColor = node.Color;
                style.onNormal.textColor = node.Color;
                style.focused.textColor = node.Color;
                style.onFocused.textColor = node.Color;
                style.active.textColor = node.Color;
                style.onActive.textColor = node.Color;

                node.IsExpanded = EditorGUILayout.Foldout(node.IsExpanded, node.Label, true, style);

                if (node.IsExpanded)
                {
                    foreach (var child in node.Children)
                    {
                        DrawTreeNode(child, indent + 1);
                    }
                }
            }
            else
            {
                // 叶子节点
                var style = new GUIStyle(EditorStyles.label);
                style.normal.textColor = node.Color;
                EditorGUILayout.LabelField(node.Label, style);
            }

            EditorGUI.indentLevel = indent;
        }
    }
}