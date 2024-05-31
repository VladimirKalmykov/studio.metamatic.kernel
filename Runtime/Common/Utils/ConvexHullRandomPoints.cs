using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Metamatic
{
    public class ConvexHullRandomPoints
    {
        public static List<(Vector2, Vector2)> GenerateMatrixPointsInConvexHull(Vector2[] hull, Vector2 gridSize, Vector3[] deformedVertices, Vector2[] uvArray)
        {
            List<(Vector2, Vector2)> points = new List<(Vector2, Vector2)>();

            // Находим крайние точки выпуклой оболочки
            float minX = hull[0].x;
            float maxX = hull[0].x;
            float minY = hull[0].y;
            float maxY = hull[0].y;

            foreach (var point in hull)
            {
                if (point.x < minX) minX = point.x;
                if (point.x > maxX) maxX = point.x;
                if (point.y < minY) minY = point.y;
                if (point.y > maxY) maxY = point.y;
            }

            // Вычисляем размеры сетки
            float stepX = (maxX - minX) / gridSize.x;
            float stepY = (maxY - minY) / gridSize.y;

            // Генерируем точки внутри выпуклой оболочки
            for (float x = minX; x <= maxX; x += stepX)
            {
                for (float y = minY; y <= maxY; y += stepY)
                {
                    Vector2 point = new Vector2(x, y);
                    if (IsPointInPolygon(hull, point))
                    {
                        Vector2 uv = CalculateUV(point, hull, deformedVertices, uvArray);
                        points.Add((point, uv));
                    }
                }
            }

            return points;
        }

        private static bool IsPointInPolygon(Vector2[] polygon, Vector2 point)
        {
            int polygonLength = polygon.Length, i = 0;
            bool inside = false;
            float pointX = point.x, pointY = point.y;
            float startX, startY, endX, endY;
            Vector2 endPoint = polygon[polygonLength - 1];
            endX = endPoint.x;
            endY = endPoint.y;

            while (i < polygonLength)
            {
                startX = endX;
                startY = endY;
                endPoint = polygon[i++];
                endX = endPoint.x;
                endY = endPoint.y;
                inside ^= (endY > pointY ^ startY > pointY) && ((pointX - endX) < (pointY - endY) * (startX - endX) / (startY - endY));
            }

            return inside;
        }

        public static List<(Vector2, Vector2)> GenerateRandomPointsInConvexHull(Vector2[] hull, int numPoints, Vector3[] deformedVertices, Vector2[] uvArray)
        {
            List<(Vector2, Vector2)> points = new List<(Vector2, Vector2)>();

            // Используем триангулятор для получения треугольников
            Metamatic.Triangulator triangulator = new Metamatic.Triangulator(hull);
            int[] indices = triangulator.Triangulate();

            List<Vector2[]> triangles = new List<Vector2[]>();
            for (int i = 0; i < indices.Length; i += 3)
            {
                Vector2[] triangle = new Vector2[3];
                triangle[0] = hull[indices[i]];
                triangle[1] = hull[indices[i + 1]];
                triangle[2] = hull[indices[i + 2]];
                triangles.Add(triangle);
            }

            List<float> areas = new List<float>();
            float totalArea = 0f;

            // Вычисляем площадь каждого треугольника и общий вес
            foreach (var triangle in triangles)
            {
                float area = CalculateTriangleArea(triangle);
                areas.Add(area);
                totalArea += area;
            }

            // Генерируем точки, распределенные по площади треугольников
            for (int i = 0; i < numPoints; i++)
            {
                float randomWeight = Random.Range(0f, totalArea);
                float cumulativeWeight = 0f;
                Vector2[] selectedTriangle = null;

                for (int j = 0; j < triangles.Count; j++)
                {
                    cumulativeWeight += areas[j];
                    if (cumulativeWeight >= randomWeight)
                    {
                        selectedTriangle = triangles[j];
                        break;
                    }
                }

                if (selectedTriangle != null)
                {
                    Vector2 randomPoint = GenerateRandomPointInTriangle(selectedTriangle);
                    Vector2 uv = CalculateUV(randomPoint, selectedTriangle, deformedVertices, uvArray);
                    points.Add((randomPoint, uv));
                }
            }

            return points;
        }

        public static List<Vector2> GenerateRandomPointsInConvexHull(Vector2[] hull, int numPoints)
        {
            List<Vector2> points = new List<Vector2>();

            // Используем триангулятор для получения треугольников
            Metamatic.Triangulator triangulator = new Metamatic.Triangulator(hull);
            int[] indices = triangulator.Triangulate();

            List<Vector2[]> triangles = new List<Vector2[]>();
            for (int i = 0; i < indices.Length; i += 3)
            {
                Vector2[] triangle = new Vector2[3];
                triangle[0] = hull[indices[i]];
                triangle[1] = hull[indices[i + 1]];
                triangle[2] = hull[indices[i + 2]];
                triangles.Add(triangle);
            }

            List<float> areas = new List<float>();
            float totalArea = 0f;

            // Вычисляем площадь каждого треугольника и общий вес
            foreach (var triangle in triangles)
            {
                float area = CalculateTriangleArea(triangle);
                areas.Add(area);
                totalArea += area;
            }

            // Генерируем точки, распределенные по площади треугольников
            for (int i = 0; i < numPoints; i++)
            {
                float randomWeight = Random.Range(0f, totalArea);
                float cumulativeWeight = 0f;
                Vector2[] selectedTriangle = null;

                for (int j = 0; j < triangles.Count; j++)
                {
                    cumulativeWeight += areas[j];
                    if (cumulativeWeight >= randomWeight)
                    {
                        selectedTriangle = triangles[j];
                        break;
                    }
                }

                if (selectedTriangle != null)
                {
                    Vector2 randomPoint = GenerateRandomPointInTriangle(selectedTriangle);
                    points.Add(randomPoint);
                }
            }

            return points;
        }

        private static Vector2 CalculateUV(Vector2 point, Vector2[] triangle, Vector3[] deformedVertices, Vector2[] uvArray)
        {
            Vector2 a = triangle[0];
            Vector2 b = triangle[1];
            Vector2 c = triangle[2];

            int indexA = FindVertexIndex(a, deformedVertices);
            int indexB = FindVertexIndex(b, deformedVertices);
            int indexC = FindVertexIndex(c, deformedVertices);

            Vector2 uvA = uvArray[indexA];
            Vector2 uvB = uvArray[indexB];
            Vector2 uvC = uvArray[indexC];

            float denominator = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
            float u = ((b.y - c.y) * (point.x - c.x) + (c.x - b.x) * (point.y - c.y)) / denominator;
            float v = ((c.y - a.y) * (point.x - c.x) + (a.x - c.x) * (point.y - c.y)) / denominator;
            float w = 1 - u - v;

            return u * uvA + v * uvB + w * uvC;
        }

        private static int FindVertexIndex(Vector2 vertex, Vector3[] deformedVertices)
        {
            for (int i = 0; i < deformedVertices.Length; i++)
            {
                if (Vector2.Distance(vertex, new Vector2(deformedVertices[i].x, deformedVertices[i].y)) < 0.001f)
                {
                    return i;
                }
            }
            return -1; // or throw an error if appropriate
        }

        private static float CalculateTriangleArea(Vector2[] triangle)
        {
            Vector2 a = triangle[0];
            Vector2 b = triangle[1];
            Vector2 c = triangle[2];
            return Mathf.Abs(a.x * (b.y - c.y) + b.x * (c.y - a.y) + c.x * (a.y - b.y)) / 2f;
        }

        private static Vector2 GenerateRandomPointInTriangle(Vector2[] triangle)
        {
            Vector2 a = triangle[0];
            Vector2 b = triangle[1];
            Vector2 c = triangle[2];

            float r1 = Mathf.Sqrt(Random.Range(0f, 1f));
            float r2 = Random.Range(0f, 1f);

            float x = (1 - r1) * a.x + r1 * (1 - r2) * b.x + r1 * r2 * c.x;
            float y = (1 - r1) * a.y + r1 * (1 - r2) * b.y + r1 * r2 * c.y;

            return new Vector2(x, y);
        }
    }
}
