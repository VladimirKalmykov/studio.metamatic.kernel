using System.Collections.Generic;
using System.Linq;
using Metamatic.DevTools;

namespace Metamatic {
    using System;
    using UnityEngine;

    public abstract class StatefulBehaviour : MonoBehaviour
    {
        protected event Action OnStatePreLateUpdate;
        #if DEBUG
        [SerializeField, HideInInspector] protected string[] statful_EditorShowStateKeys;
        public bool debugStatefulBehaviour = false;
        #endif
        public class Debouncer
        {
            private float m_Duration = 1f;
            private float m_StartTime;
            private Action m_Action;
            private bool m_IsThrottle = false;
            public event Action OnDispose;

            private StatefulBehaviour m_Host;

            public Debouncer(StatefulBehaviour statefulBehaviour)
            {
                m_Host = statefulBehaviour;
            }

            public void Debounce(Action action, float duration, bool throttle = false)
            {
                if (!throttle || m_StartTime == 0f)
                {
                    m_StartTime = Time.time;
                }
                m_IsThrottle = throttle;
                m_Duration = duration;
                m_Action = action;
                m_Host.OnStatePreLateUpdate += Tick;
            }

            public void Tick()
            {
                if (m_StartTime == 0f || Time.time - m_StartTime < m_Duration) return;
                m_StartTime = 0f;
                Apply();
            }

            private void Apply()
            {
                m_StartTime = 0f;
                m_Host.OnStatePreLateUpdate -= Tick;
                m_Action.Invoke();
            }

            public void Dispose()
            {
                m_Host.OnStatePreLateUpdate -= Tick;
                m_StartTime = 0f;
                m_Action = null;
            }
        }

        public Debouncer CreateDebouncer()
        {
            var valueStabilizer = new Debouncer(this);

            return valueStabilizer;
        }

        protected void StatePreLateUpdate()
        {
            OnStatePreLateUpdate?.Invoke();
        }
        
    }
    
    public abstract class StatefulBehaviour<State>: StatefulBehaviour, ISerializationCallbackReceiver
        where State : struct
    {
        private class Branch
        {
            public string name;
            public State state;

            public Branch(string name, State currentState = default)
            {
                this.name = name;
                this.state = currentState;
            }

        }
        private Dictionary<string, Branch> stateful_Branches = new Dictionary<string, Branch>();
        private string stateful_CurrentBranchName = "master";
        public delegate void StateChangeHandler(ref State state, ref State prevState);

        public delegate void StateModifier(ref State state);
        public State state;
        private State stateful_NextState;
        private State stateful_OverloadedState;
        private bool stateful_ShouldUpdate = false;
        private bool stateful_UpdateInProcess = false;
        public event StateChangeHandler stateListeners;
        private event StateModifier m_PostUpdateModifiying = delegate {};

        protected virtual void OnStateChange(ref State state, ref State prevState) {}

        protected virtual void OnInitState() {}

        public void SetState(StateModifier modifier) {
            if (stateful_UpdateInProcess)
            {
                m_PostUpdateModifiying += modifier;
            }
            else
            {
                modifier(ref stateful_NextState);
                ForceUpdateState();
            }
        }

        public void SetState(in State nextState) {
            stateful_NextState = nextState;
            stateful_ShouldUpdate = true;
        }
        
        /// <summary>
        /// Return by ref next state for direct editing (fastest way to mofidy next state, use it inside cycles and frames)
        /// </summary>
        /// <returns>Next state</returns>
        public ref State GetNextStateRef()
        {
            if (stateful_UpdateInProcess)
            {
                throw new Exception("You can not get reference to state while update in process");
            }
            return ref stateful_NextState;
        }
        
        /// <summary>
        /// Force state engine to update state at the end of this frame
        /// </summary>
        public void ForceUpdateState()
        {
            stateful_ShouldUpdate = true;
        }

        public Action SubscribeState(StateChangeHandler handler) {
            stateListeners += handler;
            handler(ref state, ref state);
            return () => {
                UnsubscribeState(handler);
            };
        }

        public void UnsubscribeState(StateChangeHandler handler) {
            stateListeners -= handler;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() {}
        
        #if DEBUG
        private string[] statful_cached_AddedEditorShowStateKeys;
        private bool stateful_ShouldApplyLogging;
        #endif
        void ISerializationCallbackReceiver.OnAfterDeserialize() {
            OnInitState();
            
            #if DEBUG
            stateful_ShouldApplyLogging = true;
#endif
        }

        protected virtual void LateUpdate()
        {
            StatePreLateUpdate();
            if (!stateful_ShouldUpdate) return;
            stateful_ShouldUpdate = false;
            stateful_UpdateInProcess = true;
            stateListeners?.Invoke(ref stateful_NextState, ref state);
            
            #if DEBUG
            if (stateful_ShouldApplyLogging)
            {
                stateful_ShouldApplyLogging = false;
                ApplyLogging();
            }
            #endif
            
            OnStateChange(ref stateful_NextState, ref state);
            state = stateful_NextState;
            m_PostUpdateModifiying.Invoke(ref stateful_NextState);
            m_PostUpdateModifiying = delegate { };
            stateful_UpdateInProcess = false;
        }

        public void CreateBranch(string branchName, string sourceBranch = "")
        {
            if (branchName == stateful_CurrentBranchName || stateful_Branches.ContainsKey(branchName))
            {
                throw new Exception($"State branch ${branchName} already exists");
            }
            if (!stateful_Branches.ContainsKey(stateful_CurrentBranchName))
            {
                stateful_Branches.Add(stateful_CurrentBranchName, new Branch(stateful_CurrentBranchName, this.state));
            }

            if (sourceBranch == "")
            {
                sourceBranch = stateful_CurrentBranchName;
            }

            var nestState = stateful_Branches[sourceBranch].state;
            this.state = nestState;
            stateful_CurrentBranchName = branchName;
        }

        public void SwitchBranch(string branchName)
        {
            if (!stateful_Branches.ContainsKey(branchName))
            {
                throw new Exception($"State branch ${branchName} doesn't exist");
            }
            var currentBranch = stateful_Branches[stateful_CurrentBranchName];
            currentBranch.state = this.state;
            this.state = stateful_Branches[branchName].state;
            stateful_CurrentBranchName = branchName;
        }

        public void RemoveBranch(string branchName)
        {
            if (!stateful_Branches.ContainsKey(branchName))
            {
                throw new Exception($"State branch ${branchName} doesn't exist");
            }

            if (branchName == stateful_CurrentBranchName)
            {
                throw new Exception($"Unable to remove branch ${branchName} beacause it is current branch");
            }

            stateful_Branches.Remove(branchName);
        }
        
#if DEBUG
        private void ApplyLogging()
        {
            var type = this.GetType();
            var stateType = state.GetType();
            var sectionName = $"{type.Name} ({name})";
            if (statful_cached_AddedEditorShowStateKeys != null) foreach (var addedKey in statful_cached_AddedEditorShowStateKeys)
            {
                if (!statful_EditorShowStateKeys.Contains(addedKey))
                {
                    UILog.Remove(sectionName, addedKey);
                }
            }
            foreach (var fieldName in statful_EditorShowStateKeys)
            {
                var field = stateType.GetField(fieldName);
                UILog.Add(sectionName, fieldName, () => field.GetValue(state));
            }

            statful_cached_AddedEditorShowStateKeys = statful_EditorShowStateKeys.ToArray();
        }
#endif
    }
}