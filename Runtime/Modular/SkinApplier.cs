// namespace Metamatic {
//     using UnityEngine;
//     using Sirenix.OdinInspector;
//
//     [ExecuteAlways]
//     public class SkinApplier : MonoBehaviour, ISerializationCallbackReceiver
//     {
//         [HideInPlayMode]
//         public ScriptableSkin defaultSkin;
//         private ScriptableSkin debug_Skin;
//         private ScriptableSkin m_Skin {
//             get {
//                 return debug_Skin;
//             }
//             set {
//                 debug_Skin = value;
//             }
//         }
//         private bool m_Subscribed = false;
//         [ShowInInspector, HideInEditorMode]
//         public ScriptableSkin skin {
//             set {
//                 if (m_Skin != null) {
//                     m_Skin.OnSerializedFieldsChange -= HandleSerializedFieldsChange;
//                     m_Subscribed = false;
//                 }
//                 m_Skin = value;
//                 if (m_Skin != null) {
//                     m_Skin.OnSerializedFieldsChange += HandleSerializedFieldsChange;
//                     m_Subscribed = true;
//                 }
//                 Apply();
//             }
//             get {
//                 return m_Skin;
//             }
//         }
//
//         void ISerializationCallbackReceiver.OnBeforeSerialize() {}
//         void ISerializationCallbackReceiver.OnAfterDeserialize() {
//             m_Skin = defaultSkin;
//         }
//
//         void HandleSerializedFieldsChange() {
//             #if UNITY_EDITOR
//             if (!UnityEditor.PrefabUtility.IsPartOfAnyPrefab(this) && gameObject.activeInHierarchy) {
//             #endif
//             Apply();
//             #if UNITY_EDITOR
//             }
//             #endif
//         }
//
//         void Start() {
//             #if UNITY_EDITOR
//             if (!UnityEditor.PrefabUtility.IsPartOfAnyPrefab(this) && gameObject.activeInHierarchy) {
//             #endif
//                 Apply();
//             #if UNITY_EDITOR
//             }
//             #endif
//         }
//
//         void OnEnable() {
//             if (m_Skin != null && !m_Subscribed) {
//                 m_Skin.OnSerializedFieldsChange += HandleSerializedFieldsChange;
//                 m_Subscribed = true;
//             }
//         }
//
//         void OnDisable() {
//             if (m_Skin != null) {
//                 m_Skin.OnSerializedFieldsChange -= HandleSerializedFieldsChange;
//                 m_Subscribed = false;
//             }
//         }
//
//         [Button, HideInPlayMode]
//         void Apply() {
//             if (m_Skin != null && gameObject.activeInHierarchy) {
//                 m_Skin.Apply(gameObject);
//             }
//         }
//
//         #if UNITY_EDITOR
//         void OnValidate() {
//             if (!Application.isPlaying && m_Skin != defaultSkin) {
//                 skin = defaultSkin;
//             }
//         }
//         #endif
//     }
//
// }