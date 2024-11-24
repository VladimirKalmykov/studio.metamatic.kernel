using System;
using Cysharp.Threading.Tasks;
using Metamatic;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Metamatic.Framework.Workshop
{
    [Serializable]
    public struct HealthState
    {
        public float maxHealth;
        public float health;
    }
    public class Health: EntityComponent<HealthState>
    {
        #region Fields
        [SerializeField] private float m_DefaultMaxHealth;
        [Tooltip("Auto heal per second")]
        [SerializeField] public float autoHeal = 0f;
        [SerializeField][Tooltip("Entity can not die")]
        public bool immortal;
        [SerializeField][Tooltip("Entity can not take damage")]
        public bool invulnerable;
        #endregion
        [ShowInInspector, ReadOnly]
        public float maxHealth
        {
            get => State.maxHealth;
            set => State.maxHealth = value;
        }
        [ShowInInspector, ReadOnly]
        public float health {
            get => State.health;
            set => State.health = value;
        }
        /// <summary>
        /// Determine if the entity is alive (not killed)
        /// </summary>
        public bool isAlive => !m_IsKilled;
        
        private event Action<GameObject> OnDie;
        private event Action<GameObject> OnResurrect;
        private event Action<float> OnDamage;
        private bool m_IsKilled = false;
        
        public (float damage, ImpactType impactType, float time) RecentDamage { get; private set; }

        protected override HealthState GetDefaultState()
        {
            return new HealthState()
            {
                maxHealth = m_DefaultMaxHealth,
                health = m_DefaultMaxHealth
            };
        }

        /// <summary>
        /// Set max health of the entity
        /// </summary>
        /// <param name="health"></param>
        /// <param name="resetCurrent">Set current health to max value</param>
        public void SetMaxHealth(float health, bool resetCurrent = false)
        {
            maxHealth = health;
            if (resetCurrent)
            {
                this.health = maxHealth;
            }
        }

        public void TakeDamage(GameObject producer, ImpactType impactType, float damage)
        {
            #if METAMATIC_VERBOSE_LOG
            Console.Verbose("Health",$"ApplyDamage {damage} to {gameObject.name}");            
            #endif
            if (health <= 0)
            {
                /* Already dead */
                return;
            }
            if (invulnerable)
            {
                /* Can not take damage */
                return;
            }
            OnDamage?.Invoke(damage);
            RecentDamage = (damage, impactType, Time.time);
            health = Mathf.Max(health - damage);
            if (health <= 0 && immortal == false)
            {
                Effect<KillSpell>.Cast(producer, gameObject);
            }
        }
        
        /// <summary>
        /// Heal the entity
        /// </summary>
        /// <param name="hp">Quantity of hp</param>
        /// <param name="overHeal">Can be healed more that max health</param>
        public void TakeHeal(float hp, bool overHeal = false)
        {
            if (health <= 0)
            {
                /* Already dead */
                return;
            }
            health = overHeal ? (health + hp) : Mathf.Min(maxHealth, health + hp);
        }
        
        /// <summary>
        /// Immediately kill the entity
        /// </summary>
        /// <param name="killer"></param>
        public void ConfirmedKill(GameObject killer)
        {
            m_IsKilled = true;
            OnDie?.Invoke(killer);
        }
        
        public void Resurrect(GameObject resurrectionist, float health = 0)
        {
            m_IsKilled = false;
            this.health = health != 0 ? health : maxHealth;
            OnResurrect?.Invoke(resurrectionist);
        }
        
        /// <summary>
        /// Subscribe to die event (event contains the killer)
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public DisposableAction SubscribeToDie(Action<GameObject> handler)
        {
            OnDie += handler;
            return new DisposableAction(() => OnDie -= handler);
        }
        
        public DisposableAction SubscribeToResurrect(Action<Object> handler)
        {
            OnResurrect += handler;
            return new DisposableAction(() => OnResurrect -= handler);
        }
        
        public DisposableAction SubscribeToDamage(Action<float> handler)
        {
            OnDamage += handler;
            return new DisposableAction(() => OnDamage -= handler);
        }

        protected override void OnEntityReset()
        {
            health = maxHealth;
            m_IsKilled = false;
        }

        protected override void OnEntityUpdate(float deltaTime)
        {
            if (autoHeal > 0)
            {
                health = Mathf.Min(maxHealth, health + autoHeal * deltaTime);
            }
        }
    }
}