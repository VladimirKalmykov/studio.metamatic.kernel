using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public readonly struct DisposableOperationLegacy: IDisposable
    {
        public static DisposableOperationLegacy Create(Func<Signal, UniTask> operationAction)
        {
            var instance = DisposableOperationImpl.Get(operationAction);
            return new DisposableOperationLegacy(instance);
        }
        // ReSharper disable once InconsistentNaming
        public static DisposableOperationLegacy Empty => default;
        
        private readonly DisposableOperationImpl m_Impl;
        private readonly int m_Version;

        public DisposableOperationLegacy(DisposableOperationImpl impl)
        {
            m_Impl = impl;
            m_Version = impl.version;
        }

        public void Dispose()
        {
            if (m_Impl == null || m_Impl.version != m_Version) return;
            m_Impl.Return();
        }
    }

    public class DisposableOperationImpl: IResettable, IDisposable
    {
        public static DisposableOperationImpl Get(Func<Signal, UniTask> operationAction)
        {
            var instance = ParamLessPool<DisposableOperationImpl>.GetShared();
            instance.Init(operationAction, () =>
            {
                ParamLessPool<DisposableOperationImpl>.ReturnShared(instance);
            }).Forget();
            return instance;
        }

        public int version { get; private set; }
        private SignalSource m_CancellationSignalSource;
        private Action m_OnReturn;
        private async UniTaskVoid Init(Func<Signal, UniTask> operationAction, Action onReturn)
        {
            m_OnReturn = onReturn;
            m_CancellationSignalSource = SignalSource.Create();
            try
            {
                await operationAction(m_CancellationSignalSource.Signal);
            }
            catch (OperationCanceledException)
            {
                // Skip
            }
            catch (Exception e)
            {
                Console.Error("Disposable operation", e);
            }
        }

        /// <summary>
        /// Return to the pool
        /// </summary>
        public void Return()
        {
            m_OnReturn?.Invoke();
        }

        /// <summary>
        /// Reset will be called automatically from the pool when the instance is returned.
        /// </summary>
        public void Reset()
        {
            unchecked
            {
                version++;
            }
            m_OnReturn = null;
            m_CancellationSignalSource.TryRaise();
        }

        /// <summary>
        /// Dispose will be called automatically from the pool when the instance is released.
        /// </summary>
        public void Dispose()
        {
            Reset();
        }
    }
}