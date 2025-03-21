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
            public string place;
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
            #if UNITY_EDITOR
            ProcessDevelopmentLoadingScenario().Forget();
            #endif
            await ready();
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
            var worldData = GameSystem.virtualWorld.Save();

            var saveData = new SaveData()
            {
                dateTime = DateTime.Now.ToString(),
                place = m_CurrentChapterAddress,
                world = worldData
            };
            Console.Debug("Launcher", $"Saving game data: {saveData}");
            await Repository.LocalStorage.Set(address, saveData, cancellationSignal);
        }

        public async UniTask Load(string saveAddress = "latest", bool seamless = false, Signal signal = default)
        {
            var saveData = await Repository.LocalStorage.Get<SaveData>(saveAddress, signal);
            Console.Debug("Launcher", $"Loaded game data: {saveData}");
            
            if (GameSystem.isGameRunning)
            {
                if (!seamless)
                {
                    GameSystem.ResetGame();
                }
            }
            else
            {
                GameSystem.StartGame();
            }
            
            await LaunchGame(saveData.place, seamless, signal);
            
            var worldData = saveData.world;
            
            await GameSystem.virtualWorld.Load(worldData);
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
        public async UniTask LaunchGame(string addressOrGuid, bool seamless, Signal cancellationSignal) // Place place
        {
            try
            {
                Console.Debug("Launcher", "Launching game.");
                if (!GameSystem.StartGame())
                {
                    throw new Exception("Game is already running.");
                }
                // Activate load game scene
                SceneInstance loadSceneInstance = default;
                if (!seamless)
                {
                    loadSceneInstance = await m_LoadGameSceneSummon.Wait(cancellationSignal);
                    loadSceneInstance.ActivateAsync().ToUniTask().Forget();
                }

                // Unload main menu scene
                await Addressables.UnloadSceneAsync(m_MainMenuSceneInstance, false);
                // Show load game scene
                // Launch the game async
                using var summonChapter = ResourcesSystem.Summon<Place>(addressOrGuid);
                var chapter = await summonChapter.Wait(default);
            
                await GameSystem.ActivateScenes(chapter.baseScene, chapter.gameScene, seamless);
                var servicesReadyTasks = Service.Services.Values.Select(service => service.WhenReady(cancellationSignal)).ToArray();
                await UniTask.WhenAll(servicesReadyTasks);
                // Hide load game scene
                if (!seamless)
                {
                    await Addressables.UnloadSceneAsync(loadSceneInstance, false);
                }

                m_CurrentChapterAddress = addressOrGuid;
            } catch (Exception e)
            {
                Console.Error("Launcher", e);
            }
        }
    }
}