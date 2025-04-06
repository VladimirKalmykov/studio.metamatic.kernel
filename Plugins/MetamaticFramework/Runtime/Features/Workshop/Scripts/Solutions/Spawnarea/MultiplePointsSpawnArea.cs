using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metamatic.Framework.Workshop
{
    public class MultiplePointsSpawnArea : SpawnArea
    {
        [ShowInInspector]
        [SerializeField] private List<Spawnpoint> m_Spawnpoints = new();
        
        #if UNITY_EDITOR
        [SerializeField] [HideInInspector] public bool editor_ShowGrid = false;
        [SerializeField] [HideInInspector] public bool editor_SnapToGrid = false;
        [SerializeField] [HideInInspector] public Grid2DSettings editor_GridSettings = default;
        #endif

        public IReadOnlyList<Spawnpoint> GetSpawnpoints() => m_Spawnpoints;

        public Spawnpoint CreateSpawnpoint(Vector3 position, Quaternion rotation)
        {
            string id = null;
            bool idIsValid = false;
            byte attempts = 0;

            while (!idIsValid && ++attempts < 10)
            {
                id = IdsGenerator.GenerateShortGuid();
                // Check if the id is unique
                if (m_Spawnpoints == null || !m_Spawnpoints.Any(sp => sp.id == id))
                {
                    idIsValid = true;
                }
            }
            var spawnpoint = new Spawnpoint
            {
                id = id,
                position = position,
                rotation = rotation
            };
            m_Spawnpoints.Add(spawnpoint);
            return spawnpoint;
        }

        public bool RemoveSpawnpoint(string id)
        {
            var spawnpoint = m_Spawnpoints.FirstOrDefault(sp => sp.id == id);
            if (spawnpoint.id != null)
            {
                m_Spawnpoints.Remove(spawnpoint);
                return true;
            }
            return false;
        }

        public bool TryGetSpawnpoint(string id, out Spawnpoint spawnpoint)
        {
            spawnpoint = m_Spawnpoints.FirstOrDefault(sp => sp.id == id);
            if (spawnpoint.id != null)
            {
                return true;
            }
            return false;
        }

        public override bool TryGetSpawnpoint(out Spawnpoint spawnpoint)
        {
            if (m_Spawnpoints.Count == 0)
            {
                spawnpoint = default;
                return false;
            }

            spawnpoint = m_Spawnpoints[Random.Range(0, m_Spawnpoints.Count)];
            return true;
        }

        public override IEnumerator<Spawnpoint> GetEnumerator()
        {
            foreach (var spawnpoint in m_Spawnpoints)
            {
                yield return spawnpoint;
            }
        }

        public override bool TryGetSpawnPoint(string spawnpointId, out Spawnpoint spawnpoint)
        {
            spawnpoint = m_Spawnpoints.FirstOrDefault(sp => sp.id == spawnpointId);
            if (spawnpoint != null && spawnpoint.id != null)
            {
                return true;
            }
            return false;
        }
    }
}