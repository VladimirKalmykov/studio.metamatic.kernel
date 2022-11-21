using System;
using UnityEngine;

namespace Metamatic
{
    public abstract class DatabaseStore
    {
        public readonly DatabaseEntity entity;
        public abstract void OnFrameEnd();

        public DatabaseStore(DatabaseEntity entity)
        {
            this.entity = entity;
            Database.OnLifecycleEndFrame += OnFrameEnd;
        }
        
        public void Dispose()
        {
            Database.OnLifecycleEndFrame -= OnFrameEnd;
        }
    }

    public class DatabaseStore<TState> : DatabaseStore, IDisposable
    {
        public delegate void StateChangeHandler(ref TState state, ref TState prevState);

        public delegate void StateModifier(ref TState state);
        public TState state;
        private TState m_NextState;
        private bool m_ShouldUpdate = false;
        private bool m_UpdateInProcess = false;
        private event StateModifier m_PostUpdateModifiying = delegate {};
       
        public event StateChangeHandler stateListeners;

        protected virtual void OnStateChange(ref TState state, ref TState prevState) {}

        protected virtual void OnInitState() {}

        public void SetState(StateModifier modifier) {
            if (m_UpdateInProcess)
            {
                m_PostUpdateModifiying += modifier;
            }
            else
            {
                modifier(ref m_NextState);
                /*
                 * If the application is not playing then we need to force the update immediately
                 */
                #if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    ForceUpdateState();
                }
                #endif
            }
        }
        
        /// <summary>
        /// Set state and immediately update
        /// </summary>
        /// <param name="modifier"></param>
        /// <param name="immediate"></param>
        public void SetState(StateModifier modifier, bool immediate)
        {
            SetState(modifier);
            ForceUpdateState();
        }

        /// <summary>
        /// Return by ref next state for direct editing (fastest way to mofidy next state, use it inside cycles and frames)
        /// </summary>
        /// <returns>Next state</returns>
        public ref TState GetNextStateRef()
        {
            return ref m_NextState;
        }
        
        public DatabaseStore(DatabaseEntity entity, TState defaultState): base(entity)
        {
            state = defaultState;
            m_NextState = defaultState;
            OnInitState();
        }

        /// <summary>
        /// Force state engine to update state at the end of this frame
        /// </summary>
        public void ForceUpdateState()
        {
            m_ShouldUpdate = true;
            OnFrameEnd();
        }

        public Action SubscribeState(StateChangeHandler handler, bool skipInitial = false) {
            stateListeners += handler;
            if (!skipInitial)
            {
                handler(ref m_NextState, ref state);
            }
            return () => {
                UnsubscribeState(handler);
            };
        }

        public void UnsubscribeState(StateChangeHandler handler) {
            stateListeners -= handler;
        }

        public override void OnFrameEnd()
        {
            if (!m_ShouldUpdate) return;
            m_ShouldUpdate = false;
            m_UpdateInProcess = true;
            stateListeners?.Invoke(ref m_NextState, ref state);
            
            OnStateChange(ref m_NextState, ref state);
            state = m_NextState;
            m_PostUpdateModifiying.Invoke(ref m_NextState);
            m_PostUpdateModifiying = delegate { };
            m_UpdateInProcess = false;
        }
    }
}