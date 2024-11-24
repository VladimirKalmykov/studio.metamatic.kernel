
using Cysharp.Threading.Tasks;
using Metamatic;
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
    }
}