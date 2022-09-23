Получение цвета текстуры по координатам
--

Для начала у файла изображения необходимо включить режим чтения/записи. Алгоритм позволяет получить цвет на спрайте по мировым координатам.

```c#
// Точки в мировом пространстве
var point = new Vector2(0.5, 0.3);
// Получение массива пикселей тектуры
var pixels = sprite.texture.GetPixels();
// Получение координат текстуры в спрайте
var textureRect = sprite.textureRect;
// Получение ширины и высоты текстуры
var (width, height) = ((int)sprite.texture.width, (int)sprite.texture.height);
// Получение информации об ограничительном прямоугольнике изображения
var bounds = sprite.bounds;
// Получение координаты точки в локальном пространстве в относительном представлении
var xRatio = ((point.x - bounds.min.x) / (bounds.max.x - bounds.min.x));
var yRatio = ((point.y - bounds.min.y) / (bounds.max.y - bounds.min.y));
// Получение локальных координат на текстуре
var x = (int)(xRatio * textureRect.width) + textureRect.x;
var y = (int)(yRatio * textureRect.height) + textureRect.y;
// Получение информации о пикселе
var pixelIndex = (int)(y * width + x);
var color = pixels[pixelIndex];
```

### Недостаток алгоритма

- Не учитывает поворот изображения