using System;

namespace Metamatic.Summoners
{
    /// <summary>
    /// Allows to summon specified prefab's instance from a pool.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Obsolete("Use ConfigurablePool<T>.GetSummoner instead.")]
    public class ComponentPrefabFromPoolSummoner<T>: SummonerLegacy<T>, IDisposable where T: UnityEngine.Component
    {
        private readonly ConfigurablePool<T> m_Pool;
        
        public ComponentPrefabFromPoolSummoner(T prefab, int initialCapacity = 0, int maxCapacity = int.MaxValue)
        {
            m_Pool = new ConfigurablePool<T>(new ComponentPrefabPoolConfig<T>(prefab), initialCapacity, maxCapacity);
        }
        
        protected override (T, Token96Bit) OnSummon()
        {
            return m_Pool.Get();
        }

        protected override void OnBanish(Token96Bit token)
        {
            m_Pool.Return(token);
        }

        public void Dispose()
        {
            m_Pool?.Dispose();
        }
    }
}