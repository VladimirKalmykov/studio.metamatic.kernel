using System;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using Metamatic.SaveSystem;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;

namespace Metamatic.Framework.Workshop
{
    /// <summary>
    /// Entity component is a base class for all other components that are attached to an Entity. E.g. Health, Inventory, etc.
    /// </summary>
    [RequireComponent(typeof(Entity))]
    public abstract class EntityComponent: MonoBehaviour, ISavable
    {
        [DerivedVersionControl]
        [Serializable]
        public class SaveData
        {
            [JsonProperty("t")]
            public Type ComponentType;
        }
        [Serializable]
        [Version("1.0.0")]
        public class SaveData<T>: SaveData
        {
            [JsonProperty("d")]
            public T data;
            
            public SaveData(EntityComponent component, T data)
            {
                this.data = data;
                ComponentType = component.GetType();
            }
        }
        
        private float m_LastTimeUpdate;
        private event Action OnDestroyEvent;
        private Entity m_Entity;
        private bool m_EntityIsSet;
        public abstract Type stateType { get; }
        protected bool IsStateInitializedFromOutside;
        
        #region Public properties
        public Entity entity
        {
            get
            {
                if (!m_EntityIsSet)
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
            m_Entity = GetComponent<Entity>();
            m_EntityIsSet = true;
            entity.RegisterComponent(this);
            OnDestroyEvent += entity.SubscribeToEntityReset(OnEntityReset);
            OnDestroyEvent += entity.SubscribeToEntityUpdate(InternalOnEntityUpdate);
            OnDestroyEvent += m_Entity.SubscribeReady(OnInitialize);
        }
        
        protected virtual void OnDestroy()
        {
            if (entity != null)
            {
                entity.UnregisterComponent(this);
            }
            
            OnDestroyEvent?.Invoke();
        }
        #endregion

        private void OnInitializeInternal()
        {
            OnInitialize();
            if (!IsStateInitializedFromOutside)
            {
                OnInitializeStateWithDefaultValues();
            }
            InternalOnEntityUpdate();
        }

        protected void InternalOnEntityUpdate()
        {
            var deltaTime = Time.time - m_LastTimeUpdate;
            m_LastTimeUpdate = Time.time;
            OnEntityUpdate(deltaTime);
        }

        protected abstract void OnEntityReset();
        protected abstract void OnEntityUpdate(float deltaTime);
        protected abstract void OnInitializeStateWithDefaultValues();
        protected virtual void OnInitialize()
        {
            
        }

        public abstract object Save();

        public abstract UniTask Load(object data);
        public UniTask Bootstrap()
        {
            OnInitializeStateWithDefaultValues();
            return UniTask.CompletedTask;
        }
    }
    
    public abstract class EntityComponent<T>: EntityComponent
    {
        public T State;
        public delegate void StateModifier(ref T state);
        public override Type stateType => typeof(T);
        public T GetState() => State;
        public ref T GetStateRef() => ref State;

        public T state
        {
            get => State;
            set
            {
                State = value;
                IsStateInitializedFromOutside = true;
                InternalOnEntityUpdate();
            }
        }
        
        public ref T stateRef => ref State;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetState(T nextState)
        {
            State = nextState;
            IsStateInitializedFromOutside = true;
            InternalOnEntityUpdate();
        }
        
        public void EditState(StateModifier modifier)
        {
            modifier(ref State);
        }

        protected override void OnInitializeStateWithDefaultValues()
        {
            State = GetDefaultState();
        }
        
        protected override void OnEntityReset() {
            State = GetDefaultState();
        }

        protected abstract T GetDefaultState();
        
        public override object Save()
        {
            return new SaveData<T>(this, State);
        }

        public override UniTask Load(object data)
        {
            State = ((SaveData<T>) data).data;
            return UniTask.CompletedTask;
        }
    }
}