using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Metamatic.Framework.Workshop;
using UnityEngine;

namespace Metamatic.Framework
{
    public static class PlayerSystem
    {
        private static SignedIndexedHashPool<Entity> m_PlayersRegistry = new SignedIndexedHashPool<Entity>(4);
        private static Dictionary<int, Entity> m_Players = new Dictionary<int, Entity>();
        public static event Action<Entity, int> OnPlayerRegistered;
        public static event Action<Entity, int> OnPlayerUnregistered;
        
        public static bool TryGetPlayer(int index, out Entity player)
        {
            return m_Players.TryGetValue(index, out player);
        }
        
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
        public static DisposableAction RegisterPlayer(Entity player, int index)
        {
            var token = m_PlayersRegistry.Add(player);
            if (!m_Players.TryAdd(index, player))
            {
                Console.Error("PlayerSystem", "Player with index " + index + " already exists.");
            }
            else
            {
                OnPlayerRegistered?.Invoke(player, index);
            }

            return DisposableAction.Create(() =>
            {
                m_Players.Remove(index);
                m_PlayersRegistry.Remove(token);
                OnPlayerUnregistered?.Invoke(player, index);
            });
        }
        
        public static DisposableAction WatchPlayer(Action<Entity> action, int index)
        {
            if (m_Players.TryGetValue(index, out var player))
            {
                action(player);
            }
            OnPlayerRegistered += OnPlayerRegisteredHandler;

            return DisposableAction.Create(() =>
            {
                OnPlayerRegistered -= OnPlayerRegisteredHandler;
            });
            
            void OnPlayerRegisteredHandler(Entity entity, int i)
            {
                if (i == index)
                {
                    action(entity);
                }
            }
        }

        public static DisposableAction WatchPlayerAsync(Func<Entity, Signal, UniTaskVoid> handler, int index)
        {
            SignalSource css = default;
            if (m_Players.TryGetValue(index, out var player))
            {
                css.Dispose();
                css = SignalSource.Create();
                handler(player, css.Signal).Forget();
            }
            OnPlayerRegistered += OnPlayerRegisteredHandler;
            OnPlayerUnregistered += OnPlayerUnregisteredHandler;

            return DisposableAction.Create(() =>
            {
                css.Dispose();
                OnPlayerRegistered -= OnPlayerRegisteredHandler;
                OnPlayerUnregistered -= OnPlayerUnregisteredHandler;
            });
            
            void OnPlayerRegisteredHandler(Entity entity, int i)
            {
                if (i == index)
                {
                    css.Dispose();
                    css = SignalSource.Create();
                    handler(entity, css.Signal).Forget();
                }
            }
            
            void OnPlayerUnregisteredHandler(Entity entity, int i)
            {
                if (i == index)
                {
                    css.Dispose();
                }
            }
        }
    }
}