using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Metamatic
{

    /// <summary>
    /// Типобезопасный аналог AsyncExchange. В отличии от AsyncBridge позволяет 
    ///   • <typeparamref name="TIn"/>  – то, что поступает *внутрь* из внешнего кода.
    ///   • <typeparamref name="TOut"/> – то, что отдаётся *наружу* внутренним кодом.
    /// </summary>
    public readonly struct AsyncBridge2<TIn, TOut> : IUniTaskAsyncDisposable, IAbortable
    {
        public static AsyncBridge2<TIn, TOut> Create(
            BridgeCore2<TIn, TOut>.AsyncAction action,
            BridgeMode mode = BridgeMode.Default,
            object[] deps = null)
        {
            var core = ParamLessPool<BridgeCore2<TIn, TOut>>.GetShared();
            core.Init(action, mode, deps);
            return new AsyncBridge2<TIn, TOut>(core);
        }

        private readonly BridgeCore2<TIn, TOut> _core;
        private readonly int _version;
        private AsyncBridge2(BridgeCore2<TIn, TOut> core)
        {
            _core = core;
            _version = core.version;
        }

        private BridgeCore2<TIn, TOut> Resolve() => _core.version == _version ? _core : null;

        public UniTask task => Resolve()?.task ?? UniTask.CompletedTask;
        public void Send(TIn value) => Resolve()?.Send(value);
        public UniTask<TOut> Receive(Signal token = default) =>
            Resolve()?.Receive(token) ?? UniTask.FromCanceled<TOut>();
        public UniTask<TOut> Duplex(TIn value, Signal token = default) =>
            Resolve()?.Duplex(value, token) ?? UniTask.FromCanceled<TOut>();
        
        public async UniTask DisposeAsync()
        {
            var c = Resolve();
            try { if (c != null) await c.ResetAsync(); }
            finally { ParamLessPool<BridgeCore2<TIn, TOut>>.ReturnShared(c); }
        }

        public bool isAborted => Resolve()?.isAborted ?? true;
        public void Abort()
        {
            var c = Resolve();
            if (c != null)
            {
                c.Abort();
            }
        }

        public DisposableAction AddAbortedListenerOnce(Action action)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Главная «сердцевина», основана на твоём DisposedAsyncExchangeCore, но обобщена.
    /// </summary>
    public class BridgeCore2<TIn, TOut>
    {
        /* -------------------- РЕЖИМ -------------------- */
        private BridgeMode _mode;
        private Queue<TIn>  _inputQueue;
        private Queue<TOut> _outputQueue;

        /* -------------------- ВНУТРЕННИЙ ОБМЕННИК -------------------- */
        public readonly struct Exchanger
        {
            private readonly BridgeCore2<TIn, TOut> _m;
            internal Exchanger(BridgeCore2<TIn, TOut> master) => _m = master;

            public bool isDisposedAsync => _m.isDisposedAsync;
            public bool isAborted      => _m.isAborted;

            /* ---------- RECEIVE ---------- */
            public async UniTask<TIn> Receive()
            {
                if (_m._mode == BridgeMode.Buffering && _m._inputQueue.Count > 0)
                    return _m._inputQueue.Dequeue();

                DisposableAction reg = default;
                if (_m._inputCs == null)
                {
                    _m._expectedInputType = typeof(TIn);
                    var src = SafeUniTaskCompletionSource<TIn>.Create();
                    _m._inputCs = src;
                    reg = _m.cancellationSignal.Register(() => src.TrySetCanceled());
                }
                if (_m._inputCs is SafeUniTaskCompletionSource<TIn> cs)
                {
                    var val = await cs.Task;
                    reg.Dispose();
                    _m._inputCs = null;
                    return val;
                }
                throw new InvalidOperationException($"Expected {_m._expectedInputType}, but got {_m._inputCs?.GetType()}");
            }

            /* ---------- SEND ---------- */
            public void Send(TOut value)
            {
                if (_m._mode == BridgeMode.Buffering && _m._outputCs == null)
                {
                    _m._outputQueue.Enqueue(value);
                    return;
                }
                if (_m._outputCs == null)
                {
                    _m._expectedOutputType = typeof(TOut);
                    _m._outputCs = SafeUniTaskCompletionSource<TOut>.Create();
                }
                if (_m._outputCs is SafeUniTaskCompletionSource<TOut> cs)
                {
                    if (cs.Task.Status == UniTaskStatus.Pending)
                        cs.TrySetResult(value);
                    else
                        throw new InvalidOperationException("Output completion source is already resolved");
                }
                else
                    throw new InvalidOperationException($"Expected {_m._expectedOutputType}, but got {_m._outputCs?.GetType()}");
            }
            public void Send() => Send(default);

            /* ---------- DUPLEX ---------- */
            public async UniTask<TIn> Duplex(TOut value)
            {
                var t = await Receive();
                Send(value);
                return t;
            }
        }

        /* -------------------- ПОЛЯ -------------------- */
        private object _inputCs;
        private object _outputCs;

        private Type _expectedInputType;
        private Type _expectedOutputType;

        private AsyncCancellationSource _acs;
        public Signal cancellationSignal => _acs.Signal;
        public AsyncCancellation cancellation => _acs.Cancellation;

        public int  version { get; private set; }
        private readonly ReusableUniTaskCompletionSource _done = ReusableUniTaskCompletionSource.Create();
        public UniTask task => _done.Task;

        public bool  isDisposedAsync { get; private set; }
        public bool isGoingToBeResetAsync { get; private set; }
        public bool  isAborted      { get; private set; }
        public bool  isDisposedSync { get; private set; }
        public event Action OnAborted;

        public delegate UniTask AsyncAction(Exchanger x, AsyncCancellation cts);
        private AsyncAction _action;

        /* -------------------- ЖИЗНЕННЫЙ ЦИКЛ -------------------- */
        public void Init(AsyncAction action, BridgeMode mode, object[] deps = null)
        {
            _action = action;
            _mode   = mode;
            if (mode == BridgeMode.Buffering)
            {
                _inputQueue  = new Queue<TIn>();
                _outputQueue = new Queue<TOut>();
            }
            _acs    = AsyncCancellationSource.Create();
            RunAsync();
        }

        private async void RunAsync()
        {
            try
            {
                await _action(new Exchanger(this), cancellation);
            }
            catch (OperationCanceledException) { /* ok */ }
            catch (Exception e) { _done.TrySetException(e); }
            finally { _done.TrySetResult(); }
        }

        /* -------------------- SEND from внешнего -------------------- */
        public void Send(TIn value)
        {
            if (_mode == BridgeMode.Buffering && _inputCs == null)
            {
                _inputQueue.Enqueue(value);
                return;
            }
            if (_inputCs == null)
            {
                _expectedInputType = typeof(TIn);
                _inputCs = SafeUniTaskCompletionSource<TIn>.Create();
            }
            if (_inputCs is SafeUniTaskCompletionSource<TIn> cs)
            {
                if (cs.Task.Status == UniTaskStatus.Pending)
                    cs.TrySetResult(value);
                else
                    throw new InvalidOperationException("Input completion source is already resolved");
            }
            else
                throw new InvalidOperationException($"Expected {_expectedInputType}, but got {_inputCs?.GetType()}");
        }
        public void Send() => Send(default);

        /* -------------------- RECEIVE from внешнего -------------------- */
        public async UniTask<TOut> Receive(Signal ext = default)
        {
            if (_mode == BridgeMode.Buffering && _outputQueue.Count > 0)
                return _outputQueue.Dequeue();

            ext.ThrowIfRaised();
            cancellationSignal.ThrowIfRaised();

            DisposableAction regSelf = default, regExt = default;
            if (_outputCs == null)
            {
                _expectedOutputType = typeof(TOut);
                _outputCs = SafeUniTaskCompletionSource<TOut>.Create();
                regSelf = cancellationSignal.Register(() => ((SafeUniTaskCompletionSource<TOut>)_outputCs).TrySetCanceled());
            }
            if (_outputCs is SafeUniTaskCompletionSource<TOut> cs)
            {
                regExt = ext.Register(() => cs.TrySetCanceled());
                var v = await cs.Task;
                regSelf.Dispose();
                regExt.Dispose();
                _outputCs = null;
                return v;
            }
            throw new InvalidOperationException($"Expected {_expectedOutputType}, but got {_outputCs?.GetType()}");
        }

        public async UniTask<TOut> Duplex(TIn value, Signal token = default)
        {
            var t = await Receive(token);
            Send(value);
            return t;
        }

        /* -------------------- DISPOSE / RESET -------------------- */
        public void Reset(bool fromAsync = false)
        {
            if (!fromAsync) _acs.TryCancelSync();
            unchecked { version++; }
            isDisposedSync = false;

            _inputCs = _outputCs = null;
            _expectedInputType = _expectedOutputType = null;
            _action = null;
            isDisposedAsync = false;
            isGoingToBeResetAsync = false;
            
            _done.TrySetResult();
            _done.Reset();

            if (_mode == BridgeMode.Buffering)
            {
                _inputQueue?.Clear();
                _outputQueue?.Clear();
            }
        }
        public void Reset() => Reset(false);
        public async UniTask ResetAsync()
        {
            if (isGoingToBeResetAsync) return;
            isGoingToBeResetAsync = true;
            _acs.TryCancelAsync();
            try { await _done.Task; }
            finally { Reset(true); }
        }

        public void Abort()
        {
            if (isAborted) return;
            isAborted = true;
            OnAborted?.Invoke();
            _acs.TryCancelSync();
        }

        public void Dispose()
        {
            if (isDisposedSync) return;
            isDisposedSync = true;
            _acs.Dispose();
        }
    }
}
