namespace Metamatic {
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using UnityEngine;

    public interface ISet<T> {
        Type type {
            get;
        }
        int Count {
            get;
        }
        T GetRandom();
    }

    public class Set<T>: ISet<T> {
        public Type type {
            get;
        } = typeof(T);

        public List<T> items = new List<T>();

        public int Count {
            get {
                return items.Count;
            }
        }

        public Set(T[] items): this() {
            this.items = items.ToList();
        }

        public Set(List<T> items): this() {
            this.items = items;
        }

        public Set() {}

        public T GetRandom() {
            return items.Count == 0
                ? default(T)
                : items[UnityEngine.Random.Range(0, items.Count - 1)];
        }
    }
}