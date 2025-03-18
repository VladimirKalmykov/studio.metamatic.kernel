using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
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
        public enum GameEvent
        {
            IntroSceneComplete,
        }
        
        private static Observable<VirtualWorld> m_ObservableVirtualWorld = new Observable<VirtualWorld>(); 
        #region Public API

        public static VirtualWorld virtualWorld => m_ObservableVirtualWorld.Value;
        public static bool isGameRunning { get; private set; }
        private static AssetReference _CurrentBaseSceneRef;
        private static AsyncDisposableSummon<SceneInstance> _CurrentBaseSceneSummon =
            AsyncDisposableSummon<SceneInstance>.Empty;
        private static Dictionary<AssetReference, AsyncDisposableSummon<SceneInstance>> _CurrentAdditionalSceneSummons =
            new();
        private static List<UniTask> _UnloadTasks = new();

        static GameSystem()
        {
            
        }

        public static bool StartGame()
        {
            if (isGameRunning)
            {
                return false;
            }
            isGameRunning = true;
            /* Create shadow world */
            m_ObservableVirtualWorld.Value = new VirtualWorld();
            return true;
        }
        
        public static bool ResetGame()
        {
            if (!isGameRunning)
            {
                return false;
            }
            virtualWorld.Dispose();
            m_ObservableVirtualWorld.Value = new VirtualWorld();
            return true;
        }

        public static bool ExitGame()
        {
            if (!isGameRunning)
            {
                return false;
            }
            virtualWorld.Dispose();
            isGameRunning = false;
            return true;
        }
        
        public static DisposableAction WatchVirtualWorldAsync(Func<VirtualWorld, Signal, UniTaskVoid> action)
        {
            return m_ObservableVirtualWorld.WatchAsync(action);
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