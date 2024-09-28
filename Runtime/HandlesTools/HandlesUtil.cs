#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Metamatic
{
    public static class HandlesUtil
    {
        public static void DrawArrow(Vector3 start, Vector3 end, float arrowSize, Color color, float repeatArrow = 0f)
        {
            #if UNITY_EDITOR
            // Рисуем линию
            Handles.color = color;
            Handles.DrawLine(start, end);

            // Вычисление направления стрелки
            Vector3 direction = (end - start).normalized;

            // Вычисляем точки треугольной стрелки
            Vector3 right = Quaternion.LookRotation(Vector3.forward) * Quaternion.Euler(0, 0, 135) * direction * arrowSize;
            Vector3 left = Quaternion.LookRotation(Vector3.forward) * Quaternion.Euler(0, 0, -135) * direction * arrowSize;

            // Вершины треугольника (стрелки)
            Vector3[] arrowVertices = new Vector3[3]
            {
                end,            // Вершина треугольника (остриё стрелки)
                end + right,    // Правая точка треугольника
                end + left      // Левая точка треугольника
            };

            // Устанавливаем цвет и рисуем закрашенный треугольник
            Handles.color = color;
            Handles.DrawAAConvexPolygon(arrowVertices);

            if (repeatArrow > 0f)
            {
                // Draw arrows every repeatArrow units
                float distance = Vector3.Distance(start, end);
                int arrowsCount = Mathf.FloorToInt(distance / repeatArrow);
                for (int i = 1; i < arrowsCount; i++)
                {
                    // Triangle vertices
                    Vector3[] arrowVerticesRepeat = new Vector3[3]
                    {
                        Vector3.Lerp(start, end, i / (float)arrowsCount), // Triangle vertex (arrow tip)
                        Vector3.Lerp(start, end, i / (float)arrowsCount) + right, // Right point of the triangle
                        Vector3.Lerp(start, end, i / (float)arrowsCount) + left // Left point of the triangle
                    };
                    
                    // Set color and draw filled triangle
                    Handles.DrawAAConvexPolygon(arrowVerticesRepeat);
                }
            }
            
            #endif
        }
        
        public static void DrawFilledCircle(Vector3 position, float radius, Color color)
        {
            #if UNITY_EDITOR
            Handles.color = color;
            Handles.DrawSolidDisc(position, Vector3.forward, radius);
            #endif
        }
        
        public static void DrawWireCircle(Vector3 position, float radius, Color color)
        {
            #if UNITY_EDITOR
            Handles.color = color;
            Handles.DrawWireDisc(position, Vector3.forward, radius);
            #endif
        }
    }
}