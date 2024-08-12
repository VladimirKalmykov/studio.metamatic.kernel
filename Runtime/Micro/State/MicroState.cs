using System;
using System.Runtime.CompilerServices;
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
        public DisposableAction Subscribe(Action<TValue> handler, bool skipInitial = false);
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

        public TValue uniqValue
        {
            set {
                /* Compare old value with new value */
                if (GetValue().Equals(value)) return;
                SetValue(value);
            }
        }
        
        public abstract bool HasValue();

        public MicroStateReadOnly()
        {
            m_DisposeCancellationTokenSource = new CancellationTokenSource();
        }

        ~MicroStateReadOnly()
        {
            Dispose();
        }

        public abstract DisposableAction Subscribe(Action<TValue> handler, bool skipInitial = false);
        
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

        public UniTask<TValue> WaitUntilValueIs(Func<TValue, bool> callback,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return UniTask.FromCanceled<TValue>(cancellationToken);
            var combinedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(new[]
                { m_DisposeCancellationTokenSource.Token, cancellationToken });
            var combinedCancellationToken = combinedCancellationTokenSource.Token;
            if (callback(GetValue())) return UniTask.FromResult(GetValue());
            var tcs = new UniTaskCompletionSource<TValue>();
            Action<TValue> handler = null;
            handler = v =>
            {
                if (callback(v))
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
        public abstract ref TValue GetRef();

        public abstract void SetValue(TValue value);
        
        public override void Dispose()
        {
            m_DisposeCancellationTokenSource?.Cancel();
            m_DisposeCancellationTokenSource?.Dispose();
            OnChange = null;
            GC.SuppressFinalize(this);
        }
    }

    public class MicroState<TValue> : MicroStateReadOnly<TValue>
    {
        private TValue m_Value;
        public delegate void MutateHandler(ref TValue value);
        
        public MicroState(TValue initialValue = default): base()
        {
            m_Value = initialValue;
        }

        public override bool HasValue()
        {
            return true;
        }

        public DisposableAction Subscribe(Action<TValue> handler)
        {
            return Subscribe(handler, false);
        }

        public override DisposableAction Subscribe(Action<TValue> handler, bool skipInitial)
        {
            AddListener(handler);
            if (!skipInitial)
            {
                handler(GetValue());
            }
            return new DisposableAction(() =>
            {
                RemoveListener(handler);
            });
        }
        
        // TODO: Do I need [MethodImpl(MethodImplOptions.AggressiveInlining)] here?
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override TValue GetValue()
        {
            return m_Value;
        }
        
        public override ref TValue GetRef()
        {
            return ref m_Value;
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

        public override bool HasValue()
        {
            return m_Getter != null;
        }

        public override DisposableAction Subscribe(Action<TValue> handler, bool skipInitial = false)
        {
            throw new NotImplementedException();
        }

        public override TValue GetValue()
        {
            return m_Getter != null ? m_Getter.Invoke() : default;
        }

        public override ref TValue GetRef()
        {
            /* Get by reference is not supported for this type of state */
            throw new Exception("Not supported");
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
        private event Action OnDisposed;
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

        public override DisposableAction Subscribe(Action<TValue> handler, bool skipInitial = false)
        {
            Action releaseValueListener = default;
            var releaseProxyListener = m_Proxy.Subscribe((MicroStateReadOnly<TValue> value) =>
            {
                releaseValueListener?.Invoke();
                if (value != null)
                {
                    releaseValueListener = value.Subscribe(handler);
                }
                else
                {
                    handler(default);
                }
            }, skipInitial);
            DisposableAction commonDisposable = default;
            commonDisposable = new DisposableAction(() =>
            {
                OnDisposed -= commonDisposable.Dispose;
                releaseValueListener?.Invoke();
                releaseProxyListener.Invoke();
            });
            OnDisposed += commonDisposable.Dispose;

            return commonDisposable;
        }
        
        public override bool HasValue()
        {
            return m_Proxy.value != null;
        }

        public override TValue GetValue()
        {
            if (m_Proxy.value == null)
            {
                return default(TValue);
            }
            return m_Proxy.value.GetValue();
        }

        public override ref TValue GetRef()
        {
            if (m_Proxy == null)
            {
                throw new NullReferenceException("Proxy value is null. Check HasValue() before calling GetValue()");
            }
            ref var r = ref m_Proxy.value.GetRef();
            return ref r;
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
        
        public new void Dispose()
        {
            m_Proxy.Dispose();
            base.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}