using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public abstract class Store: IStore
    {
        protected const string MASTER_BRANCH_NAME = "master";
        protected SignalSource m_DisposeCancellationSignalSource = SignalSource.Create();
        public Signal disposeCancellationSignal => m_DisposeCancellationSignalSource.Signal;
        public bool isDisposed { get; private set; }
        protected event Action OnDispose;
        public UniTask asyncDisposingTask { get; protected set; }
        public readonly string address;
        public readonly Type type;
        protected Store(string address, Type type)
        {
            this.address = address;
            this.type = type;
        }
        public abstract UniTask Sync(Signal signal);
        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }
            isDisposed = true;
            m_DisposeCancellationSignalSource.TryRaise();
            OnDispose?.Invoke();
        }
        public DisposableAction SubscribeDispose(Action action)
        {
            OnDispose += action;
            return new DisposableAction(() =>
            {
                OnDispose -= action;
            });
        }
    }
    public class Store<T>: Store, IStore<T>
    {
        /// <summary>
        /// When store is async disposing, it will be added to this dictionary until it will be disposed
        /// If next instance of the same type will be created, it will be disposed immediately
        /// </summary>
        protected static Dictionary<(Type, string), SignalSource> m_AsyncDisposingCancellationSignalSources = new();
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
            public long version;
            public event Action<T> OnChange;

            public bool isDirty;
            public Branch(Store<T> store, string name, T value, bool isDirty)
            {
                this.name = name;
                this.m_Value = value;
                this.isDirty = isDirty;
                m_Store = store;
            }
            
            public ref T GetRef()
            {
                return ref m_Value;
            }
            
            public void SetValue(T value)
            {
                m_Value = value;
                isDirty = true;
            }
            
            /// <summary>
            /// Copy all state from another branch
            /// </summary>
            /// <param name="branch"></param>
            public void CopyFrom(Branch branch) {
                m_Value = branch.m_Value;
                isDirty = branch.isDirty;
                version = branch.version;
            }
            
            /// <summary>
            /// Reset branch with specified value and version (it will be not dirty)
            /// </summary>
            /// <param name="value"></param>
            /// <param name="version"></param>
            public void Reset(T value, long version)
            {
                m_Value = value;
                this.version = version;
                isDirty = false;
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
        public delegate UniTask<T> GetDefaultValue(string address, Signal signal);
        public readonly RepositoryLegacy repository;
        private readonly Dictionary<string, Branch> m_Branches = new();
        private readonly Observable<Branch> m_CurrentBranch = new();
        private readonly DeferLock m_SyncLock = new ();
        private readonly DeferLock m_CheckoutBranchLock = new ();
        private UniTaskCompletionSource m_SynchronizedTaskCompletionSource = new ();
        public bool synchronized { get; private set; }
        public UniTask synchronizationTask => m_SynchronizedTaskCompletionSource.Task;

        public long version => m_CurrentBranch.value.version;

        public bool isDirty => m_CurrentBranch.value.isDirty;

        /* Here will be property that will be increased when Inquire method will be called */
        private List<object> m_Inquiriences = new();
        private readonly GetDefaultValue getDefaultValue;
        
        public T value
        {
            get => Get();
            set => Set(value);
        }
        
        internal Store(RepositoryLegacy repository, string address, GetDefaultValue getDefaultValue): base(address, typeof(T))
        {
            this.repository = repository;
            this.getDefaultValue = getDefaultValue;
            m_Branches[MASTER_BRANCH_NAME] = new Branch(this, MASTER_BRANCH_NAME, default, true);
            m_CurrentBranch.value = m_Branches[MASTER_BRANCH_NAME];
            asyncDisposingTask = PostInitialization().Preserve();
        }
        
        internal Store(string address, GetDefaultValue getDefaultValue): this(RepositoryLegacy.global, address, getDefaultValue)
        {
            
        }
        
        /// <summary>
        /// Await until store will be synchronized
        /// </summary>
        /// <param name="signal"></param>
        public async UniTask WhenSynchronized(Signal signal)
        {
            var doNotWaitSignal = false;
            var cancellationTask = UniTask.WaitUntil(() => doNotWaitSignal || signal.IsCancellationRequested);
            var completedTask = await UniTask.WhenAny(synchronizationTask, cancellationTask);
            doNotWaitSignal = true;
            if (completedTask == 1)
            {
                signal.ThrowIfRaised();
            }
        }
        
        private async UniTask PostInitialization()
        {
            try {
                await Sync(disposeCancellationSignal);
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            await disposeCancellationSignal;
            var asyncDisposingCancellationSignalSource = SignalSource.Create();
            m_AsyncDisposingCancellationSignalSources.Add((GetType(), address), asyncDisposingCancellationSignalSource);
            /* Clear all branches */
            m_Branches.Clear();
            m_CurrentBranch.value = null;
            m_Inquiriences.Clear();
            try
            {
                /* Remote data for current address and type in repository's local memory */
                await repository.inMemoryStorage.Delete<T>(address, asyncDisposingCancellationSignalSource.Signal);
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
                m_AsyncDisposingCancellationSignalSources.Remove((GetType(), address));
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
        /// <param name="name">Branch name</param>
        /// <param name="checkout">Make branch current</param>
        /// <returns></returns>
        private void InternalCreateBranch(string name, bool checkout = false)
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
            if (checkout)
            {
                m_CurrentBranch.value = newBranch;
            }
        }

        /// <summary>
        /// Make branch with name current
        /// </summary>
        /// <param name="branchName"></param>
        /// <param name="sourceBranchName"></param>
        /// <param name="createIfNotExists"></param>
        /// <param name="removeAfterDispose"></param>
        /// <exception cref="Exception"></exception>
        public DisposableAction Checkout(string branchName, string sourceBranchName = MASTER_BRANCH_NAME, bool createIfNotExists = false, bool removeAfterDispose = true)
        {
#if DEBUG
            if (!synchronized)
            {
                throw new System.Exception("Store is not synchronized yet");
            }
#endif
            if (m_CheckoutBranchLock.IsLocked)
            {
                throw new System.Exception($"Checkout operation is locked by branch {m_CurrentBranch.value.name}. Dispose previous checkout operation first");
            }
            if (!m_Branches.ContainsKey(sourceBranchName))
            {
                throw new System.Exception($"Branch with name {sourceBranchName} not found");
            }
            if (!m_Branches.ContainsKey(branchName))
            {
                if (createIfNotExists)
                {
                    InternalCreateBranch(branchName);
                }
                else
                {
                    throw new System.Exception($"Branch with name {branchName} not found");
                }
            }
            var previousBranchName = m_CurrentBranch.value.name;
            var unlock = m_CheckoutBranchLock.Lock();
            m_CurrentBranch.value = m_Branches[branchName];
            m_CurrentBranch.value.CopyFrom(m_Branches[sourceBranchName]);
            return DisposableAction.Create(() =>
            {
                unlock.Dispose();
                if (m_Branches.ContainsKey(previousBranchName))
                {
                    m_CurrentBranch.value = m_Branches[previousBranchName];
                }
                else
                {
                    m_CurrentBranch.value = m_Branches[MASTER_BRANCH_NAME];
                }

                if (removeAfterDispose && createIfNotExists)
                {
                    /* Remove created branch */
                    m_Branches.Remove(branchName);
                }
            });
        }

        public DisposableAction Checkout(string branchName, bool createIfNotExists = false, bool removeAfterDispose = true)
        {
            return Checkout(branchName, MASTER_BRANCH_NAME, createIfNotExists, removeAfterDispose);
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
        
        public ref T GetByRef()
        {
#if DEBUG
            if (!synchronized)
            {
                throw new System.Exception("Store is not synchronized yet");
            }
#endif
            return ref m_CurrentBranch.value.GetRef();
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

        public async UniTask<T> GetPristineValue(Signal signal)
        {
            /* Read value from the in-memory storage */
            var (data, metadata, version) = await repository.inMemoryStorage.Get<T>(address, signal);
            return data;
        }

        public void Modify(IStore<T>.Modifier action)
        {
            Set(action(Get()));
        }

        public void ModifyRef(IStore<T>.ModifierRef action)
        {
            ref T r = ref m_CurrentBranch.value.GetRef();
            action(ref r);
        }

        public void Populate(T value)
        {
            /* Modify every branch */
            foreach (var branch in m_Branches.Values)
            {
                branch.value = value;
            }
        }

        public void Populate(IStore<T>.Modifier modifier)
        {
            /* Modify every branch */
            foreach (var branch in m_Branches.Values)
            {
                branch.value = modifier(branch.value);
            }
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
            var branchSubscription = m_CurrentBranch.Watch(branch =>
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

        public void Merge(string fromBranch, string toBranch, IStore<T>.Merger merger)
        {
            var nextValue = merger(m_Branches[fromBranch].value, m_Branches[toBranch].value);
            m_Branches[toBranch].value = nextValue;
        }

        public void Merge(string fromBranch, IStore<T>.Merger merger)
        {
            Merge(fromBranch, MASTER_BRANCH_NAME, merger);
        }

        public void Merge(IStore<T>.Merger merger)
        {
            Merge(m_CurrentBranch.value.name, MASTER_BRANCH_NAME, merger);
        }

        /// <summary>
        /// Commit branch changes to the in-memory storage
        /// </summary>
        /// <param name="branchName"></param>
        /// <param name="signal"></param>
        /// <exception cref="Exception"></exception>
        public async UniTask Commit(Signal signal)
        {
            var branch = m_Branches["master"];
            using (await m_SyncLock.Lock(signal))
            {
                var (data, metadata, inMemoryVersion) = await this.repository.inMemoryStorage.Get<T>(address, signal);
                if (inMemoryVersion <= branch.version)
                {
                    await this.repository.inMemoryStorage.Set(address, branch.value, version + 1, signal);
                }
                else
                {
                    throw new System.Exception($"In-memory storage version is greater than branch version. In-memory version: {inMemoryVersion}, branch version: {branch.version}");
                }
            }
        }
        
        /// <summary>
        /// Sync current branch with remote storages
        /// </summary>
        /// <param name="signal"></param>
        public override async UniTask Sync(Signal signal)
        {
            var combinedSignal = SignalSource.CreateLinked(signal, disposeCancellationSignal).Signal;
            var disposeLock = await m_SyncLock.Lock(combinedSignal);
            try
            {
                if (m_SynchronizedTaskCompletionSource.Task.Status != UniTaskStatus.Pending)
                {
                    m_SynchronizedTaskCompletionSource = new UniTaskCompletionSource();
                }

                var versionedData = await repository.synchronizer.Sync<T>(address, getDefaultValue, combinedSignal);
                m_CurrentBranch.value.value = versionedData.data;
                m_CurrentBranch.value.isDirty = false;
                m_CurrentBranch.value.version = versionedData.version;
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
            finally
            {
                disposeLock.Dispose();
            }
        }
        
        
        
        /// <summary>
        /// Reset current branch to the last saved state (or all branches if populate is true)
        /// </summary>
        /// <param name="populate"></param>
        /// <param name="cancellationSignal"></param>
        public async UniTask Reset(bool populate, Signal cancellationSignal)
        {
#if DEBUG
            if (!synchronized)
            {
                throw new System.Exception("Store is not synchronized yet");
            }
#endif
            using (await m_SyncLock.Lock(cancellationSignal))
            {
                var (data, metadata, version) = await repository.inMemoryStorage.Get<T>(address, CancellationToken.None);
                if (populate)
                {
                    /* Apply value to all branches */
                    foreach (var branch in m_Branches.Values)
                    {
                        branch.Reset(data, version);
                    }
                }
                else
                {
                    m_CurrentBranch.value.Reset(data, version);
                }
            }
        }
        
        /// <summary>
        /// Reset current branch to the last saved state
        /// </summary>
        /// <param name="cancellationSignal"></param>
        public async UniTask Reset(Signal cancellationSignal)
        {
            await Reset(false, cancellationSignal);
        }

        public async UniTask<IDisposable> Lock(Signal signal)
        {
           var disposeLock = await m_SyncLock.Lock(signal);

           return new DisposableAction(() =>
           {
               disposeLock.Dispose();
           });
        }

        public async UniTask DisposeAsync()
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