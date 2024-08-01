namespace Metamatic
{
    using System;
    using System.Collections.Generic;

    public class WeakList<T> where T : class
    {
        private readonly List<WeakReference<T>> m_List = new List<WeakReference<T>>();

        public void Add(T item)
        {
            m_List.Add(new WeakReference<T>(item));
        }

        public void Remove(T item)
        {
            m_List.RemoveAll(wr => {
                if (wr.TryGetTarget(out var target))
                {
                    return target == item;
                }
                return false;
            });
        }

        public IEnumerable<T> GetAliveItems()
        {
            foreach (var weakReference in m_List)
            {
                if (weakReference.TryGetTarget(out var target))
                {
                    yield return target;
                }
            }
        }
        
        public bool Contains(T item)
        {
            return m_List.Exists(wr => {
                if (wr.TryGetTarget(out var target))
                {
                    return target == item;
                }
                return false;
            });
        }

        public void CleanUp()
        {
            m_List.RemoveAll(wr => !wr.TryGetTarget(out _));
        }
    }
}