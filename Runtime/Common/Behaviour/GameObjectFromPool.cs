namespace Metamatic {
    using System;
    using UnityEngine;

    /// <summary>
    /// This class will be added to every game object from the object pool
    /// TODO: Decide which class can be used instead of this class, because this class is not used in the project.
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