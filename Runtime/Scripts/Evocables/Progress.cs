using System;
using System.Runtime.CompilerServices;
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
    
    public readonly struct Progress: IObservableValue<StepProgress>
    {
        private readonly ProgressSourceImpl m_Impl;
        private readonly int m_Version;
        private bool isAlive => m_Impl != null;
        
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
        
        public DisposableAction Watch(Action<StepProgress> onChange, bool skipInitial = false, Func<StepProgress, bool> validator = null)
        {
            if (m_Impl.Version != m_Version)
            {
                if (!skipInitial)
                {
                    onChange(new StepProgress(1, 1));
                }
                return DisposableAction.Empty;
            }

            if (validator != null)
            {
                return TrustedWatchWithValidator(onChange, validator, skipInitial);
            }

            if (!skipInitial)
            {
                onChange(m_Impl.State);
            }

            m_Impl.OnChange += onChange;
            var impl = m_Impl;
            return DisposableAction.Create(() => impl.OnChange -= onChange);
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
        
        private DisposableAction TrustedWatchWithValidator(Action<StepProgress> onChange, Func<StepProgress, bool> validator, bool skipInitial = false)
        {
            var currentValue = new StepProgress(m_Impl.LocalTotal, m_Impl.LocalPass);
            if (!skipInitial && validator(currentValue))
            {
                onChange(currentValue);
            }

            m_Impl.OnChange += Handler;
            var impl = m_Impl;
            return DisposableAction.Create(() => impl.OnChange -= Handler);
            
            void Handler(StepProgress p)
            {
                if (validator(p))
                {
                    onChange(p);
                }
            };
        }
    }
    
    

    public readonly struct ProgressSource: IDisposable
    {
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

    public sealed class ProgressSourceImpl: IResettable
    {
        private static readonly SignedIndexedPool<bool> StepTokens = new(1000);
        
        internal int LocalTotal;
        internal int LocalPass;
        internal int Version;
        internal event Action<StepProgress> OnChange;
        internal readonly SignedIndexedHashPool<(Progress, IDisposable)> SubProgresses = new(50);

        // ReSharper disable once InconsistentNaming
        public StepProgress State
        {
            get
            {
                var total = LocalTotal;
                var pass = LocalPass;
                foreach (var (subProgress, index, signature) in SubProgresses)
                {
                    var stepProgress = subProgress.Item1.State;
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
            EmitProgressChange();
            var token = StepTokens.Add(true);
            return DisposableAction<Token96Bit>.Create(CompleteTask, token);
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
            var bindToken = SubProgresses.Add((progress, progress.Watch(EmitProgressChange)));
            var disposer = DisposableAction<Token96Bit>.Create(ReleaseBind, bindToken);
            var subscribeReset = progress.SubscribeBeforeResetOnce(disposer.Dispose);
            
            return new CompositeDisposable(subscribeReset, disposer);
        }

        private void EmitProgressChange(StepProgress subProgress)
        {
            EmitProgressChange();
        }
        
        private void EmitProgressChange()
        {
            OnChange?.Invoke(State);
        }

        private void ReleaseBind(Token96Bit bindToken)
        {
            if (SubProgresses.Remove(bindToken, out var subProgress))
            {
                subProgress.Item2.Dispose();
                if (subProgress.Item1.IsCompleted)
                {
                    var state = subProgress.Item1.State;
                    LocalTotal += state.Total;
                    LocalPass += state.Pass;
                }
            }
        }
        
        private void CompleteTask(Token96Bit token)
        {
            if (StepTokens.Remove(token))
            {
                LocalPass++;
                EmitProgressChange();
            }
        }
        
        public event Action OnBeforeReset;

        public void Reset()
        {
            OnBeforeReset?.Invoke();
            OnBeforeReset = null;
            unchecked
            {
                Version += 1;
                SubProgresses.Clear();
                LocalPass = 0;
                LocalTotal = 0;
            }
        }
    }
}