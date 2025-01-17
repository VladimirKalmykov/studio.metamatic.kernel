 using System;
using System.Runtime.CompilerServices;
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
        public DisposableAction Watch(Action<TValue> handler, bool skipInitial = false, Func<TValue, bool> validator = null);
    }
    public abstract class ObservableReadOnly<TValue>: MicroStateBase, IObservableValue<TValue>, IResettable
    {
        private static bool DefaultValidator(TValue value)
        {
            return true;
        }
        private event Action<TValue> OnChange;
        private event Action OnDisposedOrReset;
        public int version { get; protected set; }

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
            DisposeOnTheMainThread().Forget();
        }
        
        private async UniTaskVoid DisposeOnTheMainThread()
        {
            await UniTask.SwitchToMainThread();
            Dispose();
        }

        public abstract DisposableAction Watch(Action<TValue> handler, bool skipInitial, Func<TValue, bool> validator = null);

        public DisposableAction Watch(Action<TValue> handler)
        {
            return Watch(handler, false);
        }
        
        public DisposableAction WatchChange(Action handler, bool skipInitial = false, Func<TValue, bool> validator = null)
        {
            return Watch(_ => handler(), skipInitial, validator);
        }

        /// <summary>
        /// Watch lifecycle of the value. Every time value changed, cancellation signal will be called
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="validator"></param>
        /// <returns></returns>
        public DisposableAction WatchAsync(Func<TValue, Signal, UniTaskVoid> handler, Func<TValue, bool> validator = null)
        {
            SignalSource valueChangeSignalSource = default;
            var unwatch = Watch(Watcher, false, validator);
            void DisposeHandler()
            {
                unwatch.Dispose();
                unwatch = default;
                valueChangeSignalSource.TryRaise();
            }
            OnDisposedOrReset += DisposeHandler;
            
            return DisposableAction.Create(() =>
            {
                OnDisposedOrReset -= DisposeHandler;
                valueChangeSignalSource.TryRaise();
                unwatch.Dispose();
            });

            void Watcher(TValue value)
            {
                valueChangeSignalSource.TryRaise();
                valueChangeSignalSource = SignalSource.Create();
                handler(value, valueChangeSignalSource.Signal).Forget();
            }
        }
        
        public DisposableAction SubscribeOnce(Action<TValue> handler)
        {
            Action<TValue> onceHandler = null;
            onceHandler = v =>
            {
                OnChange -= onceHandler;
                handler(v);
            };
            OnChange += onceHandler;
            return new DisposableAction(() =>
            {
                OnChange -= onceHandler;
            });
        }

        public UniTask<TValue> WaitUntilValueIs(Func<TValue, bool> validator,
            Signal cancellationSignal)
        {
            if (cancellationSignal.isRaised) return UniTask.FromCanceled<TValue>();
            if (validator(GetValue())) return UniTask.FromResult(GetValue());
            
            Action finalizeHandler = null;
            var tcs = SafeUniTaskCompletionSource<TValue>.Create();

            OnDisposedOrReset += DisposeHandler;

            if (cancellationSignal.CanBeCanceled)
            {
                finalizeHandler += cancellationSignal.Register(() =>
                {
                    OnChange -= Handler;
                    OnDisposedOrReset -= DisposeHandler;
                    finalizeHandler?.Invoke();
                    finalizeHandler = null;
                    tcs.TrySetCanceled();
                });
            }

            OnChange += Handler;
            
            return tcs.Task;

            void Handler(TValue value)
            {
                if (validator(value))
                {
                    OnChange -= Handler;
                    OnDisposedOrReset -= DisposeHandler;
                    finalizeHandler?.Invoke();
                    finalizeHandler = null;
                    tcs.TrySetResult(value);
                }
            }

            void DisposeHandler()
            {
                finalizeHandler?.Invoke();
                tcs.TrySetCanceled();
            }
        }

        public UniTask<TValue> WaitUntilValueIs(TValue value, Signal cancellationSignal = default)
        {
            return WaitUntilValueIs(Validator, cancellationSignal);

            bool Validator(TValue nextValue)
            {
                return nextValue.Equals(value);
            }
        }
        
        public UniTask<TValue> WaitUntilValueIsNotNull(Signal signal = default)
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

        public void Reset()
        {
            // Dispose and recreate cancellation token source
            OnDisposedOrReset?.Invoke();
            // Set value to default
            SetValue(default);
            // Reset all listeners
            OnChange = null;
        }
        
        public override void Dispose()
        {
            OnDisposedOrReset?.Invoke();
            OnChange = null;
            GC.SuppressFinalize(this);
        }
    }

    public class Observable<TValue> : ObservableReadOnly<TValue>
    {
        private TValue m_Value;
        public delegate void MutateHandler(ref TValue value);
        
        public Observable()
        {
            
        }
        public Observable(TValue initialValue)
        {
            m_Value = initialValue;
        }

        public override bool HasValue()
        {
            return true;
        }

        public override DisposableAction Watch(Action<TValue> handler, bool skipInitial, Func<TValue, bool> validator = null)
        {
            if (validator != null)
            {
                return WatchWithValidator(handler, validator, skipInitial);
            }
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
        
        private DisposableAction WatchWithValidator(Action<TValue> handler, Func<TValue, bool> validator, bool skipInitial)
        {
            AddListener(Handler);
            if (!skipInitial && validator(GetValue()))
            {
                handler(GetValue());
            }
            return new DisposableAction(() =>
            {
                RemoveListener(Handler);
            });
            void Handler(TValue value)
            {
                if (validator(value))
                {
                    handler(value);
                }
            }
        }
        
        /// <summary>
        /// Reduce value and watch unique lifecycle of the new value relative to the old value
        /// </summary>
        /// <param name="reducer"></param>
        /// <param name="handler"></param>
        /// <param name="defaultValue"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public DisposableAction ReduceLifecycle<T>(Func<TValue, T> reducer, Func<T, Signal, UniTaskVoid> handler, T defaultValue = default) {
            var sequentialHandler = new SequentialHandler<T>(handler);
            var memoizedValue = defaultValue;
            void Handler(TValue value)
            {
                var nextValue = reducer(value);
                var bothNull = nextValue == null && memoizedValue == null;
                if (!bothNull && (nextValue == null || memoizedValue == null || !nextValue.Equals(memoizedValue)))
                {
                    memoizedValue = nextValue;
                    sequentialHandler.Handle(nextValue);
                }
            }
            
            AddListener(Handler);

            return DisposableAction.Create(() =>
            {
                RemoveListener(Handler);
            });
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
            unchecked
            {
                version++;
            }
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

        public override DisposableAction Watch(Action<TValue> handler, bool skipInitial, Func<TValue, bool> validator = null)
        {
            Action releaseValueListener = default;
            var releaseProxyListener = m_Proxy.Watch((ObservableReadOnly<TValue> val) =>
            {
                releaseValueListener?.Invoke();
                if (val != null)
                {
                    releaseValueListener = val.Watch(handler, skipInitial, validator);
                }
                else
                {
                    handler(default);
                }
            });
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