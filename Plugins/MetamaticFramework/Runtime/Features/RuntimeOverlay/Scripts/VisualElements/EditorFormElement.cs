namespace Metamatic.Framework
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class EditorFormElement : VisualElement
    {
        private Type targetType;
        private readonly Dictionary<string, (Field field, VisualElement editor)> fieldElements = new ();

        public EditorFormElement(Type type)
        {
            targetType = type;
            GenerateForm();
        }

        private void GenerateForm()
        {
            // Use reflection to get public fields
            FieldInfo[] fields = targetType.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (FieldInfo field in fields)
            {
                var fieldElement = new Field(field.Name);

                // Create an editor control based on the field type
                VisualElement fieldEditor = CreateFieldEditor(field);
                if (fieldEditor != null)
                {
                    fieldElements.Add(field.Name, (fieldElement, fieldEditor));
                    fieldElement.AddContent(fieldEditor);
                }
                else
                {
                    // If no editor is available, display a readonly label
                    Label readOnlyLabel = new Label($"Unsupported field type: {field.FieldType.Name}");
                    fieldElement.AddContent(readOnlyLabel);
                }
                Add(fieldElement);
            }
        }

        private VisualElement CreateFieldEditor(FieldInfo field)
        {
            Type fieldType = field.FieldType;

            // Handle basic types
            if (fieldType == typeof(int))
            {
                IntegerField intField = new IntegerField();
                return intField;
            }
            else if (fieldType == typeof(float))
            {
                FloatField floatField = new FloatField();
                return floatField;
            }
            else if (fieldType == typeof(string))
            {
                TextField textField = new TextField();
                return textField;
            }
            else if (fieldType == typeof(bool))
            {
                Toggle toggle = new Toggle();
                return toggle;
            }
            else if (fieldType.IsEnum)
            {
                EnumField enumField = new EnumField((Enum)Enum.GetValues(fieldType).GetValue(0));
                return enumField;
            }
            else if (fieldType == typeof(Vector2))
            {
                Vector2Field vector2Field = new Vector2Field();
                return vector2Field;
            }
            else if (fieldType == typeof(Vector3))
            {
                Vector3Field vector3Field = new Vector3Field();
                return vector3Field;
            }
            else if (fieldType == typeof(bool))
            {
                Toggle toggle = new Toggle();
                return toggle;
            }
            else
            {
                // Unsupported field type
                return null;
            }
        }

        // Method to create an instance of the struct with values from the form
        public object Submit()
        {
            object instance = Activator.CreateInstance(targetType);

            foreach (var kvp in fieldElements)
            {
                string fieldName = kvp.Key;
                VisualElement editor = kvp.Value.editor;
                FieldInfo field = targetType.GetField(fieldName);

                if (field != null)
                {
                    object value = GetValueFromEditor(field.FieldType, editor);
                    field.SetValue(instance, value);
                }
            }

            return instance;
        }

        private object GetValueFromEditor(Type fieldType, VisualElement editor)
        {
            if (fieldType == typeof(int))
            {
                return ((IntegerField)editor).value;
            }
            else if (fieldType == typeof(float))
            {
                return ((FloatField)editor).value;
            }
            else if (fieldType == typeof(string))
            {
                return ((TextField)editor).value;
            }
            else if (fieldType == typeof(bool))
            {
                return ((Toggle)editor).value;
            }
            else if (fieldType.IsEnum)
            {
                return ((EnumField)editor).value;
            }
            else if (fieldType == typeof(Vector2))
            {
                return ((Vector2Field)editor).value;
            }
            else if (fieldType == typeof(Vector3))
            {
                return ((Vector3Field)editor).value;
            } else if (fieldType == typeof(bool))
            {
                return ((Toggle)editor).value;
            }
            else
            {
                return null;
            }
        }
    }
}