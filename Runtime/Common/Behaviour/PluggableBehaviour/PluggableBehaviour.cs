using System.Linq;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Metamatic {
    public abstract class PluggableBehaviour: MonoBehaviour {
        [Header("Preset")]
        public string presetName;
        [Header("Additional pluginNames")]
        [HideInInspector]
        public string[] pluginNames = new string[0];

        public virtual void OnPluggableValidate() {}
    }
    /// <summary>
    /// Space object that can move by the thrusters
    /// </summary>
    public abstract class PluggableBehaviour<PresetT, PluginT>: PluggableBehaviour
        where PresetT: MonoBehaviour
        where PluginT: MonoBehaviour
    {
#region Public properties
        /// <summary>
        /// Reference to the current preset
        /// </summary>
        /// <value></value>
        public PresetT preset {
            get {
                return m_Preset;
            }
            private set {
                m_Preset = value;
                CachePresetData(value);
            }
        }
        public Type presetBaseType {
            get; private set;
        } = typeof(PresetT);
        public Type pluginBaseType {
            get; private set;
        } = typeof(PluginT);
#endregion

#region Private properties
        /// <summary>
        /// Cached preset name
        /// </summary>
        private string m_PresetName;
        /// <summary>
        /// Cached plugin names
        /// </summary>
        private string[] m_CurrentPluginNames = new string[0];
        /// <summary>
        /// Contains preset reference (if exists)
        /// </summary>
        private PresetT m_Preset;
        private Type[] cached_PresetRequiredComponents = new Type[0];
        private string[] cached_PresetRequiredComponentFullnames = new string[0];
#endregion

        void Awake() {
            m_PresetName = presetName;
            m_CurrentPluginNames = pluginNames;

            if (presetName != "") {
                LoadPreset(presetName);
            }
            RefreshPlugins(pluginNames);
        }

        public void LoadPreset(string presetName) {
            #if UNITY_EDITOR
                Stopwatch stopWatch = null;
                stopWatch = new Stopwatch();
                stopWatch.Start();
            #endif
            if (presetName != m_PresetName) {
                if (m_PresetName != "" && m_PresetName != null) {
                    Type existenPresetType = Assembly.GetAssembly(this.GetType()).GetType(m_PresetName, false);
                    if (existenPresetType != null) {
                        Component currentPreset = GetComponent(existenPresetType) as Component;
                        if (currentPreset != null) {
                            if (Application.isPlaying) {
                                Destroy(currentPreset);
                            } else {
                                InvokeOnEndOfFame(() => {
                                    DestroyImmediate(currentPreset);
                                });
                            }
                            preset = null;
                        }
                    }
                }
                m_PresetName = presetName;

                if (presetName.Length > 0) {
                    Type presetType = Assembly.GetAssembly(this.GetType()).GetType(presetName, false);
                    if (presetType == null || presetType.IsInstanceOfType(typeof(PresetT))) {
                        UnityEngine.Debug.LogWarning($"Undefined preset {presetName}");
                    } else {
                        InvokeOnEndOfFame(() => {
                            PresetT newPreset = (PresetT)gameObject.GetComponent(presetType);
                            if (newPreset == null) {
                                newPreset = (PresetT)gameObject.AddComponent(presetType);
                            }
                            preset = newPreset;
                        });
                    }
                }
            }
            #if UNITY_EDITOR
                stopWatch.Stop();
                // Get the elapsed time as a TimeSpan value.
                TimeSpan ts = stopWatch.Elapsed;
            #endif
        }

        public void RefreshPlugins(string[] pluginNames) {
            InvokeOnEndOfFame(() => {
                if (pluginNames != m_CurrentPluginNames) {
                    /* Remove excess plugins */
                    foreach(string pluginName in m_CurrentPluginNames) {
                        if (!pluginNames.Contains(pluginName) && !DoesPresetContainsPlugin(pluginName)) {
                            Type pluginComponent = Assembly.GetAssembly(this.GetType()).GetType(pluginName, false);
                            if (pluginComponent != null) {
                                UnityEngine.Object component = GetComponent(pluginComponent) as UnityEngine.Object;
                                 if (Application.isPlaying) {
                                    Destroy(component);
                                } else {
                                    DestroyImmediate(component);
                                }
                            }
                        }
                    }
                    m_CurrentPluginNames = pluginNames;

                    /* Add components */
                    List<string> addedPlugins = new List<string>();
                    foreach(string pluginName in pluginNames) {
                        if (addedPlugins.Contains(pluginName)) {
                            continue;
                        }
                        Type pluginComponent = Assembly.GetAssembly(this.GetType()).GetType(pluginName, false);
                        if (pluginComponent != null) {
                            Component currentComponent = GetComponent(pluginComponent);
                            if (currentComponent == null) {
                                gameObject.AddComponent(pluginComponent);
                            }
                        }
                    }
                }
            });
        }
        
        /// <summary>
        /// Cache preset data
        /// </summary>
        /// <param name="preset"></param>
        void CachePresetData(UnityEngine.Object preset) {
            if (preset == null) {
                cached_PresetRequiredComponents = new Type[0];
                cached_PresetRequiredComponentFullnames = new string[0];
                return;
            }
            /* Get preset required component type fullnames */
            List<Type> presetRequiredComponents = new List<Type>();
            var attributes = preset.GetType().GetCustomAttributes(true);
            foreach (object attribute in attributes)
            {
                RequireComponent requireComponent = attribute as RequireComponent;
                if (requireComponent != null) {
                    if (requireComponent.m_Type0 != null) {
                        presetRequiredComponents.Add(requireComponent.m_Type0);
                    }
                    if (requireComponent.m_Type1 != null) {
                        presetRequiredComponents.Add(requireComponent.m_Type1);
                    }
                    if (requireComponent.m_Type2 != null) {
                        presetRequiredComponents.Add(requireComponent.m_Type2);
                    }
                }
            }

            cached_PresetRequiredComponents = presetRequiredComponents.ToArray();
            cached_PresetRequiredComponentFullnames = presetRequiredComponents.Select(type => type.FullName).ToArray();
        }

#region Helper methods
        /// <summary>
        /// Destroy object only on the end of current frame (immediate for editor mode)
        /// </summary>
        /// <param name="obj"></param>
        private void SafeDestroy(UnityEngine.Object obj) {
            InvokeOnEndOfFame(() => {
                if (Application.isPlaying) {
                    Destroy(obj);
                } else {
                    DestroyImmediate(obj);
                }
            });
        }
        private bool DoesPresetContainsPlugin(string name) {
            if (preset == null) {
                return false;
            }
            return cached_PresetRequiredComponentFullnames.Contains(name);
        }
        private void InvokeOnEndOfFame(Action action)
        {
            if (gameObject.activeInHierarchy) {
                StartCoroutine(EnumerableInvokeOnEndOfFame(action));
            }
            action();
        }

        private IEnumerator EnumerableInvokeOnEndOfFame(Action action) {
            yield return new WaitForEndOfFrame();
            action();
        }
#endregion

#if UNITY_EDITOR
        public override void OnPluggableValidate() {
            LoadPreset(presetName);
            RefreshPlugins(pluginNames);
        }
#endif
    }
}