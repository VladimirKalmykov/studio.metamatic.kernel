using Metamatic.Framework.Workshop;
using UnityEngine;

namespace Metamatic.Framework
{
    public static class PlayerSystem
    {
        private static SignedIndexedHashPool<Entity> m_PlayersRegistry = new SignedIndexedHashPool<Entity>(4);
        
        /// <summary>
        /// Check whether the game object is a player.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public static bool IsPlayer(GameObject gameObject, out Entity entity)
        {
            if (gameObject.TryGetComponent<Entity>(out entity))
            {
                return IsPlayer(entity);
            }
            
            entity = null;
            return false;
        }
        
        /// <summary>
        /// Check whether the entity is a player.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static bool IsPlayer(Entity entity)
        {
            return m_PlayersRegistry.Contains(entity);
        }

        /// <summary>
        /// Register the player entity.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static DisposableAction RegisterPlayer(Entity player)
        {
            var token = m_PlayersRegistry.Add(player);
            
            return DisposableAction.Create(() =>
            {
                m_PlayersRegistry.Remove(token);
            });
        }
    }
}