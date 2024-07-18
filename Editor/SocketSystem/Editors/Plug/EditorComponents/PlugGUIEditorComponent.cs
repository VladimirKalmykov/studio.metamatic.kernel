using System;
using System.Linq;
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
        private Label m_ConnectionLabel;
        private VisualElement m_ConnectionContainer;
        private VisualElement m_ConnectionContentContainer;
        private VisualElement m_SocketIndicatorDot;
        private VisualElement m_PinsContainer;
        private Toggle m_AutoConnectSocketByNameElement;
        private TextField m_SocketNameElement;
        private SocketScheme m_SocketScheme;
        private AutoConnectStrategies m_AutoConnectStrategy;
        private bool m_AutoConnectByName;
        private Action m_UnsubscribeAutoLocked;
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
            m_UnsubscribeAutoLocked = plug.autoLocked.Subscribe(HandleAutoLockedChanged);
            HandleReconnectIn(true);
            HandleErrorChanged();
        }
        
        public override void OnDisable()
        {
            m_ConnectionContainer.style.display = DisplayStyle.None;
            m_SocketScheme.OnReconnectIn -= HandleReconnectIn;
            m_SocketScheme.OnErrorChanged -= HandleErrorChanged;
            m_UnsubscribeAutoLocked?.Invoke();
        }
        
        private void HandleAutoLockedChanged(bool autoLocked)
        {
            m_ConnectionLabel.text = autoLocked ? "Connection (Locked)" : "Connection";
        }
        
        private void HandleReconnectIn(bool connected)
        {
            var scheme = m_SocketScheme.GetConnectedInScheme();
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
            
            /* Header */
            var header = new VisualElement();
            header.AddToClassList("PlugHeader");
            var label = new Label("PLUG");
            label.AddToClassList("Title");
            header.Add(label);
            var typeContainer = new VisualElement();
            typeContainer.AddToClassList("TypeContainer");
            var typeLabel = new Label(MiscUtils.GetGenericName(target.GetType().BaseType, true, true));
            typeLabel.AddToClassList("Type");
            typeContainer.Add(typeLabel);
            header.Add(typeContainer);
            root.Add(header);
            
            /* Settings */
            var settings = new VisualElement();
            settings.AddToClassList("Settings");
            root.Add(settings);
            
            /* Autoconnect */
            var autoConnectProp = serializedObject.FindProperty("autoConnect");
            m_AutoConnectStrategy = (AutoConnectStrategies) autoConnectProp.enumValueIndex;
            var autoConnectToggle = new EnumField("Auto Connect")
            {
                bindingPath = autoConnectProp.propertyPath
            };

            autoConnectToggle.BindProperty(autoConnectProp);
            autoConnectToggle.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue == null)
                {
                    return;
                }
                m_AutoConnectStrategy = (AutoConnectStrategies) evt.newValue;
                UpdateSettings();
            });
            settings.Add(autoConnectToggle);
            autoConnectToggle.SetEnabled(!Application.isPlaying);
            
            /* Connect by name */
            var autoConnectByNameProp = serializedObject.FindProperty("autoConnectByName");
            m_AutoConnectByName = autoConnectByNameProp.boolValue;
            var autoConnectByNameToggle = m_AutoConnectSocketByNameElement = new Toggle("Named")
            {
                bindingPath = autoConnectByNameProp.propertyPath
            };
            autoConnectByNameToggle.BindProperty(autoConnectByNameProp);
            autoConnectByNameToggle.RegisterValueChangedCallback(evt =>
            {
                m_AutoConnectByName = evt.newValue;
                UpdateSettings();
            });
            settings.Add(autoConnectByNameToggle);
            
            /* Debug mode toggle (boolean switcher) */
            // var debugModeProp = serializedObject.FindProperty("debugMode");
            // var debugModeToggle = new Toggle("Debug Mode")
            // {
            //     bindingPath = debugModeProp.propertyPath
            // };
            // debugModeToggle.BindProperty(debugModeProp);
            // settings.Add(debugModeToggle);
            
            
            /* Socket name */
            var socketNameProp = serializedObject.FindProperty("socketName");
            var socketNameField = m_SocketNameElement = new TextField("Socket Name")
            {
                bindingPath = socketNameProp.propertyPath
            };
            socketNameField.BindProperty(socketNameProp);
            settings.Add(socketNameField);
            
            
            /* Pins container */
            m_PinsContainer = new VisualElement();
            root.Add(m_PinsContainer);
            
            /* Create connection view */
            var connectionView = m_ConnectionContainer = CreateConnectionView();
            root.Add(connectionView);
            
            /* Create error message */
            var errorMessage = CreateErrorMessage();
            root.Add(errorMessage);
            
            RedrawPinsView();
            UpdateSettings();
            
            return root;
        }

        private void UpdateSettings()
        {
            m_AutoConnectSocketByNameElement.style.display = m_AutoConnectStrategy != AutoConnectStrategies.Disabled ? DisplayStyle.Flex : DisplayStyle.None;
            m_SocketNameElement.style.display = m_AutoConnectStrategy != AutoConnectStrategies.Disabled && m_AutoConnectByName ? DisplayStyle.Flex : DisplayStyle.None;
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
            var label = m_ConnectionLabel = new Label("Connection");
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
            var plug = (Plug) target;
            return CreatePins(plug.outputPinTypes, plug.outputPins, "Output Pins", (nextValue, index) =>
            {
                var nextPins = plug.outputPins.ToArray();
                nextPins[index] = nextValue;
                plug.outputPins = nextPins;
                EditorUtility.SetDirty(plug);
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
            var plug = (Plug) target;
            return CreatePins(plug.inputPinTypes, plug.inputPins, "Input Pins", (nextValue, index) =>
            {
                var nextPins = plug.inputPins.ToArray();
                nextPins[index] = nextValue;
                plug.inputPins = nextPins;
                EditorUtility.SetDirty(plug);
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