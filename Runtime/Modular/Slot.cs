using System;
using System.Collections.Generic;

namespace Metamatic {
    using UnityEngine;
    using Sirenix.OdinInspector;

    [ExecuteAlways]
    public class Slot: MonoBehaviour
    {
        [Tooltip("Can have only one module")]
        public bool single = false;
        public Module fallbackModule;
        public bool designMode = false;
        [ShowIf("designMode"), BoxGroup("Design")]
        public Color gizmosColor = Color.cyan;
        [ShowIf("designMode"), BoxGroup("Design")]
        public Color gizmosColorTop = Color.red;
        [ShowIf("designMode"), BoxGroup("Design")]
        public float gizmosLineLength = 8f;
        
        private List<ModuleAttachment> m_Attachments = new List<ModuleAttachment>();
        private ModuleAttachment m_FallbackAttachement = null;

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
        }

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
        public ModuleAttachment PlugIn(Module module, Action<Slot, ModuleAttachment> internalPlugIn)
        {
            if (module.slot == this) return module.attachment;
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
            if (m_FallbackAttachement != null)
            {
                Debug.Log($"Release fallback: {m_FallbackAttachement.module}");
                m_FallbackAttachement.Release();
                m_FallbackAttachement = null;
            }

            var modulesToUnattach = m_Attachments.ToArray();
            

            var attachement = new ModuleAttachment(this, item);

            internalPlugIn(this, attachement);
            item.gameObject.SetActive(true);
            m_Attachments.Add(attachement);
            
            if (single)
            {
                /* Remove all attached modules */
                foreach (var attachment in modulesToUnattach)
                {
                    attachment.Release();
                }
            }
            
            item.gameObject.SetActive(true);
            
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
                throw new Exception($"Module '{module.name}', that you're trying to unplug is not in attached modules list of the slot");
            }
            // var attachement = m_Attachments[attachementIndex];
            m_Attachments.RemoveAt(attachementIndex);

            internalUnplug(this);

            if (m_FallbackAttachement == null && m_Attachments.Count == 0)
            {
                Fallback();
            }
        }

        private void Fallback()
        {
            if (fallbackModule)
            {
                Debug.Log($"Fallback: {fallbackModule.name}");
                m_FallbackAttachement = PlugIn(fallbackModule);
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
        public void OnDrawGizmos()
        {
            if (!designMode) return;
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