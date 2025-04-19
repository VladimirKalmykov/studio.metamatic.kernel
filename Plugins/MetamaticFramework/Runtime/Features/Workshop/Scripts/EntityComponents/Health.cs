using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metamatic.Framework.Workshop
{
    [Serializable]
    public struct HealthState
    {
        public float healthTaken;
        public float healthSpent;
    }
    public class Health: EntityStore<HealthState>
    {
        #region Exposed writable Fields
        [ShowInInspector] public float maxHealth;
        #endregion
        
        #region Exposed readonly Fields
        [ShowInInspector, HideInEditorMode] public float health { get; private set; }
        #endregion
        
        #region Fields
        [SerializeField][Tooltip("Entity can not take damage")]
        public bool invulnerable;
        #endregion
        
        #region Exposed computed properties
        [ShowInInspector, HideInEditorMode] public float healthRatio => health / maxHealth;
        #endregion

        protected override HealthState GetDefaultState()
        {
            return new HealthState()
            {
                healthTaken = 0f,
                healthSpent = 0f
            };
        }

        public void DrainHealth(float damage)
        {
            if (invulnerable)
            {
                return;
            }
            if (damage <= 0f)
            {
                return;
            }
            if (State.healthTaken <= State.healthSpent)
            {
                return;
            }
            State.healthSpent += damage;
        }
        
        // Effect<KillSpell>.Cast(producer, gameObject);

        /// <summary>
        /// Take heal
        /// </summary>
        /// <param name="hp"></param>
        /// <param name="overHeal"></param>
        public void TakeHeal(float hp, bool overHeal = false)
        {
            var nextHealthTaken = State.healthTaken + hp;
            if (nextHealthTaken - State.healthSpent <= maxHealth || overHeal)
            {
                State.healthTaken = nextHealthTaken;
            }
        }

        protected override void OnEntityReset()
        {
            OnEntityUpdate(0f);
        }

        protected override void OnEntityUpdate(float deltaTime)
        {
            health = State.healthTaken - State.healthSpent;
        }
    }
}