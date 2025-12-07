
using Unity.VisualScripting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.UI
{
    [CustomEditor(typeof(CustomButton), true)]
    [CanEditMultipleObjects]
    /// <summary>
    /// Custom Editor for the Toggle Component.
    /// Extend this class to write a custom editor for a component derived from Toggle.
    /// </summary>
    public class CustomButtonEditor : SelectableEditor
    {
        SerializedProperty m_IsToggleProperty;
        SerializedProperty m_OnClickProperty;
        SerializedProperty m_OnValueChangedProperty;
        SerializedProperty m_TransitionProperty;
        SerializedProperty m_GraphicProperty;
        SerializedProperty m_GroupProperty;
        SerializedProperty m_IsOnProperty;


        protected override void OnEnable()
        {
            base.OnEnable();
            m_IsToggleProperty = serializedObject.FindProperty("m_IsToggle");
            m_TransitionProperty = serializedObject.FindProperty("toggleTransition");
            m_GraphicProperty = serializedObject.FindProperty("graphic");
            m_GroupProperty = serializedObject.FindProperty("m_Group");
            m_IsOnProperty = serializedObject.FindProperty("m_IsOn");
            m_OnValueChangedProperty = serializedObject.FindProperty("onValueChanged");
            m_OnClickProperty = serializedObject.FindProperty("m_OnClick");
        }

        public override void OnInspectorGUI()
        {
            
            serializedObject.Update();
            CustomButton toggle = serializedObject.targetObject as CustomButton;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_IsToggleProperty);
            if (EditorGUI.EndChangeCheck())
            {
                if (!Application.isPlaying)
                    EditorSceneManager.MarkSceneDirty(toggle.gameObject.scene);
                toggle.isToggle = m_IsToggleProperty.boolValue;
            }

            base.OnInspectorGUI();
            EditorGUILayout.PropertyField(m_OnClickProperty);
            if (!toggle.isToggle)
            {
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EditorGUILayout.Space();
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_IsOnProperty);
            if (EditorGUI.EndChangeCheck())
            {
                if (!Application.isPlaying)
                    EditorSceneManager.MarkSceneDirty(toggle.gameObject.scene);
    
                ToggleGroup group = m_GroupProperty.objectReferenceValue as ToggleGroup;

                toggle.isOn = m_IsOnProperty.boolValue;
                if (group != null && group.isActiveAndEnabled && toggle.IsActive())
                {
                    if (toggle.isOn || (!group.AnyTogglesOn() && !group.allowSwitchOff))
                    {
                        toggle.isOn = true;
                        group.NotifyToggleOn(toggle);
                    }
                }
            }
            EditorGUILayout.PropertyField(m_TransitionProperty);
            EditorGUILayout.PropertyField(m_GraphicProperty);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_GroupProperty);
            if (EditorGUI.EndChangeCheck())
            {
                if (!Application.isPlaying)
                    EditorSceneManager.MarkSceneDirty(toggle.gameObject.scene);

                ToggleGroup group = m_GroupProperty.objectReferenceValue as ToggleGroup;
                toggle.group = group;
            }

            EditorGUILayout.Space();

            // Draw the event notification options
            EditorGUILayout.PropertyField(m_OnValueChangedProperty);
            

            serializedObject.ApplyModifiedProperties();
        }
    }
}
