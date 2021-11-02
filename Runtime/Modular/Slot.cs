namespace Metamatic {
    using UnityEngine;
    using Sirenix.OdinInspector;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using UnityEngine.AddressableAssets;

    [ExecuteAlways]
    public class Slot: Element {
        UseConfigurator useConfigurator;
        // public Stock stock; 
        [BoxGroup("Configurable")]
        [Tooltip("Children module can be attached by the confugurator")]
        public bool configurable = false;
        [BoxGroup("Configurable")]
        [ShowIf("configurable")]
        public GameObject defaultPrefab;
        [BoxGroup("Configurable")]
        [ShowIf("configurable")]
        public bool runInEditor = false;
        [BoxGroup("Configurable")]
        [ShowIf("configurable")]
        [Tooltip("Use only local name to get prefab from configuration instead of absolute")]
        public bool useLocalName = false;
        [BoxGroup("Design")]
        public Color gizmosColor = Color.cyan;
        [BoxGroup("Design")]
        public Color gizmosColorTop = Color.red;
        [BoxGroup("Design")]
        public float gizmosLineLength = 8f;

        private GameObject prefab;
        private GameObject moduleGameObject = null;
        private ModulePreset m_ActiveModulePreset = null;

        void Start() {
            m_CachedRunInEditor = runInEditor;
            m_CachedConfigurable = configurable;
            if (configurable) {
                GetConfigurator();
            }
        }

        void GetConfigurator() {
            if (useConfigurator != null) {
                useConfigurator.UnsubscribeConfigurationChange(HandleConfigurationChange);
            }
            useConfigurator = GetComponent<UseConfigurator>() ?? gameObject.AddComponent<UseConfigurator>();
            useConfigurator.SubscribeConfigurationChange(HandleConfigurationChange);
            if (Application.isPlaying || (runInEditor && !useConfigurator.runInEditor)) {
                useConfigurator.ForceRefresh();
            }
        }

        void ReleaseConfigurator() {
            if (useConfigurator != null) {
                useConfigurator.UnsubscribeConfigurationChange(HandleConfigurationChange);
                useConfigurator = null;
            }
            if (this.prefab != null) {
                Clear();
            }
        }

        void OnDestroy() {
            if (useConfigurator != null) {
                useConfigurator.UnsubscribeConfigurationChange(HandleConfigurationChange);
            }
        }

        public void Clear() {
            this.prefab = null;
            #if UNITY_EDITOR
            var isPartOfAnyPrefab = UnityEditor.PrefabUtility.IsPartOfAnyPrefab(this);
            #endif
            
            /* Clear all */
            #if UNITY_EDITOR
            if (Application.isPlaying || !isPartOfAnyPrefab) {
            #endif
                foreach(Transform child in transform) {
                    #if UNITY_EDITOR
                    if (Application.isPlaying) {
                    #endif
                        child.SetParent(null);
                        GameObject.Destroy(child.gameObject);
                    #if UNITY_EDITOR
                    } else {
                        GameObject.DestroyImmediate(child.gameObject);
                    }
                    #endif
                }
            #if UNITY_EDITOR
            }
            #endif
        }

        void HandleConfigurationChange(Configuration config) {
            #if UNITY_EDITOR
            var isPartOfAnyPrefab = UnityEditor.PrefabUtility.IsPartOfAnyPrefab(this);
            if (isPartOfAnyPrefab) {
                return;
            }
            #endif

            if (config == null) {
                if (defaultPrefab != null && defaultPrefab != this.prefab) {
                    ApplyPrefab(
                        defaultPrefab,
                        #if UNITY_EDITOR
                        isPartOfAnyPrefab
                        #else
                        false
                        #endif
                    );
                }
                return;
            }
            
            var modulePreset = config.GetModulePreset(useLocalName ? name : relativeName);

            if (m_ActiveModulePreset != null && m_ActiveModulePreset != modulePreset) {
                m_ActiveModulePreset.Unsubscribe(HandleModulePresetChange);
            }
            
            if (modulePreset != null) {
                m_ActiveModulePreset = modulePreset;
                m_ActiveModulePreset.Subscribe(HandleModulePresetChange);
            }
        }

        async void HandleModulePresetChange(ModulePreset modulePreset) {
            #if UNITY_EDITOR
            var isPartOfAnyPrefab = UnityEditor.PrefabUtility.IsPartOfAnyPrefab(this);
            #else
            var isPartOfAnyPrefab = false;
            #endif
            GameObject prefab = null;
            prefab = await modulePreset.GetPrefabAsync();
                if (m_ActiveModulePreset != modulePreset) {
                    return;
                }

            if (prefab == null && defaultPrefab != null) {
                prefab = defaultPrefab;
            }
            
            if (prefab != this.prefab) {
                ApplyPrefab(prefab, isPartOfAnyPrefab);
            }

            #if UNITY_EDITOR
            if (isPartOfAnyPrefab) {
                return;
            }
            #endif

            if (modulePreset != null) {
                (moduleGameObject.GetComponent<Configurator>() ?? moduleGameObject.AddComponent<Configurator>()).configuration = modulePreset.configuration;
            }

            if (moduleGameObject != null && modulePreset != null) {
                var skin = await modulePreset.GetSkinAsync();
                /* Do not continue if another modulePreset already was handled */
                if (m_ActiveModulePreset != modulePreset) {
                    return;
                }
                var skinApplier = moduleGameObject.GetComponent<SkinApplier>() ?? moduleGameObject.AddComponent<SkinApplier>();
                skinApplier.skin = skin != null ? skin : skinApplier.defaultSkin;
            }
        }

        void ApplyPrefab(GameObject prefab, bool isPartOfAnyPrefab) {
            this.prefab = prefab;
            ClearChildren();

            if (prefab != null) {
                #if UNITY_EDITOR
                if (!Application.isPlaying) {
                    if (!isPartOfAnyPrefab) {
                        UnityEditor.EditorApplication.delayCall += () => {
                            moduleGameObject = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, transform);
                            moduleGameObject.transform.localPosition = Vector3.zero;
                            moduleGameObject.transform.localRotation = Quaternion.identity;
                        };
                    } else {
                        return;
                    }
                } else {
                #endif
                    moduleGameObject = Instantiate(prefab, transform);
                    moduleGameObject.transform.localPosition = Vector3.zero;
                    moduleGameObject.transform.localRotation = Quaternion.identity;
                #if UNITY_EDITOR
                }
                #endif
                
            }
        }

        void ClearChildren() {
            #if UNITY_EDITOR
            var isPartOfAnyPrefab = UnityEditor.PrefabUtility.IsPartOfAnyPrefab(this);
            #endif

            /* Clear all */
            #if UNITY_EDITOR
            if (Application.isPlaying || !isPartOfAnyPrefab) {
            #endif
                Transform[] children = new Transform[transform.childCount];
                int index = 0;
                foreach(Transform child in transform) {
                    children[index++] = child;
                }

                #if UNITY_EDITOR
                if (Application.isPlaying) {
                #endif
                    for (int i = transform.childCount; i > 0; --i) {
                        children[0].SetParent(null);
                        GameObject.Destroy(children[0].gameObject);
                    }
                #if UNITY_EDITOR
                } else {
                    UnityEditor.EditorApplication.delayCall += () => {
                        for (int i = transform.childCount; i > 0; --i) {
                            DestroyImmediate(transform.GetChild(0).gameObject);
                        }
                    };
                }
                #endif

                // 1234
                
            #if UNITY_EDITOR
            }
            #endif
        }

        bool m_CachedRunInEditor = false;
        bool m_CachedConfigurable = false;

        #if (UNITY_EDITOR)
        void OnValidate() {
            if (runInEditor != m_CachedRunInEditor || configurable != m_CachedConfigurable) {
                m_CachedRunInEditor = runInEditor;
                m_CachedConfigurable = configurable;
                if (!Application.isPlaying) {
                    if (runInEditor && configurable) {
                        GetConfigurator();
                    } else {
                        ReleaseConfigurator();
                    }
                }
            }
        }
        #endif

        #if (UNITY_EDITOR)
        public void OnDrawGizmos() {
            Gizmos.color = gizmosColor;
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = rotationMatrix;
            /* Draw player object face direction */
            Gizmos.DrawLine(Vector3.zero, (Vector3.forward.normalized * gizmosLineLength));
            Gizmos.color = gizmosColorTop;
            Gizmos.DrawLine(Vector3.zero, (Vector3.Cross(Vector3.forward, Vector3.right).normalized * gizmosLineLength));
        }
        #endif
    }
}