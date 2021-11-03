using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Metamatic {
    public static class AddressableUtils {
        public static Task<TObject> SafeLoadAssetAsync<TObject>(AssetReferenceT<TObject> assetReference) where TObject : UnityEngine.Object {
            if (!assetReference.IsValid()) {
                return assetReference.LoadAssetAsync<TObject>().Task;
            }
            var tsc = new TaskCompletionSource<TObject>();
            var operationHandler = assetReference.OperationHandle;
            tsc.SetResult((TObject)operationHandler.Result);
            return tsc.Task;
        }
    }
}