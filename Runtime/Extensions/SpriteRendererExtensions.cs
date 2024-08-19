using System.Collections.Generic;
using UnityEngine;

namespace Metamatic
{
    public static class SpriteRendererExtensions
    {
        /// <summary>
        /// Get color matrix from sprite renderer. It requires image to be read/write enabled.
        /// </summary>
        /// <param name="spriteRenderer"></param>
        /// <param name="cellSize"></param>
        /// <returns></returns>
        public static (Vector2 position, Color color)[] GetColorMatrix(this SpriteRenderer spriteRenderer, float cellSize)
        {
            var texture = ExtractTextureFromSprite(spriteRenderer);
            List<(Vector2 position, Color color)> colorMatrix = new List<(Vector2 position, Color color)>();

            // Получаем размеры текстуры
            int width = texture.width;
            int height = texture.height;

            // Получаем pivot, чтобы учитывать его при расчете позиции
            Vector2 pivot = spriteRenderer.sprite.pivot;
            Vector2 pivotOffset = new Vector2(pivot.x / width, pivot.y / height);

            // Пробегаемся по текстуре сеткой с заданным размером ячеек
            for (float x = 0; x < width; x += cellSize)
            {
                for (float y = 0; y < height; y += cellSize)
                {
                    // Преобразуем координаты ячейки в пиксельные координаты
                    int pixelX = Mathf.FloorToInt(x);
                    int pixelY = Mathf.FloorToInt(y);

                    // Получаем цвет пикселя
                    Color color = texture.GetPixel(pixelX, pixelY);

                    // Пропускаем прозрачные пиксели
                    if (color.a == 0)
                        continue;

                    // Преобразуем координаты в локальные координаты спрайта
                    Vector2 localPos = new Vector2((x - pivot.x) / width * spriteRenderer.sprite.bounds.size.x, 
                        (y - pivot.y) / height * spriteRenderer.sprite.bounds.size.y);

                    // Учитываем масштаб объекта
                    localPos = Vector2.Scale(localPos, spriteRenderer.transform.lossyScale);

                    // Преобразуем локальные координаты в мировое пространство
                    Vector2 worldPos = spriteRenderer.transform.TransformPoint(localPos);

                    // Добавляем позицию и цвет в матрицу
                    colorMatrix.Add((worldPos, color));
                }
            }

            return colorMatrix.ToArray();
        }
        
        public static Texture2D ExtractTextureFromSprite(this SpriteRenderer spriteRenderer)
        {
            var sprite = spriteRenderer.sprite;
            // Получаем текстуру и координаты текстуры из спрайта
            Texture2D texture = sprite.texture;
            Rect rect = sprite.textureRect;

            // Создаем новую текстуру с размерами соответствующими спрайту
            Texture2D newTexture = new Texture2D((int)rect.width, (int)rect.height);

            // Копируем пиксели из текстуры спрайта
            Color[] pixels = texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
            newTexture.SetPixels(pixels);
            newTexture.Apply();
            return newTexture;
        }
    }
}