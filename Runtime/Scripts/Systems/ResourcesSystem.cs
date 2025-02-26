using System;
using System.ComponentModel;
using Cysharp.Threading.Tasks;
using Metamatic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Metamatic.Systems
{
    public static class ResourcesSystem
    {
        private static readonly SignedIndexedPool<Action> HandleReleaseActions = new (1000);
        /// <summary>
        /// Summon addressable resource asynchronously.
        /// </summary>
        /// <param name="assetReference"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>Disposable AsyncSummon that contains async task of resource.</returns>
        public static AsyncSummon<T> Summon<T>(AssetReferenceT<T> assetReference) where T : UnityEngine.Object
        {
            var tcs = SafeUniTaskCompletionSource<T>.Create();
            var handle = Addressables.LoadAssetAsync<T>(assetReference);
            var token = HandleReleaseActions.Add(() => Addressables.Release(handle));
            
            handle.Completed += HandleCompleted;
            
            return new AsyncSummon<T>(token, tcs.Task, ReleaseSummon);
            
            void HandleCompleted(AsyncOperationHandle<T> operationHandle)
            {
                if (operationHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    tcs.TrySetResult(operationHandle.Result);
                }
                else
                {
                    tcs.TrySetException(new Exception($"Failed to load asset {assetReference}"));
                }
            }
        }
        
        public static AsyncSummon<object> Summon(AssetReference assetReference) where T : UnityEngine.Object
        {
            var tcs = SafeUniTaskCompletionSource<object>.Create();
            var handle = Addressables.LoadAssetAsync(assetReference);
            var token = HandleReleaseActions.Add(() => Addressables.Release(handle));
            
            handle.Completed += HandleCompleted;
            
            return new AsyncSummon<T>(token, tcs.Task, ReleaseSummon);
            
            void HandleCompleted(AsyncOperationHandle<T> operationHandle)
            {
                if (operationHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    tcs.TrySetResult(operationHandle.Result);
                }
                else
                {
                    tcs.TrySetException(new Exception($"Failed to load asset {assetReference}"));
                }
            }
        }
        
        /// <summary>
        /// Same as Summon but with converter function.
        /// </summary>
        /// <param name="assetReference"></param>
        /// <param name="converter"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TOutput"></typeparam>
        /// <returns></returns>
        public static AsyncSummon<TOutput> SummonConverted<T, TOutput>(AssetReferenceT<T> assetReference, Func<T, TOutput> converter) where T : UnityEngine.Object
        {
            var tcs = SafeUniTaskCompletionSource<T>.Create();
            var handle = Addressables.LoadAssetAsync<T>(assetReference);
            var token = HandleReleaseActions.Add(() => Addressables.Release(handle));
            
            handle.Completed += HandleCompleted;
            
            return new AsyncSummon<TOutput>(token, tcs.Task.ContinueWith(converter), ReleaseSummon);
            
            void HandleCompleted(AsyncOperationHandle<T> operationHandle)
            {
                if (operationHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    tcs.TrySetResult(operationHandle.Result);
                }
                else
                {
                    tcs.TrySetException(new Exception($"Failed to load asset {assetReference}"));
                }
            }
        }
        
        private static void ReleaseSummon(Token96Bit token)
        {
            if (HandleReleaseActions.Remove(token, out var release))
            {
                release();
            }
        }
    }
}