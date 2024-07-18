using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gilzoide.EasyProjectSettings
{
    public static class ProjectSettings
    {
        public static ScriptableObject Load(Type type)
        {
            ProjectSettingsAttribute attribute = GetAttribute(type);
#if UNITY_EDITOR
            return (ScriptableObject) (attribute.IsRelativeToAssets
                ? AssetDatabase.LoadAssetAtPath(attribute.AssetPath, type)
                : UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget(attribute.AssetPath).FirstOrDefault());
#else
            return Resources.Load<ScriptableObject>(attribute.ResourcesPath);
#endif
        }

        public static T Load<T>() where T : ScriptableObject
        {
            return (T) Load(typeof(T));
        }

        public static async UniTask<ScriptableObject> LoadAsync(Type type, CancellationToken cancellationToken = default)
        {
            ProjectSettingsAttribute attribute = GetAttribute(type);
#if UNITY_EDITOR
            if (!attribute.IsRelativeToAssets)
            {
                return (ScriptableObject) UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget(attribute.AssetPath).FirstOrDefault();
            }
#endif
            var loadOperation = Resources.LoadAsync(attribute.ResourcesPath, type);
            while (!loadOperation.isDone)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await UniTask.Yield();
            }
            return (ScriptableObject) loadOperation.asset;
        }

        public static async UniTask<T> LoadAsync<T>(CancellationToken cancellationToken = default) where T : ScriptableObject
        {
            return (T) await LoadAsync(typeof(T), cancellationToken);
        }

        public static bool TryLoad(Type type, out ScriptableObject settings)
        {
            settings = Load(type);
            return settings != null;
        }

        public static bool TryLoad<T>(out T settings) where T : ScriptableObject
        {
            settings = Load<T>();
            return settings != null;
        }

        public static ScriptableObject LoadOrCreate(Type type)
        {
            if (!TryLoad(type, out ScriptableObject settingsObject))
            {
                settingsObject = ScriptableObject.CreateInstance(type);
#if UNITY_EDITOR
                Save(settingsObject);
#endif
            }
            return settingsObject;
        }

        public static T LoadOrCreate<T>() where T : ScriptableObject
        {
            return (T) LoadOrCreate(typeof(T));
        }

        public static async UniTask<ScriptableObject> LoadOrCreateAsync(Type type, CancellationToken cancellationToken = default)
        {
            var settingsObject = await LoadAsync(type, cancellationToken);
            if (settingsObject == null)
            {
                settingsObject = ScriptableObject.CreateInstance(type);
#if UNITY_EDITOR
                Save(settingsObject);
#endif
            }
            return settingsObject;
        }

        public static async UniTask<T> LoadOrCreateAsync<T>(CancellationToken cancellationToken = default) where T : ScriptableObject
        {
            return (T) await LoadOrCreateAsync(typeof(T), cancellationToken);
        }

        public static ProjectSettingsAttribute GetAttribute(Type type)
        {
            ProjectSettingsAttribute attribute = type.GetCustomAttribute<ProjectSettingsAttribute>();
            if (attribute == null)
            {
                throw new ProjectSettingsException($"Type {type.Name} doesn't have a ProjectSettingsAttribute");
            }
            return attribute;
        }

#if UNITY_EDITOR
        public static void Save(Object obj)
        {
            if (obj == null)
            {
                return;
            }

            ProjectSettingsAttribute attribute = GetAttribute(obj.GetType());
            string directoryPath = Path.GetDirectoryName(attribute.AssetPath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            
            if (attribute.IsRelativeToAssets)
            {
                if (!AssetDatabase.Contains(obj))
                {
                    AssetDatabase.CreateAsset(obj, attribute.AssetPath);
                }
#if UNITY_2020_3_OR_NEWER
                AssetDatabase.SaveAssetIfDirty(obj);
#endif
            }
            else
            {
                UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new[] { obj }, attribute.AssetPath, true);
            }
        }
#endif
    }
}
