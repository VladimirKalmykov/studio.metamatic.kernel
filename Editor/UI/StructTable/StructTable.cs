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
        
        public static VisualElement DataToVisualElement(object data, Type dataType)
        {
            // Если data - примитивный тип, возвращаем его строковое представление
            

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
                var valueLabel = new Label();
                valueLabel.AddToClassList("Value");

                var value = prop.GetValue(data);
                if (value != null && value.GetType().IsValueType)
                {
                    valueLabel.text = value.GetType().IsPrimitive ? value.ToString() : JsonUtility.ToJson(value);
                }
                else
                {
                    valueLabel.text = value?.ToString() ?? "null";
                }

                row.Add(keyLabel);
                row.Add(valueLabel);
                table.Add(row);
            }

            return table;
        }
    }
}