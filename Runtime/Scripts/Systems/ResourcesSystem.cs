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
            
            return new AsyncSummon<T>(token, tcs.Task, Release);
            
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
        
        public static AsyncSummon<T> Summon<T>(AssetReference assetReference) where T : UnityEngine.Object
        {
            var tcs = SafeUniTaskCompletionSource<T>.Create();
            var handle = Addressables.LoadAssetAsync<UnityEngine.Object>(assetReference);
            var token = HandleReleaseActions.Add(() => Addressables.Release(handle));
            
            handle.Completed += HandleCompleted;
            
            return new AsyncSummon<T>(token, tcs.Task, Release);
            
            void HandleCompleted(AsyncOperationHandle<UnityEngine.Object> operationHandle)
            {
                if (operationHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    tcs.TrySetResult((T)operationHandle.Result);
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
            
            return new AsyncSummon<TOutput>(token, tcs.Task.ContinueWith(converter), Release);
            
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

        public static IDisposable LoadScene(AssetReference asset, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            var handle = Addressables.LoadSceneAsync(asset, mode);
            var token = HandleReleaseActions.Add(() => Addressables.UnloadSceneAsync(handle));
            return DisposableAction.Create(() => Release(token));
        }
        
        private static void Release(Token96Bit token)
        {
            if (HandleReleaseActions.Remove(token, out var release))
            {
                release();
            }
        }
    }
}