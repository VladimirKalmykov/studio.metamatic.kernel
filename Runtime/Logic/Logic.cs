using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Metamatic
{
    /// <summary>
    /// Powerful mechanism to implement game logic separating the mono behaviour script.
    /// </summary>
    /// <usage>
    ///  1. Create derived class with implementation of your logic.
    ///  2. Create struct for TSettings and TState
    ///  3. Instantiate class and pass any behaviour component as first argument
    ///  4. Create class derived from LogicAsset and create asset with serialized settings 
    /// </usage>
    /// <details>
    /// Interaction with logic:
    /// 1. U can use state, setState API just like with statefulBehaviour.
    /// 2. U can listen for events
    /// </details>
    public abstract class Logic
    {
        public class EventData
        {
            public class EventHandler
            {
                public readonly Type type;
                public readonly Action<EventData> syntheticHandler;

                public EventHandler(Action<EventData> handler, Type type)
                {
                    this.type = type;
                    syntheticHandler = handler;
                }
            }

            public class EventHandler<T>: EventHandler
            {
                
                public readonly Action<T> originalHandler;
                public EventHandler(Action<T> handler, Action<EventData> syntheticHandler): base(syntheticHandler, typeof(T))
                {
                    originalHandler = handler;
                }
            }
            public readonly Type type;

            public EventData(Type type)
            {
                this.type = type;
            }
        }

        public class EventData<T> : EventData
            where T : notnull
        {
            public readonly T data;
            public EventData(T data): base(typeof(T))
            {
                this.data = data;
            }
        }

        public abstract void Dispose();
        private List<EventData.EventHandler> listeners = new List<EventData.EventHandler>();
        protected void Emit<T>(T data) where T : notnull
        {
            var type = typeof(T);
            var eventData = new EventData<T>(data);
            var localListeners = listeners.Where(l => l.type == type).ToArray();
            for (var i = 0; i < localListeners.Count(); i++)
            {
                listeners[i].syntheticHandler(eventData);
            }
        }

        public void AddListener<T>(Action<T> handler)
        {
            Action<EventData> syntheticHandler = eventData =>
            {
                var typedEventData = eventData as EventData<T>;
                if (typedEventData != null)
                {
                    handler((eventData as EventData<T>).data);
                }
            };
            var eventHandler = new EventData.EventHandler<T>(handler, syntheticHandler);
            listeners.Add(eventHandler);
        }

        public void RemoveListener<T>(Action<T> handler)
        {
            var eventHandlerIndex = listeners.FindIndex(eh =>
            {
                var typedEv = eh as EventData.EventHandler<T>;
                return typedEv != null && handler == typedEv.originalHandler;
            });
            if (eventHandlerIndex >= 0)
            {
                listeners.RemoveAt(eventHandlerIndex);
            }
        }
    }

    public abstract class Logic<TState>: Logic
    where TState: struct
    {
        #region State implementations
        private TState m_Stateful_state;
        private TState m_Stateful_prevState;
        private bool m_Stateful_shouldUpdate = false;
        public TState state {
            get { return m_Stateful_state; }
            protected set {
                m_Stateful_state = value;
                m_Stateful_shouldUpdate = true;
            }
        }
        public event Action<TState, TState> StateListeners;

        protected virtual void OnStateChange(TState state, TState prevState) {}

        protected virtual void OnInitState() {}

        public void SetState(System.Func<TState, TState> modifier) {
            state = modifier(state);
        }

        public void SetState(TState nextState) {
            state = nextState;
        }

        public Action SubscribeState(Action<TState, TState> handler) {
            StateListeners += handler;
            handler(state, default);
            return () => {
                UnsubscribeState(handler);
            };
        }

        public void UnsubscribeState(Action<TState, TState> handler) {
            StateListeners -= handler;
        }

        protected void StateTick()
        {
            if (!m_Stateful_shouldUpdate) return;
            m_Stateful_shouldUpdate = false;
            OnStateChange(m_Stateful_state, m_Stateful_prevState);
            StateListeners?.Invoke(m_Stateful_state, m_Stateful_prevState);
            m_Stateful_prevState = m_Stateful_state;
        }
        #endregion

        public Logic(TState initialState)
        {
            m_Stateful_state = initialState;
        }
    }

    public abstract class Logic<TSettings, TState>: Logic<TState>
    where TSettings: struct
    where TState: struct
    {
        protected readonly BehaviourTicker ticker;
        protected readonly TSettings settings;
        protected readonly Behaviour behaviour;
        public Logic(Behaviour behaviour, TSettings settings, TState state) : base(state)
        {
            this.behaviour = behaviour;
            this.settings = settings;
            
            /* Connect special ticker to the behaviour */
            this.ticker = this.behaviour.gameObject.AddComponent<BehaviourTicker>();
            this.ticker.OnLateUpdate += StateTick;
            
            this.settings = settings;
        }
    }
}