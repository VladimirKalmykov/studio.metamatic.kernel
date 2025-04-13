using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Metamatic
{
    public partial class EditorAddressablesUtils
    {
        /// <summary>
        /// Untested!
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        public static string EditorOnlyGetAddressFromAsset(Object asset)
        {
            if (asset == null)
                return null;

            // Получаем путь ассета в проекте
            var assetPath = AssetDatabase.GetAssetPath(asset);
            // Получаем GUID по пути
            var guid = AssetDatabase.AssetPathToGUID(assetPath);

            // Получаем настройки Addressables
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogWarning("AddressableAssetSettings не найдены.");
                return null;
            }

            // Находим запись по GUID
            var entry = settings.FindAssetEntry(guid);
            return entry != null ? entry.address : null;
        }
        
        public static string EditorOnlyGetAddressFromAssetReference(AssetReference assetReference)
        {
            if (assetReference == null)
                return null;

            // Получаем GUID из AssetReference
            var guid = assetReference.RuntimeKey.ToString();

            // Получаем настройки Addressables
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogWarning("AddressableAssetSettings не найдены.");
                return null;
            }

            // Находим запись по GUID
            var entry = settings.FindAssetEntry(guid);
            return entry != null ? entry.address : null;
        }
        
        public static AssetReference GetAssetReferenceByAddress(string address)
        {
            if (string.IsNullOrEmpty(address))
                return null;
        
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogWarning("AddressableAssetSettings не найдены.");
                return null;
            }
        
            // Перебираем все группы и записи для поиска по адресу
            foreach (var group in settings.groups)
            {
                foreach (var entry in group.entries)
                {
                    if (entry.address == address)
                    {
                        // Создаем AssetReference по GUID найденной записи.
                        return new AssetReference(entry.guid);
                    }
                }
            }
            Debug.LogWarning($"Запись с адресом {address} не найдена.");
            return null;
        }

        public static AssetReference GetAssetReferenceByGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return null;
            
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogWarning("AddressableAssetSettings не найдены.");
                return null;
            }
            // Перебираем все группы и записи для поиска по GUID
            foreach (var group in settings.groups)
            {
                foreach (var entry in group.entries)
                {
                    if (entry.guid == guid)
                    {
                        // Создаем AssetReference по GUID найденной записи.
                        return new AssetReference(entry.guid);
                    }
                }
            }
            
            Debug.LogWarning($"Запись с GUID {guid} не найдена.");
            return null;
        }
    }
}