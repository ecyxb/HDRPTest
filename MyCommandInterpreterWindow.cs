using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EventFramework;
using UnityEditor;

public class MyCommandInterpreterWindow : CommandInterpreterWindow
{
    [MenuItem("Tools/ÃüÁî½âÊÍÆ÷ %#T")]
    public static void ShowWindow()
    {
        var window = GetWindow<MyCommandInterpreterWindow>("ÃüÁî½âÊÍÆ÷");
        window.minSize = new Vector2(400, 300);
    }
}