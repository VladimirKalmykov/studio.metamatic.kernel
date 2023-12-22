using System;
using Metamatic.Extensions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Application = UnityEngine.Device.Application;

namespace Metamatic
{
    public class SocketGUIEditorComponent : EditorComponent
    {
        private VisualElement m_ErrorContainer;
        private Label m_ErrorLabel;
        private VisualElement m_PlugContainer;
        private VisualElement m_PlugIndicatorDot;
        private VisualElement m_Plugs;
        private SocketScheme m_SocketScheme;
        public SocketGUIEditorComponent(ComposableEditor composableEditor) : base(composableEditor)
        {
        }

        public override void OnEnable()
        {
            m_Plugs.style.display = Application.isPlaying ? DisplayStyle.Flex : DisplayStyle.None;
            var socket = (Socket) target;
            m_SocketScheme = socket.socketScheme;
            m_SocketScheme.OnReconnectOut += HandleReconnectOut;
            m_SocketScheme.OnErrorChanged += HandleErrorChanged;
            HandleReconnectOut();
            HandleErrorChanged();
        }
        
        public override void OnDisable()
        {
            m_Plugs.style.display = DisplayStyle.None;
            m_SocketScheme.OnReconnectOut -= HandleReconnectOut;
            m_SocketScheme.OnErrorChanged -= HandleErrorChanged;
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

        private void HandleReconnectOut()
        {
            var schemes = m_SocketScheme.debug_GetConnectedOutSchemes();
            m_PlugContainer.Clear();

            if (!Application.isPlaying)
            {
                m_PlugIndicatorDot.RemoveFromClassList("Disconnected");
                m_PlugIndicatorDot.RemoveFromClassList("Connected");
                return;
            }
            
            if (schemes.Length > 0)
            {
                m_PlugIndicatorDot.RemoveFromClassList("Disconnected");
                m_PlugIndicatorDot.AddToClassList("Connected");
                
                foreach (var scheme in schemes)
                {
                    var relatedObjectField = new ObjectField()
                    {
                        value = scheme.debug_RelatedObject,
                        objectType = scheme.debug_RelatedObject.GetType()
                    };
                    m_PlugContainer.Add(relatedObjectField);
                }
            }
            else
            {
                m_PlugContainer.Add(new Label("No plugs connected."));
                m_PlugIndicatorDot.RemoveFromClassList("Connected");
                m_PlugIndicatorDot.AddToClassList("Disconnected");
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            /* Create output pins */
            var outputPins = CreateOutputPins();
            if (outputPins != null)
            {
                root.Add(outputPins);
            }
            
            /* Create input pins */
            var inputPins = CreateInputPins();
            if (inputPins != null)
            {
                root.Add(inputPins);
            }
            
            /* Create plugs */
            m_Plugs = CreatePlugs();
            m_Plugs.style.display = DisplayStyle.None;
            root.Add(m_Plugs);
            
            /* Create error message */
            var errorMessage = CreateErrorMessage();
            root.Add(errorMessage);
            
            return root;
        }

        private VisualElement CreatePlugs()
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
            var indicatorDot = m_PlugIndicatorDot = new VisualElement();
            indicatorDot.AddToClassList("Dot");
            indicator.Add(indicatorDot);
            labelContainer.Add(indicator);
            var label = new Label("Plugs");
            labelContainer.Add(label);
            root.Add(header);
            
            /* Create content */
            var content = m_PlugContainer = new VisualElement();
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
            var socket = (Socket) target;
            
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
                content.Add(SocketSystemEditorUtils.CreatePinView(types[i], pins[i], pins[i] == null ? Color.black : pins[i].debugColor, evt =>
                {
                    pins[index] = evt.newValue as Pin;
                    EditorUtility.SetDirty(target);
                }));
            }

            return root;
        }

        private VisualElement CreateOutputPins()
        {
            return CreatePins(((Socket) target).outputPinTypes, ((Socket) target).outputPins, "Output Pins");
        }
        
        private VisualElement CreateInputPins()
        {
            return CreatePins(((Socket) target).inputPinTypes, ((Socket) target).inputPins, "Input Pins");
        }
    }
}