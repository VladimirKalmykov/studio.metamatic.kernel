using System.Linq;
using UnityEngine.U2D.Animation;

namespace Metamatic
{
    using UnityEngine;

    public static class UVHelper
    {
        public static Vector2 GetUVFromWorldPosition(Vector3 point, SpriteSkin spriteSkin, SpriteRenderer spriteRenderer)
        {
            // Получаем деформированные вершины и их UV-координаты
            Vector3[] deformedVertices = spriteSkin.GetDeformedVertexPositionData().ToArray();
            Vector2[] uvArray = spriteRenderer.sprite.uv;
            
            // Преобразуем мировые координаты в локальные
            Vector3 localPoint = spriteRenderer.transform.InverseTransformPoint(point);

            // Найдем треугольник, в который попадает точка
            for (int i = 0; i < deformedVertices.Length; i += 3)
            {
                Vector3 v0 = deformedVertices[i];
                Vector3 v1 = deformedVertices[i + 1];
                Vector3 v2 = deformedVertices[i + 2];

                if (IsPointInTriangle(localPoint, v0, v1, v2))
                {
                    // Используем барицентрические координаты для вычисления UV
                    Vector2 uv0 = uvArray[i];
                    Vector2 uv1 = uvArray[i + 1];
                    Vector2 uv2 = uvArray[i + 2];

                    Vector2 uv = GetBarycentricCoordinates(localPoint, v0, v1, v2, uv0, uv1, uv2);
                    return uv;
                }
            }

            // Если не найден треугольник, возвращаем UV (0, 0)
            return Vector2.zero;
        }

        private static bool IsPointInTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            // Проверка, находится ли точка в треугольнике, используя метод пересечения векторов
            Vector3 v0 = c - a;
            Vector3 v1 = b - a;
            Vector3 v2 = p - a;

            float dot00 = Vector3.Dot(v0, v0);
            float dot01 = Vector3.Dot(v0, v1);
            float dot02 = Vector3.Dot(v0, v2);
            float dot11 = Vector3.Dot(v1, v1);
            float dot12 = Vector3.Dot(v1, v2);

            float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
            float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
            float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

            return (u >= 0) && (v >= 0) && (u + v < 1);
        }

        private static Vector2 GetBarycentricCoordinates(Vector3 p, Vector3 a, Vector3 b, Vector3 c, Vector2 uv0, Vector2 uv1, Vector2 uv2)
        {
            // Вычисляем барицентрические координаты
            Vector3 v0 = b - a;
            Vector3 v1 = c - a;
            Vector3 v2 = p - a;

            float d00 = Vector3.Dot(v0, v0);
            float d01 = Vector3.Dot(v0, v1);
            float d11 = Vector3.Dot(v1, v1);
            float d20 = Vector3.Dot(v2, v0);
            float d21 = Vector3.Dot(v2, v1);

            float denom = d00 * d11 - d01 * d01;
            float v = (d11 * d20 - d01 * d21) / denom;
            float w = (d00 * d21 - d01 * d20) / denom;
            float u = 1.0f - v - w;

            // Используем барицентрические координаты для интерполяции UV
            return u * uv0 + v * uv1 + w * uv2;
        }
    }
}