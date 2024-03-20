using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Metamatic
{
    public class AddressablesManager
    {
        // TODO: Cleanup instances on unload
        public static Dictionary<int, string> mapInstanceIDToAssetGUID = new Dictionary<int, string>();
        public static DisposableOperation<TObject> LoadAssetAsync<TObject>(AssetReferenceT<TObject> assetReference, CancellationToken cancellationToken, float releaseDelay = 0f) where TObject : UnityEngine.Object
        {
            return new DisposableOperation<TObject>((resolve, reject, token) =>
            {
                if (!assetReference.RuntimeKeyIsValid())
                {
                    throw new Exception("Asset reference is invalid");
                }
                if (assetReference.OperationHandle.IsValid())
                {
                    Addressables.ResourceManager.Acquire(assetReference.OperationHandle);
                }
    
                var aoh = assetReference.OperationHandle.IsValid() ? (assetReference.OperationHandle.Convert<TObject>()) : assetReference.LoadAssetAsync<TObject>();
        
                // Обработка отмены
                if (token.IsCancellationRequested)
                {
                    Debug.Log($"Immediately Cancel asset reference {assetReference}");
                    // Если токен уже отменен, отменяем загрузку и выгружаем ассет
                    Action<AsyncOperationHandle<TObject>> completeHandler = null;
                    completeHandler = handle => { aoh.Completed -= completeHandler; /* Release with delay */
                        AsyncUtils.ExecuteDelayed(() =>
                        {
                            Addressables.Release(aoh);
                        }, releaseDelay); };
                    aoh.Completed += completeHandler;
                    cancellationToken.ThrowIfCancellationRequested();
                }
                else
                {
                    // Регистрируем обработку отмены
                    token.Register(() =>
                    {
                        if (aoh.IsDone)
                        {
                            /* Release with delay */
                            AsyncUtils.ExecuteDelayed(() =>
                            {
                                Addressables.Release(aoh);
                            }, releaseDelay);
                        }
                        else
                        {
                            reject(new OperationCanceledException());
                            // Выгружаем ассет по готовности
                            aoh.Completed += handle =>
                            {
                                /* Release with delay */
                                AsyncUtils.ExecuteDelayed(() =>
                                {
                                    Addressables.Release(aoh);
                                }, releaseDelay);
                            };
                        }
                    });
                }

                aoh.Completed += handle =>
                {
                    if (token.IsCancellationRequested)
                    {
                        reject(new OperationCanceledException());
                    } else if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        mapInstanceIDToAssetGUID[handle.Result.GetInstanceID()] = assetReference.AssetGUID;
                        resolve(handle.Result);
                    }
                    else
                    {
                        reject(handle.OperationException);
                    }
                };
            });
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