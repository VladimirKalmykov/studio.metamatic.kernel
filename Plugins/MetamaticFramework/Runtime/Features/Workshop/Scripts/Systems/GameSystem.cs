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
        // TODO: Add loading list 
        
        #region Constants
        public const string ConfigSkipGameIntroKeyName = "Metamatic_ConfigSkipGameIntro";
        public const string ConfigSkipMainMenuOnStartKeyName = "Metamatic_ConfigSkipMainMenuOnStart";
        public const string ConfigForceStartChapterGuid = "Metamatic_ConfigForceStartChapterGuid";
        #endregion
        public enum GameEvent
        {
            IntroSceneComplete,
        }
        
        private static Observable<VirtualWorld> m_ObservableVirtualWorld = new Observable<VirtualWorld>(); 
        #region Public API

        public static VirtualWorld virtualWorld => m_ObservableVirtualWorld.Value;
        public static bool isGameRunning { get; private set; }
        private static AssetReference _CurrentBaseSceneRef;
        private static AssetReference _CurrentGameSceneRef;
        private static AsyncDisposableSummon<SceneInstance> _CurrentBaseSceneSummon =
            AsyncDisposableSummon<SceneInstance>.Empty;
        private static AsyncDisposableSummon<SceneInstance> _CurrentGameSceneSummon =
            AsyncDisposableSummon<SceneInstance>.Empty;
        private static List<UniTask> _UnloadTasks = new();

        static GameSystem()
        {
            
        }

        public static bool StartGame()
        {
            Console.Debug("GameSystem", "StartGame");
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
        
        public static async UniTask ActivateScenes(AssetReference baseScene, AssetReference gameScene = null, bool trySeamless = false)
        {
            try
            {
                if (!trySeamless || _CurrentBaseSceneRef != baseScene)
                {
                    // Unload previous base scene
                    await _CurrentBaseSceneSummon.DisposeAsync();
                }
                
                if (!trySeamless || (_CurrentGameSceneRef != null && _CurrentGameSceneRef != gameScene))
                {
                    await _CurrentGameSceneSummon.DisposeAsync();
                }
                _CurrentGameSceneRef = gameScene;
                _CurrentBaseSceneRef = baseScene;
            
                // Load base scene
                _CurrentBaseSceneSummon = ResourcesSystem.LoadScene(_CurrentBaseSceneRef, LoadSceneMode.Additive);
            
                // Load game scene
                _CurrentBaseSceneSummon = ResourcesSystem.LoadScene(_CurrentGameSceneRef, LoadSceneMode.Additive);
            
                await UniTask.WhenAll(_CurrentBaseSceneSummon.ToUniTask(), _CurrentGameSceneSummon.ToUniTask());
            } catch (Exception e)
            {
                Console.Error("GameSystem", e);
                
                // Unload all scenes
                await _CurrentBaseSceneSummon.DisposeAsync();
                await _CurrentGameSceneSummon.DisposeAsync();
            }
        }
        #endregion
        
    }
}