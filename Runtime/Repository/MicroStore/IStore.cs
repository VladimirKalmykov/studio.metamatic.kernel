using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public interface IStore: IDisposable, IAcquirable
    {
        public enum DataSource
        {
            Default,
            Fallback,
            Persistent,
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

        public UniTask Pull(CancellationToken token = default);
        public UniTask Save(CancellationToken token = default);
        public UniTask WaitUntilIdle(CancellationToken cancellationToken = default);
        public UniTask WaitUntilSynchronized(CancellationToken cancellationToken = default);
        public void CommitChanges();
        public UniTask CommitChanges(bool save, CancellationToken token = default);
        public UniTask<IDisposable> Lock(CancellationToken cancellationToken);
    }
    public interface IStore<T>: IStore
        where T : struct
    {
        public delegate void MutateHandler(ref T value);
        public class Branch: IAcquirable, IDisposable
        {
            private readonly MicroStore<T> m_Store;
            public readonly string name;
            public readonly bool locked;
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
                this.locked = locked;
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
        public T value { get; }
        public void SetValue(T nextValue);
        public void Mutate(MutateHandler mutationHandler);
        public void TransformValue(Func<T, T> transformHandler);
        public DisposableAction Subscribe(Action<T> handler);
        public Action SubscribeMetadata(Action<T, Metadata> handler);
        public UniTask Pull(bool populate = false, CancellationToken token = default, T defaultValue = default);
        public UniTask<T> WaitUntilValueIs(Func<T, bool> condition, CancellationToken cancellationToken = default);
        public UniTask CommitChanges(Func<T, T, T> commitHandler, bool save = false, CancellationToken token = default);
        public UniTask Populate(Func<T, T> populateHandler, bool save = false, CancellationToken token = default);
        public IDisposable Checkout(string branchName = "default", bool locked = false);
    }
}