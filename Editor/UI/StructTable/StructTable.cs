using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic.UI
{
    public class StructTable: VisualElement
    {
        public StructTable(object data, Type dataType) : base()
        {
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/studio.metamatic.kernel/Editor/UI/StructTable/StructTable.uss"));
            Clear();
            Add(DataToVisualElement(data, dataType));
        }
        
        public static VisualElement DataToVisualElement(object rawData, Type dataType)
        {
            var data = rawData;
            Type underlyingType = Nullable.GetUnderlyingType(dataType);
            if (underlyingType != null)
            {
                dataType = underlyingType;
                if (rawData != null)
                {
                    var nullableType = typeof(Nullable<>).MakeGenericType(dataType);
                    data = nullableType.GetProperty("Value").GetValue(rawData);
                }
            }

            // Создаем табличное представление для сложных типов
            var table = new VisualElement();
            table.AddToClassList("Table");
            
            if (dataType.IsPrimitive || dataType == typeof(string) || dataType == typeof(decimal))
            {
                var row = new VisualElement();
                row.AddToClassList("Row");
                var valueLabel = new Label();
                valueLabel.AddToClassList("Value");
                valueLabel.text = data?.ToString() ?? "null";
                row.Add(valueLabel);
                table.Add(row);
                return table;
            }
            
            var props = dataType.GetFields();
            foreach (var prop in props)
            {
                var row = new VisualElement();
                row.AddToClassList("Row");

                var keyLabel = new Label(prop.Name);
                keyLabel.AddToClassList("Key");
                var valueElement = new VisualElement(); // Элемент для значения, который может быть Label или ColorField
                valueElement.AddToClassList("Value");

                var value = prop.GetValue(data);
                if (value is Color colorValue)
                {
                    // Создаем элемент для отображения превью цвета
                    var colorPreview = new VisualElement();
                    colorPreview.AddToClassList("ColorPreview");
                    colorPreview.style.backgroundColor = colorValue;
                    valueElement.Add(colorPreview);
                }
                else if (value != null && value.GetType().IsValueType)
                {
                    var valueLabel = new Label();
                    valueLabel.AddToClassList("ValueLabel");
                    valueLabel.text = value.GetType().IsPrimitive ? value.ToString() : JsonUtility.ToJson(value);
                    valueElement.Add(valueLabel);
                }
                else
                {
                    var valueLabel = new Label();
                    valueLabel.AddToClassList("ValueLabel");
                    valueLabel.text = value?.ToString() ?? "null";
                    valueElement.Add(valueLabel);
                }

                row.Add(keyLabel);
                row.Add(valueElement);
                table.Add(row);
            }

            return table;
        }
    }
}