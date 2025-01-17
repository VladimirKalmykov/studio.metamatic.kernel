using System;
using System.Collections.Generic;

namespace Metamatic
{
    public readonly struct Summon<T>: IDisposable
    {
        private readonly Summoner<T> m_Summoner;
        private readonly Guid m_InstanceId;
        
        public T instance => m_Summoner.GetInstance(m_InstanceId);
        
        public Summon(Summoner<T> summoner, Guid instanceId)
        {
            m_Summoner = summoner;
            m_InstanceId = instanceId;
        }

        public void Dispose()
        {
            m_Summoner.Banish(m_InstanceId);
        }
    }
    public abstract class Summoner<T>
    {
        private readonly Dictionary<Guid, T> m_Instances = new ();
        
        public Summon<T> Summon()
        {
            var instanceId = Guid.NewGuid();
            var instance = OnSummon();
            m_Instances.Add(instanceId, instance);
            return new Summon<T>(this, instanceId);
        }
        
        public void Banish(Guid guid)
        {
            if (m_Instances.Remove(guid, out var instance))
            {
                OnBanish(instance);
            }
        }
        
        public T GetInstance(Guid guid)
        {
            if (!m_Instances.ContainsKey(guid))
            {
                return default;
            }
            return m_Instances[guid];
        }
        
        protected abstract T OnSummon();
        protected abstract void OnBanish(T instance);
    }
}