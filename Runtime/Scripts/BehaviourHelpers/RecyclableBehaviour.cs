using System;

namespace Metamatic
{
    public class RecyclableBehaviour: DisposableBehaviour
    {
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