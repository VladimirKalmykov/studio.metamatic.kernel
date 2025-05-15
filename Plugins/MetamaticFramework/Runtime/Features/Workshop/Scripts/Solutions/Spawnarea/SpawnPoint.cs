using System;
using System.Collections.Generic;
using UnityEngine;

namespace Metamatic.Framework.Workshop
{
    public class SpawnPoint: SpawnArea
    {
        private Spawnpoint m_CachedSpawnpoint;
        public override bool TryGetSpawnpoint(out Spawnpoint spawnpoint)
        {
            if (m_CachedSpawnpoint == null)
            {
                m_CachedSpawnpoint = new Spawnpoint() {
                    id = id,
                    position = transform.position,
                    rotation = transform.rotation
                };
            }

            spawnpoint = m_CachedSpawnpoint;
            return true;
        }

        public override IEnumerator<Spawnpoint> GetEnumerator()
        {
            TryGetSpawnpoint(out var spawnpoint);
            yield return spawnpoint;
        }

        public override bool TryGetSpawnPoint(string spawnpointId, out Spawnpoint spawnpoint)
        {
            if (spawnpointId == id)
            {
                return TryGetSpawnpoint(out spawnpoint);
            }
            else
            {
                spawnpoint = null;
                return false;
            }
        }
        
        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Draw the spawn point in the editor
            Gizmos.color = devToolsColor;
            Gizmos.DrawSphere(transform.position, 0.5f);
        }
#endif
    }
}