using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    /// <summary>
    /// Experimental async observable that allows you to add async transformers to the source value
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DevStatus(DevRating.Experimental)]
    public readonly struct AsyncObservable<T> : IDisposable, IObservableValue<T>
    {
        private readonly AsyncObservableImpl<T> _impl;
        private readonly int _version;
        public bool isValid => _impl != null && _version == _impl.version;
        internal AsyncObservableImpl<T> impl => isValid ? _impl : null;
        
        
        public static AsyncObservable<T> Create(T initialValue = default)
        {
            return new AsyncObservable<T>(initialValue);
        }
        
        public static AsyncObservable<T> From(IObservableValue<T> source)
        {
            return new AsyncObservable<T>(source);
        }
        
        private AsyncObservable(T initialValue)
        {
            _impl = ParamLessPool<AsyncObservableImpl<T>>.GetShared();
            _impl.Init(initialValue);
            _version = _impl.version;
        }

        private AsyncObservable(IObservableValue<T> source)
        {
            _impl = ParamLessPool<AsyncObservableImpl<T>>.GetShared();
            _impl.Init(source);
            _version = _impl.version;
        }

        public void Dispose()
        {
            if (!isValid) return;
            _impl.Reset();
            ParamLessPool<AsyncObservableImpl<T>>.ReturnShared(_impl);
        }

        public T value
        {
            get
            {
                if (!isValid) throw new InvalidOperationException("AsyncObservable is no longer valid.");
                return _impl.Value;
            }
        }

        public DisposableAction Watch(Action<T> handler, bool skipInitial = false, Func<T, bool> validator = null)
        {
            if (!isValid) throw new InvalidOperationException("AsyncObservable is no longer valid.");
            return _impl.Watch(handler, skipInitial, validator);
        }

        // Прокси-методы добавления трансформеров, чтобы не лезть в impl снаружи

        public IDisposable AddTransform(Func<T, T> sync)
        {
            if (!isValid) throw new InvalidOperationException("AsyncObservable is no longer valid.");
            return _impl.AddTransform(sync);
        }

        public IDisposable AddTransform(Func<T, Signal, UniTask<T>> async)
        {
            if (!isValid) throw new InvalidOperationException("AsyncObservable is no longer valid.");
            return _impl.AddTransform(async);
        }

        // Зависимый трансформер: слушаем внешний observable и переигрываем цепочку
        public IDisposable AddTransform<TDep>(IObservableValue<TDep> dep, Func<T, TDep, T> sync)
        {
            if (!isValid) throw new InvalidOperationException("AsyncObservable is no longer valid.");
            return _impl.AddTransform(dep, sync);
        }

        public IDisposable AddTransform<TDep>(IObservableValue<TDep> dep, Func<T, TDep, Signal, UniTask<T>> async)
        {
            if (!isValid) throw new InvalidOperationException("AsyncObservable is no longer valid.");
            return _impl.AddTransform(dep, async);
        }

        public IObservableValue<TOut> Select<TOut>(Func<T, TOut> selector, bool compare, Func<TOut, TOut, bool> comparator = null)
        {
            return new SelectedObservable<T, TOut>(this, selector, compare, comparator);
        }
    }

    public sealed class AsyncObservableImpl<T> : IResettable
    {
        // Внутренний юнит трансформера с «могилкой» и необязательной подпиской на dep
        private struct Entry
        {
            public bool Alive;
            public bool IsAsync;
            public Func<T, T> Sync;
            public Func<T, Signal, UniTask<T>> Async;
            public DisposableAction DepSubscription; // может быть Empty
        }

        public delegate UniTask<T> AsyncTransformDelegate(T currentValue, Signal cancellationSignal);
        public delegate T SyncTransformDelegate(T currentValue);

        private T _lastInput;
        private T _computed;
        public T Value => _computed;

        public int version { get; private set; }

        private readonly List<Entry> _entries = new();
        private int _aliveCount;
        private SignalSource _reset = default;

        // Подписчики на выход значения
        private readonly List<Action<T>> _watchers = new();
        private int _watchersAlive;

        public void Init(T initialValue)
        {
            _lastInput = initialValue;
            Compute(initialValue).Forget();
        }

        public void Init(IObservableValue<T> observableValue)
        {
            // подписаться и сразу стартануть на текущее
            var d = observableValue.Watch(OnInputChanged, skipInitial: false);
            // сохраним подписку как зависимость входа
            _inputSubscription = d;
        }

        private DisposableAction _inputSubscription;

        public void Reset()
        {
            _inputSubscription.Dispose();
            // Принудительно отменяем текущий прогон
            _reset.TryRaise();
            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry.DepSubscription.isEmpty) continue;
                entry.DepSubscription.Dispose();
                entry.DepSubscription = DisposableAction.Empty;
                entry.Alive = false;
                _entries[i] = entry;
            }
            _aliveCount = 0;
            _entries.Clear();

            // watcher’ы нам не принадлежат — только чистим список
            _watchers.Clear();
            _watchersAlive = 0;

            version++;
            _reset.Dispose();
            _reset = default;
        }

        private void OnInputChanged(T value)
        {
            _lastInput = value;
            Compute(value).Forget();
        }

        // Публичные AddTransform — без лишних аллокаций в hot-path
        public IDisposable AddTransform(Func<T, T> sync)
        {
            var idx = _entries.Count;
            _entries.Add(new Entry
            {
                Alive = true,
                IsAsync = false,
                Sync = sync,
                DepSubscription = DisposableAction.Empty
            });
            _aliveCount++;

            return MakeRemoval(idx);
        }

        public IDisposable AddTransform(Func<T, Signal, UniTask<T>> async)
        {
            var idx = _entries.Count;
            _entries.Add(new Entry
            {
                Alive = true,
                IsAsync = true,
                Async = async,
                DepSubscription = DisposableAction.Empty
            });
            _aliveCount++;

            return MakeRemoval(idx);
        }

        public IDisposable AddTransform<TDep>(IObservableValue<TDep> dep, Func<T, TDep, T> sync)
        {
            // кешируем текущую dep, чтобы не дергать dep.value в горячем цикле
            var currentDep = dep.value;
            void OnDepChanged(TDep d)
            {
                currentDep = d;
                _reset.TryRaise();
                // перезапуск с последним входом
                Compute(_lastInput).Forget();
            }

            var sub = dep.Watch(OnDepChanged, skipInitial: true);

            var idx = _entries.Count;
            _entries.Add(new Entry
            {
                Alive = true,
                IsAsync = false,
                Sync = (x) => sync(x, currentDep),
                DepSubscription = sub
            });
            _aliveCount++;

            return MakeRemoval(idx);
        }

        public IDisposable AddTransform<TDep>(IObservableValue<TDep> dep, Func<T, TDep, Signal, UniTask<T>> async)
        {
            var currentDep = dep.value;
            void OnDepChanged(TDep d)
            {
                currentDep = d;
                _reset.TryRaise();
                Compute(_lastInput).Forget();
            }

            var sub = dep.Watch(OnDepChanged, skipInitial: true);

            var idx = _entries.Count;
            _entries.Add(new Entry
            {
                Alive = true,
                IsAsync = true,
                Async = (x, s) => async(x, currentDep, s),
                DepSubscription = sub
            });
            _aliveCount++;

            return MakeRemoval(idx);
        }

        private IDisposable MakeRemoval(int idx)
        {
            var disposed = 0;
            return new DisposableAction(() =>
            {
                if (System.Threading.Interlocked.Exchange(ref disposed, 1) == 1) return;
                if ((uint)idx >= (uint)_entries.Count) return;
                var entry = _entries[idx];
                if (!entry.Alive) return;

                // отписаться от dep, если была
                entry.DepSubscription.Dispose();
                entry.DepSubscription = DisposableAction.Empty;

                entry.Alive = false;
                _entries[idx] = entry;
                _aliveCount--;
                _reset.TryRaise();
                Compute(_lastInput).Forget();

                if (_aliveCount * 2 < _entries.Count)
                    Compact();
            });
        }

        private void Compact()
        {
            int w = 0;
            for (int r = 0; r < _entries.Count; r++)
            {
                if (!_entries[r].Alive)
                {
                    // от греха — гарантируем снятие dep
                    _entries[r].DepSubscription.Dispose();
                    continue;
                }
                if (w != r) _entries[w] = _entries[r];
                w++;
            }
            _entries.RemoveRange(w, _entries.Count - w);
        }

        // Подписки на итоговое значение
        public DisposableAction Watch(Action<T> handler, bool skipInitial = false, Func<T, bool> validator = null)
        {
            if (!skipInitial)
            {
                if (validator == null || validator(_computed))
                    handler?.Invoke(_computed);
            }

            var idx = _watchers.Count;
            _watchers.Add(handler);
            _watchersAlive++;

            var disposed = 0;
            return new DisposableAction(() =>
            {
                if (System.Threading.Interlocked.Exchange(ref disposed, 1) == 1) return;
                if ((uint)idx >= (uint)_watchers.Count) return;
                if (_watchers[idx] == null) return;
                _watchers[idx] = null;
                _watchersAlive--;
                if (_watchersAlive * 2 < _watchers.Count)
                    CompactWatchers();
            });
        }

        private void CompactWatchers()
        {
            int w = 0;
            for (int r = 0; r < _watchers.Count; r++)
            {
                if (_watchers[r] == null) continue;
                if (w != r) _watchers[w] = _watchers[r];
                w++;
            }
            _watchers.RemoveRange(w, _watchers.Count - w);
        }

        private async UniTaskVoid Compute(T input)
        {
            try
            {
                _reset.TryRaise();
                _reset = SignalSource.Create();

                var next = input;

                // Линейный проход без LINQ и без аллокаций
                for (int i = 0; i < _entries.Count; i++)
                {
                    if (!_entries[i].Alive) continue;
                    if (!_entries[i].IsAsync)
                    {
                        var f = _entries[i].Sync;
                        next = f != null ? f(next) : next;
                    }
                    else
                    {
                        var f = _entries[i].Async;
                        if (f != null)
                            next = await f(next, _reset.Signal);
                    }
                }

                _computed = next;
                unchecked { version++; }

                // оповещение подписчиков
                for (int i = 0; i < _watchers.Count; i++)
                {
                    var w = _watchers[i];
                    if (w == null) continue;
                    w(_computed);
                }
            }
            catch (OperationCanceledException)
            {
                // ожидаемо
            }
            catch (Exception e)
            {
                Console.Error("AsyncObservable", e);
            }
        }
    }
}
