
using Cysharp.Threading.Tasks;
using Metamatic;
using Metamatic.Effects;
using UnityEngine;

namespace Metamatic.Framework.Workshop
{
    /// <summary>
    /// Service considers health of the entities and reacts to it. Character can not die without this service.
    /// </summary>
    public class HealthService: Service
    {
        protected override async UniTask RunService(Ready ready, CancellationSignal signal)
        {
            await ready();
        }

        public static void DieRequest(Object killer, Health health)
        {
            /* Create die effect */
            Effect<DieEffect>.Spawn(killer, health, default);
            /* Confirm die */
            health.Kill(killer);
        }
    }
}