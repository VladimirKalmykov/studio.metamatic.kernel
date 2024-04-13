using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public abstract class MicroStateBase: IDisposable
    
    {
        #if DEBUG
        public string debugName;
        #endif
        public abstract void Dispose();
    }
    interface IObservableValue<TValue>
    {
        public TValue value { get; }
        public DisposableAction Subscribe(Action<TValue> handler);
    }
    public abstract class MicroStateReadOnly<TValue>: MicroStateBase, IObservableValue<TValue>
    {
        private event Action<TValue> OnChange;
        private CancellationTokenSource m_DisposeCancellationTokenSource;

        public TValue value
        {
            get => GetValue();
            set => SetValue(value);
        }

        public MicroStateReadOnly()
        {
            m_DisposeCancellationTokenSource = new CancellationTokenSource();
        }

        public abstract DisposableAction Subscribe(Action<TValue> handler);
        
        public Action SubscribeOnce(Action<TValue> handler)
        {
            Action<TValue> onceHandler = null;
            onceHandler = v =>
            {
                handler(v);
                OnChange -= onceHandler;
            };
            OnChange += onceHandler;
            return () =>
            {
                OnChange -= onceHandler;
            };
        }
        
        /// <summary>
        /// Wait until value changed once
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public UniTask<TValue> WaitUntilChangeOnce(CancellationToken cancellationToken = default)
        {
            var taskCompletionSource = new UniTaskCompletionSource<TValue>();
            var release = SubscribeOnce(v =>
            {
                taskCompletionSource.TrySetResult(v);
            });
            cancellationToken.Register(() =>
            {
                taskCompletionSource.TrySetCanceled(cancellationToken);
                release();
            });
            return taskCompletionSource.Task;
        }
        
        public UniTask<TValue> WaitUntilValueIs(TValue value, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return UniTask.FromCanceled<TValue>(cancellationToken);
            var combinedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(new[]
                { m_DisposeCancellationTokenSource.Token, cancellationToken });
            var combinedCancellationToken = combinedCancellationTokenSource.Token;
            if (GetValue().Equals(value)) return UniTask.FromResult(value);
            var tcs = new UniTaskCompletionSource<TValue>();
            Action<TValue> handler = null;
            handler = v =>
            {
                if (v.Equals(value))
                {
                    combinedCancellationTokenSource.Dispose();
                    tcs.TrySetResult(v);
                    OnChange -= handler;
                }
            };
            OnChange += handler;
            var unregister= combinedCancellationToken.Register(() =>
            {
                tcs.TrySetCanceled(combinedCancellationToken);
                OnChange -= handler;
            });
            tcs.Task.GetAwaiter().OnCompleted(() =>
            {
                unregister.Dispose();
            });
            return tcs.Task;
        }
        
        public UniTask<TValue> WaitUntilValueIsNot(TValue value, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return UniTask.FromCanceled<TValue>(cancellationToken);
            var combinedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(new[]
                { m_DisposeCancellationTokenSource.Token, cancellationToken });
            var combinedCancellationToken = combinedCancellationTokenSource.Token;
            if (!GetValue().Equals(value)) return UniTask.FromResult(value);
            var tcs = new UniTaskCompletionSource<TValue>();
            Action<TValue> handler = null;
            handler = v =>
            {
                if (!v.Equals(value))
                {
                    combinedCancellationTokenSource.Dispose();
                    tcs.TrySetResult(v);
                    OnChange -= handler;
                }
            };
            OnChange += handler;
            var unregister= combinedCancellationToken.Register(() =>
            {
                tcs.TrySetCanceled(combinedCancellationToken);
                OnChange -= handler;
            });
            tcs.Task.GetAwaiter().OnCompleted(() =>
            {
                unregister.Dispose();
            });
            return tcs.Task;
        }
        
        public UniTask<TValue> WaitUntilValueIsNotNull(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return UniTask.FromCanceled<TValue>(cancellationToken);
            if (GetValue() != null) return UniTask.FromResult(value);
            var combinedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(new[]
                { m_DisposeCancellationTokenSource.Token, cancellationToken });
            var combinedCancellationToken = combinedCancellationTokenSource.Token;
            var tcs = new UniTaskCompletionSource<TValue>();
            Action<TValue> handler = null;
            handler = v =>
            {
                if (combinedCancellationToken.IsCancellationRequested) return;
                if (v != null)
                {
                    combinedCancellationTokenSource.Dispose();
                    tcs.TrySetResult(v);
                    OnChange -= handler;
                }
            };
            OnChange += handler;
            var unregister= combinedCancellationToken.Register(() =>
            {
                OnChange -= handler;
                tcs.TrySetCanceled(cancellationToken);
            });
            tcs.Task.GetAwaiter().OnCompleted(() =>
            {
                unregister.Dispose();
            });
            return tcs.Task;
        }

        protected void EmitValue(TValue v)
        {
            OnChange?.Invoke(v);
        }
        protected void AddListener(Action<TValue> handler)
        {
            OnChange += handler;
        }
        
        protected void RemoveListener(Action<TValue> handler)
        {
            OnChange -= handler;
        }

        public abstract TValue GetValue();

        public abstract void SetValue(TValue value);
        
        public override void Dispose()
        {
            m_DisposeCancellationTokenSource?.Cancel();
            OnChange = null;
        }
    }

    public class MicroState<TValue> : MicroStateReadOnly<TValue>
    {
        private TValue m_Value;
        public delegate void MutateHandler(ref TValue value);
        
        public MicroState(TValue initialValue = default)
        {
            m_Value = initialValue;
        }

        public override DisposableAction Subscribe(Action<TValue> handler)
        {
            AddListener(handler);
            handler(GetValue());
            return new DisposableAction(() =>
            {
                RemoveListener(handler);
            });
        }

        public override TValue GetValue()
        {
            return m_Value;
        }

        /// <summary>
        /// Set value. This action will automatically unsubscribe plugin
        /// </summary>
        /// <param name="value"></param>
        public override void SetValue(TValue value)
        {
            m_Value = value;
            EmitValue(value);
        }
        
        public void Mutate(MutateHandler mutationHandler)
        {
            mutationHandler(ref m_Value);
        }
    }

    public class MicroStateGetter<TValue> : MicroStateReadOnly<TValue>
    {
        private Func<TValue> m_Getter;
        public MicroStateGetter(Func<TValue> getter) : base()
        {
            m_Getter = getter;
        }

        public override DisposableAction Subscribe(Action<TValue> handler)
        {
            throw new NotImplementedException();
        }

        public override TValue GetValue()
        {
            return m_Getter != null ? m_Getter.Invoke() : default;
        }

        public override void SetValue(TValue value)
        {
            throw new Exception("Set value of ObservableGettableValue is out of concept");
        }

        public void EmitUpdate()
        {
            EmitValue(value);
        }
    }

    public class MicroStateProxy<TValue> : MicroStateReadOnly<TValue>, IDisposable
    {
        protected readonly MicroState<MicroStateReadOnly<TValue>> m_Proxy = new MicroState<MicroStateReadOnly<TValue>>();
        public MicroStateProxy(): base()
        {
            
        }
        public MicroStateProxy(MicroStateReadOnly<TValue> other): base()
        {
            SetProxy(other);
        }

        ~MicroStateProxy()
        {
            Dispose();
        }

        public override DisposableAction Subscribe(Action<TValue> handler)
        {
            Action releaseValueListener = default;
            var releaseProxyListener = m_Proxy.Subscribe((MicroStateReadOnly<TValue> value) =>
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

        public override TValue GetValue()
        {
            if (m_Proxy == null)
            {
                throw new NullReferenceException("Proxy value is null");
            }
            return m_Proxy.value.GetValue();
        }

        public override void SetValue(TValue value)
        {
            throw new Exception("Unable to set value of proxy value");
        }
        
        /// <summary>
        /// Plug another observable value to get its value every time it changed
        /// </summary>
        /// <param name="proxy"></param>
        public void SetProxy(MicroStateReadOnly<TValue> proxy)
        {
            m_Proxy.value = proxy;
        }
        
        public void Dispose()
        {
            m_Proxy.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}