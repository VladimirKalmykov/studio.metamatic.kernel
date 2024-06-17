using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Metamatic.StorageDriver;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace Metamatic
{
    public class Store: IDisposable
    {
        protected CancellationTokenSource m_DisposeCancellationTokenSource = new ();
        public CancellationToken disposeCancellationToken => m_DisposeCancellationTokenSource.Token;
        public bool isDisposed { get; private set; }
        public UniTask asyncDisposingTask { get; protected set; }
        protected Store()
        {
            
        }
        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }
            isDisposed = true;
            m_DisposeCancellationTokenSource.Cancel();
        }
    }
    public class Store<T>: Store, IStore<T>
    {
        /// <summary>
        /// When store is async disposing, it will be added to this dictionary until it will be disposed
        /// If next instance of the same type will be created, it will be disposed immediately
        /// </summary>
        protected Dictionary<(Type, string), CancellationTokenSource> m_AsyncDisposingCancellationTokens = new();
        private class Branch
        {
            private readonly Store<T> m_Store;
            public readonly string name;
            private T m_Value;
            public T value
            {
                get => m_Value;
                set => SetValue(value);
            }
            public event Action<T> OnChange;

            public bool isDirty;
            public Branch(Store<T> store, string name, T value, bool isDirty)
            {
                this.name = name;
                this.m_Value = value;
                this.isDirty = isDirty;
                m_Store = store;
            }
            
            public void SetValue(T value)
            {
                m_Value = value;
                isDirty = true;
            }
            
            public DisposableAction Subscribe(Action<T> action)
            {
                OnChange += action;
                action(m_Value);
                return new DisposableAction(() =>
                {
                    OnChange -= action;
                });
            }
        }

        public readonly string address;
        public readonly Repository repository;
        private readonly Dictionary<string, Branch> m_Branches = new();
        private readonly MicroState<Branch> m_CurrentBranch = new();
        private readonly DeferLock m_DeferLock = new DeferLock();
        public bool synchronized { get; private set; } = false;
        public UniTask synchronizationTask { get; private set; }
        public long version { get; private set; } = 0;
        /* Here will be property that will be increased when Inquire method will be called */
        private List<object> m_Inquiriences = new();
        private UniTaskCompletionSource m_SynchronizedTaskCompletionSource = new();
        
        
        internal Store(Repository repository, string address)
        {
            this.repository = repository;
            this.address = address;
            m_Branches["default"] = new Branch(this, "default", default, true);
            m_CurrentBranch.value = m_Branches["default"];
            asyncDisposingTask = PostInitialization();
        }
        
        internal Store(string address): this(Repository.global, address)
        {
            
        }
        
        /// <summary>
        /// Await until store will be synchronized
        /// </summary>
        /// <param name="cancellationToken"></param>
        public async UniTask WhenSynchronized(CancellationToken cancellationToken)
        {
            var cancellationTask = UniTask.WaitUntilCanceled(cancellationToken);
            var completedTask = await UniTask.WhenAny(synchronizationTask, cancellationTask);
            if (completedTask == 1)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
        
        private async UniTask PostInitialization()
        {
            try {
                await Sync(disposeCancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            await disposeCancellationToken;
            var asyncDisposingCancellationTokenSource = new CancellationTokenSource();
            m_AsyncDisposingCancellationTokens.Add((GetType(), address), asyncDisposingCancellationTokenSource);
            /* Clear all branches */
            m_Branches.Clear();
            m_CurrentBranch.value = null;
            m_Inquiriences.Clear();
            try
            {
                /* Remote data for current address and type in repository's local memory */
                await repository.inMemoryStorage.Delete<T>(address, asyncDisposingCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                m_AsyncDisposingCancellationTokens.Remove((GetType(), address));
            }
        }
        
        public DisposableAction Inquire()
        {
            var inquireince = new object();
            m_Inquiriences.Add(inquireince);
            return new DisposableAction(() =>
            {
                m_Inquiriences.Remove(inquireince);
                if (m_Inquiriences.Count == 0)
                {
                    Dispose();
                }
            });
        }
        
        /// <summary>
        /// Add new branch
        /// </summary>
        /// <param name="name"></param>
        /// <param name="makeCurrent"></param>
        /// <returns></returns>
        public DisposableAction AddBranch(string name, bool makeCurrent = false)
        {
#if DEBUG
            if (!synchronized)
            {
                throw new System.Exception("Store is not synchronized yet");
            }
#endif
            if (m_Branches.ContainsKey(name))
            {
                throw new System.Exception($"Branch with name {name} already exists");
            }
            var currentBranch = m_CurrentBranch.value;
            var newBranch = m_Branches[name] = new Branch(this, name, currentBranch.value, currentBranch.isDirty);
            if (makeCurrent)
            {
                m_CurrentBranch.value = newBranch;
            }
            return new DisposableAction(() =>
            {
                m_CurrentBranch.value = m_Branches["default"];
                m_Branches.Remove(name);
            });
        }
        
        public void CheckoutBranch(string name)
        {
#if DEBUG
            if (!synchronized)
            {
                throw new System.Exception("Store is not synchronized yet");
            }
#endif
            if (!m_Branches.ContainsKey(name))
            {
                throw new System.Exception($"Branch with name {name} not found");
            }
            m_CurrentBranch.value = m_Branches[name];
        }
        
        /// <summary>
        /// Set value
        /// </summary>
        /// <param name="value"></param>
        public void Set(T value)
        {
#if DEBUG
            if (!synchronized)
            {
                throw new System.Exception("Store is not synchronized yet");
            }
#endif
            m_CurrentBranch.value.value = value;
        }
        
        /// <summary>
        /// Get value
        /// </summary>
        /// <returns></returns>
        public T Get()
        {
#if DEBUG
            if (!synchronized)
            {
                throw new System.Exception("Store is not synchronized yet");
            }
#endif
            return m_CurrentBranch.value.value;
        }
        
        /// <summary>
        /// Immediately get current current branch value and subscribe to value changes.
        /// If current branch will be changed, subscription will be updated.
        /// This method will return DisposableAction that will unsubscribe from value changes.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public DisposableAction Subscribe(Action<T> action)
        {
            IDisposable valueSubscription = default; 
            var branchSubscription = m_CurrentBranch.Subscribe(branch =>
            {
                valueSubscription?.Dispose();
                valueSubscription = branch.Subscribe(action);
            });
            
            return new DisposableAction(() =>
            {
                valueSubscription?.Dispose();
                branchSubscription.Dispose();
            });
        }

        /// <summary>
        /// Commit branch changes
        /// </summary>
        /// <param name="branchName"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="Exception"></exception>
        public async UniTask Commit(string branchName, CancellationToken cancellationToken)
        {
            if (!m_Branches.ContainsKey(branchName))
            {
                throw new System.Exception($"Branch with name {branchName} not found");
            }
            var branch = m_Branches[branchName];
            using (await m_DeferLock.Lock(cancellationToken))
            {
                await this.repository.inMemoryStorage.Set(address, branch.value, version + 1, cancellationToken);
            }
        }
        
        /// <summary>
        /// Commit current branch changes
        /// </summary>
        /// <param name="cancellationToken"></param>
        public UniTask Commit(CancellationToken cancellationToken)
        {
            return Commit(m_CurrentBranch.value.name, cancellationToken);
        }
        
        /// <summary>
        /// Sync current branch with remote storages
        /// </summary>
        /// <param name="cancellationToken"></param>
        public async UniTask Sync(CancellationToken cancellationToken)
        {
            var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, disposeCancellationToken).Token;
            using (await m_DeferLock.Lock(combinedCancellationToken))
            {
                try
                {
                    if (m_SynchronizedTaskCompletionSource.Task.Status != UniTaskStatus.Pending)
                    {
                        m_SynchronizedTaskCompletionSource = new UniTaskCompletionSource();
                    }
                    var versionedData = await repository.synchronizer.Sync<T>(address, combinedCancellationToken);
                    m_CurrentBranch.value.value = versionedData.data;
                    version = versionedData.version;
                    synchronized = true;
                    m_SynchronizedTaskCompletionSource.TrySetResult();
                }
                catch (OperationCanceledException)
                {
                    m_SynchronizedTaskCompletionSource.TrySetCanceled();
                    throw;
                }
                catch (Exception e)
                {
                    m_SynchronizedTaskCompletionSource.TrySetException(e);
                    throw;
                }
            }
        }
        
        
        
        /// <summary>
        /// Reset current branch to the last saved state (or all branches if populate is true)
        /// </summary>
        /// <param name="populate"></param>
        /// <param name="cancellationToken"></param>
        public async UniTask Reset(bool populate, CancellationToken cancellationToken)
        {
#if DEBUG
            if (!synchronized)
            {
                throw new System.Exception("Store is not synchronized yet");
            }
#endif
            using (await m_DeferLock.Lock(cancellationToken))
            {
                var (data, metadata, version) = await repository.inMemoryStorage.Get<T>(address, CancellationToken.None);
                if (populate)
                {
                    /* Apply value to all branches */
                    foreach (var branch in m_Branches.Values)
                    {
                        branch.value = data;
                    }
                }
                else
                {
                    m_CurrentBranch.value.value = data;
                }
                this.version = version;
            }
        }
        
        /// <summary>
        /// Reset current branch to the last saved state
        /// </summary>
        /// <param name="cancellationToken"></param>
        public async UniTask Reset(CancellationToken cancellationToken)
        {
            await Reset(false, cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            if (isDisposed)
            {
                return;
            }

            Dispose();
            await asyncDisposingTask;
        }
    }
}