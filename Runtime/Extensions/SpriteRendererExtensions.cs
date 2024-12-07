using System.Collections.Generic;
using UnityEngine;

namespace Metamatic
{
    public static class SpriteRendererExtensions
    {
        /// <summary>
        /// Get color palette from sprite renderer. It requires image to be read/write enabled.
        /// </summary>
        /// <param name="spriteRenderer"></param>
        /// <param name="cellSize">Increase cell size to make it less expensive (1 - best for quality, greater than 1 - for best performance)</param>
        /// <returns></returns>
        public static ColorPalette ExtractColorPalette(this SpriteRenderer spriteRenderer, float cellSize = 1f)
        {
            var sprite = spriteRenderer.sprite;
            Texture2D texture = sprite.texture;
            Rect rect = sprite.textureRect;

            HashSet<Color> uniqueColors = new HashSet<Color>();

            // Получаем размеры текстуры
            int width = (int)rect.width;
            int height = (int)rect.height;

            // Пробегаемся по всей текстуре пиксель за пикселем
            for (float x = 0; x < width; x += cellSize)
            {
                for (float y = 0; y < height; y += cellSize)
                {
                    // Преобразуем координаты в пиксельные координаты текстуры
                    int pixelX = (int)(rect.x + x);
                    int pixelY = (int)(rect.y + y);

                    // Получаем цвет пикселя
                    Color color = texture.GetPixel(pixelX, pixelY);

                    // Добавляем цвет в HashSet
                    uniqueColors.Add(color);
                }
            }

            return new ColorPalette(uniqueColors);
        }
        
        /// <summary>
        /// Get color matrix from sprite renderer. It requires image to be read/write enabled.
        /// </summary>
        /// <param name="spriteRenderer"></param>
        /// <param name="cellSize"></param>
        /// <returns></returns>
        public static (Vector2 position, Color color)[] GetWorldColorMatrix(this SpriteRenderer spriteRenderer, float cellSize)
        {
            var sprite = spriteRenderer.sprite;
            Texture2D texture = sprite.texture;
            Rect rect = sprite.textureRect;

            List<(Vector2 position, Color color)> colorMatrix = new List<(Vector2 position, Color color)>();

            // Получаем размеры текстуры
            int width = (int)rect.width;
            int height = (int)rect.height;

            // Получаем pivot, чтобы учитывать его при расчете позиции
            Vector2 pivot = sprite.pivot;
            Vector2 pivotOffset = new Vector2(pivot.x / width, pivot.y / height);

            // Пробегаемся по текстуре сеткой с заданным размером ячеек
            for (float x = 0; x < width; x += cellSize)
            {
                for (float y = 0; y < height; y += cellSize)
                {
                    // Преобразуем координаты ячейки в пиксельные координаты
                    int pixelX = Mathf.FloorToInt(rect.x + x);
                    int pixelY = Mathf.FloorToInt(rect.y + y);

                    // Получаем цвет пикселя
                    Color color = texture.GetPixel(pixelX, pixelY);

                    // Пропускаем прозрачные пиксели
                    if (color.a == 0)
                        continue;

                    // Преобразуем координаты в локальные координаты спрайта
                    Vector2 localPos = new Vector2((x - pivot.x) / width * sprite.bounds.size.x, 
                        (y - pivot.y) / height * sprite.bounds.size.y);

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
        
        /// <summary>
        /// Получает матрицу цветов из SpriteRenderer. Требует, чтобы изображение было доступно для чтения/записи.
        /// </summary>
        /// <param name="spriteRenderer"></param>
        /// <param name="cellSize"></param>
        /// <returns></returns>
        public static (Vector2 position, Color color)[] GetLocalColorMatrix(this SpriteRenderer spriteRenderer, float cellSize)
        {
            var sprite = spriteRenderer.sprite;
            Texture2D texture = sprite.texture;
            Rect rect = sprite.textureRect;

            List<(Vector2 position, Color color)> colorMatrix = new List<(Vector2 position, Color color)>();

            int width = (int)rect.width;
            int height = (int)rect.height;

            Vector2 pivot = sprite.pivot;
            float pixelsPerUnit = sprite.pixelsPerUnit;

            // Вычисляем коэффициент преобразования из пикселей в единицы Unity
            float unitsPerPixel = 1.0f / pixelsPerUnit;

            // Пробегаемся по текстуре сеткой с заданным размером ячеек
            for (float x = 0; x < width; x += cellSize)
            {
                for (float y = 0; y < height; y += cellSize)
                {
                    int pixelX = Mathf.FloorToInt(rect.x + x);
                    int pixelY = Mathf.FloorToInt(rect.y + y);

                    Color color = texture.GetPixel(pixelX, pixelY);

                    if (color.a == 0)
                        continue;

                    // Преобразуем координаты в локальные координаты спрайта в единицах Unity
                    Vector2 localPos = new Vector2(
                        (x - pivot.x) * unitsPerPixel,
                        (y - pivot.y) * unitsPerPixel
                    );

                    // Добавляем локальную позицию и цвет в матрицу
                    colorMatrix.Add((localPos, color));
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