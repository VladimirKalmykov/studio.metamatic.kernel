using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public readonly struct AsyncExchange: IUniTaskAsyncDisposable, IDisposable
    {
        public static AsyncExchange Create(DisposedAsyncExchangeCore.AsyncAction action, object[] dependencies = null)
        {
            var core = ParamLessPool<DisposedAsyncExchangeCore>.GetShared();
            core.Init(action, dependencies);
            return new AsyncExchange(core);
        }
        
        private readonly DisposedAsyncExchangeCore m_Core;
        private readonly int m_Version;
        
        public AsyncExchange(DisposedAsyncExchangeCore core)
        {
            m_Core = core;
            m_Version = core.version;
        }
        
        private DisposedAsyncExchangeCore ResolveCore()
        {
            if (m_Core.version != m_Version)
            {
                return null;
            }
            return m_Core;
        }
        
        #region Public API
        public UniTask task => ResolveCore()?.task ?? UniTask.CompletedTask;
        public void Send<TInput>(TInput inputValue) => ResolveCore()?.Send(inputValue);
        
        public void Send() => ResolveCore()?.Send();

        public UniTask<TOutput> Receive<TOutput>(Signal token = default) =>
            ResolveCore()?.Receive<TOutput>(token) ?? UniTask.FromCanceled<TOutput>();
        
        public UniTask Receive(Signal token = default) => ResolveCore()?.Receive(token) ?? UniTask.FromCanceled();
        
        public UniTask<TOutput> Duplex<TInput, TOutput>(TInput value, Signal token = default) =>
            ResolveCore()?.Duplex<TInput, TOutput>(value, token) ?? UniTask.FromCanceled<TOutput>();
        
        public UniTask Duplex<TInput>(TInput value, Signal token = default) => ResolveCore()?.Duplex<TInput>(value, token) ?? UniTask.FromCanceled();
        
        public UniTask<TOutput> Duplex<TOutput>(Signal token = default) =>
            ResolveCore()?.Duplex<TOutput>(token) ?? UniTask.FromCanceled<TOutput>();
        
        public UniTask Duplex(Signal token = default) => ResolveCore()?.Duplex(token) ?? UniTask.FromCanceled();
        #endregion
        
        public void Dispose()
        {
            var core = ResolveCore();
            if (core != null)
            {
                core.Reset();
                ParamLessPool<DisposedAsyncExchangeCore>.ReturnShared(core);
            }
        }

        public async UniTask DisposeAsync()
        {
            var core = ResolveCore();
            try
            {
                if (core != null)
                {
                    await core.ResetAsync();
                }
            }
            finally
            {
                ParamLessPool<DisposedAsyncExchangeCore>.ReturnShared(core);
            }
        }
    }
    public class DisposedAsyncExchangeCore : IUniTaskAsyncDisposable, IExposedDisposedSync, IExposedDisposedAsync, IAbortable, IResettable
    {
        public readonly struct Exchanger: IUniTaskAsyncDisposable
        {
            private readonly DisposedAsyncExchangeCore m_Master;
            public bool isDisposedAsync => m_Master.isDisposedAsync;
            internal Exchanger(DisposedAsyncExchangeCore master)
            {
                m_Master = master;
            }
            public bool isAborted => m_Master.isAborted;
            
            /// <summary>
            /// Wait for the input value
            /// </summary>
            /// <typeparam name="TInput"></typeparam>
            /// <returns></returns>
            /// <exception cref="InvalidOperationException"></exception>
            public async UniTask<TInput> Receive<TInput>()
            {
                DisposableAction registration = default;
                if (m_Master.m_InputCompletionSource == null)
                {
                    m_Master.m_ExpectedInputType = typeof(TInput);
                    var source = SafeUniTaskCompletionSource<TInput>.Create();
                    m_Master.m_InputCompletionSource = source;
                    registration = m_Master.cancellationSignal.Register(() =>
                    {
                        source.TrySetCanceled();
                    });
                }
                if (m_Master.m_InputCompletionSource is SafeUniTaskCompletionSource<TInput> inputCompletionSource)
                {
                    var value = await inputCompletionSource.Task;
                    registration.Dispose();
                    m_Master.m_InputCompletionSource = null;
                    return value;
                }

                throw new InvalidOperationException($"Expected input type is {m_Master.m_ExpectedInputType}, but got {m_Master.m_InputCompletionSource.GetType()}");
            }

            public async UniTask Receive()
            {
                await Receive<object>();
            }
            
            /// <summary>
            /// Two-way exchange. Send output and receive input.
            /// </summary>
            /// <param name="outputValue"></param>
            /// <typeparam name="TInput"></typeparam>
            /// <typeparam name="TOutput"></typeparam>
            /// <returns></returns>
            /// <exception cref="InvalidOperationException"></exception>
            public UniTask<TInput> Duplex<TInput, TOutput>(TOutput outputValue)
            {
                var receiveTask = Receive<TInput>();
                Send(outputValue);
                return receiveTask;
            }
            
            public UniTask Duplex<TOutput>(TOutput outputValue)
            {
                return Duplex<object, TOutput>(outputValue);
            }
            
            public UniTask<TInput> Duplex<TInput>()
            {
                return Duplex<TInput, object>(null);
            }
            
            public UniTask Duplex()
            {
                return Duplex<object, object>(null);
            }
            
            /// <summary>
            /// Should be called only once at the end of the exchange
            /// </summary>
            /// <param name="outputValue"></param>
            /// <typeparam name="TOutput"></typeparam>
            /// <exception cref="InvalidOperationException"></exception>
            public void Send<TOutput>(TOutput outputValue)
            {
                if (m_Master.m_OutputCompletionSource == null)
                {
                    m_Master.m_ExpectedOutputType = typeof(TOutput);
                    var outputSource = SafeUniTaskCompletionSource<TOutput>.Create();
                    m_Master.m_OutputCompletionSource = outputSource;
                }
                
                /* Check that m_OutputCompletionSource is not resolved yet */
                if (m_Master.m_OutputCompletionSource is SafeUniTaskCompletionSource<TOutput> outputCompletionSource)
                {
                    if (outputCompletionSource.Task.Status == UniTaskStatus.Pending)
                    {
                        outputCompletionSource.TrySetResult(outputValue);
                    }
                    else
                    {
                        throw new InvalidOperationException("Output completion source is already resolved");
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Expected output type is {m_Master.m_ExpectedOutputType}, but got {m_Master.m_OutputCompletionSource.GetType()}");
                }
            }

            public void Send()
            {
                Send<object>(null);
            }

            public UniTask DisposeAsync()
            {
                return m_Master.DisposeAsync();
            }
        } 
        
        /// <summary>
        /// This completion source is used to send value to the output
        /// </summary>
        private object m_OutputCompletionSource;
        /// <summary>
        /// This completions source is used to receive value from the input
        /// </summary>
        private object m_InputCompletionSource;
        /// <summary>
        /// Common cancellation token for the exchange
        /// </summary>
        private SignalSource m_CancellationSignalSource;
        /// <summary>
        /// Contains the expected input type
        /// </summary>
        private Type m_ExpectedInputType;
        /// <summary>
        /// Contains the expected output type
        /// </summary>
        private Type m_ExpectedOutputType;
        /// <summary>
        /// This completion source is used to wait for the async dispose
        /// </summary>
        private readonly ReusableUniTaskCompletionSource m_GeneralCompletionSource = ReusableUniTaskCompletionSource.Create();

        public int version { get; private set; } = 0;
        /// <summary>
        /// Public reference to the cancellation token (that is called when the behaviour is disposed)
        /// </summary>
        public Signal cancellationSignal => m_CancellationSignalSource.Signal;
        /// <summary>
        /// Disposing is called asynchronously (DisposeAsync() method is called), otherwise it is called synchronously (Dispose() method is called)
        /// </summary>
        public bool isDisposedAsync { get; private set; }

        public event Action<Exception> OnDisposedAsync;
        public UniTask task => m_GeneralCompletionSource.Task;

        public delegate UniTask AsyncAction(Exchanger exchange, Signal cancellationSignal);

        private AsyncAction m_Action;

        public void Init(AsyncAction action, object[] dependencies = null)
        {
            m_Action = action;
            m_CancellationSignalSource = SignalSource.Create();
            if (dependencies != null)
            {
                OnDisposedSync += ObserveExposedDisposable.Create(dependencies, Abort);
            }
            RunAsync();
        }

        ~DisposedAsyncExchangeCore()
        {
            throw new InvalidOperationException("You should dispose ExchangeBehaviour manually");
        }

        public void Reset()
        {
            Reset(false);
        }
        
        public void Reset(bool fromAsyncReset)
        {
            if (!fromAsyncReset)
            {
                m_CancellationSignalSource.Dispose();
            }
            unchecked
            {
                version++;
            }
            OnDisposedSync?.Invoke();
            OnDisposedSync = null;
            isDisposedSync = false;
            m_GeneralCompletionSource.TrySetResult();
            m_GeneralCompletionSource.Reset();
            m_InputCompletionSource = null;
            m_OutputCompletionSource = null;
            OnDisposedAsync = null;
            isDisposedAsync = false;
            m_Action = null;
            m_ExpectedInputType = null;
            m_ExpectedOutputType = null;
        }
        
        public async UniTask ResetAsync()
        {
            m_CancellationSignalSource.Dispose();
            try
            {
                await m_GeneralCompletionSource.Task;
            }
            catch (Exception e)
            {
                OnDisposedAsync?.Invoke(e);
                OnDisposedAsync = null;
                isDisposedAsync = true;
            }
            finally
            {
                if (!isDisposedAsync)
                {
                    isDisposedAsync = true;
                    OnDisposedAsync?.Invoke(null);
                    OnDisposedAsync = null;
                }
                Reset(true);
            }
        }

        private async void RunAsync()
        {
            try
            {
                await m_Action(new Exchanger(this), m_CancellationSignalSource.Signal);
            }
            catch (OperationCanceledException)
            {
                // Do nothing
            }
            catch (Exception e)
            {
                m_GeneralCompletionSource.TrySetException(e);
            }
            finally
            {
                // TODO: Handle situation when the output or input tasks still pending
                m_GeneralCompletionSource.TrySetResult();
            }
        }

        #region Public API

        public void Send<TInput>(TInput inputValue)
        {
            if (m_InputCompletionSource == null)
            {
                m_ExpectedInputType = typeof(TInput);
                var source = SafeUniTaskCompletionSource<TInput>.Create();
                m_InputCompletionSource = source;
            }
            if (m_InputCompletionSource is SafeUniTaskCompletionSource<TInput> inputCompletionSource)
            {
                if (inputCompletionSource.Task.Status == UniTaskStatus.Pending)
                {
                    inputCompletionSource.TrySetResult(inputValue);
                } else
                {
                    throw new InvalidOperationException("Input completion source is already resolved");
                }
            }
            else
            {
                throw new InvalidOperationException($"Expected input type is {m_ExpectedInputType}, but got {m_InputCompletionSource.GetType()}");
            }
        }

        public void Send()
        {
            Send<object>(null);
        }
        
        public async UniTask<TOutput> Receive<TOutput>(Signal signal = default)
        {
            signal.ThrowIfRaised();
            cancellationSignal.ThrowIfRaised();
            DisposableAction registrationSelf = default;
            DisposableAction registrationExternal = default;
            if (m_OutputCompletionSource == null)
            {
                m_ExpectedOutputType = typeof(TOutput);
                var source = SafeUniTaskCompletionSource<TOutput>.Create();
                m_OutputCompletionSource = source;
                registrationSelf = cancellationSignal.Register(() =>
                {
                    source.TrySetCanceled();
                });
            }
            
            if (m_OutputCompletionSource is SafeUniTaskCompletionSource<TOutput> outputCompletionSource)
            {
                registrationExternal = signal.Register(() =>
                {
                    outputCompletionSource.TrySetCanceled();
                });
                var value = await outputCompletionSource.Task;
                registrationSelf.Dispose();
                registrationExternal.Dispose();
                m_OutputCompletionSource = null;
                return value;
            }
            throw new InvalidOperationException($"Expected output type is {m_ExpectedOutputType}, but got {m_OutputCompletionSource.GetType()}");
        }
        
        public async UniTask Receive(Signal signal = default)
        {
            await Receive<object>(signal);
        }

        public UniTask<TOutput> Duplex<TInput, TOutput>(TInput value,
            Signal token = default)
        {
            var receiveTask = Receive<TOutput>(token);
            Send(value);
            return receiveTask;
        }
        
        public UniTask Duplex<TInput>(TInput value,
            Signal token = default)
        {
            return Duplex<TInput, object>(value, token);
        }
        
        public UniTask<TOutput> Duplex<TOutput>(Signal token = default)
        {
            return Duplex<object, TOutput>(default, token);
        }
        
        public UniTask Duplex(Signal token = default)
        {
            return Duplex<object, object>(default, token);
        }

        #endregion

        public void Dispose()
        {
            Dispose(false);
        }

        public void Dispose(bool isAsync)
        {
            if (isDisposedSync)
            {
#if DEBUG
                throw new InvalidOperationException("ExchangeBehaviour is already disposed");
#endif
                return;
            }
            
            isDisposedSync = true;
            OnDisposedSync?.Invoke();

            m_CancellationSignalSource.Dispose();

            if (!isAsync)
            {
                DisposeAsync().Forget();
            }
            
            GC.SuppressFinalize(this);
        }

        public async UniTask DisposeAsync()
        {
            Dispose(true);
            try
            {
                await m_GeneralCompletionSource.Task;
            }
            catch (Exception e)
            {
                OnDisposedAsync?.Invoke(e);
                OnDisposedAsync = null;
                isDisposedAsync = true;
            }
            finally
            {
                if (!isDisposedAsync)
                {
                    isDisposedAsync = true;
                    OnDisposedAsync?.Invoke(null);
                    OnDisposedAsync = null;
                }
            }
        }

        public bool isAborted { get; private set; }
        public event Action OnAborted;
        public void Abort()
        {
            if (isAborted)
            {
                return;
            }
            isAborted = true;
            OnAborted?.Invoke();
            OnAborted = null;
            m_CancellationSignalSource.TryRaise();
        }

        public DisposableAction AddAbortedListenerOnce(Action action)
        {
            OnAborted += action;
            return DisposableAction.Create(() => OnAborted -= action);
        }

        public bool isDisposedSync { get; private set;  }
        public event Action OnDisposedSync;
    }
}