using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Metamatic
{
    /// <summary>
    /// An event that has an initiator
    /// </summary>
    public abstract class InstantAction<ResultT>
        where ResultT: struct
    {
        public delegate void Modifier(MutablePayload<ResultT> value, CancellationToken cancellationToken);
        private event Modifier OnInvoke;
        private event Action OnEmit;
        public readonly List<CancellationTokenSource> processes = new List<CancellationTokenSource>();
        public Behaviour initiator
        {
            get;
            private set;
        }

        private readonly MutablePayload<ResultT> m_DefaultPayload = new MutablePayload<ResultT>(default, true);

        /// <summary>
        /// Invoke action with payload. If there had no handlers attached, payload won't be created.
        /// </summary>
        /// <param name="initiator"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected MutablePayload<ResultT> Invoke(Behaviour initiator, CancellationToken cancellationToken = default)
        {
            this.initiator = initiator;
            OnEmit?.Invoke();
            if (OnInvoke == null) return null;
            var payload = new MutablePayload<ResultT>();
            OnInvoke?.Invoke(payload, cancellationToken);
            return payload;
        }
        
        public void AddHandler(Modifier modifier)
        {
            OnInvoke += modifier;
        }

        public void RemoveHandler(Modifier modifier)
        {
            OnInvoke -= modifier;
        }
        
        public void AddListener(Action listener)
        {
            OnEmit += listener;
        }

        public void RemoveListener(Action listener)
        {
            OnEmit -= listener;
        }
    }
}