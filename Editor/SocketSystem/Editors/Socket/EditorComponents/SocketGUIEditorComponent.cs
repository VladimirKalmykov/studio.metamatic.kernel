using Metamatic.Extensions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Metamatic
{
    public class SocketGUIEditorComponent : EditorComponent
    {
        private VisualElement m_ErrorContainer;
        private Label m_ErrorLabel;
        private VisualElement m_PlugContainer;
        private SocketScheme m_SocketScheme;
        public SocketGUIEditorComponent(ComposableEditor composableEditor) : base(composableEditor)
        {
        }

        public override void OnEnable()
        {
            var socket = (Socket) target;
            m_SocketScheme = socket.socketScheme;
            m_SocketScheme.OnReconnect += HandleReconnect;
            m_SocketScheme.OnErrorChanged += HandleErrorChanged;
            HandleReconnect();
            HandleErrorChanged();
        }
        
        public override void OnDisable()
        {
            m_SocketScheme.OnReconnect -= HandleReconnect;
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

        private void HandleReconnect()
        {
            var schemes = m_SocketScheme.debug_GetConnectedSchemes();
            m_PlugContainer.Clear();
            foreach (var scheme in schemes)
            {
                // TODO: Create plug
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            
            /* Create header */
            var header = new VisualElement();
            header.AddToClassList("SocketGUIHeader");
            header.Add(new Label("Socket"));
            root.Add(header);

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
            var plugs = CreatePlugs();
            root.Add(plugs);
            
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
            var label = new Label("Plugs");
            header.Add(label);
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

        private VisualElement CreateOutputPins()
        {
            var socket = (Socket) target;
            var pinTypes = socket.outputPinTypes;
            
            if (pinTypes.Length == 0)
            {
                return null;
            }
            
            var root = new VisualElement();
            root.AddToClassList("Section");
            
            /* Create header */
            var header = new VisualElement();
            header.AddToClassList("Header");
            var label = new Label("Output Pins");
            header.Add(label);
            root.Add(header);
            
            /* Create content */
            var content = new VisualElement();
            content.AddToClassList("Content");
            root.Add(content);
            
            

            for (int i = 0; i < pinTypes.Length; i++)
            {
                var objectField = new ObjectField()
                {
                    objectType = pinTypes[i],
                    value = socket.outputPins[i]
                };

                var index = i;
                objectField.RegisterValueChangedCallback(evt =>
                {
                    socket.outputPins[index] = evt.newValue as Pin;
                    EditorUtility.SetDirty(target);
                });

                content.Add(objectField);
            }

            return root;
        }
        
        private VisualElement CreateInputPins()
        {
            var socket = (Socket) target;
            var pinTypes = socket.inputPinTypes;
            
            if (pinTypes.Length == 0)
            {
                return null;
            }
            
            var root = new VisualElement();
            root.AddToClassList("Section");
            
            /* Create header */
            var header = new VisualElement();
            header.AddToClassList("Header");
            var label = new Label("Input Pins");
            header.Add(label);
            root.Add(header);
            
            /* Create content */
            var content = new VisualElement();
            content.AddToClassList("Content");
            root.Add(content);
            
            

            for (int i = 0; i < pinTypes.Length; i++)
            {
                var objectField = new ObjectField()
                {
                    objectType = pinTypes[i],
                    value = socket.inputPins[i]
                };

                var index = i;
                objectField.RegisterValueChangedCallback(evt =>
                {
                    socket.inputPins[index] = evt.newValue as Pin;
                    EditorUtility.SetDirty(target);
                });

                content.Add(objectField);
            }

            return root;
        }
    }
}