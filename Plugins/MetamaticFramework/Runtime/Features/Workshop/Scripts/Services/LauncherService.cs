using System;
using Metamatic.SaveSystem;
using Metamatic.Systems;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Metamatic.Framework.Workshop
{
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using Metamatic;
    using Newtonsoft.Json;

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
        [SerializeField] private AssetReference m_StartScene;
        [SerializeField] private AssetReference m_MainMenuScene;
        [SerializeField] private AssetReference m_LoadGameScene;
        #endregion
        
        #region Private properties
        private AsyncDisposableSummon<SceneInstance> m_StartSceneSummon = AsyncDisposableSummon<SceneInstance>.Empty;
        private AsyncDisposableSummon<SceneInstance> m_MainMenuSceneSummon = AsyncDisposableSummon<SceneInstance>.Empty;
        #endregion

        protected override async UniTask RunService(Ready ready, Signal signal)
        {
            /* Preload and activate start scene */
            m_StartSceneSummon = ResourcesSystem.LoadScene(m_StartScene, LoadSceneMode.Additive, activationOnLoad: true);
            /* While the start scene is loading, load the main menu scene */
            m_MainMenuSceneSummon = ResourcesSystem.LoadScene(m_MainMenuScene, LoadSceneMode.Additive, activationOnLoad: false);
            /* Wait for start clip end */
            await Dispatcher.global.WhenEmitted<string>("start_scene_complete", signal);
            /* Unload start scene */
            m_StartSceneSummon.DisposeAsync().Forget();
            /* Activate main menu scene */
            var mainMenuScene = await m_MainMenuSceneSummon.Wait(signal);
            await mainMenuScene.ActivateAsync().ToUniTask();
            await ready();
        }

        public async UniTask Save(string address = "latest", Signal cancellationSignal = default)
        {
            var data = GameSystem.shadowWorld.Save();

            Console.Debug("Launcher", $"Saving game data: {data}");
            await Repository.LocalStorage.Set(address, data, cancellationSignal);
        }

        public async UniTask Load(string saveAddress = "latest", Signal signal = default)
        {
            var saveData = await Repository.LocalStorage.Get<SaveData>(saveAddress, signal);
            Console.Debug("Launcher", $"Loaded game data: {saveData}");
            
            await GameSystem.shadowWorld.Load(saveData);
        }

        /// <summary>
        /// Start the game from the beginning.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async UniTask Launch(AssetReferenceT<Chapter> chapterReferenceT) // Chapter chapter
        {
            // Show load game scene
            // Launch the game async
            using var summonChapter = ResourcesSystem.Summon<Chapter>(chapterReferenceT);
            var chapter = await summonChapter.Wait(default);
            
            await GameSystem.ActivateScenes(chapter.baseScene, chapter.scenes);
            var servicesReadyTasks = Service.Services.Values.Select(service => service.WhenReady(cancellationSignal)).ToArray();
            await UniTask.WhenAll(servicesReadyTasks);
            
            var tasks = new List<UniTask>();
            foreach (var service in Service.Services.Values)
            {
                if (service is ISavable savable)
                {
                    tasks.Add(savable.Bootstrap());
                }
            }
            
            await UniTask.WhenAll(tasks);
        }
    }
}