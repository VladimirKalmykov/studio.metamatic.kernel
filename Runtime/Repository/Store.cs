using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Metamatic.StorageDriver;
using UnityEngine.PlayerLoop;

namespace Metamatic
{
    public class Store: IDisposable
    {
        protected CancellationTokenSource m_DisposeCancellationTokenSource = new ();
        public CancellationToken disposeCancellationToken => m_DisposeCancellationTokenSource.Token;
        protected bool m_Disposed { get; private set; }= false;
        protected Store()
        {
            
        }
        public void Dispose()
        {
            if (m_Disposed)
            {
                return;
            }
            m_Disposed = true;
            m_DisposeCancellationTokenSource.Cancel();
        }
    }
    public class Store<T>: Store
    {
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
            public bool isDirty { get; private set; }
            public Branch(Store<T> store, string name, T value)
            {
                this.name = name;
                m_Store = store;
            }
            
            public void SetValue(T value)
            {
                m_Value = value;
                isDirty = true;
            }
        }

        public readonly string address;
        public readonly Repository repository;
        private readonly Dictionary<string, Branch> m_Branches = new();
        private readonly MicroState<Branch> m_CurrentBranch = new();
        private readonly DeferLock m_DeferLock = new DeferLock();
        public UniTask initializationTask;
        public long version { get; private set; } = 0;
        /* Here will be property that will be increased when Inquire method will be called */
        public int inquired { get; private set; } = 0;
        
        
        internal Store(Repository repository, string address)
        {
            this.repository = repository;
            this.address = address;
            m_Branches["default"] = new Branch(this, "default", default);
            m_CurrentBranch.value = m_Branches["default"];
            initializationTask = Initialization();
        }
        
        public Store(string address): this(Repository.global, address)
        {
        }
        
        private async UniTask Initialization()
        {
            using (await m_DeferLock.Lock(CancellationToken.None))
            {
                var versionedData = await repository.synchronizer.Sync<T>(address, disposeCancellationToken);
                m_CurrentBranch.value.value = versionedData.data;
                version = versionedData.version;
            }
        }
        
        public void Inquire()
        {
            if (m_Disposed)
            {
                throw new System.Exception("Store is disposed");
            }
            inquired++;
            
        }
        
        public void Release()
        {
            if (m_Disposed)
            {
                throw new System.Exception("Store is disposed");
            }
            inquired--;
            if (inquired == 0)
            {
                Dispose();
            }
        }
        
        /// <summary>
        /// Add new branch
        /// </summary>
        /// <param name="name"></param>
        /// <param name="makeCurrent"></param>
        /// <returns></returns>
        public DisposableAction AddBranch(string name, bool makeCurrent = false)
        {
            if (m_Branches.ContainsKey(name))
            {
                throw new System.Exception($"Branch with name {name} already exists");
            }
            var newBranch = m_Branches[name] = new Branch(this, name, m_CurrentBranch.value.value);
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
            m_CurrentBranch.value.value = value;
        }
        
        /// <summary>
        /// Get value
        /// </summary>
        /// <returns></returns>
        public T Get()
        {
            return m_CurrentBranch.value.value;
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
                await this.repository.localStorage.Set(address, branch.value, version + 1, cancellationToken);
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
            using (await m_DeferLock.Lock(cancellationToken))
            {
                var versionedData = await repository.synchronizer.Sync<T>(address, cancellationToken);
                m_CurrentBranch.value.value = versionedData.data;
                version = versionedData.version;
            }
        }
        
        /// <summary>
        /// Reset current branch to the last saved state (or all branches if populate is true)
        /// </summary>
        /// <param name="populate"></param>
        /// <param name="cancellationToken"></param>
        public async UniTask Reset(bool populate, CancellationToken cancellationToken)
        {
            using (await m_DeferLock.Lock(cancellationToken))
            {
                var (data, metadata, version) = await repository.localStorage.Get<T>(address, CancellationToken.None);
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
    }
}