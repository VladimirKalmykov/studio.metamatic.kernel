using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    /// <summary>
    /// Режим работы моста.
    /// Default – текущее поведение (единоразовая таска).
    /// Buffering – все сообщения накапливаются в памяти.
    /// </summary>
    public enum BridgeMode
    {
        Default,
        Buffering
    }

    /// <summary>
    /// Типобезопасный аналог AsyncExchange.
    ///   • <typeparamref name="TIn"/>  – то, что поступает *внутрь* из внешнего кода.
    ///   • <typeparamref name="TOut"/> – то, что отдаётся *наружу* внутренним кодом.
    /// </summary>
    public readonly struct AsyncBridge<TIn, TOut> : IUniTaskAsyncDisposable, IDisposable
    {
        public static AsyncBridge<TIn, TOut> Create(
            BridgeCore<TIn, TOut>.AsyncAction action,
            BridgeMode mode = BridgeMode.Default,
            object[] deps = null)
        {
            var core = ParamLessPool<BridgeCore<TIn, TOut>>.GetShared();
            core.Init(action, mode, deps);
            return new AsyncBridge<TIn, TOut>(core);
        }

        private readonly BridgeCore<TIn, TOut> _core;
        private readonly int _version;
        private AsyncBridge(BridgeCore<TIn, TOut> core)
        {
            _core = core;
            _version = core.version;
        }

        private BridgeCore<TIn, TOut> Resolve() => _core.version == _version ? _core : null;

        public UniTask task => Resolve()?.task ?? UniTask.CompletedTask;
        public void Send(TIn value) => Resolve()?.Send(value);
        public UniTask<TOut> Receive(Signal token = default) =>
            Resolve()?.Receive(token) ?? UniTask.FromCanceled<TOut>();
        public UniTask<TOut> Duplex(TIn value, Signal token = default) =>
            Resolve()?.Duplex(value, token) ?? UniTask.FromCanceled<TOut>();

        public void Dispose()
        {
            var c = Resolve();
            if (c != null)
            {
                c.Reset();
                ParamLessPool<BridgeCore<TIn, TOut>>.ReturnShared(c);
            }
        }
        public async UniTask DisposeAsync()
        {
            var c = Resolve();
            try { if (c != null) await c.ResetAsync(); }
            finally { ParamLessPool<BridgeCore<TIn, TOut>>.ReturnShared(c); }
        }
    }

    /// <summary>
    /// Главная «сердцевина», основана на твоём DisposedAsyncExchangeCore, но обобщена.
    /// </summary>
    public class BridgeCore<TIn, TOut> : IUniTaskAsyncDisposable, IDisposable
    {
        /* -------------------- РЕЖИМ -------------------- */
        private BridgeMode _mode;
        private Queue<TIn>  _inputQueue;
        private Queue<TOut> _outputQueue;

        /* -------------------- ВНУТРЕННИЙ ОБМЕННИК -------------------- */
        public readonly struct Exchanger : IUniTaskAsyncDisposable
        {
            private readonly BridgeCore<TIn, TOut> _m;
            internal Exchanger(BridgeCore<TIn, TOut> master) => _m = master;

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

            public UniTask DisposeAsync() => _m.DisposeAsync();
        }

        /* -------------------- ПОЛЯ -------------------- */
        private object _inputCs;
        private object _outputCs;

        private Type _expectedInputType;
        private Type _expectedOutputType;

        private SignalSource _css;
        public Signal cancellationSignal => _css.Signal;

        public int  version { get; private set; }
        private readonly ReusableUniTaskCompletionSource _done = ReusableUniTaskCompletionSource.Create();
        public UniTask task => _done.Task;

        public bool  isDisposedAsync { get; private set; }
        public bool  isAborted      { get; private set; }
        public bool  isDisposedSync { get; private set; }
        public event Action OnAborted;
        public event Action OnDisposedSync;
        public event Action<Exception> OnDisposedAsync;

        public delegate UniTask AsyncAction(Exchanger x, Signal cts);
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
            _css    = SignalSource.Create();
            RunAsync();
        }

        private async void RunAsync()
        {
            try
            {
                await _action(new Exchanger(this), cancellationSignal);
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
            if (!fromAsync) _css.Dispose();
            unchecked { version++; }
            OnDisposedSync?.Invoke();
            isDisposedSync = false;

            _done.TrySetResult();
            _done.Reset();

            _inputCs = _outputCs = null;
            _expectedInputType = _expectedOutputType = null;
            _action = null;
            isDisposedAsync = false;

            if (_mode == BridgeMode.Buffering)
            {
                _inputQueue?.Clear();
                _outputQueue?.Clear();
            }
        }
        public void Reset() => Reset(false);
        public async UniTask ResetAsync()
        {
            _css.Dispose();
            try { await _done.Task; }
            finally { Reset(true); }
        }

        public void Abort()
        {
            if (isAborted) return;
            isAborted = true;
            OnAborted?.Invoke();
            _css.TryRaise();
        }

        /* IUniTaskAsyncDisposable / IDisposable */
        public void Dispose() { Dispose(false); }
        private void Dispose(bool async)
        {
            if (isDisposedSync) return;
            isDisposedSync = true;
            OnDisposedSync?.Invoke();
            _css.Dispose();
            if (!async) DisposeAsync().Forget();
            GC.SuppressFinalize(this);
        }
        public async UniTask DisposeAsync()
        {
            Dispose(true);
            try { await _done.Task; }
            catch (Exception e) { OnDisposedAsync?.Invoke(e); isDisposedAsync = true; }
            finally { if (!isDisposedAsync) { isDisposedAsync = true; OnDisposedAsync?.Invoke(null); } }
        }
    }
}
