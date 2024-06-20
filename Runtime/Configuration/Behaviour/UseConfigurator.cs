namespace Metamatic {
    using UnityEngine;

    public class UseConfigurator: MonoBehaviour {
        public bool runInEditor;
        protected Configurator configurator;
        Configurator cached_Configurator;
        public event System.Action<Configuration> OnConfigurationChange;
         public bool wasStarted {
            get; private set;
        }
        public bool hasInitialized {
            get; private set;
        }

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
                
                hasInitialized = true;
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
                hasInitialized = true;
                ConfigurationConsumerInitialize();
            }
            #endif
            wasStarted = true;

            if (configurator == null) {
                OnConfigurationChange?.Invoke(null);
            }
        }

        public void SubscribeConfigurationChange(System.Action<Configuration> handler) {
            if (hasInitialized) {
                handler(configurator?.configuration ?? null);
            }
            OnConfigurationChange += handler;
        }

        public void UnsubscribeConfigurationChange(System.Action<Configuration> handler) {
            OnConfigurationChange -= handler;
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
        Configurator m_CachedConfigurator;
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
            OnConfigurationChange?.Invoke(configurator.configuration);
        }
    }
}