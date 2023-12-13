using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Reflection;
using Metamatic;
using UnityEditor.UIElements;

public class PinInspectorWindow : EditorWindow
{
    public static void OpenIfNotOpened()
    {
        var window = GetWindow<PinInspectorWindow>();
        if (window == null)
        {
            ShowWindow();
        }
    }
    private Pin m_CurrentComponent;
    private VisualElement m_RootElement;
    private VisualElement m_ValueContainer;
    private Type m_DataType;
    private Pin m_CachedComponent;
    private ObjectField m_ComponentSelector;

    [MenuItem("Window/Pin Inspector")]
    public static void ShowWindow()
    {
        var window = GetWindow<PinInspectorWindow>();
        window.titleContent = new GUIContent("Pin inspector");
    }

    public void OnEnable()
    {
        Debug.Log("Enable");
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        m_RootElement = rootVisualElement;
        
        m_ComponentSelector = new ObjectField("Pin");
        m_ComponentSelector.objectType = typeof(Pin);
        m_ComponentSelector.RegisterValueChangedCallback(evt =>
        {
            var nextPin = evt.newValue as Pin;
            if (nextPin != null)
            {
                HandleComponentChange(nextPin);
                UpdateData();
            }
            else
            {
                m_CurrentComponent = null;
            }
            SetCurrentComponent(m_CurrentComponent);
        });
        m_ComponentSelector.value = null;
        
        m_RootElement.Add(m_ComponentSelector);

        m_ValueContainer = new VisualElement();
        m_RootElement.Add(m_ValueContainer);
        
        /* Get component type */
        HandleComponentChange(m_CurrentComponent);
    }

    private void HandleComponentChange(Pin component)
    {
        if (m_CurrentComponent != null)
        {
            m_CurrentComponent.OnDebugDataChanged -= UpdateData;
        }

        if (component == null)
        {
            ClearDataInView();
            return;
        }
        component.OnDebugDataChanged += UpdateData;
        m_CurrentComponent = component;
        Type componentType = component.GetType();
        m_DataType = GetGenericArgumentFromPinInstance(componentType);
        UpdateData();
    }

    private void UpdateData()
    {
        RefreshDataInView();
    }

    private void RefreshDataInView()
    {
        if (m_CurrentComponent != null)
        {
            if (m_DataType != null)
            {
                object data = ((dynamic)m_CurrentComponent).GetWireObjectData();
                m_ValueContainer.Clear();
                m_ValueContainer.Add(EditorUtils.ValueToTable(data, m_DataType));
            }
        }
    }

    private void ClearDataInView()
    {
        m_ValueContainer.Clear();
    }

    private Pin m_BackUpComponent;
    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        Debug.Log($"state: {state}");
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            m_BackUpComponent = m_CurrentComponent;
            m_ComponentSelector.value = null;
            SetCurrentComponent(null);
            ClearDataInView();
        } else if (state == PlayModeStateChange.EnteredPlayMode)
        {
            SetCurrentComponent(m_BackUpComponent);
            m_ComponentSelector.value = m_BackUpComponent;
        }
    }
    public void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        if (m_CurrentComponent != null)
        {
            Debug.Log("Unsubscribe OnDebugDataChanged");
            m_CurrentComponent.OnDebugDataChanged -= UpdateData;
            m_CurrentComponent = null;
        }
    }

    public void SetCurrentComponent(Pin component)
    {
        HandleComponentChange(component);
        RefreshDataInView();
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