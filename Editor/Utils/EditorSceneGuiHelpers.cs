using Metamatic.Framework;
using UnityEditor;
using UnityEngine;

namespace Metamatic
{
    public static class EditorSceneGuiHelpers
    {
        public enum GridAppearance
        {
            Lines,
            Dots
        }

        public static void DrawGrid2D(
            Vector3 originPosition,
            Grid2DSettings settings
            )
        {
            var position = originPosition + settings.offset;
            var size = settings.gridSize;
            var cellSize = settings.cellSize;
            var rotation = settings.rotation;
            var color = settings.color;
            var appearance = settings.appearance;
            var weight = settings.weight;

            // Защита от слишком мелких ячеек, чтобы не повесить редактор
            var normalizedCellSize = new Vector2(
                Mathf.Max(cellSize.x, 0.0001f),
                Mathf.Max(cellSize.y, 0.0001f)
            );

            // Сколько максимум ячеек допустим (чтобы не отрисовать тысячи линий)
            float maxCellsX = size.x / normalizedCellSize.x;
            float maxCellsY = size.y / normalizedCellSize.y;
            if (maxCellsX * maxCellsY > 10000) 
            {
                Debug.LogWarning("Grid size is too large or cellSize is too small, skipping drawing.");
                return;
            }

            // Цвет
            Handles.color = color;

            // -----------------------------
            // 1) Подготовим локальные границы от -size/2 до +size/2
            // -----------------------------
            float halfW = size.x * 0.5f;
            float halfH = size.y * 0.5f;

            // В локальных координатах центр = (0,0).
            // Точки / линии хотим рисовать кратно cellSize.
            // Для этого округлим границы к ближайшим делениям сетки
            float minX = Mathf.Floor(-halfW / normalizedCellSize.x) * normalizedCellSize.x;
            float maxX = Mathf.Ceil( halfW / normalizedCellSize.x) * normalizedCellSize.x;
            float minY = Mathf.Floor(-halfH / normalizedCellSize.y) * normalizedCellSize.y;
            float maxY = Mathf.Ceil( halfH / normalizedCellSize.y) * normalizedCellSize.y;

            // -----------------------------
            // 2) Матрица, переносящая (0,0) локального пространства в (position) + поворот
            // -----------------------------
            Matrix4x4 gridMatrix = Matrix4x4.TRS(
                new Vector3(position.x, position.y, 0),
                Quaternion.Euler(rotation),
                Vector3.one
            );

            // -----------------------------
            // 3) Рисуем либо линии, либо точки
            // -----------------------------
            if (appearance == Grid2DSettings.AppearanceType.Lines)
            {
                // Вертикальные пунктирные линии
                for (float x = minX; x <= maxX; x += normalizedCellSize.x)
                {
                    Vector3 p1 = gridMatrix.MultiplyPoint3x4(new Vector3(x, minY, 0f));
                    Vector3 p2 = gridMatrix.MultiplyPoint3x4(new Vector3(x, maxY, 0f));
                    Handles.DrawLine(p1, p2, weight);
                }

                // Горизонтальные пунктирные линии
                for (float y = minY; y <= maxY; y += normalizedCellSize.y)
                {
                    Vector3 p1 = gridMatrix.MultiplyPoint3x4(new Vector3(minX, y, 0f));
                    Vector3 p2 = gridMatrix.MultiplyPoint3x4(new Vector3(maxX, y, 0f));
                    Handles.DrawLine(p1, p2, weight);
                }
            }
            else if (appearance == Grid2DSettings.AppearanceType.DottedLines)
            {
                var screenSpaceSize = settings.screenSpaceSize == 0 ? 2f : settings.screenSpaceSize;
                // Вертикальные пунктирные линии
                for (float x = minX; x <= maxX; x += normalizedCellSize.x)
                {
                    Vector3 p1 = gridMatrix.MultiplyPoint3x4(new Vector3(x, minY, 0f));
                    Vector3 p2 = gridMatrix.MultiplyPoint3x4(new Vector3(x, maxY, 0f));
                    Handles.DrawDottedLine(p1, p2, /*screenSpaceSize=*/screenSpaceSize);
                }

                // Горизонтальные пунктирные линии
                for (float y = minY; y <= maxY; y += normalizedCellSize.y)
                {
                    Vector3 p1 = gridMatrix.MultiplyPoint3x4(new Vector3(minX, y, 0f));
                    Vector3 p2 = gridMatrix.MultiplyPoint3x4(new Vector3(maxX, y, 0f));
                    Handles.DrawDottedLine(p1, p2, /*screenSpaceSize=*/screenSpaceSize);
                }
            }
            else // GridAppearance.Dots
            {
                // Двойной цикл по точкам пересечения
                for (float x = minX; x <= maxX; x += normalizedCellSize.x)
                {
                    for (float y = minY; y <= maxY; y += normalizedCellSize.y)
                    {
                        Vector3 localPos = new Vector3(x, y, 0f);
                        Vector3 dotPosition = gridMatrix.MultiplyPoint3x4(localPos);

                        // dotSize=0 -> условный "1 пиксель", но ниже возьмём 0.1f для наглядности
                        float pixelSize = (weight == 0) ? 0.1f : weight;

                        // Конвертируем "в пиксели" через HandleUtility.GetHandleSize
                        // Коэффициент 0.1f можно варьировать, чтобы подобрать нужный масштаб
                        float worldDotSize = pixelSize * HandleUtility.GetHandleSize(dotPosition) * 0.1f;

                        Handles.DrawSolidDisc(dotPosition, Vector3.forward, worldDotSize);
                    }
                }
            }
        }
    }
}
