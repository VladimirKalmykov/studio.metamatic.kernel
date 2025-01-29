using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    /// <summary>
    /// Leased resource is a resource that is leased from the pool (or some other sync storage) and should be returned to the pool after usage.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public readonly struct LeasedResource<T>: IDisposable
    {
        private static readonly Action<T> DefaultReturn = _ => { };
        public static readonly LeasedResource<T> Empty = new LeasedResource<T>(default, DefaultReturn, _ => false, false); 
        private readonly T m_Property;
        private readonly Action<T> m_Return;
        private readonly Func<T, bool> m_IsInPool;
        private readonly bool m_IsNew;
        public bool isEmpty => m_Return == DefaultReturn;
        /// <summary>
        /// Get the resource synchronously. Try to use this this property only if you are sure that the resource is already initialized.
        /// </summary>
        public T syncResource => m_IsInPool(m_Property) ? default : m_Property;

        public UniTask<T> resource
        {
            get
            {
                if (m_IsNew)
                {
                    return GetAsync(default);
                }

                return UniTask.FromResult(m_Property);
            }
        }
        
        public LeasedResource(T property, Action<T> returnAction, Func<T, bool> isInPool, bool isNew)
        {
            m_Property = property;
            m_Return = returnAction;
            m_IsInPool = isInPool;
            m_IsNew = isNew;
        }
        
        /// <summary>
        /// Get the resource asynchronously on the next frame to ensure that resource is initialized (e.g. for MonoBehaviour objects).
        /// </summary>
        /// <param name="cancellationSignal"></param>
        /// <returns></returns>
        public async UniTask<T> GetAsync(Signal cancellationSignal)
        {
            await UniTask.DelayFrame(1);
            cancellationSignal.ThrowIfRaised();
            return m_Property;
        }


        public void Dispose()
        {
            if (!m_IsInPool(m_Property))
            {
                m_Return(m_Property);
            }
        }
    }
}