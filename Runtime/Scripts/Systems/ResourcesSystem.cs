using System;
using System.Collections.Generic;
using System.ComponentModel;
using Cysharp.Threading.Tasks;
using Metamatic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Metamatic.Systems
{
    public static class ResourcesSystem
    {
        private static readonly SignedIndexedPool<Action> HandleReleaseActions = new (500);
        private static readonly SignedIndexedPool<AsyncOperationHandle<SceneInstance>> LoadSceneOperationHandles = new (500);
        private static Dictionary<string, IDisposable> _ScheduledReleases = new Dictionary<string, IDisposable>();
        /// <summary>
        /// Summon addressable resource asynchronously.
        /// </summary>
        /// <param name="assetReference"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>Disposable AsyncSummon that contains async task of resource.</returns>
        public static AsyncSummon<T> Summon<T>(AssetReferenceT<T> assetReference) where T : UnityEngine.Object
        {
            return Summon<T>(assetReference.AssetGUID);
        }
        
        public static AsyncSummon<T> Summon<T>(AssetReference assetReference) where T : UnityEngine.Object
        {
            return Summon<T>(assetReference.AssetGUID);
        }
        
        public static AsyncSummon<T> Summon<T>(string address) where T : UnityEngine.Object
        {
            var tcs = SafeUniTaskCompletionSource<T>.Create();
            var handle = Addressables.LoadAssetAsync<UnityEngine.Object>(address);
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
                    tcs.TrySetException(new Exception($"Failed to load asset {address}"));
                }
            }
        }
        
        /// <summary>
        /// Summon addressable resource asynchronously with a grace period. After disposing the summon, the resource will be released after the specified grace period.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="gracePeriod">Delay in seconds</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static AsyncSummon<T> Summon<T>(string address, int gracePeriod) where T : UnityEngine.Object
        {
            var tcs = SafeUniTaskCompletionSource<T>.Create();
            var handle = Addressables.LoadAssetAsync<UnityEngine.Object>(address);
            if (_ScheduledReleases.TryGetValue(address, out var scheduledRelease))
            {
                // If we already have a scheduled release, we can just reuse it
                scheduledRelease.Dispose();
                _ScheduledReleases.Remove(address);
            }
            var token = HandleReleaseActions.Add(() =>
            {
                _ScheduledReleases[address] = Schedule(gracePeriod, () =>
                {
                    if (_ScheduledReleases.Remove(address, out var release))
                    {
                        Addressables.Release(handle);
                    }
                });
            });
            
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
                    tcs.TrySetException(new Exception($"Failed to load asset {address}"));
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

        public static AsyncDisposableSummon<SceneInstance> LoadScene(
            AssetReference asset,
            UnityEngine.SceneManagement.LoadSceneMode mode,
            bool activateOnLoad = true
        )
        {
            var handle = Addressables.LoadSceneAsync(asset, mode, SceneReleaseMode.OnlyReleaseSceneOnHandleRelease, activateOnLoad: activateOnLoad);
            var tcs = SafeUniTaskCompletionSource<SceneInstance>.Create();
            var token = LoadSceneOperationHandles.Add(handle);
            
            handle.Completed += HandleCompleted;
            
            return new AsyncDisposableSummon<SceneInstance>(token, tcs.Task, ReleaseScene);
            
            void HandleCompleted(AsyncOperationHandle<SceneInstance> operationHandle)
            {
                if (operationHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    tcs.TrySetResult(operationHandle.Result);
                }
                else
                {
                    tcs.TrySetException(new Exception($"Failed to load scene {asset}"));
                }
            }
        }
        
        private static DisposableAction Schedule(float delay, Action onComplete, PlayerLoopTiming playerLoopTiming = PlayerLoopTiming.EarlyUpdate)
        {
            var css = SignalSource.Create();
            PlayerLoop.Delay(delay, playerLoopTiming, css.Signal).ContinueWith(onComplete).Forget();
            return DisposableAction.Create(css.Dispose);
        }
        
        private static void Release(Token96Bit token)
        {
            if (HandleReleaseActions.Remove(token, out var release))
            {
                release();
            }
        }
        
        private static async UniTask ReleaseScene(Token96Bit token)
        {
            if (LoadSceneOperationHandles.Remove(token, out var handle))
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    try
                    {
                        var sceneInstance = handle.Result;
                        if (sceneInstance.Scene.isLoaded)
                        {
                            await Addressables.UnloadSceneAsync(sceneInstance).ToUniTask();
                        }
                        else
                        {
                            Addressables.Release(handle);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // ignored
                        Console.Debug("ResourcesSystem", "Scene unload operation was cancelled");
                    }
                    catch (Exception e)
                    {
                        Console.Error("ResourcesSystem", $"Failed to unload scene: {e}");
                    }
                } else {
                    Addressables.Release(handle);
                }
            }
        }
    }
}