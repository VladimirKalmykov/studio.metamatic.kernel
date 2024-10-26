using System;
using Cysharp.Threading.Tasks;

namespace Metamatic.Framework
{
    public abstract class WidgetLegacy: IDisposable
    {
        public DevToolsOverlay overlay { get; private set; }
        private CancellationSignalSource m_DisposeSignal;
        public WidgetLegacy(DevToolsOverlay overlay)
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