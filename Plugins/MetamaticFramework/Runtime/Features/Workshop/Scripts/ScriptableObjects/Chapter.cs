using System;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Metamatic.Framework.Workshop
{
    [CreateAssetMenu(fileName = "Chapter", menuName = "Metamatic/Workshop/Chapter")]
    public class Chapter: ScriptableObject
    {
        [ReadOnly][SerializeField] public string id;
        public string title;
        public AssetReference baseScene;
        public AssetReference gameScene;
        
#if UNITY_EDITOR
        // Метод вызывается при изменениях в редакторе
        protected void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
            {
                GenerateUniqueId();
                // Отмечаем объект как изменённый, чтобы изменения сохранились
                EditorUtility.SetDirty(this);
            }
        }
        
        private void GenerateUniqueId(bool force = false)
        {
            id = Guid.NewGuid().ToString();
        }
        
        protected void Reset()
        {
            GenerateUniqueId();
        }
#endif
    }
}