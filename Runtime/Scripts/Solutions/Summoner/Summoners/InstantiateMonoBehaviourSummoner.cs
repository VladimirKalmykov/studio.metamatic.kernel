using UnityEngine;

namespace Metamatic.Summoners
{
    /// <summary>
    /// Simple instantiate mono behaviour summoner.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class InstantiateMonoBehaviourSummoner<T> : SummonerLegacy<T> where T : MonoBehaviour
    {
        private T m_Prefab;
        private IndexedPool<T> m_Instances;
        public InstantiateMonoBehaviourSummoner(T prefab) : base()
        {
            m_Prefab = prefab;
        }
        protected override (T, int) OnSummon()
        {
            var instance =  UnityEngine.Object.Instantiate(m_Prefab);
            return (instance, m_Instances.Add(instance));
        }

        protected override void OnBanish(int id)
        {
            if (m_Instances.Remove(id, out var instance))
            {
                UnityEngine.Object.Destroy(instance);
            }
        }
    }
}