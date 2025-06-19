using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Cysharp.Threading.Tasks;
using Metamatic;
using Sirenix.OdinInspector.Editor.GettingStarted;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Metamatic.Systems
{
    public readonly struct AddressableAssetMetadata<T>
    {
        public readonly T Asset;
        private readonly IResourceLocation m_Location;
        /// <summary>
        /// The key the asset was requested with (asset guid or address)
        /// </summary>
        public readonly string Key;
        // ReSharper disable once InconsistentNaming
        public string PrimaryKey => m_Location.PrimaryKey;

        public AddressableAssetMetadata(T asset, string key, IResourceLocation location)
        {
            this.Asset = asset;
            this.Key = key;
            this.m_Location = location;
        }
    }
    public static class ResourcesSystem
    {
        private static readonly Dictionary<string, string> m_MapAssetGuidToAssetAddress = new Dictionary<string, string>();
        private static readonly SignedIndexedPool<Action> HandleReleaseActions = new (500);
        private static readonly SignedIndexedPool<AsyncOperationHandle<SceneInstance>> LoadSceneOperationHandles = new (500);
        private static Dictionary<string, IDisposable> _ScheduledReleases = new Dictionary<string, IDisposable>();

        public static async UniTask<string> GetAssetAddress(string assetGuid)
        {
            if (m_MapAssetGuidToAssetAddress.TryGetValue(assetGuid, out var address))
            {
                return address;
            }
            var handle = Addressables.LoadResourceLocationsAsync(assetGuid);
            var task = handle.ToUniTask();
            var locations = await task;
            if (locations.Count == 0)
            {
                return null;
            }

            var primaryKey = locations[0].PrimaryKey;
            m_MapAssetGuidToAssetAddress[assetGuid] = primaryKey;

            return locations[0].PrimaryKey;
        }
        
        public static async UniTask<IResourceLocation> GetAssetLocation(string assetGuid)
        {
            if (m_MapAssetGuidToAssetAddress.TryGetValue(assetGuid, out var address))
            {
                return null;
            }
            var handle = Addressables.LoadResourceLocationsAsync(assetGuid);
            var task = handle.ToUniTask();
            var locations = await task;
            if (locations.Count == 0)
            {
                return null;
            }

            return locations[0];
        }
        
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
            
            var token = HandleReleaseActions.Add(() =>
            {
                _ScheduledReleases[address] = Schedule(gracePeriod, () =>
                {
                    Addressables.Release(handle);
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
        
        public static AsyncSummon<IList<T>> SummonMany<T>(IEnumerable keys) where T : UnityEngine.Object
        {
            var tcs = SafeUniTaskCompletionSource<IList<T>>.Create();
            // Загружаем все ассеты сразу, без промежуточных колбэков
            var handle = Addressables.LoadAssetsAsync<T>((IEnumerable)keys, (result) =>
            {
                Console.Debug("ResourcesSystem", "SummonManyResult", result.GetType().Name);
            }, Addressables.MergeMode.Union, true);
    
            // Регистрируем единую «разрушайку»
            var token = HandleReleaseActions.Add(() => Addressables.Release(handle));
    
            handle.Completed += op =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                    tcs.TrySetResult(op.Result);
                else
                    tcs.TrySetException(new Exception($"Failed to load assets"));
            };
    
            return new AsyncSummon<IList<T>>(token, tcs.Task, Release);
        }

        public static AsyncSummon<IList<T>> SummonMany<T>(IEnumerable<AssetReferenceT<T>> addresses)
            where T : UnityEngine.Object
        {
            return SummonMany<T>(addresses.Select(a => a.AssetGUID));
        }

        public static AsyncSummon<IList<T>> SummonMany<T>(string[] keys) where T : UnityEngine.Object
        {
            return SummonMany<T>(keys.AsEnumerable());
        }

        public static AsyncSummon<AddressableAssetMetadata<T>> SummonWithMetadata<T>(string key) where T: UnityEngine.Object
        {
            var css = SignalSource.Create();
            var task = InternalGetWithMetadata<T>(key, css.Signal);

            var token = HandleReleaseActions.Add(css.TryRaise);
            
            return new AsyncSummon<AddressableAssetMetadata<T>>(token, task, Release);
        }

        public static AsyncSummon<AddressableAssetMetadata<T>> SummonWithMetadata<T>(AssetReference key)
            where T : UnityEngine.Object
        {
            return SummonWithMetadata<T>(key.AssetGUID);
        }

        public static AsyncSummon<AddressableAssetMetadata<T>[]> SummonManyWithMetadata<T>(IEnumerable<string> keys) where T : UnityEngine.Object
        {
            var css = SignalSource.Create();
            var task = InternalGetManyWithMetadata<T>(keys, css.Signal);
            
            // Регистрируем единую «разрушайку»
            var token = HandleReleaseActions.Add(css.TryRaise);
            
            return new AsyncSummon<AddressableAssetMetadata<T>[]>(token, task, Release);
        }

        public static AsyncSummon<AddressableAssetMetadata<T>[]> SummonManyWithMetadata<T>(IEnumerable<AssetReference> assetReferences)
            where T : UnityEngine.Object
        {
            return SummonManyWithMetadata<T>(assetReferences.Select(a => a.AssetGUID));
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

        /// <summary>
        /// Check whether the asset guid is valid
        /// </summary>
        /// <param name="assetGuid"></param>
        /// <returns></returns>
        public static async UniTask<bool> IsValid(string assetGuid)
        {
            AsyncOperationHandle<IList<IResourceLocation>> handle = Addressables.LoadResourceLocationsAsync(assetGuid);

            await handle.Task;

            return handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null && handle.Result.Count > 0;
        }
        
        /// <summary>
        /// Check many asset guids for valid state
        /// </summary>
        /// <param name="assetGuids"></param>
        /// <returns></returns>
        public static async UniTask<Dictionary<string, bool>> AreValid(IEnumerable<string> assetGuids)
        {
            var guids = assetGuids.ToList();

            // Запрашиваем все ресурсы одним вызовом
            var handle = Addressables.LoadResourceLocationsAsync(guids);
            await handle.Task;

            var result = new Dictionary<string, bool>();

            if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
            {
                // Составим хешсет всех успешно найденных ресурсов
                HashSet<string> foundKeys = handle.Result
                    .Select(loc => loc.PrimaryKey)
                    .ToHashSet();

                // Проверим какие GUID были найдены
                foreach (string guid in guids)
                {
                    result[guid] = foundKeys.Contains(guid);
                }
            }
            else
            {
                // В случае ошибки считаем, что ничего не найдено
                foreach (string guid in guids)
                    result[guid] = false;
            }

            return result;
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

        private static async UniTask<AddressableAssetMetadata<T>> InternalGetWithMetadata<T>(string key, Signal cancellationSignal) where T : UnityEngine.Object
        {
            var locationsHandle = Addressables.LoadResourceLocationsAsync(key);
            cancellationSignal.Register(() =>
            {
                Addressables.Release(locationsHandle);
            });
            var locations = await locationsHandle.ToUniTask(cancellationToken: cancellationSignal);
            if (locations.Count == 0)
            {
                throw new KeyNotFoundException($"Failed to load asset with key: {key}");
            }
            var handle = Addressables.LoadAssetAsync<T>(locations[0]);
            cancellationSignal.Register(() =>
            {
                Addressables.Release(handle);
            });
            var asset = await handle.Task;
            
            return new AddressableAssetMetadata<T>(asset, key, locations[0]);
        }
        
        private static async UniTask<AddressableAssetMetadata<T>[]> InternalGetManyWithMetadata<T>(IEnumerable<string> keys, Signal cancellationSignal) where T : UnityEngine.Object
        {
            try
            {
                var uniqueKeys = ParamLessPool<HashSet<string>>.GetShared();
                foreach (var key in keys)
                {
                    uniqueKeys.Add(key);
                }

                var arrayUniqueKeys = uniqueKeys.ToArray();
                uniqueKeys.Clear();
                ParamLessPool<HashSet<string>>.ReturnShared(uniqueKeys);

                var locationsHandle =
                    Addressables.LoadResourceLocationsAsync(arrayUniqueKeys, Addressables.MergeMode.Union);
                cancellationSignal.Register(() => { Addressables.Release(locationsHandle); });
                var locations = await locationsHandle.ToUniTask(cancellationToken: cancellationSignal);

                var tasks = new UniTask<T>[locations.Count];
                var handles = new AsyncOperationHandle<T>[locations.Count];
                var index = 0;
                foreach (var resourceLocation in locations)
                {
                    var handle = Addressables.LoadAssetAsync<T>(resourceLocation.PrimaryKey);
                    handles[index] = handle;
                    tasks[index] = handle.ToUniTask(cancellationToken: cancellationSignal);
                    index++;
                }

                cancellationSignal.Register(() =>
                {
                    foreach (var handle in handles)
                    {
                        Addressables.Release(handle);
                    }
                });
                var loadedAssets = await UniTask.WhenAll(tasks);
                var resultMetadata = new AddressableAssetMetadata<T>[locations.Count];

                index = 0;
                foreach (var asset in loadedAssets)
                {
                    resultMetadata[index] = new AddressableAssetMetadata<T>(asset, arrayUniqueKeys[index], locations[index]);
                    index++;
                }

                return resultMetadata;
            }
            catch (OperationCanceledException)
            {
                // Ignore
                throw;
            }
            catch (Exception e)
            {
                Console.Error("ResourcesSystem", $"Failed to get many with metadata: {e}");
                throw;
            }
        }
    }
}