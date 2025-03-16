using System;
using UnityEngine;

namespace Metamatic
{
    public class RecyclableBehaviour: DisposableBehaviour
    {
        public static void Banish(GameObject gameObject)
        {
            if (gameObject.TryGetComponent<RecyclableBehaviour>(out var recyclableBehaviour))
            {
                recyclableBehaviour.Recycle();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        public event Action OnRecycle;
        public void Recycle()
        {
            OnRecycle?.Invoke();
            OnRecycle = null;
            Abort();
        }

        private void OnDestroy()
        {
            Abort();
        }
    }
}