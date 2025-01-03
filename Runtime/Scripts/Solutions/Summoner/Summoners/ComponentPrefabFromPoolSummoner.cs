using System;

namespace Metamatic.Summoners
{
    /// <summary>
    /// Allows to summon specified prefab's instance from a pool.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ComponentPrefabFromPoolSummoner<T>: Summoner<T>, IDisposable where T: UnityEngine.Component
    {
        private readonly ConfigurablePool<T> m_Pool;
        
        public ComponentPrefabFromPoolSummoner(T prefab, int initialCapacity = 0, int maxCapacity = int.MaxValue)
        {
            m_Pool = new ConfigurablePool<T>(new ComponentPrefabPoolConfig<T>(prefab), initialCapacity, maxCapacity);
        }
        
        protected override T OnSummon()
        {
            return m_Pool.Get();
        }

        protected override void OnBanish(T instance)
        {
            m_Pool.Return(instance);
        }

        public void Dispose()
        {
            m_Pool?.Dispose();
        }
    }
}