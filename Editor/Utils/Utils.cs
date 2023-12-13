using System;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic
{
    public class EditorUtils
    {
        public static VisualElement ValueToTable(object data, Type dataType)
        {
            // Если data - примитивный тип, возвращаем его строковое представление
            if (dataType.IsPrimitive || dataType == typeof(string) || dataType == typeof(decimal))
            {
                return new Label(data.ToString());
            }

            // Создаем табличное представление для сложных типов
            var table = new VisualElement();
            table.style.flexDirection = FlexDirection.Column;
            var props = dataType.GetFields();
            Debug.Log($"props: {JsonUtility.ToJson(props)}");
            foreach (var prop in props)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;

                var keyLabel = new Label(prop.Name);
                var valueLabel = new Label();

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