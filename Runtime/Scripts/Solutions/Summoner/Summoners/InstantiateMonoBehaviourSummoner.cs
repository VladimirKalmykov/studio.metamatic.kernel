using UnityEngine;

namespace Metamatic.Summoners
{
    /// <summary>
    /// Simple instantiate mono behaviour summoner.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class InstantiateMonoBehaviourSummoner<T> : Summoner<T> where T : MonoBehaviour
    {
        private T m_Prefab;
        public InstantiateMonoBehaviourSummoner(T prefab) : base()
        {
            m_Prefab = prefab;
        }
        protected override T OnSummon()
        {
            return UnityEngine.Object.Instantiate(m_Prefab);
        }

        protected override void OnBanish(T instance)
        {
            UnityEngine.Object.Destroy(instance.gameObject);
        }
    }
}