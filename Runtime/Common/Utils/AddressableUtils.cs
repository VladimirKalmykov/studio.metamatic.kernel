using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Metamatic {
    public static class AddressableUtils
    {
        
        [Obsolete("Use LoadAsyncAsset instead", true)]
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
        
        [Obsolete("Use LoadAssetAsync with cancellationToken instead", true)]
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
        
        [Obsolete("Use LoadAssetAsync instead", true)]
        public static AssetReferenceLoadBehaviourWrapper<T> LoadAssetReferenceWithinBehaviour<T>(this Component component, AssetReferenceT<T> assetReference, CancellationToken cancellationToken = default) where T: UnityEngine.Object
        {
            return new AssetReferenceLoadBehaviourWrapper<T>(component, assetReference, cancellationToken);
        }
    }
}