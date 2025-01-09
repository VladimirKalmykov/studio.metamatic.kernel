
using Cysharp.Threading.Tasks;

namespace Metamatic.Framework.Workshop
{
    /// <summary>
    /// Service considers health of the entities and reacts to it. Character can not die without this service.
    /// </summary>
    public class HealthService: Service
    {
        protected override async UniTask RunService(Ready ready, Signal signal)
        {
            await ready();
        }
    }
}