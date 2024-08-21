using System;
using UnityEngine;

namespace Metamatic
{
    public class AbortableBehaviour: MonoBehaviour, IAbortable
    {
        public bool isAborted { get; private set; }
        public event Action OnAborted;
        public void Abort()
        {
            if (isAborted) return;
            isAborted = true;
            OnAborted?.Invoke();
        }

        private void OnDestroy()
        {
            Abort();
        }
    }
}