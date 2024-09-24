using System;
using Cysharp.Threading.Tasks;
using Metamatic;
using Metamatic.Effects;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Metamatic.Framework.Workshop
{
    public class Health: EntityComponent
    {
        #region Fields
        [SerializeField] private float m_DefaultMaxHealth;
        [Tooltip("Auto heal per second")]
        [SerializeField] public float m_AutoHeal = 0f;
        [SerializeField][Tooltip("Entity can not die")]
        public bool immortal;
        [SerializeField][Tooltip("Entity can not take damage")]
        public bool invulnerable;
        #endregion
        
        
        public float maxHealth { get; set; }
        [ShowInInspector][ReadOnly]
        public float currentHealth { get; set; }
        /// <summary>
        /// Determine if the entity is alive (not killed)
        /// </summary>
        public bool isAlive => !m_IsKilled;
        
        private event Action<Object> OnDie;
        private event Action<Object> OnResurrect;
        private event Action<float> OnDamage;
        private bool m_IsKilled = false;
        
        public (float damage, ImpactType impactType, float time) RecentDamage { get; private set; }
        
        protected override void Awake()
        {
            base.Awake();
            maxHealth = m_DefaultMaxHealth;
            currentHealth = maxHealth;
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
                currentHealth = maxHealth;
            }
        }

        public void TakeDamage(Object producer, ImpactType impactType, float damage)
        {
            #if METAMATIC_VERBOSE_LOG
            Console.Verbose("Health",$"ApplyDamage {damage} to {gameObject.name}");            
            #endif
            if (currentHealth <= 0)
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
            currentHealth = Mathf.Max(currentHealth - damage);
            if (currentHealth <= 0 && immortal == false)
            {
                HealthService.DieRequest(producer, this);
            }
        }
        
        /// <summary>
        /// Heal the entity
        /// </summary>
        /// <param name="hp">Quantity of hp</param>
        /// <param name="overHeal">Can be healed more that max health</param>
        public void TakeHeal(float hp, bool overHeal = false)
        {
            if (currentHealth <= 0)
            {
                /* Already dead */
                return;
            }
            currentHealth = overHeal ? (currentHealth + hp) : Mathf.Min(maxHealth, currentHealth + hp);
        }
        
        /// <summary>
        /// Immediately kill the entity
        /// </summary>
        /// <param name="killer"></param>
        public void Kill(Object killer)
        {
            m_IsKilled = true;
            OnDie?.Invoke(killer);
        }
        
        public void Resurrect(Object resurrectionist, float health = 0)
        {
            m_IsKilled = false;
            currentHealth = health != 0 ? health : maxHealth;
            OnResurrect?.Invoke(resurrectionist);
        }
        
        /// <summary>
        /// Subscribe to die event (event contains the killer)
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public DisposableAction SubscribeToDie(Action<Object> handler)
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

        #region MyRegion

        private void Update()
        {
            if (m_AutoHeal > 0)
            {
                currentHealth = Mathf.Min(maxHealth, currentHealth + m_AutoHeal * Time.deltaTime);
            }
        }

        #endregion

        protected override UniTask OnEntityRun(Ready ready, CancellationSignal signal)
        {
            return ready();
        }

        protected override void OnEntityReset()
        {
            currentHealth = maxHealth;
            m_IsKilled = false;
        }
    }
}