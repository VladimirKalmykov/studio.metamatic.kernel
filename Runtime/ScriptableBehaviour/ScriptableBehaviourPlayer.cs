namespace Metamatic {
    using UnityEngine;
    using Sirenix.OdinInspector;

    public class SciptableBehaviourPlayer<T> : PluggableMonoBehaviour, ISerializationCallbackReceiver
    where T: ScriptableBehaviour
    {
        [HideInPlayMode]
        [AssetList]
        [InlineEditor]
        public T defaultBehaviour;
        private T m_ActiveBehaviour;
        private T m_ActiveDisabledBehaviour;

        private bool m_BehaviourEnabled = true;

        public bool behaviourEnabled
        {
            get
            {
                return m_BehaviourEnabled;
            }
            set
            {
                if (value == m_BehaviourEnabled) return;
                m_BehaviourEnabled = value;
                if (value && m_ActiveDisabledBehaviour)
                {
                    var previouslyDisabledBehaviour = m_ActiveDisabledBehaviour;
                    m_ActiveDisabledBehaviour = null;
                    activeBehaviour = previouslyDisabledBehaviour;
                }
                else
                {
                    m_ActiveDisabledBehaviour = m_ActiveBehaviour;
                    activeBehaviour = null;
                }
            }
        }
        
        [ShowInInspector, HideInEditorMode]
        public T activeBehaviour {
            get { return m_ActiveBehaviour; }
            set {
                if (m_ActiveBehaviour == value) {
                    return;
                }
                if (m_ActiveBehaviour != null) {
                    DetachBehaviour(m_ActiveBehaviour, value != null);
                }
                m_ActiveBehaviour = value;
                AttachBehaviour(m_ActiveBehaviour);
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() {}
        void ISerializationCallbackReceiver.OnAfterDeserialize() {
            m_ActiveBehaviour = defaultBehaviour;
        }

        private bool m_Started;

        public void EnableBehaviour()
        {
            m_ActiveBehaviour.OnBehaviourEnable();
        }

        public void DisableBehaviour()
        {
            m_ActiveBehaviour.OnBehaviourDisable();
        }

        private void Awake() {
            if (m_ActiveBehaviour != null) {
                AttachBehaviour(m_ActiveBehaviour);
            }
        }

        private void OnEnable() {
            if (m_ActiveBehaviour == null) {
                enabled = false;
                return;
            }
            m_ActiveBehaviour.OnBehaviourEnable();
            OnBehaviourEnable();
        }

        private void Start() {
            m_Started = true;
            if (m_ActiveBehaviour != null) {
                StartBehaviour();
            }

            OnBehaviourStart();
        }

        private void Update() {
            m_ActiveBehaviour.OnBehaviourUpdate();
            OnBehaviourDidUpdate();
        }

        private void FixedUpdate() {
            m_ActiveBehaviour.OnBehaviourFixedUpdate();
        }

        private void LateUpdate() {
            m_ActiveBehaviour.OnBehaviourLateUpdate();
        }

        private void OnDisable() {
            if (m_ActiveBehaviour == null) {
                return;
            }
            m_ActiveBehaviour.OnBehaviourDisable();
            OnBehaviourDisable();
        }

        private void OnDestroy() {
            if (m_ActiveBehaviour != null) {
                m_ActiveBehaviour.OnBehaviourDestroy();
            }
            OnBehaviourDestroy();
        }

        private void AttachBehaviour(T scriptableBehaviour) {
            if (scriptableBehaviour == null)
            {
                enabled = false;
                return;
            }
            scriptableBehaviour.gameObject = gameObject;
            scriptableBehaviour.OnBehaviourAttach();
            enabled = true;
            if (m_Started) {
                StartBehaviour();
            }
        }

        private void StartBehaviour() {
            if (m_ActiveBehaviour != null) {
                m_ActiveBehaviour.OnBehaviourStart();
            }
        }

        private void DetachBehaviour(T scriptableBehaviour, bool dontDisable) {
            if (m_Started) {
                StopBehaviour();
            }
            scriptableBehaviour.OnBehaviourDetach();
            scriptableBehaviour.gameObject = null;
            if (!dontDisable) {
                enabled = false;
            }
        }

        private void StopBehaviour() {
            if (m_ActiveBehaviour != null) {
                m_ActiveBehaviour.OnBehaviourStop();
            }
        }

        #region Virtual Methods
        protected virtual void OnBehaviourEnable() {}
        protected virtual void OnBehaviourDisable() {}
        protected virtual void OnBehaviourDidUpdate() {}
        protected virtual void OnBehaviourStart() {}
        protected virtual void OnBehaviourDestroy() {}
        #endregion
    }
}