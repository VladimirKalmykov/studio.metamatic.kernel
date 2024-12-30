using Cysharp.Threading.Tasks;
using Metamatic.UIElements;

namespace Metamatic.Framework
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class StructEditorFormElement : AsyncVisualElement
    {
        private readonly Type m_TargetType;
        private readonly Func<object> m_ValueGetter;
        private readonly Action<object> m_ValueSetter;
        private readonly Dictionary<string, (FieldInfo fieldInfo, Field field, VisualElement editor)> m_FieldElements = new ();

        public StructEditorFormElement(Type type, Func<object> valueGetter = null, Action<object> valueSetter = null)
        {
            m_TargetType = type;
            m_ValueGetter = valueGetter;
            m_ValueSetter = valueSetter;
            GenerateForm();
        }

        private void GenerateForm()
        {
            // Use reflection to get public fields
            FieldInfo[] fields = m_TargetType.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (FieldInfo field in fields)
            {
                var fieldElement = new Field(field.Name);

                // Create an editor control based on the field type
                VisualElement fieldEditor = CreateFieldEditor(field);
                if (fieldEditor != null)
                {
                    m_FieldElements.Add(field.Name, (field, fieldElement, fieldEditor));
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
            VisualElement editorElement = null;
            // Handle basic types
            if (fieldType == typeof(int))
            {
                IntegerField intField = new IntegerField();
                if (m_ValueSetter != null)
                {
                    // Register callback to update the value in the struct
                    intField.RegisterValueChangedCallback(evt =>
                    {
                        m_ValueSetter(Submit(field.Name));
                    });
                }
                return intField;
            }

            if (fieldType == typeof(float))
            {
                FloatField floatField = new FloatField();
                if (m_ValueSetter != null)
                {
                    // Register callback to update the value in the struct
                    floatField.RegisterValueChangedCallback(evt =>
                    {
                        m_ValueSetter(Submit(field.Name));
                    });
                }
                return floatField;
            }

            if (fieldType == typeof(string))
            {
                TextField textField = new TextField();
                if (m_ValueSetter != null)
                {
                    // Register callback to update the value in the struct
                    textField.RegisterValueChangedCallback(evt =>
                    {
                        m_ValueSetter(Submit(field.Name));
                    });
                }
                return textField;
            }

            if (fieldType == typeof(bool))
            {
                Toggle toggle = new Toggle();
                if (m_ValueSetter != null)
                {
                    // Register callback to update the value in the struct
                    toggle.RegisterValueChangedCallback(evt =>
                    {
                        m_ValueSetter(Submit(field.Name));
                    });
                }
                return toggle;
            }

            if (fieldType.IsEnum)
            {
                EnumField enumField = new EnumField((Enum)Enum.GetValues(fieldType).GetValue(0));
                if (m_ValueSetter != null)
                {
                    // Register callback to update the value in the struct
                    enumField.RegisterValueChangedCallback(evt =>
                    {
                        m_ValueSetter(Submit(field.Name));
                    });
                }
                return enumField;
            }

            if (fieldType == typeof(Vector2))
            {
                Vector2Field vector2Field = new Vector2Field();
                if (m_ValueSetter != null)
                {
                    // Register callback to update the value in the struct
                    vector2Field.RegisterValueChangedCallback(evt =>
                    {
                        m_ValueSetter(Submit(field.Name));
                    });
                }
                return vector2Field;
            }

            if (fieldType == typeof(Vector3))
            {
                Vector3Field vector3Field = new Vector3Field();
                if (m_ValueSetter != null)
                {
                    // Register callback to update the value in the struct
                    vector3Field.RegisterValueChangedCallback(evt =>
                    {
                        m_ValueSetter(Submit(field.Name));
                    });
                }
                return vector3Field;
            }

            if (fieldType == typeof(bool))
            {
                Toggle toggle = new Toggle();
                if (m_ValueSetter != null)
                {
                    // Register callback to update the value in the struct
                    toggle.RegisterValueChangedCallback(evt =>
                    {
                        m_ValueSetter(Submit(field.Name));
                    });
                }
                return toggle;
            }

            // Unsupported field type
            return null;
        }

        // Method to create an instance of the struct with values from the form
        public object Submit(string onlyFieldWithName = null)
        {
            object instance = m_ValueGetter != null ? m_ValueGetter() : Activator.CreateInstance(m_TargetType);

            // The mode where only one field is updated
            if (onlyFieldWithName != null)
            {
                if (m_FieldElements.TryGetValue(onlyFieldWithName, out var fieldElement))
                {
                    FieldInfo field = fieldElement.fieldInfo;
                    object value = GetValueFromEditor(field.FieldType, fieldElement.editor);
                    field.SetValue(instance, value);
                    return instance;
                }
            }

            foreach (var kvp in m_FieldElements)
            {
                string fieldName = kvp.Key;
                VisualElement editor = kvp.Value.editor;
                FieldInfo field = m_TargetType.GetField(fieldName);

                if (field != null)
                {
                    object value = GetValueFromEditor(field.FieldType, editor);
                    field.SetValue(instance, value);
                }
            }

            return instance;
        }
        
        private void SetValuesFromOrigin(object origin)
        {
            foreach (var kvp in m_FieldElements)
            {
                FieldInfo field = kvp.Value.fieldInfo;
                SetValueFromOrigin(field, origin);
            }
        }
        
        private void SetValueFromOrigin(FieldInfo field, object origin)
        {
            object value = field.GetValue(origin);
            if (m_FieldElements.TryGetValue(field.Name, out var fieldElement))
            {
                VisualElement editor = fieldElement.editor;
                SetValueToEditor(field.FieldType, editor, value);
            }
        }
        
        private void SetValueToEditor(Type fieldType, VisualElement editor, object value)
        {
            if (fieldType == typeof(int))
            {
                ((IntegerField)editor).value = (int)value;
            }
            else if (fieldType == typeof(float))
            {
                ((FloatField)editor).value = (float)value;
            }
            else if (fieldType == typeof(string))
            {
                ((TextField)editor).value = (string)value;
            }
            else if (fieldType == typeof(bool))
            {
                ((Toggle)editor).value = (bool)value;
            }
            else if (fieldType.IsEnum)
            {
                ((EnumField)editor).value = (Enum)value;
            }
            else if (fieldType == typeof(Vector2))
            {
                ((Vector2Field)editor).value = (Vector2)value;
            }
            else if (fieldType == typeof(Vector3))
            {
                ((Vector3Field)editor).value = (Vector3)value;
            }
            else if (fieldType == typeof(bool))
            {
                ((Toggle)editor).value = (bool)value;
            }
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

        public override async UniTask OnAttachedAsync(Signal detachSignal)
        {
            if (m_ValueGetter != null)
            {
                using var updateRunner = PlayerLoop.EachFrame(() =>
                {
                    SetValuesFromOrigin(m_ValueGetter());
                }, PlayerLoopTiming.Update);
                await detachSignal;
            }
        }
    }
}