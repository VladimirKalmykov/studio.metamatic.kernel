using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// This element draws a simple line graph based on a list of integer values.
/// </summary>
public class LineGraph : VisualElement
{
    private List<int> data = new List<int>();
    private int minValue = int.MaxValue;
    private int maxValue = int.MinValue;

    public LineGraph()
    {
        this.style.backgroundColor = new StyleColor(Color.black);
        this.style.width = 500;
        this.style.height = 200;
        this.generateVisualContent += OnGenerateVisualContent;
    }

    // Добавляем новое значение в график
    public void AddDataPoint(int value)
    {
        data.Add(value);

        // Обновляем минимальные и максимальные значения
        if (value < minValue) minValue = value;
        if (value > maxValue) maxValue = value;

        // Перерисовываем график
        MarkDirtyRepaint();
    }

    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        if (data.Count < 2)
            return;

        var width = contentRect.width;
        var height = contentRect.height;

        var content = mgc.painter2D;
        content.strokeColor = Color.green;
        content.lineWidth = 2.0f;

        // Масштабируем график по высоте, основываясь на текущем минимальном и максимальном значении
        float scale = height / (maxValue - minValue);

        // Рисуем линии между точками
        for (int i = 1; i < data.Count; i++)
        {
            float x1 = (i - 1) * (width / (data.Count - 1));
            float y1 = height - (data[i - 1] - minValue) * scale;

            float x2 = i * (width / (data.Count - 1));
            float y2 = height - (data[i] - minValue) * scale;

            content.BeginPath();
            content.MoveTo(new Vector2(x1, y1));
            content.LineTo(new Vector2(x2, y2));
            content.Stroke();
        }
    }
}