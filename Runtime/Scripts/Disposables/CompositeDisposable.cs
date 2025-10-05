using System;
using System.Collections.Generic;

namespace Metamatic
{
    public readonly struct CompositeDisposable: IDisposable
    {
        private readonly IEnumerable<IDisposable> disposables;
        public CompositeDisposable(IEnumerable<IDisposable> disposables)
        {
            this.disposables = disposables;
        }
        
        public CompositeDisposable(params IDisposable[] disposables)
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