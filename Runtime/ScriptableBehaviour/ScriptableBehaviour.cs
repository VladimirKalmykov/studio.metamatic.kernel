using System;

namespace Metamatic {
    using UnityEngine;

    /// <summary>
    /// Allows to implement all typical mono behaviour events but in scriptable object (can be connected to any gameobject by pluggable behavior)
    /// </summary>
    public abstract class ScriptableBehaviour : ScriptableObject
    {
        [HideInInspector, NonSerialized]
        public GameObject gameObject;
        protected Transform transform {
            get {
                return gameObject.transform;
            }
        }
        /// <summary>
        /// Scriptable behaviour attached to mono script event
        /// </summary>
        public virtual void OnBehaviourAttach() {}
        /// <summary>
        /// Scriptable behaviour started (when scriptable behaviour attached and started by pluggable behaviour)
        /// </summary>
        public virtual void OnBehaviourStart() {}
        /// <summary>
        /// Mono behaviour enabled
        /// </summary>
        public virtual void OnBehaviourEnable() {}
        public virtual void OnBehaviourUpdate() {}
        public virtual void OnBehaviourFixedUpdate() {}
        public virtual void OnBehaviourLateUpdate() {}
        /// <summary>
        /// Mono behaviour disabled
        /// </summary>
        public virtual void OnBehaviourDisable() {}
        /// <summary>
        /// Scriptable behaviour stopped
        /// </summary>
        public virtual void OnBehaviourStop() {}
        /// <summary>
        /// Scriptable behaviour detached
        /// </summary>
        public virtual void OnBehaviourDetach() {}
        /// <summary>
        /// Mono behaviour destroyed
        /// </summary>
        public virtual void OnBehaviourDestroy() {}
    }
}