using Cysharp.Threading.Tasks;

namespace Metamatic.Framework.Workshop
{
    public class CheckpointsService: Service
    {
        protected override UniTask RunService(Ready ready, Signal signal)
        {
            var launcherService = Service.Resolve<LauncherService>(signal);
            return ready();
        }
    }
}