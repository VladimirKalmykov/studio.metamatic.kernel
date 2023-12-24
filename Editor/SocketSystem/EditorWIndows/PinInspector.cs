using System;
using Metamatic;
using Metamatic.Extensions;
using Metamatic.UI;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using ObjectField = UnityEditor.UIElements.ObjectField;

namespace Metamatic
{
    public class PinInspector
    {
        private VisualElement m_RootElement;
        private VisualElement m_Parent;
        private Pin m_Pin;
        private Button m_InspectButton;
        private VisualElement m_OutputPinObjectFieldContainer;
        private Pin m_CurrentComponent;
        private VisualElement m_ValueContainer;
        private Type m_DataType;
        private Pin m_CachedComponent;
        private ObjectField m_ComponentSelector;
        
        public VisualElement root => m_RootElement;
        public Pin pin => m_Pin;
        public PinInspector(VisualElement parent, Pin pin)
        {
            m_Parent = parent;
            m_RootElement = new VisualElement();
            m_RootElement.AddToClassList("PlayModePinContainer");
            m_Parent.Add(m_RootElement);
            m_Pin = pin;
            
            /* Add row element */
            var row = new VisualElement();
            row.AddToClassList("Row");
            m_RootElement.Add(row);
            
            /* Unwatch button */
            var unwatchButton = new Button();
            unwatchButton.text = "Unwatch";
            unwatchButton.clicked += () =>
            {
                Pin.RemoveFromInspect(pin);
                unwatchButton.SetEnabled(false);
            };
            
            var pinView = SocketSystemEditorUtils.CreatePinView(typeof(Pin), pin, pin == null ? Color.black : pin.debugColor, null, new []
            {
                (VisualElement) unwatchButton
            });
            row.Add(pinView);
            
            /* Add second row */
            var secondRow = new VisualElement();
            secondRow.AddToClassList("Row");
            m_RootElement.Add(secondRow);

            m_ValueContainer = new VisualElement();
            m_ValueContainer.AddToClassList("Fullwidth");
            secondRow.Add(m_ValueContainer);
        
            /* Get component type */
            HandleComponentChange(pin);
        }
        
        public void SetCurrentComponent(Pin component)
        {
            HandleComponentChange(component);
            UpdateData();
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
            if (m_CurrentComponent != null)
            {
                if (m_DataType != null)
                {
                    object data = ((dynamic)m_CurrentComponent).GetWireObjectData();
                    m_ValueContainer.Clear();
                    m_ValueContainer.Add(new StructTable(data, m_DataType));
                }
            }
        }
        
        private void ClearDataInView()
        {
            m_ValueContainer.Clear();
        }

        public void Dispose()
        {
            if (m_Parent.Contains(m_RootElement))
            {
                m_Parent.Remove(m_RootElement);
            }
            
            if (m_CurrentComponent != null)
            {
                m_CurrentComponent.OnDebugDataChanged -= UpdateData;
                m_CurrentComponent = null;
            }
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
}