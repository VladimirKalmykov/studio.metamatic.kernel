using Metamatic.Extensions;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Metamatic.Effects
{
    public class PinConnectionsEditorComponent : EditorComponent
    {
        private VisualElement m_LeftBigCircleStatus;
        private VisualElement m_LeftSmallCircleStatus;
        private VisualElement m_RightBigCircleStatus;
        private VisualElement m_RightSmallCircleStatus;
        private ObjectField m_InputObjectField;
        private VisualElement m_OutputPinObjectFieldContainer;
        private Pin m_Pin;
        private VisualElement m_Root;
        private Button m_InspectButton;
        private bool m_IsInspecting;
        public override VisualElement CreateInspectorGUI()
        {
            var root = m_Root = new VisualElement();
        
            // Создаем элементы INPUT и OUTPUT
            var inputBlock = CreateLeftPinBlock("INPUT");
            var centerFunctions = CreateCenterFunctions();
            var outputBlock = CreateRightPinBlock("OUTPUT");
            
            var statusBar = new VisualElement();
            statusBar.AddToClassList("StatusBar");
            
            // Добавляем их к корневому элементу
            statusBar.Add(inputBlock);
            statusBar.Add(centerFunctions);
            statusBar.Add(outputBlock);
            
            var objectInputsRow = CreateObjectInputRow();
            
            root.Add(statusBar);
            root.Add(objectInputsRow);
            root.styleSheets.Add(this.editor.LoadRelativeAsset<StyleSheet>("./pin.uss"));
            return root;
        }

        public VisualElement CreateObjectInputRow()
        {
            var root = new VisualElement();
            root.AddToClassList("ObjectInputsRow");
            
            /* Create input object field */
            var inputPinObjectFieldContainer = new VisualElement();
            var inputPinObjectField = m_InputObjectField = new ObjectField()
            {
                objectType = typeof(Pin)
            };
            
            inputPinObjectFieldContainer.Add(inputPinObjectField);
            root.Add(inputPinObjectFieldContainer);
            
            /* Create output object field container */
            m_OutputPinObjectFieldContainer = new VisualElement();
            m_OutputPinObjectFieldContainer.AddToClassList("OutputPinObjectFieldContainer");

            root.Add(m_OutputPinObjectFieldContainer);
            
            return root;
        }

        public PinConnectionsEditorComponent(ComposableEditor composableEditor) : base(composableEditor)
        {
        }
        
        private VisualElement CreateLeftPinBlock(string label)
        {
            var pinBlockContainer = new VisualElement();
            pinBlockContainer.AddToClassList("PinBlockContainer");
            
            /* Pin block */
            var pinBlock = new VisualElement();
            pinBlock.AddToClassList("PinBlock");
            
            var statusCircle = m_LeftBigCircleStatus = new VisualElement();
            statusCircle.AddToClassList("StatusCircle");
            
            var titleLabel = new Label(label);
            titleLabel.AddToClassList("PinLabel");
            pinBlock.Add(statusCircle);
            pinBlock.Add(titleLabel);
            
            /* Pin block line */
            var pinBlockLine = new VisualElement();
            pinBlockLine.AddToClassList("PinBlockLine");
            var pinBlockLineCircle = m_LeftSmallCircleStatus = new VisualElement();
            pinBlockLineCircle.AddToClassList("PinBlockLineCircle");
            
            pinBlockContainer.Add(pinBlock);
            pinBlockContainer.Add(pinBlockLine);
            pinBlockContainer.Add(pinBlockLineCircle);

            return pinBlockContainer;
        }
        
        private VisualElement CreateRightPinBlock(string label)
        {
            var pinBlockContainer = new VisualElement();
            pinBlockContainer.AddToClassList("PinBlockContainer");
            
            /* Pin block */
            var pinBlock = new VisualElement();
            pinBlock.AddToClassList("PinBlock");
            
            var statusCircle = m_RightBigCircleStatus = new VisualElement();
            statusCircle.AddToClassList("StatusCircle");
            
            var titleLabel = new Label(label);
            titleLabel.AddToClassList("PinLabel");
            pinBlock.Add(titleLabel);
            pinBlock.Add(statusCircle);
            
            /* Pin block line */
            var pinBlockLine = new VisualElement();
            pinBlockLine.AddToClassList("PinBlockLine");
            var pinBlockLineCircle = m_RightSmallCircleStatus = new VisualElement();
            pinBlockLineCircle.AddToClassList("PinBlockLineCircle");
            
            pinBlockContainer.Add(pinBlockLineCircle);
            pinBlockContainer.Add(pinBlockLine);
            pinBlockContainer.Add(pinBlock);

            return pinBlockContainer;
        }

        private VisualElement CreateCenterFunctions()
        {
            var root = new VisualElement();
            root.AddToClassList("CenterFunctions");
            m_InspectButton = new Button(() =>
            {
                Pin.ToogleInspect(m_Pin);
                PinsInspectorWindow.OpenIfNotOpened();
            })
            {
                text = "WATCH"
            };
            m_InspectButton.AddToClassList("CustomButton");
            root.Add(m_InspectButton);
            return root;
        }

        private void UpdateConnections()
        {
            var pin = m_Pin;
            m_InputObjectField.objectType = pin.GetType();
            if (pin.hasInputPin)
            {
                m_LeftBigCircleStatus.AddToClassList("StatusCircle_Enabled");
                var inputPin = pin.DebugGetInputPin();
                var inputComponent = inputPin.GetComponent();
                var isPinInstance = inputComponent is Pin;
                if (isPinInstance)
                {
                    m_InputObjectField.value = inputComponent;
                    m_InputObjectField.objectType = pin.GetType();
                    m_LeftBigCircleStatus.RemoveFromClassList("VirtualPin");
                }
                else
                {
                    m_InputObjectField.value = inputComponent;
                    m_InputObjectField.objectType = inputComponent.GetType();
                    m_LeftBigCircleStatus.AddToClassList("VirtualPin");
                }
                
                
                m_LeftSmallCircleStatus.AddToClassList("PinBlockLineCircle_Enabled");
                m_InputObjectField.RemoveFromClassList("Hidden");
            }
            else
            {
                m_LeftBigCircleStatus.RemoveFromClassList("StatusCircle_Enabled");
                m_LeftSmallCircleStatus.RemoveFromClassList("PinBlockLineCircle_Enabled");
                m_InputObjectField.AddToClassList("Hidden");
                m_LeftBigCircleStatus.RemoveFromClassList("VirtualPin");
            }

            if (pin.hasOutputPins)
            {
                m_RightBigCircleStatus.AddToClassList("StatusCircle_Enabled");
                m_RightSmallCircleStatus.AddToClassList("PinBlockLineCircle_Enabled");
                m_OutputPinObjectFieldContainer.Clear();

                var onlyVirtualPins = true;
                /* Create output object field for each */
                foreach (var outputPin in pin.DebugGetOutputPins())
                {
                    var outputComponent = outputPin.GetComponent();
                    var isPinInstance = outputComponent is Pin;
                    if (isPinInstance)
                    {
                        onlyVirtualPins = false;
                    }
                    var outputPinObjectField = new ObjectField()
                    {
                        objectType = outputComponent.GetType(),
                        value = outputPin.GetComponent()
                    };
                    m_OutputPinObjectFieldContainer.Add(outputPinObjectField);
                }

                if (onlyVirtualPins)
                {
                    m_RightBigCircleStatus.AddToClassList("VirtualPin");
                }
                else
                {
                    m_RightBigCircleStatus.RemoveFromClassList("VirtualPin");
                }
                
            }
            else
            {
                m_RightBigCircleStatus.RemoveFromClassList("StatusCircle_Enabled");
                m_RightSmallCircleStatus.RemoveFromClassList("PinBlockLineCircle_Enabled");
                m_RightBigCircleStatus.RemoveFromClassList("VirtualPin");
                m_OutputPinObjectFieldContainer.Clear();
            }
        }

        private void UpdateData()
        {
            m_LeftSmallCircleStatus.AddToClassList("PinBlockLineCircle_Active");
            m_RightSmallCircleStatus.AddToClassList("PinBlockLineCircle_Active");
            m_Root.schedule.Execute(() =>
            {
                m_LeftSmallCircleStatus.RemoveFromClassList("PinBlockLineCircle_Active");
                m_RightSmallCircleStatus.RemoveFromClassList("PinBlockLineCircle_Active");
            }).StartingIn(250);
        }
    
        private void OnAddToInspect(Pin pin)
        {
            if (pin == m_Pin)
            {
                m_IsInspecting = true;
                UpdateInspectButton();
            }            
        }
        
        private void OnRemoveFromInspect(Pin pin)
        {
            if (pin == m_Pin)
            {
                m_IsInspecting = false;
                UpdateInspectButton();
            }    
        }

        private void UpdateInspectButton()
        {
            if (m_IsInspecting)
            {
                m_InspectButton.AddToClassList("Active");
            }
            else
            {
                m_InspectButton.RemoveFromClassList("Active");
            }
        }
        
        public override void OnEnable()
        {
            base.OnEnable();
            m_Pin = (Pin)target;
            m_IsInspecting = Pin.s_Inspect.Contains(m_Pin);
            UpdateInspectButton();
            /* Set field type */
            UpdateConnections();
            m_Pin.OnReconnect += UpdateConnections;
            m_Pin.OnDebugDataChanged += UpdateData;
            Pin.OnAddToInspect += OnAddToInspect;
            Pin.OnRemoveFromInspect += OnRemoveFromInspect;
            // EditorApplication.update += UpdateFrame;
        }
        
        public override void OnDisable()
        {
            base.OnDisable();
            m_Pin.OnReconnect -= UpdateConnections;
            m_Pin.OnDebugDataChanged -= UpdateData;
            // EditorApplication.update -= UpdateFrame;
        }
    }
}