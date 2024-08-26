using System;

namespace Metamatic {
    using UnityEngine;
    using UnityEngine.Events;
    
    [Obsolete("Use Dispatcher events instead", true)]
    public class WatchGameObjectRegistryTransform: MonoBehaviour {
        [System.Serializable]
        public class TransformEvent: UnityEvent<Transform> {}
        public GameObjectRegistry activePlayer;
        public TransformEvent OnChange = new TransformEvent();
        void OnEnable() {
            HandleChange(activePlayer.first);
            activePlayer.SubscribeFirst(HandleChange);
        }

        void OnDisable() {
            HandleChange(null);
            activePlayer.UnsubscribeFirst(HandleChange);
        }

        void HandleChange(GameObject playerObject) {
            OnChange.Invoke(playerObject?.transform ?? null);
        }
    }
}