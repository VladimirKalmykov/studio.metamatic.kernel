using System;
using System.Collections.Generic;

namespace Metamatic
{
    public readonly struct ComposableDisposable: IDisposable
    {
        private readonly IEnumerable<IDisposable> disposables;
        public ComposableDisposable(IEnumerable<IDisposable> disposables)
        {
            this.disposables = disposables;
        }
        
        public ComposableDisposable(params IDisposable[] disposables)
        {
            this.disposables = disposables;
        }

        public void Dispose()
        {
            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }
        }
    }
}