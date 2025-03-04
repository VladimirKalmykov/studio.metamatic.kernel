using System;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using Metamatic.SaveSystem;
using UnityEngine;

namespace Metamatic.Framework.Workshop
{
    /// <summary>
    /// Entity component is a base class for all other components that are attached to an Entity. E.g. Health, Inventory, etc.
    /// </summary>
    [RequireComponent(typeof(Entity))]
    public abstract class EntityComponent: MonoBehaviour, ISavable
    {
        [Version("1.0.0")]
        public class SaveData
        {
            public Type ComponentType;
            public object Data;

            /// <summary>
            /// Serialization constructor
            /// </summary>
            public SaveData()
            {
                
            }
            
            public SaveData(EntityComponent component, object data)
            {
                ComponentType = component.GetType();
                this.Data = data;
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

        protected virtual void Start()
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
            OnDestroyEvent += m_Entity.SubscribeReady(OnInitializeInternal);
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
            OnBeforeSave();
            return new SaveData(this, State);
        }

        public override UniTask Load(object data)
        {
            if (data == null) return UniTask.CompletedTask;
            State = (T)((data as SaveData)?.Data);
            OnAfterLoad();
            return UniTask.CompletedTask;
        }
        
        protected virtual void OnBeforeSave()
        {
            
        }
        
        protected virtual void OnAfterLoad()
        {
            
        }
    }
}