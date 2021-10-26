namespace Metamatic {
    using UnityEngine;
    using System.Collections.Generic;
    
    public abstract class CollectionAsset<T>: ScriptableObject
        where T: UnityEngine.Object
     {
        public List<T> items = new List<T>();
    }
}