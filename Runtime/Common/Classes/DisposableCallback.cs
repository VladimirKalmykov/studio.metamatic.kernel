using System;

namespace Metamatic
{
    [Obsolete("Use DisposableAction instead")]
    public readonly struct DisposableCallback: IDisposable
    {
        private readonly Action OnDispose;
        public DisposableCallback(Action OnDispose)
        {
            this.OnDispose = OnDispose;
        }

        public void Dispose()
        {
            OnDispose.Invoke();
        }
    }
}