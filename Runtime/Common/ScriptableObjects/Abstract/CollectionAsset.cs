namespace Metamatic {
    using UnityEngine;
    using System.Collections.Generic;
    
    public abstract class CollectionAsset<T>: ScriptableObject
    {
        [SerializeField] private List<T> items = new List<T>();

        public T[] GetItems()
        {
            return items.ToArray();
        }
    }
}