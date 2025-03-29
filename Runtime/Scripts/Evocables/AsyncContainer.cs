using System;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public readonly struct AsyncContainer<T>: IDisposable
    {
        public static AsyncContainer<T> Create<TData>(Func<Action<T>, TData, Signal, UniTask> initializer, TData data)
        {
            var impl = ParamLessPool<DisposableContainerImpl<T>>.GetShared();
            impl.Initialize(initializer, data);
            return new AsyncContainer<T>(impl);
        }
        
        private readonly DisposableContainerImpl<T> m_Impl;
        private readonly int m_Version;
        
        public bool isAlive => IsValid();
        
        public AsyncContainer(DisposableContainerImpl<T> impl)
        {
            m_Impl = impl;
            m_Version = m_Impl?.version ?? -1;
        }

        public PreservedTask<T>.Awaiter GetAwaiter()
        {
            if (!IsValid())
            {
                return new PreservedTask<T>.Awaiter(Signal.Raised);
            }

            return m_Impl.PreservedTask.GetAwaiter();
        }
        
        public UniTask<T> ToUniTask(Signal cancellationSignal = default)
        {
            if (!IsValid())
            {
                return UniTask.FromCanceled<T>();
            }

            return m_Impl.PreservedTask.ToUniTask(cancellationSignal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsValid()
        {
            return m_Version >= 0 && m_Impl.version == m_Version;
        }

        public void Dispose()
        {
            if (IsValid())
            {
                ParamLessPool<DisposableContainerImpl<T>>.ReturnShared(m_Impl);
            }
        }
    }

    public class DisposableContainerImpl<T>: IResettable
    {
        private SignalSource m_ReturnSignalSource = default;
        private SafeUniTaskCompletionSource<T> m_CompletionSource;
        internal PreservedTask<T> PreservedTask = default;
        public int version { get; private set; } = 0;
        
        public void Initialize<TData>(Func<Action<T>, TData, Signal, UniTask> initializer, TData data)
        {
            InternalInitialize(initializer, data).Forget();
        }
        
        private async UniTaskVoid InternalInitialize<TData>(Func<Action<T>, TData, Signal, UniTask> initializer, TData data)
        {
            m_CompletionSource = SafeUniTaskCompletionSource<T>.Create();
            m_ReturnSignalSource = SignalSource.Create();
            PreservedTask = new PreservedTask<T>(m_CompletionSource.Task);
            try
            {
                await initializer(m_CompletionSource.TrySetResultSilently, data, m_ReturnSignalSource.Signal);
            }
            catch (OperationCanceledException)
            {
                m_CompletionSource.TrySetCanceledAndForget();
            }
            catch (Exception e)
            {
                Console.Error("DisposableContainer", e);
                m_CompletionSource.TrySetException(e);
            }
        }

        public void Reset()
        {
            unchecked
            {
                version++;
            }
            m_ReturnSignalSource.Dispose();
            PreservedTask.Dispose();
        }
    }
}