using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Framework.Workshop
{
    /// <summary>
    /// Entity component is a base class for all other components that are attached to an Entity. E.g. Health, Inventory, etc.
    /// </summary>
    [RequireComponent(typeof(Entity))]
    public abstract class EntityComponent: MonoBehaviour
    {
        private float m_LastTimeUpdate;
        private event Action OnDestroyEvent;
        private Entity m_Entity;
        
        #region Public properties
        public Entity entity
        {
            get
            {
                if (m_Entity == null)
                {
                    m_Entity = GetComponent<Entity>();
                }

                return m_Entity;
            }
        }
        #endregion
        
        #region Mono

        private void Start()
        {
            m_LastTimeUpdate = Time.time;
        }
        protected virtual void Awake()
        {
            entity.Components.Add(this);
            OnDestroyEvent += entity.SubscribeToEntityReset(OnEntityReset);
            OnDestroyEvent += entity.SubscribeToEntityUpdate(InternalOnEntityUpdate);
        }
        protected virtual void OnDestroy()
        {
            if (entity != null)
            {
                entity.Components.Remove(this);
            }
            
            OnDestroyEvent?.Invoke();
        }
        #endregion

        private void InternalOnEntityUpdate()
        {
            var deltaTime = Time.time - m_LastTimeUpdate;
            m_LastTimeUpdate = Time.time;
            OnEntityUpdate(deltaTime);
        }

        protected abstract void OnEntityReset();
        protected abstract void OnEntityUpdate(float deltaTime);
    }
}