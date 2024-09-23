using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public abstract class ObservableRefReadOnly<T>
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
    public class ObservableRef<T>: ObservableRefReadOnly<T>, IDisposable
    where T: struct
    {
        public T value;
        private bool m_IsDirty;
        private bool m_ApplyInLoop;
        private bool m_IsDisposed;
        private PlayerLoopTiming m_Timing;
        private CancellationTokenSource m_DisposeCancellationTokenSource;
        public ObservableRef(T initialValue = default)
        {
            value = initialValue;
        }
        public ObservableRef(PlayerLoopTiming timing, T initialValue = default)
        {
            value = initialValue;
            m_ApplyInLoop = true;

        }

        ~ObservableRef()
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
            m_DisposeCancellationTokenSource?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public class ObservableRefProxy<T> : ObservableRefReadOnly<T>, IDisposable
        where T : struct
    {
        private Observable<ObservableRefReadOnly<T>> m_ProxyObservable;
        public ObservableRefProxy(ObservableRefReadOnly<T> observableRef)
        {
            m_ProxyObservable = new Observable<ObservableRefReadOnly<T>>(observableRef);
        }

        public ObservableRefProxy()
        {
            m_ProxyObservable = new Observable<ObservableRefReadOnly<T>>();
        }

        ~ObservableRefProxy()
        {
            Dispose();
        }
        public void SetProxy(ObservableRefReadOnly<T> observableRef)
        {
            m_ProxyObservable.value = observableRef;
        }
        public override ref T GetValue()
        {
            if (m_ProxyObservable.value == null)
            {
                throw new NullReferenceException("Proxy value is null");
            }
            return ref m_ProxyObservable.value.GetValue();
        }

        public override DisposableAction Subscribe(ValueHandler handler)
        {
            Action releaseValueListener = default;
            var releaseProxyListener = m_ProxyObservable.Watch((ObservableRefReadOnly<T> value) =>
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
                releaseProxyListener.Invoke();
            });
        }

        public void Dispose()
        {
            m_ProxyObservable.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}