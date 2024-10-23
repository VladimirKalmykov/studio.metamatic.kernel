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
        public abstract Type stateType { get; }
        
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
    
    public abstract class EntityComponent<T>: EntityComponent
    {
        protected T State;
        public delegate void StateModifier(ref T state);
        public override Type stateType => typeof(T);
        public T GetState() => State;
        public ref T GetStateRef() => ref State;

        public T state
        {
            get => State;
            set => State = value;
        }
        public ref T stateRef => ref State;
        
        public void SetState(T state)
        {
            State = state;
        }
        
        public void EditState(StateModifier modifier)
        {
            modifier(ref State);
        }

        protected override void Awake()
        {
            base.Awake();
            State = GetDefaultState();
        }
        
        protected override void OnEntityReset() => State = GetDefaultState();

        protected abstract T GetDefaultState();
    }
}