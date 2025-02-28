using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Metamatic
{
    [Obsolete("Use ResourcesSystem instead")]
    public class AddressablesManager
    {
        // TODO: Cleanup instances on unload
        public static Dictionary<int, string> mapInstanceIDToAssetGUID = new Dictionary<int, string>();
        
        public static DisposableOperation<TObject> LoadAssetAsyncLegacy<TObject>(AssetReferenceT<TObject> assetReference, CancellationToken cancellationToken = default, float releaseDelay = 0f) where TObject : UnityEngine.Object
        {
            throw new NotImplementedException();
        }
        
        public static DisposableAsyncOperationT<GameObject> InstantiatePrefab(AssetReference assetReference, CancellationToken token, Transform transform = null)
        {
            return new DisposableAsyncOperationT<GameObject>(cancellationToken =>
            {
                var taskCompletionSource = new UniTaskCompletionSource<GameObject>();
                var instantiateOperation = Addressables.InstantiateAsync(assetReference, transform);
                instantiateOperation.Completed += handle =>
                {
                    mapInstanceIDToAssetGUID[handle.Result.GetInstanceID()] = assetReference.AssetGUID;
                    taskCompletionSource.TrySetResult(handle.Result);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        taskCompletionSource.TrySetException(new OperationCanceledException());
                        Addressables.ReleaseInstance(handle.Result);
                    }

                    CancellationTokenRegistration registration = default;
                    Action cancellationCallback = () =>
                    {
                        registration.Dispose();
                        taskCompletionSource.TrySetException(new OperationCanceledException());
                        Addressables.ReleaseInstance(handle.Result);
                    };
                    registration = cancellationToken.Register(cancellationCallback);
                };
                instantiateOperation.Destroyed += handle =>
                {
                    taskCompletionSource.TrySetException(new OperationCanceledException());
                };
                return taskCompletionSource.Task;
            }, token);
        }
        
        public static DisposableAsyncOperationT<SceneInstance> LoadSceneAsync(AssetReference sceneName, LoadSceneMode loadSceneMode, bool activateOnLoad, CancellationToken token)
        {
            return new DisposableAsyncOperationT<SceneInstance>(cancellationToken =>
            {
                var taskCompletionSource = new UniTaskCompletionSource<SceneInstance>();
                var loadSceneOperation = Addressables.LoadSceneAsync(sceneName, loadSceneMode, activateOnLoad);
                loadSceneOperation.Completed += handle =>
                {
                    taskCompletionSource.TrySetResult(handle.Result);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        taskCompletionSource.TrySetException(new OperationCanceledException());
                        Addressables.UnloadSceneAsync(handle);
                    }

                    CancellationTokenRegistration registration = default;
                    Action cancellationCallback = () =>
                    {
                        registration.Dispose();
                        taskCompletionSource.TrySetException(new OperationCanceledException());
                        Addressables.UnloadSceneAsync(handle);
                    };
                    registration = cancellationToken.Register(cancellationCallback);
                };
                loadSceneOperation.Destroyed += handle =>
                {
                    taskCompletionSource.TrySetException(new OperationCanceledException());
                };
                return taskCompletionSource.Task;
            }, token);
        }
        
        public static string GetAssetGUIDByInstance(UnityEngine.Object obj)
        {
            if (mapInstanceIDToAssetGUID.TryGetValue(obj.GetInstanceID(), out var assetGUID))
            {
                return assetGUID;
            }
            return null;
        }
    }
}