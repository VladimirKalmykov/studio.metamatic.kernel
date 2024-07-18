using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using Metamatic;
using UnityEditor.UIElements;

public class PinsInspectorWindow : EditorWindow
{
    public static void OpenIfNotOpened()
    {
        ShowWindow();
    }
    
    private VisualElement m_RootElement;
    private Pin m_CurrentComponent;
    private VisualElement m_ValueContainer;
    private Type m_DataType;
    private Pin m_CachedComponent;
    private ObjectField m_ComponentSelector;
    private Pin m_BackUpComponent;
    private List<PinInspector> m_PinInspectors = new List<PinInspector>();
    private Label m_EnterPlayModeMessage;
    private VisualElement m_EditModePinsContainer;
    private VisualElement m_PlayModePinsContainer;

    [MenuItem("Window/Pin Inspector")]
    public static void ShowWindow()
    {
        var window = GetWindow<PinsInspectorWindow>();
        window.titleContent = new GUIContent("Pins inspector");
    }

    private void CreateGUI()
    {
        m_RootElement = rootVisualElement;
        m_RootElement.AddToClassList("Body");
        m_RootElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(
            "Packages/studio.metamatic.kernel/Editor/SocketSystem/EditorWIndows/styles.uss"));
       
        
        /* Create container for edit mode pins */
        m_EditModePinsContainer = new VisualElement();
        m_EditModePinsContainer.AddToClassList("EditModePinsContainer");
        m_RootElement.Add(m_EditModePinsContainer);
        
        /* Create container for play mode pins */
        m_PlayModePinsContainer = new VisualElement();
        m_PlayModePinsContainer.AddToClassList("PlayModePinsContainer");
        m_RootElement.Add(m_PlayModePinsContainer);
        
        /* Create empty message */
        m_EnterPlayModeMessage = new Label("Choose pins to watch");
        m_EnterPlayModeMessage.AddToClassList("IntroMessage");
        m_RootElement.Add(m_EnterPlayModeMessage);
        
        if (Application.isPlaying)
        {
            RenderPlayModeUI();
        }
        else
        {
            RenderEditModeUI();
        }
    }

    public void OnEnable()
    {
        
        /* Get pins for inspecting */
        // Pin.s_Inspect.ForEach(OnAddToInspect);
        Pin.OnAddToInspect += OnAddToInspect;
        Pin.OnRemoveFromInspect += OnRemoveFromInspect;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void RenderEditModeUI()
    {
        /*
         * Render selected pins navigator
         */
        ReleaseEditModePinInspectors();
        Pin.s_Inspect.ForEach(AddEditModePinElement);
        UpdateEmptyMessage();
    }
    
    private void RenderPlayModeUI()
    {
        /*
         * Render selected pins navigator
         */
        ReleasePlayModePinInspectors();
        Pin.s_Inspect.ForEach(AddPlayModePinElement);
        UpdateEmptyMessage();
    }

    private void UpdateEmptyMessage()
    {
        if (m_RootElement == null) return;
        m_EnterPlayModeMessage.style.display = (Application.isPlaying ? m_PlayModePinsContainer : m_EditModePinsContainer).Children().Count() == 0 ? DisplayStyle.Flex : DisplayStyle.None;
    }
    
    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            /* Recreate pin inspectors */
            ReleasePlayModePinInspectors();
            RenderPlayModeUI();
        } else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            ReleasePlayModePinInspectors();
        } else if (state == PlayModeStateChange.EnteredEditMode)
        {
            ReleasePlayModePinInspectors();
            RenderEditModeUI();
        }
    }

    private void AddEditModePinElement(Pin pin)
    {
        m_EditModePinsContainer.Add(new EditModePinElement(pin));
        UpdateEmptyMessage();
    }
    
    private void RemoveEditModePinElement(Pin pin)
    {
        foreach (var child in m_EditModePinsContainer.Children().ToArray())
        {
            if (child is EditModePinElement pinElement && pinElement.pin == pin)
            {
                m_EditModePinsContainer.Remove(child);
            }
        }
        UpdateEmptyMessage();
    }
    
    private void AddPlayModePinElement(Pin pin)
    {
        if (m_RootElement == null) return;
        var pinInspector = new PinInspector(m_PlayModePinsContainer, pin);
        m_PinInspectors.Add(pinInspector);
        UpdateEmptyMessage();
    }
    
    private void RemovePlayModePinElement(Pin pin)
    {
        var pinInspector = m_PinInspectors.Find(inspector => inspector.pin == pin);
        if (pinInspector != null)
        {
            m_PinInspectors.Remove(pinInspector);
            pinInspector.Dispose();
        }

        UpdateEmptyMessage();
    }

    private void OnAddToInspect(Pin pin)
    {
        if (m_RootElement == null) return;
        if (!Application.isPlaying)
        {
            AddEditModePinElement(pin);
        }
        else
        {
            AddPlayModePinElement(pin);
        }
    }

    private void OnRemoveFromInspect(Pin pin)
    {
        if (m_RootElement == null) return;
        if (!Application.isPlaying)
        {
            RemoveEditModePinElement(pin);
        }
        else
        {
            RemovePlayModePinElement(pin);
        }
    }

    public void OnDisable()
    {
        Pin.OnAddToInspect -= OnAddToInspect;
        Pin.OnRemoveFromInspect -= OnRemoveFromInspect;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        ReleasePlayModePinInspectors();
        ReleaseEditModePinInspectors();
    }

    public void ReleasePlayModePinInspectors()
    {
        if (m_RootElement == null) return;
        m_PinInspectors.ForEach(inspector => inspector.Dispose());
        m_PinInspectors.Clear();
        m_PlayModePinsContainer.Clear();
    }
    
    public void ReleaseEditModePinInspectors()
    {
        m_EditModePinsContainer?.Clear();
    }
}