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
        private readonly SignedIndexedPool<T> m_Instances = new SignedIndexedPool<T>(100);
        public InstantiateMonoBehaviourSummoner(T prefab) : base()
        {
            m_Prefab = prefab;
        }
        protected override (T, Token96Bit) OnSummon()
        {
            var instance =  UnityEngine.Object.Instantiate(m_Prefab);
            return (instance, m_Instances.Add(instance));
        }

        protected override void OnBanish(Token96Bit token)
        {
            if (m_Instances.Remove(token, out var instance))
            {
                UnityEngine.Object.Destroy(instance);
            }
        }
    }
}