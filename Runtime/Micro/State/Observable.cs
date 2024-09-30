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
        public DisposableAction Watch(Action<TValue> handler, bool skipInitial = false);
    }
    public abstract class ObservableReadOnly<TValue>: MicroStateBase, IObservableValue<TValue>
    {
        private event Action<TValue> OnChange;
        private CancellationSignalSource m_DisposeCancellationTokenSource;

        public TValue value
        {
            get => GetValue();
            set => SetValue(value);
        }

        // ReSharper disable once InconsistentNaming
        public TValue Value
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

        public ObservableReadOnly()
        {
        }

        ~ObservableReadOnly()
        {
            Dispose();
        }

        public abstract DisposableAction Watch(Action<TValue> handler, bool skipInitial = false);
        
        public abstract DisposableAction Watch(Action<TValue> handler);
        
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

        public UniTask<TValue> WaitUntilValueIs(Func<TValue, bool> validator,
            CancellationSignal cancellationSignal)
        {
            if (cancellationSignal.IsCancellationRequested) return UniTask.FromCanceled<TValue>();
            if (validator(GetValue())) return UniTask.FromResult(GetValue());
            
            Action FinalizeHandler = null;
            var tcs = SafeUniTaskCompletionSource<TValue>.Create();
            
            if (!m_DisposeCancellationTokenSource.CanBeCancelled)
            {
                m_DisposeCancellationTokenSource = CancellationSignalSource.Create();
                var disposeSignal = m_DisposeCancellationTokenSource.Signal;
                FinalizeHandler += disposeSignal.Register(() =>
                {
                    FinalizeHandler?.Invoke();
                    tcs.TrySetCanceled();
                });
            }

            if (cancellationSignal.CanBeCanceled)
            {
                FinalizeHandler += cancellationSignal.Register(() =>
                {
                    FinalizeHandler?.Invoke();
                    tcs.TrySetCanceled();
                });
            }
            
            void Handler(TValue value)
            {
                if (validator(value))
                {
                    FinalizeHandler?.Invoke();
                    tcs.TrySetResult(value);
                }
            }
            
            OnChange += Handler;
            
            return tcs.Task;
        }

        public UniTask<TValue> WaitUntilValueIs(TValue value, CancellationSignal cancellationSignal = default)
        {
            bool Validator(TValue nextValue)
            {
                return nextValue.Equals(value);
            }
            return WaitUntilValueIs(Validator, cancellationSignal);
        }
        
        public UniTask<TValue> WaitUntilValueIsNotNull(CancellationSignal signal = default)
        {
            return WaitUntilValueIs(v => v != null, signal);
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
            m_DisposeCancellationTokenSource.TryCancel();
            OnChange = null;
            GC.SuppressFinalize(this);
        }
    }

    public class Observable<TValue> : ObservableReadOnly<TValue>
    {
        private TValue m_Value;
        public delegate void MutateHandler(ref TValue value);
        
        public Observable(TValue initialValue = default): base()
        {
            m_Value = initialValue;
        }

        public override bool HasValue()
        {
            return true;
        }

        public override DisposableAction Watch(Action<TValue> handler)
        {
            return Watch(handler, false);
        }

        public override DisposableAction Watch(Action<TValue> handler, bool skipInitial)
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
        
        /// <summary>
        /// Watch lifecycle of the value. Every time value changed, cancellation signal will be called
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public DisposableAction WatchLifecycle(Func<TValue, CancellationSignal, UniTaskVoid> handler)
        {
            return SequentialHandler<TValue>.Subscribe(Watch, handler);
        }
        
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
        
        public void SetUniqValue(TValue value)
        {
            if ((m_Value != null && value != null) && m_Value.Equals(value)) return;
            if (m_Value == null && value == null) return;
            SetValue(value);
        }
        
        public void Mutate(MutateHandler mutationHandler)
        {
            mutationHandler(ref m_Value);
        }
    }

    public class ObservableGetter<TValue> : ObservableReadOnly<TValue>
    {
        private Func<TValue> m_Getter;
        public ObservableGetter(Func<TValue> getter) : base()
        {
            m_Getter = getter;
        }

        public override bool HasValue()
        {
            return m_Getter != null;
        }

        public override DisposableAction Watch(Action<TValue> handler, bool skipInitial = false)
        {
            throw new NotImplementedException();
        }

        public override DisposableAction Watch(Action<TValue> handler)
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

    public class ObservableProxy<TValue> : ObservableReadOnly<TValue>, IDisposable
    {
        protected readonly Observable<ObservableReadOnly<TValue>> m_Proxy = new Observable<ObservableReadOnly<TValue>>();
        private event Action OnDisposed;
        public ObservableProxy(): base()
        {
            
        }
        public ObservableProxy(ObservableReadOnly<TValue> other): base()
        {
            SetProxy(other);
        }

        ~ObservableProxy()
        {
            Dispose();
        }

        public override DisposableAction Watch(Action<TValue> handler, bool skipInitial = false)
        {
            Action releaseValueListener = default;
            var releaseProxyListener = m_Proxy.Watch((ObservableReadOnly<TValue> value) =>
            {
                releaseValueListener?.Invoke();
                if (value != null)
                {
                    releaseValueListener = value.Watch(handler);
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

        public override DisposableAction Watch(Action<TValue> handler)
        {
            return Watch(handler, false);
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
        public void SetProxy(ObservableReadOnly<TValue> proxy)
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