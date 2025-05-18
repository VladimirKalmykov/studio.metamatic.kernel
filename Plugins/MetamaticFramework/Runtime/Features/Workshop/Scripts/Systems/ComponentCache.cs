using System.Collections.Generic;
using UnityEngine;

namespace Metamatic.Framework
{
    /// <summary>
    /// Простейший кэш компонентов, чтобы не вызывать <see cref="Component.GetComponent"/> по нескольку раз.
    /// Ключ – <see cref="Object.GetInstanceID"/> префаба в пуле.
    /// <para/>
    /// Слабых ссылок Unity не предоставляет, поэтому по‑простому храним до тех пор,
    /// пока жив сам GameObject (его <see cref="instanceID"/> больше не встречается в коде после Destroy).
    /// </summary>
    /// <typeparam name="T">Тип компонента</typeparam>
    public static class ComponentCache<T> where T : Component
    {
        private static readonly Dictionary<int, T> _cache = new();

        /// <summary>
        /// Вернёт компонент <typeparamref name="T"/> для данного <paramref name="go"/>.
        /// При первом обращении выполнит <c>go.GetComponent&lt;T&gt;()</c> и сохранит результат.
        /// </summary>
        public static T GetOrAdd(GameObject go)
        {
            #if DEBUG
            Debug.Assert(go != null, "ComponentCache: go is null");
            #else
            if (go == null) return null;
            #endif

            int id = go.GetInstanceID();
            if (!_cache.TryGetValue(id, out var comp) || comp == null)
            {
                comp = go.GetComponent<T>();
                _cache[id] = comp;
            }
            return comp;
        }

        /// <summary>
        /// Вызывать после <c>Object.Destroy(go)</c>, чтобы не держать лишние ссылки.
        /// </summary>
        public static void Remove(GameObject go)
        {
            if (go == null) return;
            _cache.Remove(go.GetInstanceID());
        }
    }
}