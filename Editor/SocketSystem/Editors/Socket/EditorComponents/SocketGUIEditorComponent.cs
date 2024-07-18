using System;
using System.Linq;
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
        private VisualElement m_PinsContainer;
        private SocketScheme m_SocketScheme;
        private Toggle m_LoopbackToggle;
        public SocketGUIEditorComponent(ComposableEditor composableEditor) : base(composableEditor)
        {
        }

        public override void OnEnable()
        {
            m_Plugs.style.display = Application.isPlaying ? DisplayStyle.Flex : DisplayStyle.None;
            m_LoopbackToggle.SetEnabled(!Application.isPlaying);
            var socket = (Socket) target;
            socket.debug_OnValidate += HandleRevalidate;
            m_SocketScheme = socket.socketScheme;
            m_SocketScheme.OnReconnectOut += HandleReconnectOut;
            m_SocketScheme.OnErrorChanged += HandleErrorChanged;
            HandleReconnectOut();
            HandleErrorChanged();
            UpdateUI();
        }
        
        public override void OnDisable()
        {
            var socket = (Socket) target;
            socket.debug_OnValidate -= HandleRevalidate;
            m_Plugs.style.display = DisplayStyle.None;
            m_SocketScheme.OnReconnectOut -= HandleReconnectOut;
            m_SocketScheme.OnErrorChanged -= HandleErrorChanged;
        }

        private void UpdateUI()
        {
            m_LoopbackToggle.style.display = m_SocketScheme.isMonoType ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void HandleRevalidate()
        {
            RedrawPinsView();
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
                switch (m_SocketScheme.outConnectionType)
                {
                    case ConnectionType.Loopback:
                        m_PlugContainer.Add(new Label("Loopback"));
                        m_PlugIndicatorDot.RemoveFromClassList("Disconnected");
                        m_PlugIndicatorDot.AddToClassList("Connected");
                        break;
                    default:
                        m_PlugContainer.Add(new Label("No plugs connected."));
                        m_PlugIndicatorDot.RemoveFromClassList("Connected");
                        m_PlugIndicatorDot.AddToClassList("Disconnected");
                        break;
                }
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            
            /* Create config */
            var config = CreateConfig();
            root.Add(config);
            
            /* Pins container */
            m_PinsContainer = new VisualElement();
            
            root.Add(m_PinsContainer);
            
            /* Create plugs */
            m_Plugs = CreatePlugs();
            m_Plugs.style.display = DisplayStyle.None;
            root.Add(m_Plugs);
            
            /* Create error message */
            var errorMessage = CreateErrorMessage();
            root.Add(errorMessage);

            RedrawPinsView();
            
            return root;
        }

        private VisualElement CreateConfig()
        {
            var root = new VisualElement();
            root.AddToClassList("Section");
            
            /* Create loopback toggle */
            m_LoopbackToggle = new Toggle("Loopback");
            m_LoopbackToggle.value = ((Socket) target).loopback;
            m_LoopbackToggle.RegisterValueChangedCallback(evt =>
            {
                ((Socket) target).loopback = evt.newValue;
                EditorUtility.SetDirty(target);
            });
            root.Add(m_LoopbackToggle);
            
            return root;
        }

        private void RedrawPinsView()
        {
            m_PinsContainer.Clear();
            /* Create output pins */
            var outputPins = CreateOutputPins();
            if (outputPins != null)
            {
                m_PinsContainer.Add(outputPins);
            }
            
            /* Create input pins */
            var inputPins = CreateInputPins();
            if (inputPins != null)
            {
                m_PinsContainer.Add(inputPins);
            }
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
            var label = new Label("Connections");
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

        private VisualElement CreatePins(Type[] types, Pin[] pins, string title, Action<Pin, int> OnChange)
        {
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
                    OnChange(evt.newValue as Pin, index);
                }));
            }

            return root;
        }

        private VisualElement CreateOutputPins()
        {
            var socket = (Socket) target;
            return CreatePins(socket.outputPinTypes, socket.outputPins, "Output Pins", (nextValue, index) =>
            {
                var nextPins = socket.outputPins.ToArray();
                nextPins[index] = nextValue;
                socket.outputPins = nextPins;
                EditorUtility.SetDirty(socket);
                EditorApplication.CallbackFunction delayCallHandler = null;
                delayCallHandler = () =>
                {
                    RedrawPinsView();
                    EditorApplication.delayCall -= delayCallHandler;
                };
                EditorApplication.delayCall += delayCallHandler;
            });
        }
        
        private VisualElement CreateInputPins()
        {
            var socket = (Socket) target;
            return CreatePins(socket.inputPinTypes, socket.inputPins, "Input Pins", (nextValue, index) =>
            {
                var nextPins = socket.inputPins.ToArray();
                nextPins[index] = nextValue;
                socket.inputPins = nextPins;
                EditorUtility.SetDirty(socket);
                EditorApplication.CallbackFunction delayCallHandler = null;
                delayCallHandler = () =>
                {
                    RedrawPinsView();
                    EditorApplication.delayCall -= delayCallHandler;
                };
                EditorApplication.delayCall += delayCallHandler;
            });
        }
    }
}