using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public class MicroStore<T>: IDisposable, IAcquirable
    {
        #region Static
        private static Dictionary<string, MicroStore<T>> m_Scopes = new Dictionary<string, MicroStore<T>>();
        /// <summary>
        /// Returns store for the specified scope name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static MicroStore<T> Scope(string name, T defaultValue = default)
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
        
        private static void ReleaseScope(string name)
        {
            if (m_Scopes.TryGetValue(name, out var store))
            {
                m_Scopes.Remove(name);
            }
        }
        
        public static MicroStore<T> Default => Scope("default");
        #endregion
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
        public enum DataSource
        {
            Default,
            Fallback,
            Persistent,
        }
        public class Branch: IAcquirable, IDisposable
        {
            private readonly MicroStore<T> m_Store;
            public readonly string name;
            public readonly bool locked;
            private readonly ObservableWritableValue<T> m_Value = new ObservableWritableValue<T>();
            private int m_ReferenceCount;
            public bool isAcquired => m_ReferenceCount > 0;
            public bool isDirty { get; private set; }

            public T value
            {
                get => m_Value.value;
                set => SetValue(value);
            }
            public Branch(MicroStore<T> store, string name, T value, bool locked = false)
            {
                this.m_Store = store;
                this.name = name;
                m_Value.value = value;
                this.locked = locked;
            }
            
            public void SetValue(T nextValue)
            {
                isDirty = true;
                m_Value.value = nextValue;
            }

            public void Reset()
            {
                isDirty = false;
                m_Value.value = m_Store.persistentValue;
            }
            
            public Action Subscribe(Action<T> handler)
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
                    m_Store.InternalRemoveBranch(this);
                }
            }

            public void Dispose()
            {
                Release();
            }
        }
        private Dictionary<string, Branch> m_Branches = new ();
        private ObservableWritableValue<Branch> m_CurrentBranch = new ObservableWritableValue<Branch>();
        /// <summary>
        /// Determines that store is synchronized with the persistent storage
        /// </summary>
        private ObservableWritableValue<bool> m_IsSynchronized = new ObservableWritableValue<bool>(false);
        private ObservableWritableValue<DataSource> m_DataSource = new ObservableWritableValue<DataSource>(DataSource.Default);
        public DataSource dataSource => m_DataSource.value;
        private ObservableWritableValue<IOStatuses> m_IOStatus = new ObservableWritableValue<IOStatuses>(IOStatuses.Idle);
        public ObservableValue<IOStatuses> IOStatus => m_IOStatus;
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
            var defaultBranch = new Branch(this, "default", defaultValue);
            m_Branches["default"] = defaultBranch;
            m_CurrentBranch.value = defaultBranch;
            m_PersistentValue = defaultValue;
        }
        
        /// <summary>
        /// Set new dirty value
        /// </summary>
        /// <param name="nextValue"></param>
        public void SetValue(T nextValue)
        {
            m_CurrentBranch.value.SetValue(nextValue);
        }
        
        /// <summary>
        /// Transform dirty value
        /// </summary>
        /// <param name="transformHandler"></param>
        public void TransformValue(Func<T, T> transformHandler)
        {
            m_CurrentBranch.value.value = transformHandler(m_CurrentBranch.value.value);
        }
        
        /// <summary>
        /// Subscribe to the store changes
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public Action Subscribe(Action<T> handler)
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
            return () =>
            {
                releaseSubscription?.Invoke();
            };
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
                subscription?.Invoke();
                valueSubscription?.Invoke();
            };
        }
        
        /// <summary>
        /// Subscribe to the store changes
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public Action SubscribeMetadata(Action<T, Metadata> handler)
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
        private Action SubscribeBranchAndValueMetadata_Internally(Action<T, Metadata> handler)
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

            return () =>
            {
                subscription?.Invoke();
                valueSubscription?.Invoke();
            };
        }
        
        /// <summary>
        /// Pull from persistent storage
        /// </summary>
        /// <param name="token"></param>
        public async UniTask Pull(CancellationToken token = default)
        {
            await Read(token);
        }
        
        /// <summary>
        /// Read from persistent storage
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async UniTask<T> Read(CancellationToken token = default)
        {
            try
            {
                await m_IOStatus.WaitUntilValueIs(IOStatuses.Idle, token);
                m_IOStatus.value = IOStatuses.Synchronizing;
                var value = await Repository.Global.Read<T>(name, Repository.Global.source, token);
                m_PersistentValue = value;
                m_IsSynchronized.value = true;
                m_DataSource.value = DataSource.Persistent;
                return value;
            }
            catch (OperationCanceledException)
            {
                m_IsSynchronized.value = false;
            }
            catch (DirectoryNotFoundException)
            {
                /* Anyway synchronized because it has no persistent storage record, it is normal scenario */
                m_IsSynchronized.value = true;
                m_DataSource.value = DataSource.Fallback;
                Debug.LogWarning(new DirectoryNotFoundException($"Directory not found for {name}"));
            }
            catch (FileNotFoundException)
            {
                /* Anyway synchronized because it has no persistent storage record, it is normal scenario */
                m_IsSynchronized.value = true;
                m_DataSource.value = DataSource.Fallback;
                Debug.LogWarning(new FileNotFoundException($"File not found for {name}"));
            }
            catch (System.Exception e)
            {
                m_IsSynchronized.value = false;
                m_DataSource.value = DataSource.Fallback;
                Debug.LogWarning(new ReadRepositoryException(uri: name, innerException: e, message: $"Internal store fetch error: {e.Message}"));
            }
            finally
            {
                m_IOStatus.value = IOStatuses.Idle;
            }

            return default;
        }

        /// <summary>
        /// Save to persistent storage
        /// </summary>
        /// <returns></returns>
        public async UniTask Save(CancellationToken token = default)
        {
            try
            {
                await m_IOStatus.WaitUntilValueIs(IOStatuses.Idle, token);
                m_IOStatus.value = IOStatuses.Synchronizing;
                await Repository.Global.Write<T>(name, m_PersistentValue, Repository.Global.source, token);
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
        public async UniTask CommitChanges(Func<T, T, T> commitHandler, bool save = false)
        {
            m_PersistentValue = commitHandler(m_CurrentBranch.value.value, m_PersistentValue);
            m_CurrentBranch.value.Reset();
            if (save)
            {
                await Save();
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
                await Save(token);
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
                await Save(token);
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

        public Branch Checkout(string branchName = "default", bool locked = false)
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
                var newBranch = new Branch(this, branchName, m_PersistentValue, locked);
                m_Branches[branchName] = newBranch;
                m_CurrentBranch.value = newBranch;
            }

            return m_CurrentBranch.value;
        }
        
        /// <summary>
        /// Remove branch from the store
        /// </summary>
        /// <param name="branch"></param>
        public void InternalRemoveBranch(Branch branch)
        {
            if (branch.isAcquired)
            {
                throw new Exception("Branch is still acquired");
            }
            m_Branches.Remove(branch.name);
            Checkout();
        }
        
        private ObservableWritableValue<bool> m_Locked = new ObservableWritableValue<bool>(false);
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