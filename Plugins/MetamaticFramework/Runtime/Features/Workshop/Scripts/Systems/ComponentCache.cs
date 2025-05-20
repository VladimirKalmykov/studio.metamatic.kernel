using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Metamatic.Framework
{
    /// <summary>
    /// Кеш компонентов с использованием слабых ссылок.
    /// </summary>
    [Obsolete("Redundant")]
    public static class WeakComponentCache<T> where T : Component
    {
        // Ключ — InstanceID GameObject, значение — слабая ссылка на компонент T
        private static readonly Dictionary<int, WeakReference<T>> _cache = new();

        /// <summary>
        /// Получит или добавит компонент T для данного GameObject.
        /// Если слабая ссылка "мертва" (объект освобождён GC или Destroyed),
        /// то создаст новую через go.GetComponent&lt;T&gt;().
        /// </summary>
        public static T GetOrAdd(GameObject go)
        {
            if (go == null) return null;

            int id = go.GetInstanceID();

            if (_cache.TryGetValue(id, out var weakRef))
            {
                // 1) Попытка получить цель из WeakReference
                if (weakRef.TryGetTarget(out T existing) && existing != null)
                {
                    // UnityEngine.Object.operator== вернёт false, если он Destroyed
                    return existing;
                }
                // иначе — старая ссылка "мертва"
                _cache.Remove(id);
            }

            // создаём и сохраняем новую ссылку
            T comp = go.GetComponent<T>();
            _cache[id] = new WeakReference<T>(comp);
            return comp;
        }

        /// <summary>
        /// Ручная очистка всех "мертвых" записей из кеша.
        /// Можно вызывать периодически, например,
        /// после N обращений или по таймеру.
        /// </summary>
        public static void Cleanup()
        {
            var deadKeys = _cache
                .Where(pair =>
                {
                    // если слабая ссылка больше не держит объект
                    if (!pair.Value.TryGetTarget(out T target)) return true;
                    // либо Unity-объект Destroyed
                    return target == null;
                })
                .Select(pair => pair.Key)
                .ToList();

            foreach (int key in deadKeys)
                _cache.Remove(key);
        }
    }
}
