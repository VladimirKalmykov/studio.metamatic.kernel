using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public class MicroStore<T>: IDisposable, IAcquirable, IStore<T>
        where T : struct
    {
        #region Static
        private static Dictionary<string, MicroStore<T>> m_Scopes = new();
        private static Dictionary<string, string> m_AliasValues = new();
        private static Dictionary<string, MicroStoreAlias<T>> m_Aliases = new();
        private static event Action<string, string> m_OnAliasChanged;
        /// <summary>
        /// Returns store for the specified scope name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static MicroStore<T> Id(string name, T defaultValue = default)
        {
            if (!m_Scopes.TryGetValue(name, out var store))
            {
                store = new MicroStore<T>(name, defaultValue, () =>
                {
                    ReleaseScope(name);
                });
                m_Scopes[name] = store;
            }

            store.Acquire();

            return store;
        }

        public static IStore<T> Alias(string name)
        {
            if (!m_Aliases.TryGetValue(name, out var alias))
            {
                string id;
                if (!m_AliasValues.TryGetValue(name, out id))
                {
                    id = "default";
                }
                alias = new MicroStoreAlias<T>(id);
                m_Aliases[name] = alias;
                Action<string, string> aliasChangedHandler = (aliasName, id) =>
                {
                    if (aliasName == name)
                    {
                        alias.SetId(id);
                    }
                };
                m_OnAliasChanged += aliasChangedHandler;
                alias.OnDidDispose += () =>
                {
                    m_OnAliasChanged -= aliasChangedHandler;
                    m_Aliases.Remove(name);
                };
            }
            alias.Acquire();
            return alias;
        }
        
        private static void ReleaseScope(string name)
        {
            if (m_Scopes.TryGetValue(name, out var store))
            {
                m_Scopes.Remove(name);
            }
        }
        
        public static MicroStore<T> Default => Id("default");
        /// <summary>
        /// Set alias for the store
        /// </summary>
        /// <param name="alias"></param>
        /// <param name="id"></param>
        public static void SetAlias(string alias, string id)
        {
            m_AliasValues[alias] = id;
            m_OnAliasChanged?.Invoke(alias, id);
        }
        /// <summary>
        /// Release aliases
        /// </summary>
        /// <param name="alias"></param>
        public static void ReleaseAlias(string alias)
        {
            m_AliasValues.Remove(alias);
            m_OnAliasChanged?.Invoke(alias, null);
        }
        #endregion
        public enum IOStatuses
        {
            Idle,
            Synchronizing,
        }
        
        private Dictionary<string, IStore<T>.Branch> m_Branches = new ();
        private MicroState<IStore<T>.Branch> m_CurrentBranch = new();
        /// <summary>
        /// Determines that store is synchronized with the persistent storage
        /// </summary>
        private MicroState<bool> m_IsSynchronized = new(false);
        private MicroState<IStore.DataSource> m_DataSource = new(IStore.DataSource.Default);
        public IStore.DataSource dataSource => m_DataSource.value;
        private MicroState<IOStatuses> m_IOStatus = new(IOStatuses.Idle);
        public MicroStateReadOnly<IOStatuses> IOStatus => m_IOStatus;
        private int m_ReferenceCount;
        public readonly string name;
        private T m_PersistentValue;
        public T persistentValue => m_PersistentValue;
        private Action m_ReleaseHandler;

        public T value
        {
            get => m_CurrentBranch.value.value;
            set => SetValue(value);
        }
        
        public MicroStore(string name, T defaultValue, Action onRelease)
        {
            this.name = name;
            this.m_ReleaseHandler = onRelease;
            var defaultBranch = new IStore<T>.Branch(this, InternalRemoveBranch, "default", defaultValue);
            m_Branches["default"] = defaultBranch;
            m_CurrentBranch.value = defaultBranch;
            m_PersistentValue = defaultValue;
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

        public void Mutate(IStore<T>.MutateHandler mutationHandler)
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
            Action releaseSubscription = null;
            if (m_IOStatus.value != IOStatuses.Idle)
            {
                var cancellationTokenSource = new CancellationTokenSource();
                releaseSubscription = () =>
                {
                    cancellationTokenSource.Cancel();
                };
                m_IOStatus.WaitUntilValueIs(IOStatuses.Idle, cancellationTokenSource.Token).ContinueWith((IOStatuses status) =>
                {
                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        return;
                    }
                    releaseSubscription = SubscribeBranchAndValue_Internally(handler);
                }).Forget();
            }
            else
            {
                releaseSubscription = SubscribeBranchAndValue_Internally(handler);
            }

            return new DisposableAction(() =>
            {
                releaseSubscription?.Invoke();
            });
        }
        
        /// <summary>
        /// Subscribe to both branch and value changes internally (it is used for subscribe method)
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        private Action SubscribeBranchAndValue_Internally(Action<T> handler)
        {
            Action valueSubscription = null;
            var subscription =  m_CurrentBranch.Subscribe((branch) =>
            {
                valueSubscription?.Invoke();
                valueSubscription = branch.Subscribe(handler);
            });

            return () =>
            {
                subscription.Invoke();
                valueSubscription?.Invoke();
            };
        }
        
        /// <summary>
        /// Subscribe to the store changes
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public Action SubscribeMetadata(Action<T, IStore.Metadata> handler)
        {
            Action releaseSubscription = null;
            if (m_IOStatus.value != IOStatuses.Idle)
            {
                var cancellationTokenSource = new CancellationTokenSource();
                releaseSubscription = () =>
                {
                    cancellationTokenSource.Cancel();
                };
                m_IOStatus.WaitUntilValueIs(IOStatuses.Idle, cancellationTokenSource.Token).ContinueWith((IOStatuses status) =>
                {
                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        return;
                    }
                    releaseSubscription = SubscribeBranchAndValueMetadata_Internally(handler);
                }).Forget();
            }
            else
            {
                releaseSubscription = SubscribeBranchAndValueMetadata_Internally(handler);
            }

            return () =>
            {
                releaseSubscription?.Invoke();
            };
        }
        
        /// <summary>
        /// Subscribe to both branch and value with metadata changes internally (it is used for subscribe method)
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        private Action SubscribeBranchAndValueMetadata_Internally(Action<T, IStore.Metadata> handler)
        {
            Action valueSubscription = null;
            var subscription =  m_CurrentBranch.Subscribe((branch) =>
            {
                valueSubscription?.Invoke();
                valueSubscription = branch.Subscribe(nextData =>
                {
                    handler(nextData, new IStore.Metadata(m_IsSynchronized.value, dataSource, branch.isDirty));
                });
            });

            return () =>
            {
                subscription.Invoke();
                valueSubscription?.Invoke();
            };
        }
        
        /// <summary>
        /// Pull from persistent storage
        /// </summary>
        /// <param name="token"></param>
        public async UniTask Pull(CancellationToken token = default)
        {
            await Read(token: token);
        }
        
        public async UniTask Pull(Func<T> getDefaultValue, CancellationToken token = default)
        {
            await Read(getDefaultValue: getDefaultValue, token: token);
        }

        /// <summary>
        /// Pull from persistent storage
        /// </summary>
        /// <param name="populate"></param>
        /// <param name="token"></param>
        public async UniTask Pull(bool populate = false, CancellationToken token = default, T defaultValue = default)
        {
            await Read(populate: populate, token: token, defaultValue: defaultValue);
        }

        /// <summary>
        /// Read from persistent storage
        /// </summary>
        /// <param name="populate"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async UniTask<T> Read(bool populate = false, CancellationToken token = default, T defaultValue = default, Func<T> getDefaultValue = null)
        {
            T nextValue = defaultValue;
            bool isCancelled = false;
            bool isError = false;
            try
            {
                await m_IOStatus.WaitUntilValueIs(IOStatuses.Idle, token);
                m_IOStatus.value = IOStatuses.Synchronizing;
                nextValue = await Repository.Global.Read<T>(name, token);
                m_DataSource.value = IStore<T>.DataSource.Persistent;
                return value;
            }
            catch (OperationCanceledException)
            {
                isCancelled = true;
                m_IsSynchronized.value = false;
            }
            catch (IOSource.NotFoundException nfe)
            {
                /* Anyway synchronized because it has no persistent storage record, it is normal scenario */
                nextValue = getDefaultValue?.Invoke() ?? defaultValue;
                m_IsSynchronized.value = true;
                m_DataSource.value = IStore.DataSource.Fallback;
                Debug.LogWarning(nfe);
            }
            catch (System.Exception e)
            {
                isError = true;
                m_IsSynchronized.value = false;
                m_DataSource.value = IStore.DataSource.Fallback;
                Debug.LogWarning(new ReadRepositoryException(uri: name, innerException: e, message: $"Internal store fetch error: {e.Message}"));
            }
            finally
            {
                if (!isError && !isCancelled)
                {
                    m_PersistentValue = nextValue;
                    /* Apply new value to all branches if populate option is truly */
                    if (populate)
                    {
                        foreach (var branch in m_Branches.Values)
                        {
                            branch.Reset();
                        }
                    } else if (!m_CurrentBranch.value.isDirty)
                    {
                        /* Reset current branch if it is not dirty */
                        m_CurrentBranch.value.Reset();
                    }
                    m_IsSynchronized.value = true;
                    m_IOStatus.value = IOStatuses.Idle;
                }
            }

            return default;
        }

        /// <summary>
        /// Save to persistent storage
        /// </summary>
        /// <returns></returns>
        public async UniTask Push(CancellationToken token = default)
        {
            try
            {
                await m_IOStatus.WaitUntilValueIs(IOStatuses.Idle, token);
                m_IOStatus.value = IOStatuses.Synchronizing;
                await Repository.Global.Write<T>(name, m_PersistentValue, token);
                m_IsSynchronized.value = true;
            }
            catch (System.Exception e)
            {
                throw e;
            }
            finally
            {
                m_IOStatus.value = IOStatuses.Idle;
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
        
        public async UniTask WaitUntilIdle(CancellationToken cancellationToken = default)
        {
            await m_IOStatus.WaitUntilValueIs(IOStatuses.Idle, cancellationToken);
        }
        
        /// <summary>
        /// Wait until the store synchronized with the persistent storage
        /// </summary>
        /// <param name="cancellationToken"></param>
        public async UniTask WaitUntilSynchronized(CancellationToken cancellationToken = default)
        {
            await m_IOStatus.WaitUntilValueIs(IOStatuses.Idle, cancellationToken);
            await m_IsSynchronized.WaitUntilValueIs(true, cancellationToken);
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
        
        /// <summary>
        /// Dispose store and release resources (it will not be actually disposed if it has still acquired)
        /// </summary>
        public void Dispose()
        {
            Release();
        }
        
        /// <summary>
        /// Prevent store from being disposed
        /// </summary>
        public void Acquire()
        {
            m_ReferenceCount++;
        }
        
        /// <summary>
        /// Release previously acquired store (do not call it without acquire)
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void Release()
        {
            if (m_ReferenceCount < 0)
            {
                throw new Exception("Reference count is negative. MicroStore is already released");
            }
            m_ReferenceCount--;
            if (m_ReferenceCount == 0)
            {
                this.m_ReleaseHandler?.Invoke();
                this.m_ReleaseHandler = null;
            }
        }

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
                var newBranch = new IStore<T>.Branch(this, InternalRemoveBranch, branchName, m_PersistentValue, locked);
                m_Branches[branchName] = newBranch;
                m_CurrentBranch.value = newBranch;
            }

            return m_CurrentBranch.value;
        }
        
        /// <summary>
        /// Remove branch from the store
        /// </summary>
        /// <param name="branch"></param>
        private void InternalRemoveBranch(IStore<T>.Branch branch)
        {
            if (branch.isAcquired)
            {
                throw new Exception("Branch is still acquired");
            }
            m_Branches.Remove(branch.name);
            Checkout();
        }
        
        private MicroState<bool> m_Locked = new(false);
        private Queue<(UniTaskCompletionSource<IDisposable>, CancellationToken)> m_LockQueue = new ();
        /// <summary>
        /// Lock store to avoid simultaneously read/write operations
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public UniTask<IDisposable> Lock(CancellationToken cancellationToken)
        {
            var taskCompletionSource = new UniTaskCompletionSource<IDisposable>();
            m_LockQueue.Enqueue((taskCompletionSource, cancellationToken));
            _NextLock();
            return taskCompletionSource.Task;
        }

        private async void _NextLock()
        {
            if (m_LockQueue.Count > 0)
            {
                /* What for IO operations to complete */
                await m_IOStatus.WaitUntilValueIs(IOStatuses.Idle);
                var (tcs, token) = m_LockQueue.Dequeue();
                if (token.IsCancellationRequested)
                {
                    tcs.TrySetCanceled();
                    _NextLock();
                    return;
                }

                var dispose = new DisposableCallback(() =>
                {
                    m_Locked.value = false;
                    _NextLock();
                });
                tcs.TrySetResult(dispose);
            }
        }
    }
}