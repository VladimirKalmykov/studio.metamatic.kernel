using UnityEngine;

namespace Metamatic.DebugOnly
{
    public static class Utils
    {
        #if DEBUG
        public static Color DebugGetRandomSaturatedColor()
        {
            // Генерация цвета с одним высоким (близким к 1) и двумя более низкими значениями RGB
            float high = Random.Range(0.7f, 1f); // Высокое значение для одного из каналов
            float mid = Random.Range(0.4f, 0.7f); // Среднее значение для другого канала
            float low = Random.Range(0f, 0.3f);   // Низкое значение для третьего канала

            // Случайный выбор, какие каналы будут высокими, средними и низкими
            switch (Random.Range(0, 6))
            {
                case 0: return new Color(high, mid, low, 1f);
                case 1: return new Color(high, low, mid, 1f);
                case 2: return new Color(mid, high, low, 1f);
                case 3: return new Color(mid, low, high, 1f);
                case 4: return new Color(low, high, mid, 1f);
                case 5: return new Color(low, mid, high, 1f);
                default: return new Color(high, mid, low, 1f); // Защита от неверного значения
            }
        }
        #endif
    }
}