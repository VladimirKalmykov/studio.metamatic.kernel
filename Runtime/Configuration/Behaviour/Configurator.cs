 namespace Metamatic {
    using UnityEngine;
    using UnityEngine.Events;
    using Sirenix.OdinInspector;

    public class Configurator: MonoBehaviour {
        // public Configuration localConfiguration;
        // public ScriptableConfiguration scriptableConfiguration;
        public UnityEvent OnChange = new UnityEvent();
        // private ScriptableConfiguration cached_ScriptableConfiguration;
        private Configuration m_ExternalConfiguration = new Configuration();
        [ShowInInspector, ReadOnly, HideInEditorMode]
        /// <summary>
        /// You can reference to any external configuration from outside
        /// </summary>
        public Configuration externalConfiguration {
            get {
                return m_ExternalConfiguration;
            }
            set {
                if (value != m_ExternalConfiguration) {
                    m_ExternalConfiguration.OnChange -= HandleConfigurationChange;
                    if (value != null) {
                        value.OnChange += HandleConfigurationChange;
                    }
                }
                m_ExternalConfiguration = value;
                OnChange.Invoke();
            }
        }
        public Configuration configuration {
            get {
                if (externalConfiguration != null) {
                    return externalConfiguration;
                }
                return null;
            }
            set {
                if (externalConfiguration != null) {
                    externalConfiguration.OnChange -= HandleConfigurationChange;
                }
                externalConfiguration = value;
                OnChange.Invoke();
                if (value != null) {
                    externalConfiguration.OnChange += HandleConfigurationChange;
                }
            }
        }

        void Awake() {
            // cached_ScriptableConfiguration = scriptableConfiguration;
            
            // if (scriptableConfiguration != null) {
            //     scriptableConfiguration.OnChange += HandleConfigurationChange;
            // }

            if (externalConfiguration != null) {
                externalConfiguration.OnChange += HandleConfigurationChange;
            }

            ConfiguratorAwake();
        }

        protected virtual void ConfiguratorAwake() {

        }

        void OnDestroy() {
            // if (scriptableConfiguration != null) {
            //     scriptableConfiguration.OnChange -= HandleConfigurationChange;
            // }

            if (externalConfiguration != null) {
                externalConfiguration.OnChange -= HandleConfigurationChange;
            }
        }

        void HandleConfigurationChange() {
            OnChange.Invoke();
        }

        public GameObject GetPrefab(string code, GameObject byDefault = null) {
            return configuration.GetPrefab(code, byDefault);
        }

        public int GetInt(string code, int byDefault = default) {
            return configuration.GetInt(code, byDefault);
        }

        public float GetFloat(string code, float byDefault = default) {
            return configuration.GetFloat(code, byDefault);
        }

        public Color GetColor(string code, Color byDefault = default) {
            return configuration.GetColor(code, byDefault);
        }

        public Sprite GetSprite(string code, Sprite byDefault = default) {
            return configuration.GetSprite(code, byDefault);
        }

        public Vector4 GetVector(string code, Vector4 byDefault = default) {
            return configuration.GetVector(code, byDefault);
        }

        #region Setters
        public void SetColor(string name, Color color) {
            configuration.SetColor(name, color);
        }
        #endregion

#if UNITY_EDITOR
        void OnValidate() {
            OnChange.Invoke();

            // if (scriptableConfiguration != cached_ScriptableConfiguration) {
            //     if (cached_ScriptableConfiguration != null) {
            //         cached_ScriptableConfiguration.OnChange -= HandleConfigurationChange;
            //     }
            //     cached_ScriptableConfiguration = scriptableConfiguration;
            //     if (scriptableConfiguration != null) {
            //         scriptableConfiguration.OnChange += HandleConfigurationChange;
            //     }
            // }
        }
#endif
    }
}
