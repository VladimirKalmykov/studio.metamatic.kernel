
using Cysharp.Threading.Tasks;
using Metamatic;
using Metamatic.Effects;
using UnityEngine;

namespace Metamatic.Framework.Workshop
{
    public class HealthService: Service
    {
        /*
        [SerializeField]
        private float m_MinMagnitudeToGetDamage = 1f;
        [Tooltip("The impact magnitude to get one damage. E.g. hit magnitude of 10 will deal 10 damage.")]
        [SerializeField]
        private float m_ImpactMagnitudeToGetOneDamage = 1f;
        */
        protected override async UniTask RunService(Ready ready, CancellationSignal signal)
        {
            await ready();
        }

        public static void DieRequest(Object killer, Health health)
        {
            /* Create die effect */
            Effect<DieEffect>.Spawn(killer, health, default, true);
            /* Confirm die */
            health.Kill(killer);
        }
    }
}