using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

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
        
        public static async Task<TObject> LoadAssetAsync<TObject>(AssetReferenceT<TObject> assetReference, CancellationToken cancellationToken) where TObject : UnityEngine.Object
        {
            // Проверка, была ли уже начата операция загрузки
            if (assetReference.OperationHandle.IsValid() && !assetReference.OperationHandle.IsDone)
            {
                // Ожидание завершения уже запущенной операции
                await new Promise((resolve, reject, self) =>
                {
                    assetReference.OperationHandle.Completed += (AsyncOperationHandle op) =>
                    {
                        resolve(op.Status == AsyncOperationStatus.Succeeded);
                    };
                }).Task;
            }
    
            var aoh = assetReference.LoadAssetAsync<TObject>();

            // Обработка отмены
            if (cancellationToken.IsCancellationRequested)
            {
                // Если токен уже отменен, отменяем загрузку и выгружаем ассет
                aoh.Completed += handle => { if (!handle.IsDone) Addressables.Release(handle); };
                cancellationToken.ThrowIfCancellationRequested();
            }

            // Регистрируем обработку отмены
            cancellationToken.Register(() =>
            {
                if (aoh.IsDone)
                {
                    // Если операция завершена, выгружаем ассет
                    Addressables.Release(aoh);
                }
                else
                {
                    // Выгружаем ассет по готовности
                    aoh.Completed += handle => { if (handle.IsDone) Addressables.Release(handle); };
                }
            });

            return await aoh.Task;
        }

        private static Task<TObject> WaitExistingOperation<TObject>(AssetReferenceT<TObject> assetReference, CancellationToken cancellationToken) where TObject : UnityEngine.Object
        {
            var tcs = new TaskCompletionSource<TObject>();

            assetReference.OperationHandle.Completed += (AsyncOperationHandle op) =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    tcs.SetResult(op.Result as TObject);
                }
                else
                {
                    tcs.SetException(new System.Exception("Asset loading failed."));
                }
            };

            // Регистрация отмены
            cancellationToken.Register(() =>
            {
                if (!assetReference.OperationHandle.IsDone)
                {
                    tcs.TrySetCanceled();
                    Addressables.Release(assetReference.OperationHandle);
                }
            });

            return tcs.Task;
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