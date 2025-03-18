using System;
using Metamatic.Systems;
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
        public class SaveServiceData
        {
            public Type serviceType;
            public Type dataType;
            public object data;
        }

        [Serializable]
        [Version("1.0.0")]
        public struct SaveData
        {
            public string dateTime;
            public SaveServiceData[] services;
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
        #endregion

        protected override async UniTask RunService(Ready ready, Signal signal)
        {
            /* Preload and activate start scene */
            m_StartSceneSummon = ResourcesSystem.LoadScene(startScene, LoadSceneMode.Additive, activationOnLoad: true);
            /* While the start scene is loading, load the main menu scene */
            m_MainMenuSceneSummon = ResourcesSystem.LoadScene(this.mainMenuScene, LoadSceneMode.Additive, activationOnLoad: false);
            /* Wait for start clip end */
            await Dispatcher.global.WhenEmitted<GameSystem.GameEvent>(GameSystem.GameEvent.IntroSceneComplete, signal);
            /* Unload start scene */
            m_StartSceneSummon.DisposeAsync().Forget();
            /* Activate main menu scene */
            var mainMenuScene = await m_MainMenuSceneSummon.Wait(signal);
            await mainMenuScene.ActivateAsync().ToUniTask();
            /* Load load game scene */
            m_LoadGameSceneSummon = ResourcesSystem.LoadScene(loadGameScene, LoadSceneMode.Additive, activationOnLoad: false);
            await ready();
        }

        public async UniTask Save(string address = "latest", Signal cancellationSignal = default)
        {
            var data = GameSystem.virtualWorld.Save();

            Console.Debug("Launcher", $"Saving game data: {data}");
            await Repository.LocalStorage.Set(address, data, cancellationSignal);
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
            
            await GameSystem.virtualWorld.Load(saveData);
        }

        /// <summary>
        /// Start the game from the beginning.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async UniTask LaunchGame(AssetReferenceT<Chapter> chapterReferenceT, Signal cancellationSignal) // Chapter chapter
        {
            if (!GameSystem.StartGame())
            {
                throw new Exception("Game is already running.");
            }
            // Activate load game scene
            var loadSceneInstance = await m_LoadGameSceneSummon.Wait(cancellationSignal);
            loadSceneInstance.ActivateAsync().ToUniTask().Forget();
            // Show load game scene
            // Launch the game async
            using var summonChapter = ResourcesSystem.Summon<Chapter>(chapterReferenceT);
            var chapter = await summonChapter.Wait(default);
            
            await GameSystem.ActivateScenes(chapter.baseScene, chapter.scenes);
            var servicesReadyTasks = Service.Services.Values.Select(service => service.WhenReady(cancellationSignal)).ToArray();
            await UniTask.WhenAll(servicesReadyTasks);
            // Hide load game scene
            await Addressables.UnloadSceneAsync(loadSceneInstance);
        }
    }
}