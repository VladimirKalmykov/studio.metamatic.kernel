using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Metamatic.Framework.Workshop
{
    /// <summary>
    /// Responsible for automatically spawning entities
    /// </summary>
    public class SpawnService: Service
    {
        public struct TrackedEntity
        {
            public string entityId;
            public float lastDismissedTime;
        }
        private List<TrackedEntity> m_TrackedEntities = new();
        protected override UniTask RunService(Ready ready, Signal signal)
        {
            return ready();
        }
    }
}