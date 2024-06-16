using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public partial class RepositoryLegacy
    {
        public class MicroStore<T>: IDisposable
        where T : notnull
        {
            #region Subclasses
            
            public enum DataSource
            {
                Default,
                Fallback,
                Persistent,
            }
            
            public enum IOStatuses
            {
                Idle,
                Synchronizing,
            }
            
            public struct Metadata
            {
                public readonly bool isSynchronized;
                public readonly DataSource dataSource;
                public readonly bool isDirty;
                public Metadata(bool isSynchronized, DataSource dataSource, bool isDirty)
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
                    m_Value.value = await m_Store.GetCommitedValue();
                }
                
                public void Reset(T value)
                {
                    isDirty = false;
                    m_Value.value = value;
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
            
            private MicroState<DataSource> m_DataSource = new(DataSource.Default);
            public DataSource dataSource => m_DataSource.value;
            
            private MicroState<IOStatuses> m_IOStatus = new(IOStatuses.Idle);
            public MicroStateReadOnly<IOStatuses> IOStatus => m_IOStatus;
            public readonly string address;
            public readonly RepositoryLegacy repository;
            private CancellationTokenSource m_SelfCancellationTokenSource = new();
            
            private DeferLock m_Lock = new();
            
            internal MicroStore(RepositoryLegacy repository, string address, T defaultValue = default)
            {
                var defaultBranch = new Branch(this, InternalRemoveBranch, "default", defaultValue);
                this.repository = repository;
                m_Branches["default"] = defaultBranch;
                m_CurrentBranch.value = defaultBranch;
                this.address = address;
                Sync(m_SelfCancellationTokenSource.Token);
            }
            
            internal MicroStore(string address, T defaultValue = default): this(RepositoryLegacy.global, address, defaultValue)
            {
                
            }
            
            #region Public API
            public T value
            {
                get => m_CurrentBranch.value.value;
                set => SetValue(value);
            }
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
            /// Synchronize with the persistent storage
            /// </summary>
            /// <param name="token"></param>
            /// <returns></returns>
            public async UniTask<T> Sync(CancellationToken token = default)
            {
                using (await Lock(token))
                {
                    try
                    {
                        var result = await repository.Synchronize<T>(address);
                        /* Apply to all not dirty branches */
                        foreach (var branch in m_Branches.Values)
                        {
                            if (!branch.isDirty)
                            {
                                branch.Reset(result.Item1);
                            }
                        }

                        return result.Item1;
                    }
                    catch (Exception e)
                    {
                        throw;
                    }
                }
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
            private async UniTask<DataSource> InternalReadFromPersistentStorage(bool populate = false, Func<T> getFallbackValue = null, float timeout = 0f, CancellationToken token = default)
            {
                using (await Lock(token))
                {
                    var isErrored = false;
                    try
                    {
                        m_IOStatus.value = IOStatuses.Synchronizing;
                        var readTask = repository.Read(token: token);
                        var nextValue = await readTask;
                        m_IOStatus.value = IOStatuses.Idle;
                        m_IsSynchronized.value = true;
                        m_DataSource.value = DataSource.Persistent;
                        m_CachedOriginValue = nextValue;
                    }
                    catch (Storage.NotFoundException nfe)
                    {
                        /* Anyway synchronized because it has no persistent storage record, it is normal scenario */
                        m_IOStatus.value = IOStatuses.Idle;
                       
                        if (getFallbackValue != null)
                        {
                            m_IsSynchronized.value = true;
                            m_CachedOriginValue = getFallbackValue();
                            m_DataSource.value = DataSource.Fallback;
                        }
                        else
                        {
                            m_IsSynchronized.value = false;
                            m_DataSource.value = DataSource.Default;
                            throw nfe;
                        }
                    }
                    catch (System.Exception e)
                    {
                        isErrored = true;
                        Debug.LogError(e);
                        throw new ReadRepositoryException(uri: address, innerException: e,
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
                        var writeTask = repository.Write(m_CachedOriginValue, token: token);
                        await writeTask;
                        m_IOStatus.value = IOStatuses.Idle;
                        m_IsSynchronized.value = true;
                        m_DataSource.value = DataSource.Persistent;
                    }
                    catch (System.Exception e)
                    {
                        throw new WriteRepositoryException(uri: address, innerException: e,
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
            public async void CommitChanges(CancellationToken token = default)
            {
                var combinedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, m_SelfCancellationTokenSource.Token);
                await repository.localMemory.Set(address, m_CurrentBranch.value.value, combinedCancellationTokenSource.Token);
                
                m_CurrentBranch.value.Reset();
            }
            
            /// <summary>
            /// Partial commit changes to the store
            /// </summary>
            /// <param name="commitHandler">Function that accepts dirty and persistent value and return new persistent value (it also reset dirty value)</param>
            /// <param name="save">Save to the persistent storage</param>
            public async UniTask CommitChanges(Func<T, T, T> commitHandler, bool save = false, CancellationToken token = default)
            {
                var (originalValue, metadata) = await repository.localMemory.Get<T>(address, token);
                var newValue = commitHandler(m_CurrentBranch.value.value, originalValue);
                await repository.localMemory.Set(address, newValue, token);
                m_CurrentBranch.value.Reset(newValue);
                if (save)
                {
                    await Sync(token);
                }
            }
            
            /// <summary>
            /// Apply 
            /// </summary>
            /// <param name="sync">Perform bidirectional sync</param>
            /// <param name="token"></param>
            public async UniTask CommitChanges(bool sync, CancellationToken token = default)
            {
                CommitChanges();
                if (sync)
                {
                    await Sync(token);
                }
            }
            
            /// <summary>
            /// Make changes for both dirty and persistent values and then save persistent value to the storage
            /// </summary>
            /// <param name="populateHandler"></param>
            /// <param name="token"></param>
            public async UniTask Populate(Func<T, T> populateHandler, bool commit = false, CancellationToken token = default)
            {
                var (originalValue, metadata) = await repository.localMemory.Get<T>(address, token);
                var nextOrignalValue = populateHandler(originalValue);
                await repository.localMemory.Set(address, nextOrignalValue, token);
                foreach(var branch in m_Branches.Values)
                {
                    branch.value = populateHandler(branch.value);
                }
                if (commit)
                {
                    await Sync(token);
                }
            }
            
            public async UniTask Populate(T data, bool commit = false, CancellationToken token = default)
            {
                foreach(var branch in m_Branches.Values)
                {
                    branch.Reset();
                }
                if (commit)
                {
                    await CommitChanges(true, token);
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
                    var newBranch = new Branch(this, InternalRemoveBranch, branchName, m_CachedOriginValue, locked);
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
            /// 
            public void Dispose()
            {
                m_SelfCancellationTokenSource.Cancel();
                m_Lock.Dispose();
                foreach (var branch in m_Branches)
                {
                    branch.Value.Dispose();
                }
                m_Branches.Clear();
            }
        }
        
        /// <summary>
        /// Create new store with specified address and type. If store is not found in the persistent storage, it will be created with the data from the factory (onCreateData).
        /// This store uses repository for the synchronization with the persistent storages. As one repository can has multiple sources, it is possible to use fallback sources for the store.
        /// If one source is not available, it will try to use another source. If all sources are not available, it uses local memory source, that is not persistent, but guaranteed to be available.
        /// Every store synchronizes with the persistent storage on creation and on every sync operation. When you change the store value, it will be saved to so called branch.
        /// You can create multiple branches for the store and switch between them. Every branch has its own value. If you want to save changes to the persistent storage, you need to commit changes.
        /// When you commit changes, it will be saved to the persistent storage and branch from which commit has been performed will be reset to the persistent value.
        /// </summary>
        /// <param name="address">Uniq address for the store. This address is used for getting value from persistent storage</param>
        /// <param name="onCreateData">Data factory for creating new data when no data found in persistent storage</param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async UniTask<MicroStore<T>> CreateStore<T>(string address, Func<string, CancellationToken, UniTask<T>> onCreateData, CancellationToken cancellationToken = default)
        {
            var store = new MicroStore<T>(this, address);
            try
            {
                await store.Sync(cancellationToken);
                return store;
            }
            catch (OperationCanceledException)
            {
                store.Dispose();
            }
            catch (Storage.NotFoundException)
            {
                try
                {
                    /* Connection is stable, but address is not found in all sources. Create new record is required */
                    var newData = await onCreateData(address, cancellationToken);
                    /* Apply new data to all branches and commit */
                    await store.Populate(newData, true, cancellationToken);
                }
                catch (Exception)
                {
                    /* Connection rejected, drop game */
                    store.Dispose();
                    throw;
                }

                return store;
            }
            catch (Exception)
            {
                /* Connection rejected, drop game */
                store.Dispose();
                throw;
            }
        }
    }
}