using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public abstract class MicroStateRefReadOnly<T>
    {
        public delegate void ValueHandler(ref T value);
        public event ValueHandler OnChange;
        public abstract ref T GetValue();
        public abstract DisposableAction Subscribe(ValueHandler handler);
        protected void ApplyChange(ref T value)
        {
            OnChange?.Invoke(ref value);
        }
    }
    public class MicroStateRef<T>: MicroStateRefReadOnly<T>, IDisposable
    where T: struct
    {
        public T value;
        private bool m_IsDirty;
        private bool m_ApplyInLoop;
        private bool m_IsDisposed;
        private PlayerLoopTiming m_Timing;
        private CancellationTokenSource m_DisposeCancellationTokenSource;
        public MicroStateRef(T initialValue = default)
        {
            value = initialValue;
        }
        public MicroStateRef(PlayerLoopTiming timing, T initialValue = default)
        {
            value = initialValue;
            m_ApplyInLoop = true;

        }

        ~MicroStateRef()
        {
            Dispose();
        }
        public override ref T GetValue()
        {
            return ref value;
        }

        public void Modify(ValueHandler modifier)
        {
            modifier(ref value);
            SetDirty();
        }
        public override DisposableAction Subscribe(ValueHandler handler)
        {
            OnChange += handler;
            handler(ref value);
            return new DisposableAction(() =>
            {
                OnChange -= handler;
            });
        }

        public async void SetDirty()
        {
            if (m_ApplyInLoop)
            {
                if (!m_IsDirty)
                {
                    m_IsDirty = true;
                    try
                    {
                        var token = m_DisposeCancellationTokenSource.Token;
                        await UniTask.Yield(m_Timing, token);
                        token.ThrowIfCancellationRequested();
                        m_IsDirty = false;
                        ApplyChange(ref value);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }
            }
            else
            {
                ApplyChange(ref value);
            }
        }

        public void Dispose()
        {
            m_DisposeCancellationTokenSource?.Cancel();
            GC.SuppressFinalize(this);
        }
    }

    public class MicroStateRefProxy<T> : MicroStateRefReadOnly<T>, IDisposable
        where T : struct
    {
        private MicroState<MicroStateRefReadOnly<T>> m_ProxyMicroState;
        public MicroStateRefProxy(MicroStateRefReadOnly<T> microStateRef)
        {
            m_ProxyMicroState = new MicroState<MicroStateRefReadOnly<T>>(microStateRef);
        }

        public MicroStateRefProxy()
        {
            m_ProxyMicroState = new MicroState<MicroStateRefReadOnly<T>>();
        }

        ~MicroStateRefProxy()
        {
            Dispose();
        }
        public void SetProxy(MicroStateRefReadOnly<T> microStateRef)
        {
            m_ProxyMicroState.value = microStateRef;
        }
        public override ref T GetValue()
        {
            if (m_ProxyMicroState.value == null)
            {
                throw new NullReferenceException("Proxy value is null");
            }
            return ref m_ProxyMicroState.value.GetValue();
        }

        public override DisposableAction Subscribe(ValueHandler handler)
        {
            Action releaseValueListener = default;
            var releaseProxyListener = m_ProxyMicroState.Subscribe((MicroStateRefReadOnly<T> value) =>
            {
                releaseValueListener?.Invoke();
                if (value != null)
                {
                    releaseValueListener = value.Subscribe(handler);
                }
            });
            return new DisposableAction(() =>
            {
                releaseValueListener?.Invoke();
                releaseProxyListener?.Invoke();
                releaseValueListener = null;
                releaseProxyListener = null;
            });
        }

        public void Dispose()
        {
            m_ProxyMicroState.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}