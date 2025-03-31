using System;
using Metamatic.Systems;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Metamatic.Framework.Workshop
{
    using System.Linq;
    using Cysharp.Threading.Tasks;

    /// <summary>
    /// Launcher is responsible for saving, loading the game, and start game from the beginning.
    /// </summary>
    public class LauncherService : Service
    {
        [Serializable]
        [Version("1.0.0")]
        public struct SaveData
        {
            public string dateTime;
            public string chapter;
            public object world;
        }
        
        #region Fields
        [SerializeField] private AssetReference startScene;
        [SerializeField] private AssetReference mainMenuScene;
        [SerializeField] private AssetReference loadGameScene;
        #endregion
        
        #region Private properties
        private AsyncDisposableSummon<SceneInstance> m_StartSceneSummon = AsyncDisposableSummon<SceneInstance>.Empty;
        private AsyncDisposableSummon<SceneInstance> m_MainMenuSceneSummon = AsyncDisposableSummon<SceneInstance>.Empty;
        private AsyncDisposableSummon<SceneInstance> m_LoadGameSceneSummon = AsyncDisposableSummon<SceneInstance>.Empty;
        private SceneInstance m_MainMenuSceneInstance;
        private SceneInstance m_LoadingSceneInstance;
        private string m_CurrentChapterAddress;
        #endregion

        protected override async UniTask RunService(Ready ready, Signal signal)
        {
            DontDestroyOnLoad(this.gameObject);
            /* Load editor prefs */
#if METAMATIC_SKIP_GAME_INTRO
                var skipGameIntro = true;
#else
            #if UNITY_EDITOR
                var skipGameIntro = EditorPrefs.GetBool(GameSystem.ConfigSkipGameIntroKeyName, false);
            #else
                var skipGameIntro = false;            
            #endif
#endif

            if (!skipGameIntro)
            {
                /* Preload and activate start scene */
                m_StartSceneSummon = ResourcesSystem.LoadScene(startScene, LoadSceneMode.Additive, activateOnLoad: true);
                await m_StartSceneSummon.Wait(signal);
            }
            
            /* We have to load main menu after intro scene, because of addressable bug with activateOnLoad (https://discussions.unity.com/t/loadsceneasync-activateonload-false-does-not-work/817793/12)  */
            m_MainMenuSceneSummon = ResourcesSystem.LoadScene(this.mainMenuScene, LoadSceneMode.Additive, activateOnLoad: false);

            if (!skipGameIntro)
            {
                /* Wait for start clip end */
                await Dispatcher.global.WhenEmitted<GameSystem.GameEvent>(GameSystem.GameEvent.IntroSceneComplete, signal);
                Console.Debug("Launcher", "Intro scene complete.");
            }
            
            /* Activate main menu scene */
            m_MainMenuSceneInstance = await m_MainMenuSceneSummon.Wait(signal);
            await m_MainMenuSceneInstance.ActivateAsync().ToUniTask();

            if (!skipGameIntro)
            {
                /* Unload start scene */
                m_StartSceneSummon.DisposeAsync().Forget();
            }
            
            /* Load load game scene */
            m_LoadGameSceneSummon = ResourcesSystem.LoadScene(loadGameScene, LoadSceneMode.Additive, activateOnLoad: false);

            using var watchGameAppStage = GameSystem.WatchGameAppStageAsync(OnGameAppStageChange);
            
            #if UNITY_EDITOR
            ProcessDevelopmentLoadingScenario().Forget();
            #endif
            await ready();
        }
        
        private async UniTaskVoid OnGameAppStageChange(GameAppStage stage, Signal cancellationSignal)
        {
            try
            {
                switch (stage)
                {
                    case GameAppStage.Stopped:
                    {
                        m_MainMenuSceneInstance.ActivateAsync();
                        await cancellationSignal;
                        Addressables.UnloadSceneAsync(m_MainMenuSceneInstance, false);
                        break;
                    }
                    case GameAppStage.Loading:
                    case GameAppStage.Reloading:
                        m_LoadingSceneInstance = await m_LoadGameSceneSummon.Wait(cancellationSignal);
                        m_LoadingSceneInstance.ActivateAsync();
                        await cancellationSignal;
                        await Addressables.UnloadSceneAsync(m_LoadingSceneInstance, false);
                        break;
                    case GameAppStage.Unloading:
                        // Do nothing, show black screen
                        break;
                    case GameAppStage.Running:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(stage), stage, null);
                }
            } catch (Exception e)
            {
                Console.Error("Launcher", e);
            }
        }
        
        #if UNITY_EDITOR
        private async UniTaskVoid ProcessDevelopmentLoadingScenario()
        {
            #if UNITY_EDITOR
            var forceLoadPlaceAddress = EditorPrefs.GetString(GameSystem.ConfigForceStartChapterGuid, "");
            if (String.IsNullOrEmpty(forceLoadPlaceAddress)) return;
            
            await UniTask.Delay(1000);
            LaunchGame(forceLoadPlaceAddress, false, default).Forget();
            #endif
        }
        #endif

        public async UniTask Save(string address = "latest", Signal cancellationSignal = default)
        {
            try
            {
                using var pending = PendingSystem.CreatePending("Save game");
                var worldData = GameSystem.virtualWorld.Save();

                var saveData = new SaveData()
                {
                    dateTime = DateTime.Now.ToString(),
                    chapter = m_CurrentChapterAddress,
                    world = worldData
                };
                Console.Debug("Launcher", $"Saving game data: {saveData}");
                await Repository.LocalStorage.Set(address, saveData, cancellationSignal);
            } catch (Exception e)
            {
                Console.Error("Launcher", e);
            }
        }

        public async UniTask<bool> Load(string saveAddress = "latest", bool seamless = false, Signal signal = default)
        {
            if (!seamless)
            {
                
            }

            try
            {
                using var pending = PendingSystem.CreateCriticalPending("Load game");
                // Stop game
                var stopGameTask = GameSystem.StopGame();
                var saveData = await Repository.LocalStorage.Get<SaveData>(saveAddress, signal);
                Console.Debug("Launcher", $"Loaded game data: {saveData}");
                var stopResult = await stopGameTask;

                if (stopResult)
                {
                    await GameSystem.LoadGame(saveData.chapter, saveData.world, seamless, signal);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                return false;
            } catch (Exception e)
            {
                // TODO: Fallback to error screen?
                Console.Error("Launcher", e);
                return false;
            }
        }

        public UniTask LaunchGame(AssetReferenceT<Place> chapterReferenceT, bool seamless, Signal cancellationSignal) // Place place
        {
            return LaunchGame(chapterReferenceT.AssetGUID, seamless, cancellationSignal);
        }

        /// <summary>
        /// Start the game from the beginning.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public UniTask LaunchGame(string addressOrGuid, bool seamless, Signal cancellationSignal) // Place place
        {
            return GameSystem.LaunchGame(addressOrGuid, seamless, cancellationSignal);
        }
        
        private async UniTask StopGameAsync()
        {
            try
            {
                await GameSystem.StopGame();
            } catch (Exception e)
            {
                Console.Error("Launcher", e);
            }
        }
    }
}