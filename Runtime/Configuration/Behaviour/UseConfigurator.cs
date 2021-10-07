namespace Metamatic {
    using UnityEngine;
    using UnityEngine.Events;
    using Sirenix.OdinInspector;

    public class UseConfigurator: MonoBehaviour {
        [System.Serializable]
        public class ConfigurationChangeEvent: UnityEvent<Configuration> {}
        public bool runInEditor = false;
        protected Configurator configurator;
        Configurator cached_Configurator;
        public ConfigurationChangeEvent OnConfigurationChange = new ConfigurationChangeEvent();
         public bool wasStarted {
            get; private set;
        } = false;

        void Awake() {
            #if UNITY_EDITOR
            if (Application.isPlaying) {
            #endif
                BeforeConfigurationConsumerInitialize();
                if (transform.parent != null) {
                    configurator = transform.parent.GetComponentInParent<Configurator>();
                    if (configurator != null) {
                        InternalConfigurationUpdate();
                        configurator.OnChange.AddListener(InternalConfigurationUpdate);
                    }
                }
                
                ConfigurationConsumerInitialize();
            #if UNITY_EDITOR
            }
            #endif
        }

        public void ForceRefresh() {
            if (configurator != null) {
                configurator.OnChange.RemoveListener(InternalConfigurationUpdate);
            }
            if (transform.parent != null) {
                configurator = transform.parent.GetComponentInParent<Configurator>();
                if (configurator != null) {
                    InternalConfigurationUpdate();
                    configurator.OnChange.AddListener(InternalConfigurationUpdate);
                }
            }
        }

        void Start() {
            #if UNITY_EDITOR
            if (!Application.isPlaying && runInEditor) {
                BeforeConfigurationConsumerInitialize();
                if (transform.parent != null) {
                    configurator = transform.parent.GetComponentInParent<Configurator>();
                    if (configurator != null) {
                        InternalConfigurationUpdate();
                        configurator.OnChange.AddListener(InternalConfigurationUpdate);
                    }
                }
                ConfigurationConsumerInitialize();
            }
            #endif
            wasStarted = true;
        }

        void OnTransformParentChanged() {
            ForceRefresh();
        }

        void OnDestroy() {
            if (configurator != null) {
                configurator.OnChange.RemoveListener(InternalConfigurationUpdate);
            }
        }

        T GetParentComponent<T>() {
            if (gameObject.transform.parent is null) {
                return default(T);
            }

            T component = (T)gameObject.transform.parent.gameObject.GetComponentInParent<T>();

            if (component == null) {
                return default(T);
            }

            return (T)component;
        }

#if UNITY_EDITOR
        Configurator m_CachedConfigurator = null;
        void OnValidate() {
            if (configurator != null && m_CachedConfigurator != configurator) {
                m_CachedConfigurator = configurator;
                InternalConfigurationUpdate();
            }
            OnConfigurationValidate();
        }

        protected virtual void OnConfigurationValidate() {}
#endif

        protected virtual void BeforeConfigurationConsumerInitialize() {}
        protected virtual void ConfigurationConsumerInitialize() {}
        void InternalConfigurationUpdate() {
            OnConfigurationChange.Invoke(configurator.configuration);
        }
    }
}