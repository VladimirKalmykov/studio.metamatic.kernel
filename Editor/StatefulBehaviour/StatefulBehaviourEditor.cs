using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace Metamatic
{
    [CustomEditor(typeof(Metamatic.StatefulBehaviour), true)]
    public class StatefulBehaviourEditor : Editor
    {
        private Type m_StateMachineType;
        private PropertyInfo m_StatePropertyInfo;
        private Type m_StateType;
        private SerializedProperty m_ShowStateKeys;
        private FieldInfo[] m_Fields;
        private bool[] m_ActiveKeys;
        private string[] m_CurrentlyActiveKeys;
        private void OnEnable()
        {
            m_StateMachineType = target.GetType();
            m_StatePropertyInfo = m_StateMachineType.GetProperty("state");
            m_StateType = m_StatePropertyInfo.PropertyType;
            m_ShowStateKeys = serializedObject.FindProperty("statful_EditorShowStateKeys");
            m_Fields = m_StateType.GetFields();
            m_ActiveKeys = new bool[m_Fields.Length];
        }

        public override void OnInspectorGUI()
        {
            var index = 0;
            var changed = false;
            var enabledCount = 0;
            m_CurrentlyActiveKeys = new string[m_ShowStateKeys.arraySize];

            for (var ai = 0; ai < m_ShowStateKeys.arraySize; ai++)
            {
                m_CurrentlyActiveKeys[ai] = m_ShowStateKeys.GetArrayElementAtIndex(ai).stringValue;
            }
            
            foreach (var prop in m_Fields)
            {
                var enabled = GUILayout.Toggle(m_CurrentlyActiveKeys.Contains(prop.Name), new GUIContent(prop.Name));
                if (enabled)
                {
                    enabledCount++;
                }
                if (enabled != m_ActiveKeys[index])
                {
                    changed = true;
                    m_ActiveKeys[index] = enabled;
                }

                index++;
            }

            if (changed)
            {
                if (enabledCount > m_ShowStateKeys.arraySize)
                {
                    for (var q = m_ShowStateKeys.arraySize; q < enabledCount; q++)
                    {
                        m_ShowStateKeys.InsertArrayElementAtIndex(q);
                    }
                } else if (enabledCount < m_ShowStateKeys.arraySize)
                {
                    for (var q = m_ShowStateKeys.arraySize - 1; q >= enabledCount; q--)
                    {
                        m_ShowStateKeys.DeleteArrayElementAtIndex(q);
                    }
                }
                
                
                var size = m_ShowStateKeys.arraySize;
                var enabledIndex = 0;
                for (var i = 0; i < m_ActiveKeys.Length; i++)
                {
                    if (!m_ActiveKeys[i]) continue;
                    var elementProp = m_ShowStateKeys.GetArrayElementAtIndex(enabledIndex);
                    elementProp.stringValue = m_Fields[i].Name;
                    enabledIndex++;
                }
                
                serializedObject.ApplyModifiedProperties();
            }
            else
            {
                Debug.Log("CHECK!");
            }

            
            
            base.OnInspectorGUI();
        }
    }
}