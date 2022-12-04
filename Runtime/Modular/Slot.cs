using System;
using System.Collections.Generic;

namespace Metamatic {
    using UnityEngine;
    using Sirenix.OdinInspector;

    [ExecuteAlways]
    public class Slot: Element
    {
        public Module fallbackModule;
        private UseConfigurator useConfigurator;
        // public Stock stock; 
        // [BoxGroup("Configurable")]
        // [Tooltip("Children module can be attached by the confugurator")]
        // public bool configurable = false;
        // [BoxGroup("Configurable")]
        // [ShowIf("configurable")]
        // public GameObject defaultPrefab;
        // [BoxGroup("Configurable")]
        // [ShowIf("configurable")]
        // public bool runInEditor = false;
        // [BoxGroup("Configurable")]
        // [ShowIf("configurable")]
        // [Tooltip("Use only local name to get prefab from configuration instead of absolute")]
        // public bool useLocalName = false;
        [BoxGroup("Design")]
        public Color gizmosColor = Color.cyan;
        [BoxGroup("Design")]
        public Color gizmosColorTop = Color.red;
        [BoxGroup("Design")]
        public float gizmosLineLength = 8f;
        
        private List<ModuleAttachment> m_Attachments = new List<ModuleAttachment>();
        private bool m_FallbackAttached = false;

        private void Start()
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return;
            }
            #endif
            if (m_Attachments.Count == 0)
            {
                Fallback();
            }
            // m_CachedRunInEditor = runInEditor;
            // m_CachedConfigurable = configurable;
            // if (configurable) {
            //     GetConfigurator();
            // }
        }
        
        // [Obsolete("", true)]
        // private void GetConfigurator() {
        //     if (useConfigurator != null) {
        //         useConfigurator.UnsubscribeConfigurationChange(HandleConfigurationChange);
        //     }
        //     useConfigurator = GetComponent<UseConfigurator>() ?? gameObject.AddComponent<UseConfigurator>();
        //     useConfigurator.SubscribeConfigurationChange(HandleConfigurationChange);
        //     if (Application.isPlaying || (runInEditor && !useConfigurator.runInEditor)) {
        //         useConfigurator.ForceRefresh();
        //     }
        // }

        // private void ReleaseConfigurator() {
        //     if (useConfigurator != null) {
        //         useConfigurator.UnsubscribeConfigurationChange(HandleConfigurationChange);
        //         useConfigurator = null;
        //     }
        //     if (this.prefab != null) {
        //         Clear();
        //     }
        // }
        //
        // private void OnDestroy() {
        //     if (useConfigurator != null) {
        //         useConfigurator.UnsubscribeConfigurationChange(HandleConfigurationChange);
        //     }
        // }

        public ModuleAttachment PlugIn(Module module)
        {
            return module.PlugIn(this);
        }

        /// <summary>
        /// Attach module to the slot
        /// </summary>
        /// <param name="module"></param>
        /// <param name="internallyModuleCall">Set true if method called from module itself</param>
        /// <param name="internalPlugIn"></param>
        /// <returns></returns>
        public ModuleAttachment PlugIn(Module module, Action<Slot> internalPlugIn)
        {
            Module item = null;
            if (module.gameObject.scene.name == null || module.gameObject.scene.name == gameObject.name)
            {
                /* Plugin prefab */
                item = InstanceOrigin.Clone(module);
            }
            else
            {
                item = module;
            }
            item.transform.SetParent(transform, false);
            
            /* Unplug fallback before go on */
            if (m_FallbackAttached)
            {
                var fallbackAttachement = m_Attachments[0];
                fallbackAttachement.Release();
                m_FallbackAttached = false;
            }

            var attachement = new ModuleAttachment(this, item);

            internalPlugIn(this);
            item.gameObject.SetActive(true);
            m_Attachments.Add(attachement);
            return attachement;
        }

        public void Unplug(Module module)
        {
            module.Unplug(this);
        }

        public void Unplug(Module module, Action<Slot> internalUnplug)
        {
            var attachementIndex = m_Attachments.FindIndex(a => a.module == module);
            if (attachementIndex < 0)
            {
                throw new Exception("Module you're trying to unplug is not in attached modules list of the slot");
            }
            var attachement = m_Attachments[attachementIndex];
            m_Attachments.RemoveAt(attachementIndex);

            internalUnplug(this);

            if (!m_FallbackAttached && m_Attachments.Count == 0)
            {
                Fallback();
            }
        }

        private void Fallback()
        {
            if (fallbackModule)
            {
                PlugIn(fallbackModule);
                m_FallbackAttached = true;
            }
        }
        
        [Obsolete("", true)]
        public void Clear() {
            // #if UNITY_EDITOR
            // var isPartOfAnyPrefab = UnityEditor.PrefabUtility.IsPartOfAnyPrefab(this);
            // #endif
            //
            // /* Clear all */
            // #if UNITY_EDITOR
            // if (Application.isPlaying || !isPartOfAnyPrefab) {
            // #endif
            //     foreach(Transform child in transform) {
            //         #if UNITY_EDITOR
            //         if (Application.isPlaying) {
            //         #endif
            //             child.SetParent(null);
            //             GameObject.Destroy(child.gameObject);
            //         #if UNITY_EDITOR
            //         } else {
            //             GameObject.DestroyImmediate(child.gameObject);
            //         }
            //         #endif
            //     }
            // #if UNITY_EDITOR
            // }
            // #endif
        }
        
        // [Obsolete("", true)]
        // private void HandleConfigurationChange(Configuration config) {
        //     #if UNITY_EDITOR
        //     var isPartOfAnyPrefab = UnityEditor.PrefabUtility.IsPartOfAnyPrefab(this);
        //     if (isPartOfAnyPrefab) {
        //         return;
        //     }
        //     #endif
        //
        //     if (config == null) {
        //         if (defaultPrefab != null && defaultPrefab != this.prefab) {
        //             ApplyPrefab(
        //                 defaultPrefab,
        //                 #if UNITY_EDITOR
        //                 isPartOfAnyPrefab
        //                 #else
        //                 false
        //                 #endif
        //             );
        //         }
        //         return;
        //     }
        //     
        //     var modulePreset = config.GetModulePreset(useLocalName ? name : relativeName);
        //
        //     if (m_ActiveModulePreset != null && m_ActiveModulePreset != modulePreset) {
        //         m_ActiveModulePreset.Unsubscribe(HandleModulePresetChange);
        //     }
        //
        //     if (modulePreset == null) return;
        //     m_ActiveModulePreset = modulePreset;
        //     m_ActiveModulePreset.Subscribe(HandleModulePresetChange);
        // }
        
        [Obsolete("Module preset logic is deprecated", false)]
        private async void HandleModulePresetChange(ModulePreset modulePreset) {
            // #if UNITY_EDITOR
            // var isPartOfAnyPrefab = UnityEditor.PrefabUtility.IsPartOfAnyPrefab(this);
            // #else
            // var isPartOfAnyPrefab = false;
            // #endif
            // GameObject prefab = null;
            // prefab = await modulePreset.GetPrefabAsync();
            //     if (m_ActiveModulePreset != modulePreset) {
            //         return;
            //     }
            //
            // if (prefab == null && defaultPrefab != null) {
            //     prefab = defaultPrefab;
            // }
            //
            // if (prefab != this.prefab) {
            //     ApplyPrefab(prefab, isPartOfAnyPrefab);
            // }
            //
            // #if UNITY_EDITOR
            // if (isPartOfAnyPrefab) {
            //     return;
            // }
            // #endif
            //
            // if (modulePreset != null) {
            //     (moduleGameObject.GetComponent<Configurator>() ?? moduleGameObject.AddComponent<Configurator>()).configuration = modulePreset.configuration;
            // }
            //
            // if (moduleGameObject == null || modulePreset == null) return;
            // var skin = await modulePreset.GetSkinAsync();
            // /* Do not continue if another modulePreset already was handled */
            // if (m_ActiveModulePreset != modulePreset) {
            //     return;
            // }
            // var skinApplier = moduleGameObject.GetComponent<SkinApplier>() ?? moduleGameObject.AddComponent<SkinApplier>();
            // skinApplier.skin = skin != null ? skin : skinApplier.defaultSkin;
        }
        
        [Obsolete("", true)]
        private void ApplyPrefab(GameObject prefab, bool isPartOfAnyPrefab) {
//             this.prefab = prefab;
//             ClearChildren();
//
//             if (prefab == null) return;
// #if UNITY_EDITOR
//             if (!Application.isPlaying) {
//                 if (!isPartOfAnyPrefab) {
//                     UnityEditor.EditorApplication.delayCall += () => {
//                         moduleGameObject = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, transform);
//                         moduleGameObject.transform.localPosition = Vector3.zero;
//                         moduleGameObject.transform.localRotation = Quaternion.identity;
//                     };
//                 } else {
//                     return;
//                 }
//             } else {
// #endif
//                 moduleGameObject = Instantiate(prefab, transform);
//                 moduleGameObject.transform.localPosition = Vector3.zero;
//                 moduleGameObject.transform.localRotation = Quaternion.identity;
// #if UNITY_EDITOR
//             }
// #endif
        }

        private void ClearChildren() {
            #if UNITY_EDITOR
            var isPartOfAnyPrefab = UnityEditor.PrefabUtility.IsPartOfAnyPrefab(this);
            #endif

            /* Clear all */
            #if UNITY_EDITOR
            if (!Application.isPlaying && isPartOfAnyPrefab) return;
#endif
            var children = new Transform[transform.childCount];
            var index = 0;
            foreach(Transform child in transform) {
                children[index++] = child;
            }

#if UNITY_EDITOR
            if (Application.isPlaying) {
#endif
                for (var i = transform.childCount; i > 0; --i) {
                    children[0].SetParent(null);
                    GameObject.Destroy(children[0].gameObject);
                }
#if UNITY_EDITOR
            } else {
                UnityEditor.EditorApplication.delayCall += () => {
                    for (var i = transform.childCount; i > 0; --i) {
                        DestroyImmediate(transform.GetChild(0).gameObject);
                    }
                };
            }
#endif

            // 1234
                
#if UNITY_EDITOR
#endif
        }

        private bool m_CachedRunInEditor = false;
        private bool m_CachedConfigurable = false;

        #if (UNITY_EDITOR)
        private void OnValidate()
        {
            // if (runInEditor == m_CachedRunInEditor && configurable == m_CachedConfigurable) return;
            // m_CachedRunInEditor = runInEditor;
            // m_CachedConfigurable = configurable;
            if (Application.isPlaying) return;
            // if (runInEditor && configurable) {
            //     GetConfigurator();
            // } else {
            //     ReleaseConfigurator();
            // }
        }
        #endif

        #if (UNITY_EDITOR)
        public void OnDrawGizmosSelected() {
            Gizmos.color = gizmosColor;
            var rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = rotationMatrix;
            /* Draw player object face direction */
            Gizmos.DrawLine(Vector3.zero, (Vector3.forward.normalized * gizmosLineLength));
            Gizmos.color = gizmosColorTop;
            Gizmos.DrawLine(Vector3.zero, (Vector3.Cross(Vector3.forward, Vector3.right).normalized * gizmosLineLength));
        }
        #endif
    }
}