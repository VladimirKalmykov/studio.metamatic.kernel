using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Metamatic;

namespace Metamatic
{
    public abstract class ObservableValue: IDisposable
    {
        #if DEBUG
        public string debugName;
        #endif
        public abstract void Dispose();
    }
    interface IObservableValue<TValue>
    {
        public TValue value { get; }
        public Action Subscribe(Action<TValue> handler);
    }
    public abstract class ObservableValue<TValue>: ObservableValue, IObservableValue<TValue>
    {
        private event Action<TValue> OnChange;
        private CancellationTokenSource m_DisposeCancellationTokenSource;

        public TValue value
        {
            get => GetValue();
            set => SetValue(value);
        }

        public ObservableValue()
        {
            m_DisposeCancellationTokenSource = new CancellationTokenSource();
        }

        public Action Subscribe(Action<TValue> handler)
        {
            OnChange += handler;
            handler(GetValue());
            return () =>
            {
                OnChange -= handler;
            };
        }
        
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
        
        public UniTask<TValue> WaitUntilValueIsNotNull(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return UniTask.FromCanceled<TValue>(cancellationToken);
            var combinedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(new[]
                { m_DisposeCancellationTokenSource.Token, cancellationToken });
            var combinedCancellationToken = combinedCancellationTokenSource.Token;
            if (GetValue() != null) return UniTask.FromResult(value);
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

        public abstract TValue GetValue();

        public abstract void SetValue(TValue value);
        
        public override void Dispose()
        {
            m_DisposeCancellationTokenSource?.Cancel();
            OnChange = null;
        }
    }

    public class ObservableWritableValue<TValue> : ObservableValue<TValue>
    {
        private TValue m_Value;
        
        public ObservableWritableValue(TValue initialValue = default)
        {
            m_Value = initialValue;
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
    }

    public class ObservableGettableValue<TValue> : ObservableValue<TValue>
    {
        private Func<TValue> m_Getter;
        public ObservableGettableValue(Func<TValue> getter) : base()
        {
            m_Getter = getter;
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

    public class ObservableProxyValue<TValue> : ObservableValue<TValue>
    {
        protected Action pluginUnsubscribe;
        protected ObservableValue<TValue> pluggedIn;
        public ObservableProxyValue(): base()
        {
            
        }
        public ObservableProxyValue(ObservableValue<TValue> other): base()
        {
            PlugIn(other);
        }
        
        public override TValue GetValue()
        {
            return pluggedIn.value;
        }

        public override void SetValue(TValue value)
        {
            throw new Exception("Unable to set value of proxy value");
        }
        
        /// <summary>
        /// Plug another observable value to get its value every time it changed
        /// </summary>
        /// <param name="other"></param>
        public void PlugIn(ObservableValue<TValue> other)
        {
            pluginUnsubscribe?.Invoke();
            pluggedIn = other;
            pluginUnsubscribe = other.Subscribe(v =>
            {
                EmitValue(v);
            });
            EmitValue(other.value);
        }
        
        public void Unplug()
        {
            if (pluggedIn != null)
            {
                pluggedIn = null;
                pluginUnsubscribe?.Invoke();
            }
        }
    }
}