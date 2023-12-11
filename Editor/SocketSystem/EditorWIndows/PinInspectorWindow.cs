using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Reflection;
using Metamatic;

public class PinInspectorWindow : EditorWindow
{
    private Pin m_CurrentComponent;
    private VisualElement m_RootElement;
    private Label m_DataLabel;
    private DateTime m_LastUpdateTime = DateTime.Now;
    private Type m_DataType;
    private Pin m_CachedComponent;

    [MenuItem("Window/Data Viewer")]
    public static void ShowWindow()
    {
        var window = GetWindow<PinInspectorWindow>();
        window.titleContent = new GUIContent("Pin inspector");
    }

    public void OnEnable()
    {
        m_RootElement = rootVisualElement;
        
        m_DataLabel = new Label("Waiting for data...");
        m_RootElement.Add(m_DataLabel);
        
        /* Get component type */
        if (m_CurrentComponent != null)
        {
            HandleComponentChange(m_CurrentComponent);
        }
        
        EditorApplication.update += UpdateData;
    }

    private void HandleComponentChange(Pin component)
    {
        Type componentType = component.GetType();
        m_DataType = GetGenericArgumentFromPinInstance(componentType);
    }

    private void UpdateData()
    {
        if ((DateTime.Now - m_LastUpdateTime).TotalMilliseconds >= 100)
        {
            m_LastUpdateTime = DateTime.Now;
            RefreshDataInView();
        }
    }

    private void RefreshDataInView()
    {
        if (m_CurrentComponent != null)
        {
            if (m_DataType != null)
            {
                object data = ((dynamic)m_CurrentComponent).GetWireObjectData();
                m_DataLabel.text = ConvertDataToString(data, m_DataType);
            }
        }
    }

    private static string ConvertDataToString(object data, Type dataType)
    {
        // Проверка на null
        if (data == null) 
        {
            return "null";
        }

        // Обработка примитивных типов
        if (dataType.IsPrimitive || dataType == typeof(string) || dataType == typeof(decimal))
        {
            return data.ToString();
        }

        // Обработка структур
        else if (dataType.IsValueType)
        {
            try
            {
                return JsonUtility.ToJson(data);
            }
            catch (Exception ex)
            {
                // Обработка возможных ошибок сериализации
                return $"Error serializing data: {ex.Message}";
            }
        }

        // Обработка других случаев
        else
        {
            // Можно реализовать дополнительную логику для классов
            // или вернуть строку по умолчанию
            return data.ToString();
        }
    }

    public void OnDisable()
    {
        // Очистка таймера
        EditorApplication.update -= UpdateData;
    }

    public void SetCurrentComponent(Pin component)
    {
        if (component != m_CurrentComponent)
        {
            HandleComponentChange(component);
            RefreshDataInView();
        }
    }
    
    #region internal

    private Type GetGenericArgumentFromPinInstance(Type derivedType)
    {
        while (derivedType != null && derivedType != typeof(object))
        {
            var isPinType = derivedType.IsGenericType && derivedType.GetGenericTypeDefinition() == typeof(Pin<>);
            if (isPinType)
            {
                return derivedType.GetGenericArguments()[0];
            }

            derivedType = derivedType.BaseType;
        }

        return null;
    }
    #endregion
}