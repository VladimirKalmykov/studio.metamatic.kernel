using System;
using Cysharp.Threading.Tasks;

namespace Metamatic.Framework
{
    public abstract class Widget: IDisposable
    {
        public MetamaticFrameworkOverlay overlay { get; private set; }
        private CancellationSignalSource m_DisposeSignal;
        public Widget(MetamaticFrameworkOverlay overlay)
        {
            this.overlay = overlay;
            m_DisposeSignal = CancellationSignalSource.Create();
            // ReSharper disable once VirtualMemberCallInConstructor
            OnInitializeWidget(m_DisposeSignal.Signal).Forget();
        }

        public void Dispose()
        {
            m_DisposeSignal.TryCancel();
        }

        public abstract UniTaskVoid OnInitializeWidget(CancellationSignal signal);
    }
}