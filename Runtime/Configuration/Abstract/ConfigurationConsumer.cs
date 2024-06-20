namespace Metamatic {
    using UnityEngine;
    using Sirenix.OdinInspector;

    public abstract class ConfigurationConsumer: MonoBehaviour {
        
        public bool runInEditor;
        [Tooltip("Prepend code of parent module")]
        public bool preprendModuleCode = true;
        protected Configurator configurator;
        Configurator cached_Configurator;
        ConfigurableModule parentModule;

        [ShowIf("preprendModuleCode"), ShowInInspector, ReadOnly]
        public string prefix {
            get {
                if (parentModule == null) {
                    parentModule = GetParentComponent<ConfigurableModule>();
                }
                return parentModule?.code ?? "";
            }
        }

        protected string getPrefixedCode(string code) {
            return $"{prefix}{code}";
        }

        void Awake() {
            #if UNITY_EDITOR
            if (Application.isPlaying) {
            #endif
                BeforeConfigurationConsumerInitialize();
                configurator = GetComponentInParent<Configurator>();
                if (configurator != null) {
                    ConfigurationUpdate();
                    configurator = GetComponentInParent<Configurator>();
                    configurator.OnChange.AddListener(ConfigurationUpdate);
                }

                if (preprendModuleCode) {
                    parentModule = GetParentComponent<ConfigurableModule>();
                }
                ConfigurationConsumerInitialize();
            #if UNITY_EDITOR
            }
            #endif
        }

        void Start() {
            #if UNITY_EDITOR
            if (!Application.isPlaying && runInEditor) {
                BeforeConfigurationConsumerInitialize();
                configurator = GetComponentInParent<Configurator>();
                if (configurator != null) {
                    ConfigurationUpdate();
                    configurator = GetComponentInParent<Configurator>();
                    configurator.OnChange.AddListener(ConfigurationUpdate);
                }

                if (preprendModuleCode) {
                    parentModule = GetParentComponent<ConfigurableModule>();
                }
                ConfigurationConsumerInitialize();
            }
            #endif
        }

        void OnDestroy() {
            if (configurator != null) {
                configurator.OnChange.RemoveListener(ConfigurationUpdate);
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
        void OnValidate() {
            if (configurator != null) {
                ConfigurationUpdate();
            }
            OnConfigurationValidate();
        }

        protected virtual void OnConfigurationValidate() {}
#endif

        protected virtual void BeforeConfigurationConsumerInitialize() {}
        protected virtual void ConfigurationConsumerInitialize() {}
        protected virtual void ConfigurationUpdate() {}
    }
}