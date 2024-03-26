using System;
using System.Threading;
using Codice.Client.Common.Connection.ServerAlias;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public class MicroStoreAlias<T>: IStore<T>
        where T : struct
    {
        private readonly ObservableWritableValue<MicroStore<T>> m_MicroStore = new ();
        private readonly ExclusiveExecution<string> m_IdExecution;
        public MicroStore<T> store => m_MicroStore.value;
        public T value => m_MicroStore.value.value;
        public MicroStoreAlias(string id)
        {
            m_IdExecution = new ExclusiveExecution<string>(HandleIdChange);
            m_IdExecution.Handle(id);
        }
        private int m_AcquireCount;
        public event Action OnDidDispose;
        private void HandleIdChange(string id, CancellationToken token)
        {
            var store = MicroStore<T>.Id(id);
            token.Register(() =>
            {
                store.Dispose();
            });
            m_MicroStore.value = store;
        }
        
        public void SetId(string id)
        {
            m_IdExecution.Handle(id);
        }

        public async UniTask WithStoreCycled(Func<MicroStore<T>, CancellationToken, UniTask> action, CancellationToken cancellationToken = default)
        {
            CancellationTokenSource localCancellation = null;
            var unsubscribe = m_MicroStore.Subscribe(store =>
            {
                // Cancel the previous token
                var previousCancellation = localCancellation;
                localCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                previousCancellation?.Cancel();
            });

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await action(m_MicroStore.value, localCancellation.Token);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    // Ignore only if it's the external token that was cancelled
                    cancellationToken.ThrowIfCancellationRequested();
                    // It that as an internal cancellation, we should continue cycling
                }
                catch
                {
                    unsubscribe.Invoke();
                    localCancellation?.Dispose();
                    throw;
                }
            }
            unsubscribe.Invoke();
            localCancellation?.Dispose();
        }
        
        public async UniTask WithStore(Func<MicroStore<T>, CancellationToken, UniTask> action, CancellationToken cancellationToken = default)
        {
            CancellationTokenSource localCancellation = null;
            var unsubscribe = m_MicroStore.Subscribe(store =>
            {
                // Cancel the previous token
                var previousCancellation = localCancellation;
                localCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                previousCancellation?.Cancel();
            });

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await action(m_MicroStore.value, localCancellation.Token);
                    break;
                }
                catch (OperationCanceledException)
                {
                    // Ignore only if it's the external token that was cancelled
                    cancellationToken.ThrowIfCancellationRequested();
                    // It that as an internal cancellation, we should continue cycling
                }
                catch
                {
                    unsubscribe.Invoke();
                    localCancellation?.Dispose();
                    throw;
                }
            }
            unsubscribe.Invoke();
            localCancellation?.Dispose();
        }
        
        public async UniTask<TX> WithStore<TX>(Func<MicroStore<T>, CancellationToken, UniTask<TX>> action, CancellationToken cancellationToken = default)
        {
            CancellationTokenSource localCancellation = null;
            var unsubscribe = m_MicroStore.Subscribe(store =>
            {
                // Cancel the previous token
                var previousCancellation = localCancellation;
                localCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                previousCancellation?.Cancel();
            });

            TX result = default(TX);
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    result = await action(m_MicroStore.value, localCancellation.Token);
                    break;
                }
                catch (OperationCanceledException)
                {
                    // Ignore only if it's the external token that was cancelled
                    cancellationToken.ThrowIfCancellationRequested();
                    // It that as an internal cancellation, we should continue cycling
                }
                catch
                {
                    unsubscribe.Invoke();
                    localCancellation?.Dispose();
                    throw;
                }
            }
            unsubscribe.Invoke();
            localCancellation?.Dispose();
            return result;
        }

        #region Interface Methods
        public UniTask Pull(CancellationToken cancellationToken = default)
        {
            return WithStore((store, token) => store.Pull(token), cancellationToken);
        }

        public UniTask Save(CancellationToken cancellationToken = default)
        {
            return WithStore((store, token) => store.Save(token), cancellationToken);
        }

        public UniTask WaitUntilIdle(CancellationToken cancellationToken = default)
        {
            return WithStore((store, token) => store.WaitUntilIdle(token), cancellationToken);
        }

        public UniTask WaitUntilSynchronized(CancellationToken cancellationToken = default)
        {
            return WithStore((store, token) => store.WaitUntilSynchronized(token), cancellationToken);
        }

        public void CommitChanges()
        {
            m_MicroStore.value.CommitChanges();
        }

        public UniTask CommitChanges(bool save, CancellationToken cancellationToken = default)
        {
            return WithStore((store, token) => store.CommitChanges(save, token), cancellationToken);
        }

        public UniTask<IDisposable> Lock(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void SetValue(T nextValue)
        {
            m_MicroStore.value.SetValue(nextValue);
        }

        public void Mutate(IStore<T>.MutateHandler mutationHandler)
        {
            m_MicroStore.value.Mutate(mutationHandler);
        }

        public void TransformValue(Func<T, T> transformHandler)
        {
            m_MicroStore.value.TransformValue(transformHandler);
        }

        public DisposableAction Subscribe(Action<T> handler)
        {
            Action unsubscribeData = null;
            var unsubscribeStore = m_MicroStore.Subscribe(store =>
            {
                unsubscribeData?.Invoke();
                unsubscribeData = store.Subscribe(handler);
            });
            return new DisposableAction(() =>
            {
                unsubscribeData?.Invoke();
                unsubscribeStore.Invoke();
            });
        }

        public Action SubscribeMetadata(Action<T, IStore.Metadata> handler)
        {
            Action unsubscribeData = null;
            var unsubscribeStore = m_MicroStore.Subscribe(store =>
            {
                unsubscribeData?.Invoke();
                unsubscribeData = store.SubscribeMetadata(handler);
            });
            return () =>
            {
                unsubscribeData?.Invoke();
                unsubscribeStore.Invoke();
            };
        }

        public UniTask Pull(bool populate = false, CancellationToken cancellationToken = default, T defaultValue = default)
        {
            return WithStore((store, token) => store.Pull(populate, token, defaultValue), cancellationToken);
        }

        public UniTask<T> WaitUntilValueIs(Func<T, bool> condition, CancellationToken cancellationToken = default)
        {
            return WithStore<T>((store, token) => store.WaitUntilValueIs(condition, token), cancellationToken);
        }

        public UniTask CommitChanges(Func<T, T, T> commitHandler, bool save = false, CancellationToken cancellationToken = default)
        {
            return WithStore((store, token) => store.CommitChanges(commitHandler, save, token), cancellationToken);
        }

        public UniTask Populate(Func<T, T> populateHandler, bool save = false, CancellationToken cancellationToken = default)
        {
            return WithStore((store, token) => store.Populate(populateHandler, save, token), cancellationToken);
        }

        public IDisposable Checkout(string branchName = "default", bool locked = false)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var globalToken = cancellationTokenSource.Token;
            WithStoreCycled(async (nextStore, token) =>
            {
                var branch = nextStore.Checkout(branchName, locked);
                await token;
                branch.Dispose();
            }, globalToken);
            return new DisposableCallback(() =>
            {
                cancellationTokenSource.Cancel();
            });
        }

        #endregion

        public void Dispose()
        {
            Release();
        }

        public void Acquire()
        {
            m_AcquireCount++;
        }

        public void Release()
        {
            m_AcquireCount--;
            if (m_AcquireCount == 0)
            {
                m_IdExecution.Dispose();
                OnDidDispose?.Invoke();
                OnDidDispose = null;
            }
        }
    }
}