using System;

namespace Metamatic.Framework.Editor
{
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.UIElements;
#endif
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.UIElements;

    public class AssetReferenceField : VisualElement
    {
        // Храним значение типа AssetReference.
        public AssetReference AssetReferenceValue { get; private set; }

        // Внутреннее поле для выбора объекта.
        private ObjectField objectField;
        public event Action<AssetReference> OnValueChanged;

        public AssetReferenceField(System.Type assetType)
        {
            // Инициализируем ObjectField, ограничив выбор типом ассета.
            objectField = new ObjectField
            {
                objectType = assetType,
                allowSceneObjects = false
            };
            objectField.RegisterValueChangedCallback(evt =>
            {
                // Если пользователь выбрал объект, пытаемся получить его GUID.
                Object selectedObject = evt.newValue;
#if UNITY_EDITOR
                if (selectedObject != null)
                {
                    string path = AssetDatabase.GetAssetPath(selectedObject);
                    string guid = AssetDatabase.AssetPathToGUID(path);
                    if (!string.IsNullOrEmpty(guid))
                    {
                        // Создаем новый AssetReference на основе GUID.
                        AssetReferenceValue = new AssetReference(guid);
                    }
                    else
                    {
                        AssetReferenceValue = null;
                    }
                }
                else
                {
                    AssetReferenceValue = null;
                }
#endif
                // Можно добавить вызов события или уведомление о смене значения.
                OnValueChanged?.Invoke(AssetReferenceValue);
            });

            Add(objectField);
        }

        /// <summary>
        /// Устанавливает значение поля через AssetReference.
        /// При наличии значения пытаемся загрузить ассет для отображения.
        /// </summary>
        public void SetValue(AssetReference newReference)
        {
            AssetReferenceValue = newReference;
#if UNITY_EDITOR
            if (newReference != null)
            {
                // Для редактора можно попытаться получить ассет напрямую.
                Object asset = newReference.editorAsset;
                objectField.value = asset;
            }
            else
            {
                objectField.value = null;
            }
#endif
        }
    }
}