namespace Metamatic {
    using System;
    using UnityEngine;

    [DisallowMultipleComponent]
    /// <summary>
    /// This class will be added to every game object from the object pool
    /// </summary>
    public class GameObjectFromPool: MonoBehaviour {
        public event Action OnReusableDestroy;
        public void ReusableDestroy() {
            OnReusableDestroy?.Invoke();
            this.gameObject.SetActive(false);
        }
    }
}