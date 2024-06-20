using System;
using System.Threading;

namespace Metamatic
{
    /// <summary>
    /// Create connection bridge primary-secondary
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    public class Interaction<T1, T2>
    {
        public class Handshake
        {
            public delegate void ValueHandler(T1 value);
            public readonly Interaction<T1, T2> interaction;
            private event PayloadHandler1 OnGetValue;
            private PayloadHandler2 m_SendToAnother;
            private Action dismissHandshake;
            public event Action OnDispose;
            public Handshake(Interaction<T1, T2> interaction, ref PayloadHandler1 getvalueEvent, PayloadHandler2 internalSendFromHandshake, Action dismissHandshake)
            {
                this.interaction = interaction;
                this.m_SendToAnother = internalSendFromHandshake;
                this.dismissHandshake = dismissHandshake;
                getvalueEvent = AcceptValue;
            }

            private void AcceptValue(T1 value)
            {
                OnGetValue?.Invoke(value);
            }

            public Action Subscribe(PayloadHandler1 handler)
            {
                if (m_SendToAnother == null)
                {
                    throw new Exception("You cannot subscribe to handshake which already has dismissed");
                }
                handler(interaction.payload1);
                OnGetValue += handler;
                return () =>
                {
                    OnGetValue -= handler;
                };
            }

            public void Send(T2 value)
            {
                m_SendToAnother?.Invoke(value);
            }
            /// <summary>
            /// Dismiss handshake
            /// </summary>
            public void Dispose()
            {
                if (m_SendToAnother == null)
                {
                    // Seems like already dismissed
                    return;
                }

                OnDispose?.Invoke();
                m_SendToAnother = null;
                OnGetValue = null;
                this.dismissHandshake();
            }
        }
        public delegate void PayloadHandler1(T1 value);
        public delegate void PayloadHandler2(T2 value);
        public PayloadHandler1 SendToPrimary { get; private set; }
        public PayloadHandler2 SendToSecondary { get; private set;}
        public event Action OnDispose;
        public T1 payload1 { get; private set; }
        protected T2 payload2;
        protected Handshake handshake;
        private PayloadHandler1 HandshakeValueAccepter;
        private event PayloadHandler2 OnIncomingValueChange;
        public event Action OnHandshake;
        public event Action OnHandshakeDismiss;
        
        public Interaction(T1 initialPayload, CancellationToken cancellationToken = default)
        {
            payload1 = initialPayload;
            cancellationToken.Register(Dispose);
        }

        ~Interaction()
        {
            OnDispose?.Invoke();
        }

        public Action Subscribe(PayloadHandler2 handler)
        {
            OnIncomingValueChange += handler;

            return () =>
            {
                OnIncomingValueChange -= handler;
            };
        }

        public void Send(T1 value)
        {
            this.payload1 = value;
            HandshakeValueAccepter?.Invoke(value);
        }

        public void InternalSendFromHandshake(T2 value)
        {
            OnIncomingValueChange?.Invoke(value);
        }
        
        /// <summary>
        /// Make a connection to the interaction to have two-side data transition
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Handshake ToHandshake()
        {
            if (this.handshake != null)
            {
                throw new Exception("Interaction already has handshake. Interaction can not handshake twice. Dismiss previous handshake before create new one");
            }
            this.handshake = new Handshake(this, ref HandshakeValueAccepter, InternalSendFromHandshake, InternallyDismissHandshake);
            OnHandshake?.Invoke();
            return this.handshake;
        }

        private void InternallyDismissHandshake()
        {
            if (this.handshake == null)
            {
                throw new Exception("Handshake already dismissed");
            }
            this.HandshakeValueAccepter = null;
            this.handshake = null;
            OnHandshakeDismiss?.Invoke();
        }

        public void DismissHandshake()
        {
            if (this.handshake == null)
            {
                throw new Exception("You can not dismiss handshake which already has dismissed");
            }

            this.handshake.Dispose();
        }

        public void Dispose()
        {
            OnDispose?.Invoke();
            OnDispose = null;
        }
    }

    public class CompetitiveInteraction<T1, T2>: Interaction<T1, T2>, ICompetitive
    {
        public int priority { get; private set; }
        public Action SubscribeDispose(Action handler)
        {
            OnDispose += handler;

            return () =>
            {
                OnDispose -= handler;
            };
        }

        public CompetitiveInteraction(T1 initialPayload, int priority = 0, CancellationToken cancellationToken = default): base(initialPayload, cancellationToken)
        {
            this.priority = priority;
        }
    }
}