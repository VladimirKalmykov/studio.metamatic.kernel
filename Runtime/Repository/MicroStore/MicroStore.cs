using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public class MicroStore<T>: IDisposable
    where T : notnull
    {
        #region Subclasses
        
        public enum IOStatuses
        {
            Idle,
            Synchronizing,
        }
        
        public struct Metadata
        {
            public readonly bool isSynchronized;
            public readonly MicroRepository<T>.DataSource dataSource;
            public readonly bool isDirty;
            public Metadata(bool isSynchronized, MicroRepository<T>.DataSource dataSource, bool isDirty)
            {
                this.isSynchronized = isSynchronized;
                this.dataSource = dataSource;
                this.isDirty = isDirty;
            }
        }
        
        public delegate void MutateHandler(ref T value);
        public class Branch: IAcquirable, IDisposable
        {
            private readonly MicroStore<T> m_Store;
            public readonly string name;
            private readonly MicroState<T> m_Value = new();
            private int m_ReferenceCount;
            public bool isAcquired => m_ReferenceCount > 0;
            public bool isDirty { get; private set; }
            private Action<Branch> m_OnRemoveBranch;

            public T value
            {
                get => m_Value.value;
                set => SetValue(value);
            }
            public Branch(MicroStore<T> store, Action<Branch> onRemoveBranch, string name, T value, bool locked = false)
            {
                this.m_Store = store;
                this.name = name;
                m_Value.value = value;
            }
            
            public void SetValue(T nextValue)
            {
                isDirty = true;
                m_Value.value = nextValue;
            }
            
            public void Mutate(MutateHandler mutationHandler)
            {
                isDirty = true;
                m_Value.Mutate((ref T val) => mutationHandler(ref val));
            }

            public void Reset()
            {
                isDirty = false;
                m_Value.value = m_Store.persistentValue;
            }
            
            public DisposableAction Subscribe(Action<T> handler)
            {
                return m_Value.Subscribe(handler);
            }

            public void Acquire()
            {
                m_ReferenceCount++;
            }

            public void Release()
            {
                m_ReferenceCount--;
                if (m_ReferenceCount == 0)
                {
                    m_Store?.Dispose();
                    m_Value?.Dispose();
                    m_OnRemoveBranch?.Invoke(this);
                }
            }

            public void Dispose()
            {
                Release();
            }
        }
        #endregion
        
        private Dictionary<string, Branch> m_Branches = new ();
        private MicroState<Branch> m_CurrentBranch = new();
        
        private MicroState<bool> m_IsSynchronized = new(false);
        
        private MicroState<MicroRepository<T>.DataSource> m_DataSource = new(MicroRepository<T>.DataSource.Default);
        public MicroRepository<T>.DataSource dataSource => m_DataSource.value;
        
        private MicroState<IOStatuses> m_IOStatus = new(IOStatuses.Idle);
        public MicroStateReadOnly<IOStatuses> IOStatus => m_IOStatus;
        
        /// <summary>
        /// Represents the value that is stored in the persistent storage
        /// </summary>
        private T m_PersistentValue;
        
        private DeferLock m_Lock = new();
        private MicroRepository<T> m_Repository;
        
        public MicroStore(MicroRepository<T> repository, T defaultValue = default)
        {
            m_Repository = repository;
            var defaultBranch = new Branch(this, InternalRemoveBranch, "default", defaultValue);
            m_Branches["default"] = defaultBranch;
            m_CurrentBranch.value = defaultBranch;
            m_PersistentValue = defaultValue;
        }
        
        public MicroStore(T defaultValue = default): this(new MicroRepository<T>("default"), defaultValue)
        {
            
        }
        
        public MicroStore(string persistentName, T defaultValue = default): this(new MicroRepository<T>(persistentName), defaultValue)
        {
            
        }
        
        #region Public API
        public T value
        {
            get => m_CurrentBranch.value.value;
            set => SetValue(value);
        }
        public T persistentValue => m_PersistentValue;
        /// <summary>
        /// Set new dirty value
        /// </summary>
        /// <param name="nextValue"></param>
        public void SetValue(T nextValue, bool commit = false)
        {
            m_CurrentBranch.value.SetValue(nextValue);
            if (commit)
            {
                CommitChanges();
            }
        }
        
        /// <summary>
        /// Change dirty without copying (useful for large structs)
        /// </summary>
        /// <param name="mutationHandler"></param>
        public void Mutate(MutateHandler mutationHandler)
        {
            m_CurrentBranch.value.Mutate(mutationHandler);
        }
        
        /// <summary>
        /// Transform dirty value
        /// </summary>
        /// <param name="transformHandler"></param>
        public void Modify(Func<T, T> transformHandler, bool commit = false)
        {
            m_CurrentBranch.value.value = transformHandler(m_CurrentBranch.value.value);
            if (commit)
            {
                CommitChanges();
            }
        }
        
        /// <summary>
        /// Subscribe to the store changes
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public DisposableAction Subscribe(Action<T> handler)
        {
            Action valueSubscription = null;
            var subscription =  m_CurrentBranch.Subscribe((branch) =>
            {
                valueSubscription?.Invoke();
                valueSubscription = branch.Subscribe(handler);
            });


            return new DisposableAction(() =>
            {
                subscription.Dispose();
                valueSubscription?.Invoke();
            });
        }
        
        public DisposableAction SubscribeMetadata(Action<T, Metadata> handler)
        {
            Action valueSubscription = null;
            var subscription =  m_CurrentBranch.Subscribe((branch) =>
            {
                valueSubscription?.Invoke();
                valueSubscription = branch.Subscribe(nextData =>
                {
                    handler(nextData, new Metadata(m_IsSynchronized.value, dataSource, branch.isDirty));
                });
            });

            return new DisposableAction(() =>
            {
                subscription.Invoke();
                valueSubscription?.Invoke();
            });
        }
        
        /// <summary>
        /// Pull from persistent storage
        /// </summary>
        /// <param name="token"></param>
        public async UniTask Pull(CancellationToken token = default)
        {
            await InternalReadFromPersistentStorage(token: token);
        }
        
        public async UniTask LoadFromPersistentStorage(Func<T> getDefaultValue, CancellationToken token = default)
        {
            await InternalReadFromPersistentStorage(getFallbackValue: getDefaultValue, token: token);
        }
        
        /// <summary>
        /// Pull from persistent storage
        /// </summary>
        /// <param name="populate"></param>
        /// <param name="token"></param>
        public async UniTask Pull(bool populate = false, CancellationToken token = default, Func<T> getFallbackValue = null)
        {
            await InternalReadFromPersistentStorage(populate: populate, token: token, getFallbackValue: getFallbackValue);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="populate">Apply fetched data to the all branches</param>
        /// <param name="fallbackValue">Default value for a case of throwing NotFoundException</param>
        /// <param name="getFallbackValue">Default value factory for a case of throwing NotFoundException</param>
        /// <param name="timeout">Request timeout in ms</param>
        /// <param name="token">Cancellation token</param>
        /// <exception cref="ReadRepositoryException"></exception>
        private async UniTask<MicroRepository<T>.DataSource> InternalReadFromPersistentStorage(bool populate = false, Func<T> getFallbackValue = null, float timeout = 0f, CancellationToken token = default)
        {
            using (await Lock(token))
            {
                var isErrored = false;
                try
                {
                    m_IOStatus.value = IOStatuses.Synchronizing;
                    var readTask = m_Repository.Read(token: token);
                    var nextValue = await readTask;
                    m_IOStatus.value = IOStatuses.Idle;
                    m_IsSynchronized.value = true;
                    m_DataSource.value = MicroRepository<T>.DataSource.Persistent;
                    m_PersistentValue = nextValue;
                }
                catch (IOSource.NotFoundException nfe)
                {
                    /* Anyway synchronized because it has no persistent storage record, it is normal scenario */
                    m_IOStatus.value = IOStatuses.Idle;
                   
                    if (getFallbackValue != null)
                    {
                        m_IsSynchronized.value = true;
                        m_PersistentValue = getFallbackValue();
                        m_DataSource.value = MicroRepository<T>.DataSource.Fallback;
                    }
                    else
                    {
                        m_IsSynchronized.value = false;
                        m_DataSource.value = MicroRepository<T>.DataSource.Default;
                        throw nfe;
                    }
                }
                catch (System.Exception e)
                {
                    isErrored = true;
                    Debug.LogError(e);
                    throw new ReadRepositoryException(uri: m_Repository.address, innerException: e,
                        message: $"Internal store fetch error: {e.Message}");
                }
                finally
                {
                    if (!isErrored)
                    {;
                        if (populate)
                        {
                            foreach (var branch in m_Branches.Values)
                            {
                                if (!branch.isDirty)
                                {
                                    branch.Reset();
                                }
                            }
                        }
                        else if (!m_CurrentBranch.value.isDirty)
                        {
                            /* Reset current branch if it is not dirty */
                            m_CurrentBranch.value.Reset();
                        }
                    }
                }

                return m_DataSource.value;
            }
        }

        /// <summary>
        /// Save to persistent storage
        /// </summary>
        /// <returns></returns>
        public async UniTask Push(CancellationToken token = default)
        {
            using (await Lock(token))
            {
                try
                {
                    m_IOStatus.value = IOStatuses.Synchronizing;
                    var writeTask = m_Repository.Write(m_PersistentValue, token: token);
                    await writeTask;
                    m_IOStatus.value = IOStatuses.Idle;
                    m_IsSynchronized.value = true;
                    m_DataSource.value = MicroRepository<T>.DataSource.Persistent;
                }
                catch (System.Exception e)
                {
                    throw new WriteRepositoryException(uri: m_Repository.address, innerException: e,
                        message: $"Internal store push error: {e.Message}");
                }
            }
        }
        
        public async UniTask Push(T value, CancellationToken token = default)
        {
            Populate(value);
        }
        
        /// <summary>
        /// Apply dirty changes to the store
        /// </summary>
        public void CommitChanges()
        {
            m_PersistentValue = m_CurrentBranch.value.value;
            m_CurrentBranch.value.Reset();
        }
        
        /// <summary>
        /// Partial commit changes to the store
        /// </summary>
        /// <param name="commitHandler">Function that accepts dirty and persistent value and return new persistent value (it also reset dirty value)</param>
        /// <param name="save">Save to the persistent storage</param>
        public async UniTask CommitChanges(Func<T, T, T> commitHandler, bool save = false, CancellationToken token = default)
        {
            m_PersistentValue = commitHandler(m_CurrentBranch.value.value, m_PersistentValue);
            m_CurrentBranch.value.Reset();
            if (save)
            {
                await Push(token);
            }
        }
        
        /// <summary>
        /// Apply dirty changes to the store and save to persistent storage
        /// </summary>
        public async UniTask CommitChanges(bool save, CancellationToken token = default)
        {
            CommitChanges();
            if (save)
            {
                await Push(token);
            }
        }
        
        /// <summary>
        /// Make changes for both dirty and persistent values and then save persistent value to the storage
        /// </summary>
        /// <param name="populateHandler"></param>
        /// <param name="token"></param>
        public async UniTask Populate(Func<T, T> populateHandler, bool save = false, CancellationToken token = default)
        {
            m_PersistentValue = populateHandler(m_PersistentValue);
            foreach(var branch in m_Branches.Values)
            {
                branch.value = populateHandler(branch.value);
            }
            if (save)
            {
                await Push(token);
            }
        }
        
        public async UniTask Populate(T data, bool save = false, CancellationToken token = default)
        {
            m_PersistentValue = data;
            foreach(var branch in m_Branches.Values)
            {
                branch.Reset();
            }
            if (save)
            {
                await Push(token);
            }
        }
        
        /// <summary>
        /// Wait until the store value satisfies the condition
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async UniTask<T> WaitUntilValueIs(Func<T, bool> condition, CancellationToken cancellationToken = default)
        {
            await m_IOStatus.WaitUntilValueIs(IOStatuses.Idle, cancellationToken);
            var taskCompletionSource = new UniTaskCompletionSource<T>();
            var releaseSubscription = Subscribe(obj =>
            {
                if (condition(obj))
                {
                    taskCompletionSource.TrySetResult(obj);
                }
            });
            cancellationToken.Register(releaseSubscription);
            return await taskCompletionSource.Task;
        }
        
        
        /// <summary>
        /// Make specified branch active (if it doesn't exist, it will be created)
        /// </summary>
        /// <param name="branchName"></param>
        /// <param name="locked"></param>
        /// <returns></returns>
        public IDisposable Checkout(string branchName = "default", bool locked = false)
        {
            if (m_Branches.TryGetValue(branchName, out var branch))
            {
                if (m_CurrentBranch.value != branch)
                {
                    m_CurrentBranch.value = branch;
                }
            }
            else
            {
                var newBranch = new Branch(this, InternalRemoveBranch, branchName, m_PersistentValue, locked);
                m_Branches[branchName] = newBranch;
                m_CurrentBranch.value = newBranch;
            }

            return m_CurrentBranch.value;
        }
        
        public UniTask WhenLoaded(CancellationToken token = default)
        {
            return m_IOStatus.WaitUntilValueIs(IOStatuses.Idle, token);
        }
        
        /// <summary>
        /// Remove branch from the store
        /// </summary>
        /// <param name="branch"></param>
        private void InternalRemoveBranch(Branch branch)
        {
            if (branch.isAcquired)
            {
                throw new Exception("Branch is still acquired");
            }
            m_Branches.Remove(branch.name);
            Checkout();
        }
        
        /// <summary>
        /// Lock store
        /// </summary>
        /// <returns></returns>
        public DisposableAction Lock() => m_Lock.Lock();
        public async UniTask<DisposableAction> Lock(CancellationToken token = default) {
            await m_Lock;
            if (token.IsCancellationRequested)
            {
                return new DisposableAction(() => { });
            }
            return m_Lock.Lock();
        }
        public UniTask WhenUnlocked(CancellationToken token = default) => m_Lock.WhenUnlocked(token);
        #endregion
        
        /// <summary>
        /// Dispose store and release resources (it will not be actually disposed if it has still acquired)
        /// </summary>
        public void Dispose()
        {
            m_Lock.Dispose();
            foreach (var branch in m_Branches)
            {
                branch.Value.Dispose();
            }
            m_Branches.Clear();
        }
    }
}