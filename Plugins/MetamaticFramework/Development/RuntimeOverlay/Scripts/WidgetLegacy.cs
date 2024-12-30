using System;
using Cysharp.Threading.Tasks;

namespace Metamatic.Framework
{
    public abstract class WidgetLegacy: IDisposable
    {
        public DevToolsOverlay overlay { get; private set; }
        private SignalSource m_DisposeSignal;
        public WidgetLegacy(DevToolsOverlay overlay)
        {
            this.overlay = overlay;
            m_DisposeSignal = SignalSource.Create();
            // ReSharper disable once VirtualMemberCallInConstructor
            OnInitializeWidget(m_DisposeSignal.Signal).Forget();
        }

        public void Dispose()
        {
            m_DisposeSignal.TryRaise();
        }

        public abstract UniTaskVoid OnInitializeWidget(Signal signal);
    }
}