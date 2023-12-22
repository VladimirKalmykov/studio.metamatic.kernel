using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic
{
    public class PlugGUIEditorComponent: EditorComponent

    {
        private VisualElement m_Socket;
        private VisualElement m_ErrorContainer;
        private Label m_ErrorLabel;
        private VisualElement m_ConnectionContainer;
        private VisualElement m_ConnectionContentContainer;
        private VisualElement m_SocketIndicatorDot;
        private SocketScheme m_SocketScheme;
        public PlugGUIEditorComponent(ComposableEditor composableEditor) : base(composableEditor)
        {
            
        }

        public override void OnEnable()
        {
            m_ConnectionContainer.style.display = Application.isPlaying ? DisplayStyle.Flex : DisplayStyle.None;
            var plug = (Plug) target;
            m_SocketScheme = plug.socketScheme;
            m_SocketScheme.OnReconnectIn += HandleReconnectIn;
            m_SocketScheme.OnErrorChanged += HandleErrorChanged;
            HandleReconnectIn();
            HandleErrorChanged();
        }
        
        public override void OnDisable()
        {
            m_ConnectionContainer.style.display = DisplayStyle.None;
            m_SocketScheme.OnReconnectIn -= HandleReconnectIn;
            m_SocketScheme.OnErrorChanged -= HandleErrorChanged;
        }
        
        private void HandleReconnectIn()
        {
            var scheme = m_SocketScheme.debug_GetConnectedInScheme();
            m_ConnectionContentContainer.Clear();

            if (!Application.isPlaying)
            {
                m_SocketIndicatorDot.RemoveFromClassList("Disconnected");
                m_SocketIndicatorDot.RemoveFromClassList("Connected");
                return;
            }
            
            if (scheme != null)
            {
                m_SocketIndicatorDot.RemoveFromClassList("Disconnected");
                m_SocketIndicatorDot.AddToClassList("Connected");
                
                var relatedObjectField = new ObjectField()
                {
                    value = scheme.debug_RelatedObject,
                    objectType = scheme.debug_RelatedObject.GetType()
                };
                m_ConnectionContentContainer.Add(relatedObjectField);
            }
            else
            {
                m_ConnectionContentContainer.Add(new Label("No socket connected."));
                m_SocketIndicatorDot.RemoveFromClassList("Connected");
                m_SocketIndicatorDot.AddToClassList("Disconnected");
            }
        }
        
        private void HandleErrorChanged()
        {
            var errors = m_SocketScheme.debug_Error;
            
            if (errors.Count == 0)
            {
                m_ErrorLabel.text = "";
                m_ErrorContainer.style.display = DisplayStyle.None;
                return;
            }
            m_ErrorContainer.style.display = DisplayStyle.Flex;
            /* Combine errors in to one string */
            var errorString = "";
            foreach (var error in errors)
            {
                errorString += error + "\n";
            }
            m_ErrorLabel.text = errorString;
        }
        
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            
            /* Create input pins */
            var inputPins = CreateInputPins();
            if (inputPins != null)
            {
                root.Add(inputPins);
            }
            
            /* Create output pins */
            var outputPins = CreateOutputPins();
            if (outputPins != null)
            {
                root.Add(outputPins);
            }
            
            /* Create connection view */
            var connectionView = m_ConnectionContainer = CreateConnectionView();
            root.Add(connectionView);
            
            /* Create error message */
            var errorMessage = CreateErrorMessage();
            root.Add(errorMessage);
            
            return root;
        }
        
        private VisualElement CreateConnectionView()
        {
            var root = new VisualElement();
            root.AddToClassList("Section");
            
            /* Create header */
            var header = new VisualElement();
            header.AddToClassList("Header");
            var labelContainer = new VisualElement();
            labelContainer.AddToClassList("LabelContainer");
            header.Add(labelContainer);
            var indicator = new VisualElement();
            indicator.AddToClassList("Indicator");
            var indicatorDot = m_SocketIndicatorDot = new VisualElement();
            indicatorDot.AddToClassList("Dot");
            indicator.Add(indicatorDot);
            labelContainer.Add(indicator);
            var label = new Label("Connection");
            labelContainer.Add(label);
            root.Add(header);
            
            /* Create content */
            var content = m_ConnectionContentContainer = new VisualElement();
            content.AddToClassList("Content");
            root.Add(content);
            
            return root;
        }
        
        private VisualElement CreateErrorMessage()
        {
            var root = m_ErrorContainer = new VisualElement();
            root.AddToClassList("Message");
            root.AddToClassList("Error");
            m_ErrorLabel = new Label("Plug has unspecified input or output pins.");
            root.Add(m_ErrorLabel);
            return root;
        }
        
        private VisualElement CreatePins(Type[] types, Pin[] pins, string title)
        {
            var plug = (Plug) target;
            
            if (types.Length == 0)
            {
                return null;
            }
            
            var root = new VisualElement();
            root.AddToClassList("Section");
            
            /* Create header */
            var header = new VisualElement();
            header.AddToClassList("Header");
            var label = new Label(title);
            header.Add(label);
            root.Add(header);
            
            /* Create content */
            var content = new VisualElement();
            content.AddToClassList("Content");
            root.Add(content);
            
            

            for (int i = 0; i < types.Length; i++)
            {
                var index = i;
                content.Add(SocketSystemEditorUtils.CreatePinView(types[i], pins[i], pins[i] != null ? pins[i].debugColor : Color.black, evt =>
                {
                    pins[index] = evt.newValue as Pin;
                    EditorUtility.SetDirty(target);
                }));
            }

            return root;
        }

        private VisualElement CreateOutputPins()
        {
            return CreatePins(((Plug) target).outputPinTypes, ((Plug) target).outputPins, "Output Pins");
        }
        
        private VisualElement CreateInputPins()
        {
            return CreatePins(((Plug) target).inputPinTypes, ((Plug) target).inputPins, "Input Pins");
        }
    }
}