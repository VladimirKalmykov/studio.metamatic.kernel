using System;
using Metamatic.UIToolKit;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic.Framework
{
    public class IdEditorElement: VisualElement
    {
        private string m_Value = "";
        public event Action<string> onValueChanged;
        
        public string value
        {
            get => m_Value;
            set
            {
                m_Value = value;
                if (m_IdField != null)
                    m_IdField.value = m_Value;
            }
        }
        
        private TextField m_IdField;
        public IdEditorElement(Func<string> generateId = null)
        {
            var idField = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.FlexStart,
                    alignItems = Align.Center,
                }
            };

            var label = new Label("ID");
            idField.Add(label);

            var idEditorInput = m_IdField = new TextField
            {
                value = m_Value,
                isDelayed = true,
                isReadOnly = true
            };
            idEditorInput.style.flexGrow = 1;
            idEditorInput.RegisterValueChangedCallback(evt =>
            {
                m_Value = evt.newValue;
                onValueChanged?.Invoke(m_Value);
                // Set readonly back to true
                idEditorInput.isReadOnly = true;
            });

            idField.Add(idEditorInput);
            
            // Add edit button
            var editButton = new Button()
            {
                text = "Edit"
            };
            editButton.AddToClassList(UIToolKitCore.Tailwind.MarginLeft6);
            editButton.style.height = 20;
            editButton.clicked += () =>
            {
                if (ShowEditConfirmationDialog())
                {
                    idEditorInput.isReadOnly = false;
                    idEditorInput.Focus();
                }
            };
            
            idField.Add(editButton);
            
            if (generateId != null)
            {
                var regenerateIdButton = new Button()
                {
                    text = "Generate"
                };
                regenerateIdButton.style.height = 20;
                regenerateIdButton.clicked += () =>
                {
                    // Show confirmation dialog
                    if (ShowEditConfirmationDialog())
                    {
                        // Generate new ID
                        value = generateId();
                        onValueChanged?.Invoke(m_Value);
                    }
                };
                idField.Add(regenerateIdButton);
            }
            
            

            Add(idField);
        }
        
        public bool ShowEditConfirmationDialog()
        {
            return EditorUtility.DisplayDialog("Generate ID", "Previous game saves can be incompatible with new ID. Are you sure you want to generate a new ID?", "Yes",
                "No");
        }
    }
}