using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Metamatic {
    public static class AddressableUtils
    {
        
        public class AssetReferenceLoadBehaviourWrapper<T>: IDisposable
            where T : UnityEngine.Object
        {
            public readonly CancellationToken cancellationToken;
            public readonly AssetReference assetReference;
            private  CancellationTokenRegistration cancellationTokenRegistration;
            public readonly Task<T> Task;
            public AssetReferenceLoadBehaviourWrapper(Component component, AssetReferenceT<T> assetReference)
            {
                this.assetReference = assetReference;
                this.cancellationToken = component.GetAsyncDestroyTrigger().CancellationToken;
                cancellationTokenRegistration = cancellationToken.Register(HandleCancellationFired);
                this.Task = SafeLoadAssetAsync<T>(assetReference);
            }
            
            void HandleCancellationFired()
            {
                if (assetReference.IsValid())
                {
                    assetReference.ReleaseAsset();
                }
            }

            public void Dispose()
            {
                cancellationTokenRegistration.Dispose();
            }
        }
        public static Task<TObject> SafeLoadAssetAsync<TObject>(AssetReferenceT<TObject> assetReference) where TObject : UnityEngine.Object {
            if (!assetReference.IsValid()) {
                return assetReference.LoadAssetAsync<TObject>().Task;
            }
            var tsc = new TaskCompletionSource<TObject>();
            var operationHandler = assetReference.OperationHandle;
            tsc.SetResult((TObject)operationHandler.Result);
            return tsc.Task;
        }
        
        public static AssetReferenceLoadBehaviourWrapper<T> LoadAssetReferenceWithinBehaviour<T>(this Component component, AssetReferenceT<T> assetReference) where T: UnityEngine.Object
        {
            return new AssetReferenceLoadBehaviourWrapper<T>(component, assetReference);
        }
    }
}