using Metamatic.Extensions;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine.UI;
using UnityEngine.UIElements;

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
        public override VisualElement CreateInspectorGUI()
        {
            var root = m_Root = new VisualElement();
        
            // Создаем элементы INPUT и OUTPUT
            var inputBlock = CreateLeftPinBlock("INPUT");
            var outputBlock = CreateRightPinBlock("OUTPUT");
            
            var statusBar = new VisualElement();
            statusBar.AddToClassList("StatusBar");
            
            // Добавляем их к корневому элементу
            statusBar.Add(inputBlock);
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

        private void UpdateConnections()
        {
            var pin = m_Pin;
            m_InputObjectField.objectType = pin.GetType();
            if (pin.hasInputPin)
            {
                m_LeftBigCircleStatus.AddToClassList("StatusCircle_Enabled");
                m_LeftSmallCircleStatus.AddToClassList("PinBlockLineCircle_Enabled");
                m_InputObjectField.value = pin.DebugGetInputPin();
                m_InputObjectField.RemoveFromClassList("Hidden");
            }
            else
            {
                m_LeftBigCircleStatus.RemoveFromClassList("StatusCircle_Enabled");
                m_LeftSmallCircleStatus.RemoveFromClassList("PinBlockLineCircle_Enabled");
                m_InputObjectField.AddToClassList("Hidden");
            }

            if (pin.hasOutputPins)
            {
                m_RightBigCircleStatus.AddToClassList("StatusCircle_Enabled");
                m_RightSmallCircleStatus.AddToClassList("PinBlockLineCircle_Enabled");
                m_OutputPinObjectFieldContainer.Clear();
                
                /* Create output object field for each */
                foreach (var outputPin in pin.DebugGetOutputPins())
                {
                    var outputPinObjectField = new ObjectField()
                    {
                        objectType = outputPin.GetType(),
                        value = outputPin
                    };
                    m_OutputPinObjectFieldContainer.Add(outputPinObjectField);
                }
            }
            else
            {
                m_RightBigCircleStatus.RemoveFromClassList("StatusCircle_Enabled");
                m_RightSmallCircleStatus.RemoveFromClassList("PinBlockLineCircle_Enabled");
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

        private void UpdateFrame()
        {
            
        }
        
        public override void OnEnable()
        {
            base.OnEnable();
            m_Pin = (Pin)target;
            /* Set field type */
            UpdateConnections();
            m_Pin.OnReconnect += UpdateConnections;
            m_Pin.OnDebugDataChanged += UpdateData;
            EditorApplication.update += UpdateFrame;
        }
        
        public override void OnDisable()
        {
            base.OnDisable();
            m_Pin.OnReconnect -= UpdateConnections;
            m_Pin.OnDebugDataChanged -= UpdateData;
            EditorApplication.update -= UpdateFrame;
        }
    }
}