using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Framework.Workshop
{
    public abstract class EntityAbility: MonoBehaviour
    {
        private Entity m_Entity;
        private bool m_EntityIsSet;
        private event Action OnDestroyEvent;
        private bool m_IsInitialized;
        private Action m_OnInitialized;
        private SignalSource m_DestroySignalSource = default;
        
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
        
        protected virtual void Awake()
        {
            m_Entity = GetComponent<Entity>();
            m_Entity.RegisterAbility(this);
            OnDestroyEvent += entity.SubscribeToEntityReset(OnEntityReset);
            m_DestroySignalSource = SignalSource.Create();
            OnAbilityInitialize();
            m_IsInitialized = true;
            m_OnInitialized?.Invoke();
            m_OnInitialized = null;
        }
        
        public UniTask WhenInitialized(Signal cancellationSignal = default)
        {
            if (m_IsInitialized)
            {
                return UniTask.CompletedTask;
            }

            var tcs = SafeUniTaskCompletionSource.Create();
            DisposableAction registration = default;
            registration = cancellationSignal.Register(OnCancelled);
            m_OnInitialized += OnInitialized;
            return tcs.Task;

            void OnCancelled()
            {
                m_OnInitialized -= OnInitialized;
                tcs.TrySetCanceled();
            }
            
            void OnInitialized()
            {
                registration.Dispose();
                tcs.TrySetResult();
            }
        }

        protected virtual void Start()
        {
            
        }
        
        protected virtual void OnDestroy()
        {
            if (m_Entity != null)
            {
                entity.UnregisterAbility(this);
            }
            
            OnDestroyEvent?.Invoke();
        }

        protected virtual void OnEntityReset()
        {
        }

        protected virtual void OnAbilityInitialize()
        {
        }
    }
}