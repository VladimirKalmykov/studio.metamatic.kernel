using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Serialization;

namespace Metamatic {
    using UnityEngine;
    using Sirenix.OdinInspector;

    [ExecuteAlways]
    public class SlotLegacy: MonoBehaviour
    {
        public enum UnplugReason {
            Unknown,
            ModuleDisable,
        }
        [Tooltip("Can have only one module")]
        public bool single = false;
        [Tooltip("Attached modules will be attached to the slot without changing their parent"), LabelText("Virtual")]
        public bool virtualSlot = false;
        [FormerlySerializedAs("fallbackModule")] public ModuleLegacy fallbackModuleLegacy;
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

        public ModuleAttachment PlugIn(ModuleLegacy moduleLegacy)
        {
            ModuleLegacy moduleLegacyInstance = null;
            if (moduleLegacy.gameObject.scene.name == null || moduleLegacy.gameObject.scene.name == gameObject.name)
            {
                /* Instantiate and connect prefab */
                moduleLegacyInstance = InstanceOrigin.Clone(moduleLegacy);
            }
            else
            {
                if (moduleLegacy.slotLegacy == this) return moduleLegacy.attachment;
                moduleLegacyInstance = moduleLegacy;
            }
            return moduleLegacyInstance.PlugIn(this);
        }

        /// <summary>
        /// This method is used my internal module call, don't use it as a public API
        /// </summary>
        /// <param name="module"></param>
        /// <param name="internalPlugIn"></param>
        /// <returns></returns>
        public ModuleAttachment PlugIn(ModuleLegacy moduleLegacyInstance, Action<SlotLegacy, ModuleAttachment> internalPlugIn)
        {
            if (!virtualSlot)
            {
                moduleLegacyInstance.transform.SetParent(transform, false);
            }
            
            /* Unplug fallback before go on */
            if (m_FallbackAttachement != null)
            {
                m_FallbackAttachement.Release();
                m_FallbackAttachement = null;
            }

            var modulesToUnattach = m_Attachments.ToArray();

            var attachement = new ModuleAttachment(this, moduleLegacyInstance);

            internalPlugIn(this, attachement);
            moduleLegacyInstance.gameObject.SetActive(true);
            
            if (single)
            {
                /* Remove all attached modules */
                foreach (var attachment in modulesToUnattach)
                {
                    attachment.Release();
                }
            }
            m_Attachments.Add(attachement);
            
            moduleLegacyInstance.gameObject.SetActive(true);
            
            return attachement;
        }

        public void Unplug(ModuleLegacy moduleLegacy, UnplugReason reason = UnplugReason.Unknown)
        {
            moduleLegacy.Unplug(this, reason);
        }

        public async void Unplug(ModuleLegacy moduleLegacy, Func<SlotLegacy, UnplugReason, Task> unplugModule, UnplugReason reason = UnplugReason.Unknown)
        {
            var attachementIndex = m_Attachments.FindIndex(a => a.moduleLegacy == moduleLegacy);
            if (attachementIndex < 0)
            {
                throw new Exception($"Module '{moduleLegacy.name}', that you're trying to unplug is not in attached modules list of the slot");
            }
            // var attachement = m_Attachments[attachementIndex];
            m_Attachments.RemoveAt(attachementIndex);

            await unplugModule(this, reason);

            if (m_FallbackAttachement == null && m_Attachments.Count == 0)
            {
                Fallback();
            }
        }

        private void Fallback()
        {
            if (fallbackModuleLegacy)
            {
                Debug.Log($"Fallback: {fallbackModuleLegacy.name}");
                m_FallbackAttachement = PlugIn(fallbackModuleLegacy);
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
        public SocketLegacy[] GetSockets()
        {
            return GetInternalSockets();
        }
        
        protected virtual SocketLegacy[] GetInternalSockets()
        {
            return new SocketLegacy[0];
        }

        void OnDestroy()
        {
            foreach(var attachment in m_Attachments.ToArray())
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

    public class SlotLegacy<TSocket1> : SlotLegacy
    where TSocket1: SocketLegacy
    {
        [SerializeField, BoxGroup("Sockets")]
        protected TSocket1 socket1;
        
        protected override SocketLegacy[] GetInternalSockets()
        {
            return new SocketLegacy[] {socket1};
        }
    }
    
    public class SlotLegacy<TSocket1, TSocket2> : SlotLegacy<TSocket1>
        where TSocket1: SocketLegacy
        where TSocket2: SocketLegacy
    {
        [SerializeField, BoxGroup("Sockets")]
        protected TSocket2 socket2;
        
        protected override SocketLegacy[] GetInternalSockets()
        {
            return new SocketLegacy[] {socket1, socket2};
        }
    }
    
    public class SlotLegacy<TSocket1, TSocket2, TSocket3> : SlotLegacy<TSocket1, TSocket2>
        where TSocket1: SocketLegacy
        where TSocket2: SocketLegacy
        where TSocket3: SocketLegacy
    {
        [SerializeField, BoxGroup("Sockets")]
        protected TSocket3 socket3;
        
        protected override SocketLegacy[] GetInternalSockets()
        {
            return new SocketLegacy[] {socket1, socket2, socket3};
        }
    }
}