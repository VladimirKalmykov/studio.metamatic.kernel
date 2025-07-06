using System;
using Metamatic.Systems;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using Plugins.MetamaticFramework.Runtime.Features.Workshop.Scripts.Classes;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Serialization;

namespace Metamatic.Framework.Workshop
{
    /// <summary>
    /// Launcher is responsible for saving, loading the game, and start game from the beginning.
    /// </summary>
    public class GameService : Service
    {
        #region Fields
        [BoxGroup("Scenes")]
        [SerializeField] private AssetReference startScene;
        [BoxGroup("Scenes")]
        [SerializeField] private AssetReference mainMenuScene;
        [BoxGroup("Scenes")]
        [SerializeField] private AssetReference winGameScene;
        [BoxGroup("Resources")]
        [SerializeField] private AssetReferenceT<Presenter> loadingScreen;
        [BoxGroup("Resources")]
        [SerializeField] private AssetReferenceT<Place> startPlace;
        
        [BoxGroup("Virtual world")]
        [Tooltip("If true, dematerialized entities will be virtualized and saved. If false, they will be removed from the virtual world immediately. This rule applies to all entities, except those that have been kicked.")]
        public bool virtualizeEntities = true;
        [Tooltip("Delay before dematerialized entities are removed from the virtual world. This rule applies to all entities, except those that have been kicked. Use zero to keep them in the virtual world forever.")]
        public float virtualizedEntityLifeTime = 0f;
        #endregion
        
        #region Private properties

        private AsyncStateMachine<GameLoopEvent, GameService> m_GameLoopStateMachine;
        private AsyncDisposableSummon<SceneInstance> m_StartSceneSummon = AsyncDisposableSummon<SceneInstance>.Empty;
        private SceneInstance m_LoadingSceneInstance;
        private string m_CurrentChapterAddress;
        #endregion
        
        #region Public properties
        public bool isInMainMenu => m_GameLoopStateMachine.currentState == GameLoopEvent.MenuGameLoopEvent;
        #endregion

        protected override async UniTask RunService(Ready ready, Signal signal)
        {
            DontDestroyOnLoad(this.gameObject);
            
            /* Init state machine */
            m_GameLoopStateMachine = new AsyncStateMachine<GameLoopEvent, GameService>(OnStateChange, this, GameLoopEvent.IdleGameLoopEvent, keepOnlyLatestState: true, refuseNextStateIfNotReady: true);
            
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
            
            
            if (!skipGameIntro)
            {
                /* Wait for start clip end */
                await Dispatcher.global.WhenEmitted<GameSystem.GameEvent>(GameSystem.GameEvent.IntroSceneComplete, signal);
                Console.Debug("Launcher", "Intro scene complete.");
            }

            if (!skipGameIntro)
            {
                /* Unload start scene */
                m_StartSceneSummon.DisposeAsync().Forget();
            }

            try
            {
                /* Show main menu */
                await m_GameLoopStateMachine.PushStateAsync(GameLoopEvent.MenuGameLoopEvent);
            }
            finally
            {
#if UNITY_EDITOR
                ProcessDevelopmentLoadingScenario().Forget();
#endif
                await ready();
            }
        }

        private async UniTask OnStateChange(AsyncStateMachine<GameLoopEvent, GameService>.Transition transition)
        {
            switch (transition.state)
            {
                case IdleGameLoopEvent:
                    // Skip, wait next state
                    break;
                case MenuGameLoopEvent:
                {
                    await GameSystem.TryStopGame();
                    /* We can not preload main menu before intro scene show, because of addressable bug with activateOnLoad (https://discussions.unity.com/t/loadsceneasync-activateonload-false-does-not-work/817793/12)  */
                    await using var mainMenuScene = ResourcesSystem.LoadScene(this.mainMenuScene, LoadSceneMode.Additive, activateOnLoad: true);
                    await mainMenuScene.Wait(transition.generalCancellationSignal);
                    await transition.WaitNextState();
                    break;
                }
                case LaunchGameLoopEvent launchingGameLoopEvent:
                {
                    var pending = PendingSystem.CreateCriticalPending("Launch game");
                    try
                    {
                        var seamless = launchingGameLoopEvent.Seamless;
                        var placeAddress = launchingGameLoopEvent.PlaceAddress;
                        if (!seamless)
                        {
                            await GameSystem.TryStopGame();
                            // Show loading screen
                        }

                        // Luanch game
                        await GameSystem.LaunchGame(placeAddress, seamless, virtualizeEntities, virtualizedEntityLifeTime);
                        pending.Dispose();
                        await transition.WaitNextState();
                        if (!seamless)
                        {
                            // Hide loading screen
                            break;
                        }

                        await transition.WaitNextState();
                    }
                    finally
                    {
                        pending.Dispose();
                    }
                    break;
                }
                case LoadGameLoopEvent loadGameLoopEvent:
                {
                    var pending = PendingSystem.CreateCriticalPending("Load game");
                    try
                    {
                        var saveService = await Service.Resolve<SaveService>();
                        if (!loadGameLoopEvent.seamless)
                        {
                            await GameSystem.TryStopGame();
                            // Show loading screen
                        }
                        var saveData = await saveService.Load(loadGameLoopEvent.saveAddress, seamless: loadGameLoopEvent.seamless, transition.cancellationSignal);

                        await GameSystem.LoadGame(saveData.Place, saveData.World, loadGameLoopEvent.seamless);
                        pending.Dispose();
                        if (!loadGameLoopEvent.seamless)
                        {
                            // Hide loading screen
                            break;
                        }

                        await transition.WaitNextState();
                    }
                    catch (Exception e)
                    {
                        Console.Error("GameService", e);
                    }
                    finally
                    {
                        pending.Dispose();
                    }
                    break;
                }
                case WinGameLoopEvent winGameLoopEvent:
                {
                    // Stop game
                    await GameSystem.TryStopGame();
                    await using var winGameScene = ResourcesSystem.LoadScene(this.winGameScene, LoadSceneMode.Additive, activateOnLoad: true);
                    await transition.WaitNextState();
                    break;
                }
            }
        }
        
        #if UNITY_EDITOR
        /// <summary>
        /// Performs development loading scenario.
        /// </summary>
        private async UniTaskVoid ProcessDevelopmentLoadingScenario()
        {
            #if UNITY_EDITOR
            var forceLoadPlaceAddress = EditorPrefs.GetString(GameSystem.ConfigForceStartChapterGuid, "");
            if (String.IsNullOrEmpty(forceLoadPlaceAddress)) return;
            
            // await UniTask.Delay(1000);
            await m_GameLoopStateMachine.WaitCurrentState();
            m_GameLoopStateMachine.PushState(new LaunchGameLoopEvent(forceLoadPlaceAddress, false));
            #endif
        }
        #endif

        /// <summary>
        /// Save game (can be called only during gameplay)
        /// </summary>
        /// <param name="address"></param>
        /// <param name="cancellationSignal"></param>
        /// <returns></returns>
        public async UniTask<bool> Save(string address = "latest", Signal cancellationSignal = default)
        {
            var saveService = await Service.Resolve<SaveService>();
            return await saveService.Save(address, cancellationSignal);
        }

        public async UniTask<bool> Load(string saveAddress = "latest", bool seamless = false)
        {
            var stateToken = m_GameLoopStateMachine.PushState(new LoadGameLoopEvent(saveAddress, seamless));
            if (!m_GameLoopStateMachine.isValidState(stateToken))
            {
                return false;
            }

            await m_GameLoopStateMachine.WaitState(stateToken);
            return true;
        }
        
        public UniTask LaunchNewGame()
        {
            return LaunchPlace(startPlace.AssetGUID, false);
        }

        public UniTask LaunchPlace(AssetReferenceT<Place> chapterReferenceT, bool seamless)
        {
            return LaunchPlace(chapterReferenceT.AssetGUID, seamless);
        }

        /// <summary>
        /// Start the game from the beginning.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async UniTask<bool> LaunchPlace(string placeGuid, bool seamless)
        {
            var stateToken = m_GameLoopStateMachine.PushState(new LaunchGameLoopEvent(placeGuid, seamless));
            if (!m_GameLoopStateMachine.isValidState(stateToken))
            {
                return false;
            }

            await m_GameLoopStateMachine.WaitState(stateToken);
            return true;
        }
        
        public async UniTask<bool> EnsurePlaceLoaded(string placeGuid, bool seamless)
        {
            if (GameSystem.IsPlaceActive(placeGuid))
            {
                return true;
            }

            var stateToken = m_GameLoopStateMachine.PushState(new LaunchGameLoopEvent(placeGuid, seamless));
            if (!m_GameLoopStateMachine.isValidState(stateToken))
            {
                return false;
            }

            await m_GameLoopStateMachine.WaitState(stateToken);
            return true;
        }
        
        public async UniTask<bool> ExitToMainMenu()
        {
            var stateToken = m_GameLoopStateMachine.PushState(GameLoopEvent.MenuGameLoopEvent);
            if (!m_GameLoopStateMachine.isValidState(stateToken))
            {
                return false;
            }

            await m_GameLoopStateMachine.WaitState(stateToken);
            return true;
        }

        /// <summary>
        /// Load win game scene.
        /// </summary>
        /// <returns></returns>
        public async UniTask<bool> WinGame()
        {
            var stateToken = m_GameLoopStateMachine.PushState(GameLoopEvent.WinGameLoopEvent);
            if (!m_GameLoopStateMachine.isValidState(stateToken))
            {
                return false;
            }
            await m_GameLoopStateMachine.WaitState(stateToken);
            return true;
        }
    }
}