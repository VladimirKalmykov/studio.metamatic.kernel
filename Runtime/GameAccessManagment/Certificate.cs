using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
#endif
using Sirenix.OdinInspector;
using UnityEngine.AddressableAssets;

namespace Metamatic.GameAccessManagment {
    [CreateAssetMenu(fileName = "Certificate", menuName = "Metamatic/Game Access Managment/Certificate", order = 0)]
    public class Certificate: ScriptableObject, ISerializationCallbackReceiver, IProduct {
        public enum CERTIFICATE_GRADE {
            AUTO,
            JUNK,
            REGULAR,
            LEGENDARY,
            EPICAL,
            ARCANE
        }
        #if UNITY_EDITOR
        public static List<Certificate> all = new List<Certificate>();
        [Required("$editor_invalidMessage")]
        #endif
        [ReadOnly]
        public string id = "";
        [Required]
        public string title = "";
        public CERTIFICATE_GRADE grade = CERTIFICATE_GRADE.REGULAR;
        public AssetReferenceSprite picture;
        public float price = 0f;
        void ISerializationCallbackReceiver.OnAfterDeserialize() {

        }
        void ISerializationCallbackReceiver.OnBeforeSerialize() {
            #if UNITY_EDITOR
            var safeindex = 0;
            while(id.Length == 0 || all.Exists(x => x.id == id && x != this)) {
                safeindex++;
                if (safeindex > 100) {
                    Debug.LogWarning("Got Certificate id validation cycle limit");
                    return;
                }
                id = System.Guid.NewGuid().ToString();
            }
            #endif
        }

        float IProduct.GetPrice() {
            return price;
        }

        string IProduct.GetProductName() {
            return title;
        }

        #if UNITY_EDITOR
        private string[] m_AllIds = new string[0];
        private bool m_IdValid = true;
        private string editor_invalidMessage = "";
        private const string editor_defaultInvalidMessage = "Id already exists";
        void OnEnable() {
            all.Add(this);
        }
        void OnDisable() {
            all.Remove(this);
        }

        void OnValidate() {
            m_IdValid = all.Exists(x => x.id == id && x != this);
            editor_invalidMessage = m_IdValid ? "" : editor_defaultInvalidMessage;
        }
        #endif
    }
}