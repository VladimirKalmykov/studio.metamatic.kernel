namespace Metamatic {
    using System;
    using UnityEngine;

    /// <summary>
    /// This class will be added to every game object from the object pool
    /// </summary>
    [DisallowMultipleComponent]
    public class GameObjectFromPool: MonoBehaviour {
        public event Action OnReusableDestroy;
        public void ReusableDestroy() {
            OnReusableDestroy?.Invoke();
            this.gameObject.SetActive(false);
        }
    }
}