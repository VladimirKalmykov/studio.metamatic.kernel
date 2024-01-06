using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Metamatic {
    public static class AddressableUtils
    {
        
        public class AssetReferenceLoadBehaviourWrapper<T>: IDisposable
            where T : UnityEngine.Object
        {
            public readonly CancellationToken cancellationToken;
            public readonly AssetReference assetReference;
            private  CancellationTokenRegistration cancellationTokenRegistration;
            private  CancellationTokenRegistration userCancellationTokenRegistration;
            public readonly Task<T> Task;
            public AssetReferenceLoadBehaviourWrapper(Component component, AssetReferenceT<T> assetReference, CancellationToken userCancellationToken = default)
            {
                this.assetReference = assetReference;
                this.cancellationToken = component.GetAsyncDestroyTrigger().CancellationToken;
                cancellationTokenRegistration = cancellationToken.Register(HandleCancellationFired);
                userCancellationTokenRegistration = userCancellationToken.Register(HandleCancellationFired);
                this.Task = SafeLoadAssetAsync<T>(assetReference);
            }
            
            void HandleCancellationFired()
            {
                Dispose();
            }

            public void Dispose()
            {
                cancellationTokenRegistration.Dispose();
                userCancellationTokenRegistration.Dispose();
                if (assetReference.IsValid())
                {
                    assetReference.ReleaseAsset();
                }
            }
        }
        
        [Obsolete("Use LoadAssetAsync with cancellationToken instead")]
        public static Task<TObject> SafeLoadAssetAsync<TObject>(AssetReferenceT<TObject> assetReference) where TObject : UnityEngine.Object {
            if (!assetReference.IsValid()) {
                var aoh = assetReference.LoadAssetAsync<TObject>();
                return aoh.Task;
            }
            var tsc = new TaskCompletionSource<TObject>();
            var operationHandler = assetReference.OperationHandle;
            tsc.SetResult((TObject)operationHandler.Result);
            return tsc.Task;
        }
        
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
                        Debug.Log($"Cancel asset reference {assetReference}");
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
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        resolve(handle.Result);
                    }
                    else
                    {
                        reject(handle.OperationException);
                    }
                };
            });
        }
        
        public static DisposableAsyncOperation<GameObject> InstantiatePrefab(AssetReference assetReference, CancellationToken token, Transform transform = null)
        {
            return new DisposableAsyncOperation<GameObject>(cancellationToken =>
            {
                var taskCompletionSource = new UniTaskCompletionSource<GameObject>();
                var instantiateOperation = Addressables.InstantiateAsync(assetReference, transform);
                instantiateOperation.Completed += handle =>
                {
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

                    handle.Result.transform.localPosition = Vector3.zero;
                    handle.Result.transform.localRotation = Quaternion.identity;
                };
                instantiateOperation.Destroyed += handle =>
                {
                    taskCompletionSource.TrySetException(new OperationCanceledException());
                };
                return taskCompletionSource.Task;
            }, token);
        }
        
        public static AssetReferenceLoadBehaviourWrapper<T> LoadAssetReferenceWithinBehaviour<T>(this Component component, AssetReferenceT<T> assetReference, CancellationToken cancellationToken = default) where T: UnityEngine.Object
        {
            return new AssetReferenceLoadBehaviourWrapper<T>(component, assetReference, cancellationToken);
        }
    }
}