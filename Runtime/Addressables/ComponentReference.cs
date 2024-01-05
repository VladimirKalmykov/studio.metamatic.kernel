namespace Metamatic
{
    using System;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using Object = UnityEngine.Object;
    #if UNITY_EDITOR
    using UnityEditor;
    #endif

    /// <summary>
    /// Based on addressable samples. Required because AssetReferenceT works incorrectly with components (especially with Module<>) (addressables v1.21.19). Also Odin support for addressables is buggy now. (v 3.2.1).
    /// </summary>
    /// <typeparam name="TComponent">The component type.</typeparam>
    [Serializable]
    public class ComponentReference<TComponent> : AssetReference
    {
        /// <inheritdoc />
        public ComponentReference(string guid) : base(guid)
        {
        }

        /// <inheritdoc />
        public new AsyncOperationHandle<TComponent> InstantiateAsync(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            return Addressables.ResourceManager.CreateChainOperation<TComponent, GameObject>(base.InstantiateAsync(position, Quaternion.identity, parent), GameObjectReady);
        }

        /// <inheritdoc />
        public new AsyncOperationHandle<TComponent> InstantiateAsync(Transform parent = null, bool instantiateInWorldSpace = false)
        {
            return Addressables.ResourceManager.CreateChainOperation<TComponent, GameObject>(base.InstantiateAsync(parent, instantiateInWorldSpace), GameObjectReady);
        }

        /// <inheritdoc />
        public AsyncOperationHandle<TComponent> LoadAssetAsync()
        {
            return Addressables.ResourceManager.CreateChainOperation<TComponent, GameObject>(base.LoadAssetAsync<GameObject>(), GameObjectReady);
        }

        /// <inheritdoc />
        AsyncOperationHandle<TComponent> GameObjectReady(AsyncOperationHandle<GameObject> arg)
        {
            var comp = arg.Result.GetComponent<TComponent>();
            return Addressables.ResourceManager.CreateCompletedOperation<TComponent>(comp, string.Empty);
        }

        /// <summary>
        /// Validates that the assigned asset has the component type
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool ValidateAsset(Object obj)
        {
            var go = obj as GameObject;
            return go != null && go.GetComponent<TComponent>() != null;
        }

        /// <summary>
        /// Validates that the assigned asset has the component type, but only in the Editor
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public override bool ValidateAsset(string path)
        {
    #if UNITY_EDITOR
            //this load can be expensive...
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return go != null && go.GetComponent<TComponent>() != null;
    #else
                return false;
    #endif
        }

        /// <inheritdoc />
        public void ReleaseInstance(AsyncOperationHandle<TComponent> op)
        {
            // Release the instance
            var component = op.Result as Component;
            if (component != null)
            {
                Addressables.ReleaseInstance(component.gameObject);
            }

            // Release the handle
            Addressables.Release(op);
        }
    }
}
