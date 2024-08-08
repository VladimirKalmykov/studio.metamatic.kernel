using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public class ExchangeBehaviour : IDisposable, IUniTaskAsyncDisposable
    {
        public class Exchanger
        {
            private readonly ExchangeBehaviour m_Master;
            public bool isDisposedAsync => m_Master.isDisposedAsync;
            internal Exchanger(ExchangeBehaviour master)
            {
                m_Master = master;
            }
            
            /// <summary>
            /// Wait for the input value
            /// </summary>
            /// <typeparam name="TInput"></typeparam>
            /// <returns></returns>
            /// <exception cref="InvalidOperationException"></exception>
            public async UniTask<TInput> Receive<TInput>()
            {
                CancellationTokenRegistration registration = default;
                if (m_Master.m_InputCompletionSource == null)
                {
                    m_Master.m_ExpectedInputType = typeof(TInput);
                    var source = new UniTaskCompletionSource<TInput>();
                    m_Master.m_InputCompletionSource = source;
                    registration = m_Master.cancellationToken.Register(() =>
                    {
                        source.TrySetCanceled();
                    });
                }
                if (m_Master.m_InputCompletionSource is UniTaskCompletionSource<TInput> inputCompletionSource)
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
            
            public UniTask Duplex<TInput>()
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
                    var outputSource = new UniTaskCompletionSource<TOutput>();
                    m_Master.m_OutputCompletionSource = outputSource;
                }
                
                /* Check that m_OutputCompletionSource is not resolved yet */
                if (m_Master.m_OutputCompletionSource is UniTaskCompletionSource<TOutput> outputCompletionSource)
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
            
            /// <summary>
            /// Dispose the exchange behaviour. Call this method at the end of the exchange.
            /// TODO: Consider to call this method automatically at the end of the exchange
            /// </summary>
            public void SelfDispose()
            {
                m_Master.Dispose();
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
        private CancellationTokenSource m_CancellationTokenSource;
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
        private readonly UniTaskCompletionSource m_GeneralCompletionSource = new UniTaskCompletionSource();
        /// <summary>
        /// Public reference to the cancellation token (that is called when the behaviour is disposed)
        /// </summary>
        public CancellationToken cancellationToken => m_CancellationTokenSource.Token;
        /// <summary>
        /// Disposing is called asynchronously (DisposeAsync() method is called), otherwise it is called synchronously (Dispose() method is called)
        /// </summary>
        public bool isDisposedAsync { get; private set; }
        public UniTask task => m_GeneralCompletionSource.Task;
        

        public delegate UniTask AsyncAction(Exchanger exchange, CancellationToken cancellationToken);

        public static ExchangeBehaviour Run(AsyncAction action)
        {
            return new ExchangeBehaviour(action);
        }

        private AsyncAction m_Action;

        public ExchangeBehaviour(AsyncAction action)
        {
            m_Action = action;
            m_CancellationTokenSource = new CancellationTokenSource();
            RunAsync();
        }

        ~ExchangeBehaviour()
        {
            Dispose(true);
        }

        private async void RunAsync()
        {
            try
            {
                await m_Action(new Exchanger(this), m_CancellationTokenSource.Token);
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
                var source = new UniTaskCompletionSource<TInput>();
                m_InputCompletionSource = source;
            }
            if (m_InputCompletionSource is UniTaskCompletionSource<TInput> inputCompletionSource)
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
        
        public async UniTask<TOutput> Receive<TOutput>(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            cancellationToken.ThrowIfCancellationRequested();
            CancellationTokenRegistration registrationSelf = default;
            CancellationTokenRegistration registrationExternal = default;
            if (m_OutputCompletionSource == null)
            {
                m_ExpectedOutputType = typeof(TOutput);
                var source = new UniTaskCompletionSource<TOutput>();
                m_OutputCompletionSource = source;
                registrationSelf = cancellationToken.Register(() =>
                {
                    source.TrySetCanceled();
                });
                registrationExternal = token.Register(() =>
                {
                    source.TrySetCanceled();
                });
            }
            if (m_OutputCompletionSource is UniTaskCompletionSource<TOutput> outputCompletionSource)
            {
                var value = await outputCompletionSource.Task;
                registrationSelf.Dispose();
                registrationExternal.Dispose();
                m_OutputCompletionSource = null;
                return value;
            }
            throw new InvalidOperationException($"Expected output type is {m_ExpectedOutputType}, but got {m_OutputCompletionSource.GetType()}");
        }
        
        public async UniTask Receive(CancellationToken token = default)
        {
            await Receive<object>(token);
        }

        public UniTask<TOutput> Duplex<TInput, TOutput>(TInput value,
            CancellationToken token = default)
        {
            var receiveTask = Receive<TOutput>(token);
            Send(value);
            return receiveTask;
        }
        
        public UniTask Duplex<TInput>(TInput value,
            CancellationToken token = default)
        {
            return Duplex<TInput, object>(value, token);
        }
        
        public UniTask<TOutput> Duplex<TOutput>(CancellationToken token = default)
        {
            return Duplex<object, TOutput>(default, token);
        }
        
        public UniTask Duplex(CancellationToken token = default)
        {
            return Duplex<object, object>(default, token);
        }

        #endregion

        public void Dispose()
        {
            Dispose(false);
        }

        public void Dispose(bool isCalledFromFinalizer, bool isAsync = false)
        {
            if (isCalledFromFinalizer)
            {
                throw new InvalidOperationException("You should dispose ExchangeBehaviour manually");
            }

            if (m_CancellationTokenSource.IsCancellationRequested)
            {
#if DEBUG
                throw new InvalidOperationException("ExchangeBehaviour is already disposed");
#endif
                return;
            }
            
            isDisposedAsync = isAsync;

            m_CancellationTokenSource.Cancel();
            GC.SuppressFinalize(this);
        }

        public async UniTask DisposeAsync()
        {
            Dispose(false, true);
            await m_GeneralCompletionSource.Task;
        }
    }
}