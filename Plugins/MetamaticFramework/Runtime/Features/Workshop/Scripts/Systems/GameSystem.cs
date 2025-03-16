using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Metamatic.Framework.Workshop;
using Metamatic.Systems;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Metamatic.Framework
{
    /**
     * GameSystem is responsible for managing the game launch, save, and load.
     */
    public static class GameSystem
    {
        #region Public API
        public static ShadowWorld shadowWorld { get; private set; }
        private static AssetReference _CurrentBaseSceneRef;
        private static AsyncDisposableSummon<SceneInstance> _CurrentBaseSceneSummon =
            AsyncDisposableSummon<SceneInstance>.Empty;
        private static Dictionary<AssetReference, AsyncDisposableSummon<SceneInstance>> _CurrentAdditionalSceneSummons =
            new();
        private static List<UniTask> _UnloadTasks = new();

        static GameSystem()
        {
            /* Create shadow world */
            shadowWorld = new ShadowWorld();
        }
        
        public static async UniTask ActivateScenes(AssetReference baseScene, AssetReference[] additiveScenes = null, bool trySeamless = false, Signal cancellationSignal = default)
        {
            try
            {
                if (trySeamless && _CurrentBaseSceneRef != baseScene)
                {
                    // Unload previous base scene
                    await _CurrentBaseSceneSummon.DisposeAsync();
                }
                
                // Unload previous additional scenes
                _UnloadTasks.Clear();
                foreach (var scene in _CurrentAdditionalSceneSummons.ToArray())
                {
                    /* Skip if additiveScenes contains the scene */
                    if (trySeamless && additiveScenes != null && additiveScenes.Contains(scene.Key))
                    {
                        continue;
                    }
                    _UnloadTasks.Add(scene.Value.DisposeAsync());
                    /* Remove from the list */
                    _CurrentAdditionalSceneSummons.Remove(scene.Key);
                }
                var result = await UniTask.WhenAny(cancellationSignal.ToUniTask(), UniTask.WhenAll(_UnloadTasks));
                if (result == 0)
                {
                    throw new OperationCanceledException();
                }
            }
            finally
            {
                await _CurrentBaseSceneSummon.DisposeAsync();
                _CurrentBaseSceneRef = baseScene;
                _CurrentBaseSceneSummon = ResourcesSystem.LoadScene(baseScene, LoadSceneMode.Single);
            }
            
            // Load additional scenes as additive
            if (additiveScenes == null)
            {
                return;
            }
            var additiveScenesToLoad = trySeamless ? additiveScenes.Except(_CurrentAdditionalSceneSummons.Keys) : additiveScenes;
            var waitList = new List<UniTask>(); // TODO: Use property instead of local variable
            foreach (var scene in additiveScenesToLoad)
            {
                _CurrentAdditionalSceneSummons[scene] = ResourcesSystem.LoadScene(scene, LoadSceneMode.Additive);
                waitList.Add(_CurrentAdditionalSceneSummons[scene].Wait(cancellationSignal));
            }
            
            await UniTask.WhenAll(waitList);
        }
        #endregion
        
    }
}