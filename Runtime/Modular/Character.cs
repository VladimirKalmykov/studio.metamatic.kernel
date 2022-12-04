using System;

namespace Metamatic {
    using System.Collections.Generic;
    using UnityEngine;

    public class Character: Entity, ISerializationCallbackReceiver {
        public static List<Character> all = new List<Character>();
        
        public CharacterType characterType;
        public Fraction fraction;
        /// <summary>
        /// Is current object alive (animated, live, spirited). It can be creature, robot, etc.
        /// </summary>
        public bool animated = false;
        
        #region Public Events
        public event Action<bool> OnCanControlChange;
        #endregion

        private void Awake() {
            all.Add(this);
        }

        private void OnDestroy() {
            all.Remove(this);
        }

        public void OnBeforeSerialize()
        {
            // Skip
        }

        public void OnAfterDeserialize()
        {
            // Skip
        }
    }
}