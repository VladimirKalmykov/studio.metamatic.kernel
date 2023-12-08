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
        [Tooltip("Attached modules will be attached to the slot without changing their parent"), LabelText("Virtual")]
        public bool virtualSlot = false;
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
        /// <param name="internalPlugIn"></param>
        /// <returns></returns>
        public ModuleAttachment PlugIn(Module module, Action<Slot, ModuleAttachment> internalPlugIn)
        {
            
            Module moduleInstance = null;
            if (module.gameObject.scene.name == null || module.gameObject.scene.name == gameObject.name)
            {
                /* Instantiate and connect prefab */
                moduleInstance = InstanceOrigin.Clone(module);
            }
            else
            {
                if (module.slot == this) return module.attachment;
                moduleInstance = module;
            }

            if (!virtualSlot)
            {
                moduleInstance.transform.SetParent(transform, false);
            }
            
            /* Unplug fallback before go on */
            if (m_FallbackAttachement != null)
            {
                m_FallbackAttachement.Release();
                m_FallbackAttachement = null;
            }

            var modulesToUnattach = m_Attachments.ToArray();

            var attachement = new ModuleAttachment(this, moduleInstance);

            internalPlugIn(this, attachement);
            moduleInstance.gameObject.SetActive(true);
            
            if (single)
            {
                /* Remove all attached modules */
                foreach (var attachment in modulesToUnattach)
                {
                    attachment.Release();
                }
            }
            m_Attachments.Add(attachement);
            
            moduleInstance.gameObject.SetActive(true);
            
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
        
        /**
         * Get all sockets of the slot
         */
        public Socket[] GetSockets()
        {
            return GetInternalSockets();
        }
        
        protected virtual Socket[] GetInternalSockets()
        {
            return new Socket[0];
        }

        void OnDestroy()
        {
            foreach(var attachment in m_Attachments)
            {
                attachment.Release();
            }
        }

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

    public class Slot<TSocket1> : Slot
    where TSocket1: Socket
    {
        [SerializeField, BoxGroup("Sockets")]
        protected TSocket1 socket1;
        
        protected override Socket[] GetInternalSockets()
        {
            return new Socket[] {socket1};
        }
    }
    
    public class Slot<TSocket1, TSocket2> : Slot<TSocket1>
        where TSocket1: Socket
        where TSocket2: Socket
    {
        [SerializeField, BoxGroup("Sockets")]
        protected TSocket2 socket2;
        
        protected override Socket[] GetInternalSockets()
        {
            return new Socket[] {socket1, socket2};
        }
    }
    
    public class Slot<TSocket1, TSocket2, TSocket3> : Slot<TSocket1, TSocket2>
        where TSocket1: Socket
        where TSocket2: Socket
        where TSocket3: Socket
    {
        [SerializeField, BoxGroup("Sockets")]
        protected TSocket3 socket3;
        
        protected override Socket[] GetInternalSockets()
        {
            return new Socket[] {socket1, socket2, socket3};
        }
    }
}