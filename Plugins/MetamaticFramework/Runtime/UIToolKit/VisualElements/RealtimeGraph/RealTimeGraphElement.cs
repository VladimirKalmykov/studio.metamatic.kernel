namespace Metamatic.UIToolKit
{
    using UnityEngine;
    using UnityEngine.UIElements;
    using System.Collections.Generic;

    public class RealtimeGraphElement : VisualElement
    {
        private readonly struct GraphPoint
        {
            public readonly float Time;
            public readonly float Value;
            
            public GraphPoint(float time, float value)
            {
                Time = time;
                Value = value;
            }
        }
        private readonly List<GraphPoint> m_Points = new List<GraphPoint>();
        private float m_Duration = 10f;

        // Стилизация
        private readonly Color m_DefaultGraphColor = Color.green;
        private readonly Color m_DefaultBackgroundColor = Color.black;
        private float m_LineWidth; // Теперь толщину линии можно задавать

        private float m_MinValue;
        private float m_MaxValue;

        // Поля для сетки и меток
        private float m_XGridStep;
        private float m_YGridStep;
        private Color m_GridColor = new Color(1, 1, 1, 0.1f); // Едва заметный цвет для сетки
        private Label m_MaxValueLabel;
        private Label m_MinValueLabel;

        // Поля для текущего значения
        private float m_CurrentValue;
        private Label m_CurrentValueLabel;
        private Vector2 m_FixedMinMax;
        private bool m_KeepMinMaxHistory;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="duration"></param>
        /// <param name="fixedMinMax">Фиксированные минимальное и максимальное значения (Vector2(x, y))</param>
        /// <param name="keepMinMaxHistory">Запоминать минимальные и максимальные значения за всю историю</param>
        /// <param name="lineWidth">Толщина линии графика</param>
        /// <param name="xGridStep"></param>
        /// <param name="yGridStep"></param>
        public RealtimeGraphElement(float duration, Vector2 fixedMinMax = default, bool keepMinMaxHistory = false, float lineWidth = 2f, float xGridStep = -1f, float yGridStep = -1f)
        {
            m_Duration = duration;
            m_FixedMinMax = fixedMinMax;
            m_KeepMinMaxHistory = keepMinMaxHistory;
            m_LineWidth = lineWidth; // Устанавливаем толщину линии

            // Если шаг сетки по X не указан, используем duration
            m_XGridStep = xGridStep > 0 ? xGridStep : duration;
            // Сохраняем шаг сетки по Y
            m_YGridStep = yGridStep;

            // Инициализируем минимальное и максимальное значения
            m_MinValue = float.MaxValue;
            m_MaxValue = float.MinValue;

            // Указываем, что будем кастомно рисовать элемент
            generateVisualContent += DrawGraph;

            // Создаем метки и добавляем их в элемент
            m_MaxValueLabel = new Label();
            m_MinValueLabel = new Label();

            // Размер шрифта для меток максимального и минимального значений
            var smallFontSize = 10; // Вы можете настроить это значение
            var fadedColor = new Color(1, 1, 1, 0.5f); // Белый цвет с 50% непрозрачностью

            // Настраиваем метку максимального значения
            m_MaxValueLabel.style.position = Position.Absolute;
            m_MaxValueLabel.style.left = 2;
            m_MaxValueLabel.style.top = 0;
            m_MaxValueLabel.style.color = fadedColor;
            m_MaxValueLabel.style.fontSize = smallFontSize;

            // Настраиваем метку минимального значения
            m_MinValueLabel.style.position = Position.Absolute;
            m_MinValueLabel.style.left = 2;
            m_MinValueLabel.style.bottom = 0;
            m_MinValueLabel.style.color = fadedColor;
            m_MinValueLabel.style.fontSize = smallFontSize;

            Add(m_MaxValueLabel);
            Add(m_MinValueLabel);

            // Создаем метку для текущего значения
            m_CurrentValueLabel = new Label();
            m_CurrentValueLabel.style.position = Position.Absolute;
            m_CurrentValueLabel.style.left = Length.Percent(50);
            m_CurrentValueLabel.style.top = Length.Percent(50);
            m_CurrentValueLabel.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent), 0);
            m_CurrentValueLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            m_CurrentValueLabel.style.whiteSpace = WhiteSpace.NoWrap;
            m_CurrentValueLabel.style.color = Color.white;

            // Добавляем метку в элемент
            Add(m_CurrentValueLabel);

            // Регистрируем обработчик события изменения геометрии для обновления размера шрифта
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }
        
        public void SetDuration(float duration)
        {
            this.m_Duration = duration;
        }

        public void AddValue(float time, float value)
        {
            m_Points.Add(new GraphPoint(time, value));

            // Сохраняем текущее значение
            m_CurrentValue = value;

            // Удаляем старые значения
            while (m_Points.Count > 0 && m_Points[0].Time < time - m_Duration)
            {
                m_Points.RemoveAt(0);
            }
            
            // Обновляем минимальные и максимальные значения
            UpdateMinMaxValues();

            // Обновляем отображение текущего значения
            UpdateCurrentValueLabel();

            MarkDirtyRepaint();
        }

        private void UpdateMinMaxValues()
        {
            if (m_FixedMinMax != default)
            {
                m_MinValue = m_FixedMinMax.x;
                m_MaxValue = m_FixedMinMax.y;
            }
            else
            {
                if (!m_KeepMinMaxHistory)
                {
                    m_MinValue = float.MaxValue;
                    m_MaxValue = float.MinValue;
                }
                foreach (var point in m_Points)
                {
                    if (point.Value < m_MinValue)
                    {
                        m_MinValue = point.Value;
                    }
                    if (point.Value > m_MaxValue)
                    {
                        m_MaxValue = point.Value;
                    }
                }
            }
            

            // Убедимся, что диапазон не нулевой
            if (Mathf.Approximately(m_MaxValue, m_MinValue))
            {
                m_MaxValue += 0.5f;
                m_MinValue -= 0.5f;
            }

            // Обновляем текст меток
            m_MaxValueLabel.text = $"Max: {m_MaxValue:0.000}";
            m_MinValueLabel.text = $"Min: {m_MinValue:0.000}";

            // Если шаг сетки по Y не установлен, не рассчитываем его автоматически
            if (m_YGridStep <= 0)
            {
                // В этом случае будем рисовать только горизонтальную линию на нуле
                m_YGridStep = -1f; // Поддерживаем отрицательное значение, чтобы отличить этот случай
            }
            else
            {
                // Если шаг сетки по Y задан, проверяем, что он положительный
                if (m_YGridStep > 0)
                {
                    // Автоматический шаг сетки по Y не нужен
                }
                else
                {
                    // Если шаг отрицательный или нулевой, устанавливаем его по умолчанию
                    m_YGridStep = CalculateNiceNumber(m_MaxValue - m_MinValue) / 5f;
                }
            }
        }

        private void UpdateCurrentValueLabel()
        {
            // Форматируем текущее значение
            string formattedValue;
            if (Mathf.Abs(m_CurrentValue) < 0.00001f)
            {
                formattedValue = "<0.00001";
            }
            else if (Mathf.Abs(m_CurrentValue) > 9999999f)
            {
                formattedValue = ">9999999";
            }
            else
            {
                formattedValue = m_CurrentValue.ToString("F6"); // 6 знаков после запятой
            }

            m_CurrentValueLabel.text = formattedValue;

            // Обновляем размер шрифта на основе высоты графика
            var graphHeight = contentRect.height; // Высота графика в пикселях
            if (graphHeight <= 0)
            {
                graphHeight = 100; // Значение по умолчанию, если высота еще не определена
            }
            m_CurrentValueLabel.style.fontSize = graphHeight * 0.35f; // 35% от высоты графика
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateCurrentValueLabel();
        }

        private float CalculateNiceNumber(float range)
        {
            // Функция для вычисления "красивого" числа для шага сетки
            float exponent = Mathf.Floor(Mathf.Log10(range)); // Экспонента диапазона
            float fraction = range / Mathf.Pow(10, exponent); // Дробная часть диапазона

            float niceFraction;
            if (fraction <= 1)
                niceFraction = 1;
            else if (fraction <= 2)
                niceFraction = 2;
            else if (fraction <= 5)
                niceFraction = 5;
            else
                niceFraction = 10;

            return niceFraction * Mathf.Pow(10, exponent);
        }

        private void DrawGrid(MeshGenerationContext ctx, Rect rect)
        {
            var painter = ctx.painter2D;

            painter.strokeColor = m_GridColor;
            painter.lineWidth = 1f;

            // Учитываем внутренние отступы
            float verticalPadding = m_LineWidth / 2f;
            Rect graphRect = new Rect(rect.xMin, rect.yMin + verticalPadding, rect.width, rect.height - m_LineWidth);

            // Рисуем вертикальные линии (по времени)
            if (m_XGridStep > 0)
            {
                float timeStart = m_Points[^1].Time - m_Duration;
                float timeEnd = m_Points[^1].Time;

                // Находим первое кратное m_XGridStep время, большее или равное timeStart
                float firstTimeMark = Mathf.Ceil(timeStart / m_XGridStep) * m_XGridStep;

                for (float t = firstTimeMark; t <= timeEnd; t += m_XGridStep)
                {
                    float normalizedTime = Mathf.InverseLerp(timeStart, timeEnd, t);
                    float x = Mathf.Lerp(graphRect.xMin, graphRect.xMax, normalizedTime);

                    painter.BeginPath();
                    painter.MoveTo(new Vector2(x, graphRect.yMin));
                    painter.LineTo(new Vector2(x, graphRect.yMax));
                    painter.Stroke();
                }
            }

            // Рисуем горизонтальные линии (по значению)
            if (m_YGridStep > 0)
            {
                // Начинаем от первого значения, кратного m_YGridStep и меньше m_MaxValue
                float firstValueMark = Mathf.Ceil(m_MinValue / m_YGridStep) * m_YGridStep;

                for (float value = firstValueMark; value <= m_MaxValue; value += m_YGridStep)
                {
                    float normalizedValue = Mathf.InverseLerp(m_MinValue, m_MaxValue, value);
                    float y = Mathf.Lerp(graphRect.yMax, graphRect.yMin, normalizedValue);

                    painter.BeginPath();
                    painter.MoveTo(new Vector2(graphRect.xMin, y));
                    painter.LineTo(new Vector2(graphRect.xMax, y));
                    painter.Stroke();
                }
            }
            else
            {
                // Рисуем только горизонтальную линию на нуле
                if (m_MinValue <= 0 && m_MaxValue >= 0)
                {
                    float normalizedValue = Mathf.InverseLerp(m_MinValue, m_MaxValue, 0f);
                    float y = Mathf.Lerp(graphRect.yMax, graphRect.yMin, normalizedValue);

                    painter.BeginPath();
                    painter.MoveTo(new Vector2(graphRect.xMin, y));
                    painter.LineTo(new Vector2(graphRect.xMax, y));
                    painter.Stroke();
                }
            }
        }

        private void DrawGraph(MeshGenerationContext ctx)
        {
            UpdateCurrentValueLabel();

            var painter = ctx.painter2D;
            if (painter == null || m_Points.Count < 2) return;

            var rect = contentRect;

            // Учитываем внутренние отступы
            float verticalPadding = m_LineWidth / 2f;
            Rect graphRect = new Rect(rect.xMin, rect.yMin + verticalPadding, rect.width, rect.height - m_LineWidth);

            // Рисуем фон
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.xMin, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMax));
            painter.LineTo(new Vector2(rect.xMin, rect.yMax));
            painter.ClosePath();
            painter.fillColor = m_DefaultBackgroundColor;
            painter.Fill();

            // Рисуем сетку
            DrawGrid(ctx, rect);

            // Рисуем график
            float timeStart = m_Points[^1].Time - m_Duration;
            float timeEnd = m_Points[^1].Time;

            painter.lineWidth = m_LineWidth;
            painter.strokeColor = m_DefaultGraphColor;
            painter.BeginPath();
            bool isPathOutOfBound = false;

            for (int i = 0; i < m_Points.Count; i++)
            {
                var point = m_Points[i];
                float normalizedTime = Mathf.InverseLerp(timeStart, timeEnd, point.Time);
                float x = Mathf.Lerp(graphRect.xMin, graphRect.xMax, normalizedTime);

                bool isOutOfBounds = point.Value < m_MinValue || point.Value > m_MaxValue;
                float clampedValue = Mathf.Clamp(point.Value, m_MinValue, m_MaxValue); // Ограничиваем значение
                float normalizedValue = Mathf.InverseLerp(m_MinValue, m_MaxValue, clampedValue);
                float y = Mathf.Lerp(graphRect.yMax, graphRect.yMin, normalizedValue);
                var coords = new Vector2(x, y);

                if (i == 0)
                {
                    painter.MoveTo(coords);
                }
                else
                {
                    painter.LineTo(coords);
                }

                if (isOutOfBounds)
                {
                    if (!isPathOutOfBound)
                    {
                        painter.Stroke();
                        isPathOutOfBound = true;
                        painter.strokeColor = Color.red;
                        painter.BeginPath();
                        painter.MoveTo(coords);
                    }
                }
                else
                {
                    if (isPathOutOfBound)
                    {
                        painter.Stroke();
                        isPathOutOfBound = false;
                        painter.strokeColor = m_DefaultGraphColor;
                        painter.BeginPath();
                        painter.MoveTo(coords);
                    }
                }
            }

            painter.Stroke();
        }
    }
}
