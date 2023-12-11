using Metamatic.Extensions;
using UnityEditor;
using UnityEngine.UIElements;

namespace Metamatic.Effects
{
    public class PinConnectionsEditorComponent : EditorComponent
    {
        private VisualElement m_LeftBigCircleStatus;
        private VisualElement m_LeftSmallCircleStatus;
        private VisualElement m_RightBigCircleStatus;
        private VisualElement m_RightSmallCircleStatus;
        private Pin m_Pin;
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
        
            // Создаем элементы INPUT и OUTPUT
            var inputBlock = CreateLeftPinBlock("INPUT");
            var outputBlock = CreateRightPinBlock("OUTPUT");
            
            var statusBar = new VisualElement();
            statusBar.AddToClassList("StatusBar");
            
            // Добавляем их к корневому элементу
            statusBar.Add(inputBlock);
            statusBar.Add(outputBlock);
            
            root.Add(statusBar);
            
            root.styleSheets.Add(this.editor.LoadRelativeAsset<StyleSheet>("./pin.uss"));
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

        private void UpdateData()
        {
            var pin = m_Pin;
            if (pin.hasInputPin)
            {
                m_LeftBigCircleStatus.AddToClassList("StatusCircle_Enabled");
                m_LeftSmallCircleStatus.AddToClassList("PinBlockLineCircle_Enabled");
            }
            else
            {
                m_LeftBigCircleStatus.RemoveFromClassList("StatusCircle_Enabled");
                m_LeftSmallCircleStatus.RemoveFromClassList("PinBlockLineCircle_Enabled");
            }

            if (pin.hasOutputPins)
            {
                m_RightBigCircleStatus.AddToClassList("StatusCircle_Enabled");
                m_RightSmallCircleStatus.AddToClassList("PinBlockLineCircle_Enabled");
            }
            else
            {
                m_RightBigCircleStatus.RemoveFromClassList("StatusCircle_Enabled");
                m_RightSmallCircleStatus.RemoveFromClassList("PinBlockLineCircle_Enabled");
            }
        }
        
        public override void OnEnable()
        {
            base.OnEnable();
            m_Pin = (Pin)target;
            EditorApplication.update += UpdateData;
        }
        
        public override void OnDisable()
        {
            base.OnDisable();
            EditorApplication.update -= UpdateData;
        }
    }
}