using System;
using System.Runtime.CompilerServices;
using Codice.Client.BaseCommands.Download;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public readonly struct StepProgress
    {
        public readonly int Total;
        public readonly int Pass;
        
        public StepProgress(int total, int pass)
        {
            Total = total;
            Pass = pass;
        }
    }
    
    public readonly struct Progress
    {
        private readonly ProgressSourceImpl m_Impl;
        private readonly int m_Version;
        private bool isAlive => m_Impl != null && m_Impl.Version == m_Version;
        public bool isPending => isAlive && m_Impl.isPending;
        
        public Progress(ProgressSourceImpl impl)
        {
            m_Impl = impl;
            m_Version = impl.Version;
        }
        
        public StepProgress State => m_Impl.Version != m_Version ? new StepProgress(1, 1) : m_Impl.State;

        public StepProgress value
        {
            get
            {
                if (m_Impl.Version != m_Version)
                {
                    return new StepProgress(1, 1);
                }
                return m_Impl.State;
            }
        }
        
        public float ratio
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (m_Impl.Version != m_Version)
                {
                    return 1f;
                }
                if (m_Impl.LocalTotal == 0) return 1f;
                return (float)m_Impl.LocalPass / m_Impl.LocalTotal;
            }
        }

        public bool IsCompleted
        {
            get
            {
                if (m_Impl.Version != m_Version)
                {
                    return true;
                }
                return m_Impl.LocalPass >= m_Impl.LocalTotal;
            }
        }
        
        public DisposableAction SubscribeBeforeResetOnce(Action handler)
        {
            if (m_Impl.Version != m_Version)
            {
                return DisposableAction.Empty;
            }
            m_Impl.OnBeforeReset += handler;
            
            var impl = m_Impl;
            return DisposableAction.Create(() => impl.OnBeforeReset -= handler);
        }

        public IDisposable WatchPending(Action<bool> handler)
        {
            return !isAlive ? DisposableAction.Empty : m_Impl.WatchPending(handler);
        }

        public IDisposable WatchPendingAsync(Func<Signal, UniTaskVoid> handler)
        {
            return !isAlive ? DisposableAction.Empty : m_Impl.WatchPendingAsync(handler);
        }
    }
    
    

    public readonly struct ProgressSource: IDisposable
    {
        // ReSharper disable once InconsistentNaming
        public static ProgressSource Empty => new ProgressSource(null);
        public static ProgressSource Create()
        {
            var impl = ParamLessPool<ProgressSourceImpl>.GetShared();
            return new ProgressSource(impl);
        }
        
        // ReSharper disable once InconsistentNaming
        public Progress Progress => new Progress(m_Impl);

        public float ratio
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (m_Impl.Version != m_Version)
                {
                    return 1f;
                }
                var state = m_Impl.State;
                if (state.Total == 0) return 1f;
                return (float)state.Pass / state.Total;
            }
        }
        
        private readonly ProgressSourceImpl m_Impl;
        private readonly int m_Version;
        public ProgressSource(ProgressSourceImpl impl)
        {
            m_Impl = impl;
            m_Version = impl.Version;
        }
        
        public DisposableAction<Token96Bit> CreateStep()
        {
            if (m_Impl.Version != m_Version)
            {
                throw new InvalidOperationException("ProgressSource is stale");
            }
            return m_Impl.CreateStep();
        }
        
        public UniTask<T> CreateStep<T>(UniTask<T> inputTask)
        {
            if (m_Impl.Version != m_Version)
            {
                throw new InvalidOperationException("ProgressSource is stale");
            }
            return m_Impl.CreateStep(inputTask);
        }

        public IDisposable Bind(Progress progress)
        {
            if (m_Impl.Version != m_Version)
            {
                throw new InvalidOperationException("ProgressSource is stale");
            }
            return m_Impl.Bind(progress);
        }

        public void Dispose()
        {
            if (m_Impl.Version == m_Version)
            {
                ParamLessPool<ProgressSourceImpl>.ReturnShared(m_Impl);
            }
        }
    }

    public readonly struct ProgressBound
    {
        public readonly Progress Progress;
        public readonly IDisposable Subscription;

        public ProgressBound(Progress progress, IDisposable subscription)
        {
            Progress = progress;
            Subscription = subscription;
        }
    }

    public sealed class ProgressSourceImpl: IResettable
    {
        private static readonly SignedIndexedPool<bool> StepTokens = new(1000);
        
        internal int LocalTotal;
        internal int LocalPass;
        internal int Version;
        internal readonly SignedIndexedHashPool<ProgressBound> SubProgresses = new(50);
        internal bool isPreviouslyPending = false;
        internal event Action<bool> OnStepRecordChange;

        internal bool isPending
        {
            get
            {
                if (StepTokens.Count > 0)
                {
                    return true;
                }

                if (SubProgresses.Count > 0)
                {
                    foreach (var subProgress in SubProgresses)
                    {
                        if (subProgress.value.Progress.isPending)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        // ReSharper disable once InconsistentNaming
        public StepProgress State
        {
            get
            {
                var total = LocalTotal;
                var pass = LocalPass;
                foreach (var (subProgress, index, signature) in SubProgresses)
                {
                    var stepProgress = subProgress.Progress.State;
                    total += stepProgress.Total;
                    pass += stepProgress.Pass;
                }

                return new StepProgress(total, pass);
            }
        }
        
        /// <summary>
        /// Create manual step
        /// </summary>
        /// <returns></returns>
        internal DisposableAction<Token96Bit> CreateStep()
        {
            LocalTotal += 1;
            var token = StepTokens.Add(true);
            RecalcPending();
            return DisposableAction<Token96Bit>.Create(CompleteStep, token);
        }

        /// <summary>
        /// Create step from task
        /// </summary>
        /// <param name="inputTask"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        internal async UniTask<T> CreateStep<T>(UniTask<T> inputTask)
        {
            var step = CreateStep();
            try
            {
                var value = await inputTask;
                return value;
            }
            finally
            {
                step.Dispose();
            }
        }

        internal IDisposable Bind(Progress progress)
        {
            var bindToken = SubProgresses.Add(new ProgressBound(progress, progress.WatchPending(p => RecalcPending())));
            var disposer = DisposableAction<Token96Bit>.Create(ReleaseBind, bindToken);
            var subscribeReset = progress.SubscribeBeforeResetOnce(disposer.Dispose);
            
            RecalcPending();
            
            return new CompositeDisposable(subscribeReset, disposer);
        }


        internal DisposableAction WatchPending(Action<bool> handler)
        {
            var lastIsPending = isPending;
            handler(lastIsPending);
            
            OnStepRecordChange += Handler;
            
            return DisposableAction.Create(() =>
            {
                handler(false);
                OnStepRecordChange -= Handler;
            });
    
            void Handler(bool nextIsPending)
            {
                if (nextIsPending != lastIsPending)
                {
                    lastIsPending = nextIsPending;
                    handler(lastIsPending);
                }
            }
        }

        internal DisposableAction WatchPendingAsync(Func<Signal, UniTaskVoid> handler)
        {
            SignalSource signalSource = isPending ? SignalSource.Create() : SignalSource.Empty;

            var watcher = WatchPending(Handler);

            return DisposableAction.Create(() =>
            {
                signalSource.Dispose();
                watcher.Dispose();
            });
            
            void Handler(bool nextIsPending)
            {
                signalSource.Dispose();
                
                if (nextIsPending)
                {
                    signalSource = SignalSource.Create();
                    handler(signalSource.Signal).Forget();
                }
            }
        }

        private void ReleaseBind(Token96Bit bindToken)
        {
            if (SubProgresses.Remove(bindToken, out var subProgress))
            {
                if (subProgress.Progress.IsCompleted)
                {
                    var state = subProgress.Progress.State;
                    LocalTotal += state.Total;
                    LocalPass += state.Pass;
                    RecalcPending();
                }
            }
        }
        
        private void CompleteStep(Token96Bit token)
        {
            if (StepTokens.Remove(token))
            {
                 LocalPass++;
                 RecalcPending();
            }
        }

        private void RecalcPending()
        {
            var isCurrentlyPending = isPending;
            if (isPreviouslyPending != isCurrentlyPending)
            {
                isPreviouslyPending = isCurrentlyPending;
                OnStepRecordChange?.Invoke(isCurrentlyPending);
            }

            
            if (!isCurrentlyPending)
            {
                // Reset progress
                LocalTotal = 0;
                LocalPass = 0;
            }
        }
        
        public event Action OnBeforeReset;

        public void Reset()
        {
            OnBeforeReset?.Invoke();
            OnBeforeReset = null;
            StepTokens.Clear();
            SubProgresses.Clear();
            LocalPass = 0;
            LocalTotal = 0;
            unchecked
            {
                Version += 1;
            }
        }
    }
}