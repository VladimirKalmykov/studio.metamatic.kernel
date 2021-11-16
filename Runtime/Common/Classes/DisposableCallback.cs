using System;

namespace Metamatic
{
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