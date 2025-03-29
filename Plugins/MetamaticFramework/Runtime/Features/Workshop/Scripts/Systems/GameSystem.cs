using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Metamatic.Framework.Workshop;
using Metamatic.Systems;
using UnityEngine;
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
        public const string LimbPlaceAddress = "LIMB";
        public const string SystemPlaceAddress = "SYSTEM";
        public enum Screen
        {
            Intro,
            Loading,
            MainMenu,
            Game,
        }
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
        public static Observable<Screen> m_ObservableScreen = new Observable<Screen>(Screen.Intro);
        public static Observable<string> m_ActivePlaceAddress = new Observable<string>();
        public static bool isGameRunning { get; private set; }
        public static bool isGameLoaded { get; private set; }
        public static event Action<bool> OnBeforeGameScenesLoad;
        public static event Action OnGameScenesActivated;
        public static event Action OnBeforeGameStop;
        public static event Action OnGameStopped;
        public static event Action OnGameIsLoading;
        public static event Action OnGameLoaded;
        private static AssetReference _CurrentBaseSceneRef;
        private static AssetReference _CurrentGameSceneRef;
        private static AsyncDisposableSummon<SceneInstance> _CurrentBaseSceneSummon =
            AsyncDisposableSummon<SceneInstance>.Empty;
        private static AsyncDisposableSummon<SceneInstance> _CurrentGameSceneSummon =
            AsyncDisposableSummon<SceneInstance>.Empty;

        private static SignalSource _StopGameSignal;
        private static SceneInstance _CurrentBaseSceneInstance;
        private static SceneInstance _CurrentGameSceneInstance;
        public static event Action<Exception> OnFatalError;
        
        public static string activePlaceAddress => m_ActivePlaceAddress.Value;
        
        private SignalSource m_StopGameSignalSource = default;

        static GameSystem()
        {
            
        }

        /// <summary>
        /// Returns the address of the place where the game object is located.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public static string GetPlaceAddress(GameObject gameObject)
        {
            if (gameObject.scene == _CurrentGameSceneInstance.Scene)
            {
                return m_ActivePlaceAddress.Value ?? LimbPlaceAddress;
            }

            return LimbPlaceAddress;
        }
        
        public static DisposableAction WatchPlaceActivationAsync(Func<string, Signal, UniTaskVoid> action)
        {
            return m_ActivePlaceAddress.WatchAsync(action);
        }

        public static void FatalError(Exception error)
        {
            Console.Error("GameSystem", error);
            OnFatalError?.Invoke(error); // TOOD: And what then?
            StopGame().Forget();
        }

        public static DisposableAction WatchGameLoaded(Action handler)
        {
            OnGameLoaded += handler;
            return new DisposableAction(() => OnGameLoaded -= handler);
        }
        
        public static DisposableAction WatchGameLoadedAsync(Func<Signal, UniTaskVoid> handler)
        {
            SignalSource m_StopSignalSource = default;
            
            if (isGameLoaded)
            {
                m_StopSignalSource = SignalSource.Create();
                handler(m_StopSignalSource.Signal).Forget();
            }
            
            OnGameLoaded += OnGameLoadedHandler;
            OnBeforeGameStop += OnBeforeGameStopHandler;
            
            void OnGameLoadedHandler()
            {
                m_StopSignalSource.Dispose();
                m_StopSignalSource = SignalSource.Create();
                handler(m_StopSignalSource.Signal).Forget();
            }
            
            void OnBeforeGameStopHandler()
            {
                m_StopSignalSource.TryRaise();
            }
            
            return new DisposableAction(() =>
            {
                OnGameLoaded -= OnGameLoadedHandler;
                OnBeforeGameStop -= OnBeforeGameStopHandler;
            });
        }

        /// <summary>
        /// Start game from the scratch.
        /// </summary>
        /// <param name="addressOrGuid"></param>
        /// <param name="seamless"></param>
        /// <param name="cancellationSignal"></param>
        /// <returns></returns>
        public static async UniTask<bool> LaunchGame(string addressOrGuid, bool seamless, Signal cancellationSignal)
        {
            if (isGameRunning && !seamless)
            {
                return false;
            }
            OnGameIsLoading?.Invoke();
            var initializationResult = await InternalInitializeGame(addressOrGuid, seamless, cancellationSignal);
            if (!initializationResult)
            {
                return false;
            }
            isGameLoaded = true;
            _StopGameSignal.TryRaise();
            _StopGameSignal = SignalSource.Create();
            OnGameLoaded?.Invoke();
            await Service.InternalPerformGameLoadedStageAsync();
            return true;
        }

        /// <summary>
        /// Load game from the save.
        /// </summary>
        /// <param name="addressOrGuid"></param>
        /// <param name="worldData"></param>
        /// <param name="seamless"></param>
        /// <param name="signal"></param>
        /// <returns></returns>
        public static async UniTask<bool> LoadGame(string addressOrGuid, object worldData, bool seamless = false,
            Signal signal = default)
        {
            if (isGameRunning && !seamless)
            {
                return false;
            }
            OnGameIsLoading?.Invoke();
            var initializationResult = await InternalInitializeGame(addressOrGuid, seamless, signal);
            if (!initializationResult)
            {
                return false;
            }
            await LoadWorld(worldData);
            isGameLoaded = true;
            _StopGameSignal.TryRaise();
            _StopGameSignal = SignalSource.Create();
            OnGameLoaded?.Invoke();
            Service.EmitGameLoadedEvent();
            return true;
        }

        public static async UniTask LoadWorld(object data)
        {
            await virtualWorld.Load(data);
        }
        
        private static async UniTask<bool> InternalInitializeGame(string addressOrGuid, bool seamless, Signal cancellationSignal)
        {
            try
            {
                if (isGameRunning)
                {
                    if (!seamless)
                    {
                        return false;
                    }
                }
                else
                {
                    // Create new virtual world
                    m_ActivePlaceAddress.Value = addressOrGuid;
                    m_ObservableVirtualWorld.Value = new VirtualWorld();
                }
                isGameRunning = true;
                isGameLoaded = false;
                using var pending = PendingSystem.CreateCriticalPending("Start game");
                OnBeforeGameScenesLoad?.Invoke(seamless);
                
                if (!seamless)
                {
                    // ShowLoadingScreen();
                }
                
                // Show load game scene
                // Launch the game async
                using var summonChapter = ResourcesSystem.Summon<Place>(addressOrGuid);
                var chapter = await summonChapter.Wait(default);
            
                await ActivateScenes(chapter.baseScene, chapter.gameScene, seamless);
                var servicesReadyTasks = Service.Services.Values.Select(service => service.WhenReady(cancellationSignal)).ToArray();
                await UniTask.WhenAll(servicesReadyTasks);

                OnGameScenesActivated?.Invoke();
                return true;
            } catch (Exception e)
            {
                Console.Error("Launcher", e);
                return false;
            }
        }

        public static async UniTask<bool> StopGame()
        {
            try
            {
                if (!isGameRunning)
                {
                    return false;
                }
                m_ActivePlaceAddress.Value = null;
                _StopGameSignal.TryRaise();
                isGameLoaded = false;
                OnBeforeGameStop?.Invoke();
                /* Emit stop game event to all services */
                Service.EmitStopGameEvent();
                virtualWorld.Dispose();
                isGameRunning = false;
            
                // Unload scenes
                await UniTask.WhenAll(_CurrentBaseSceneSummon.DisposeAsync(), _CurrentGameSceneSummon.DisposeAsync());
            
                OnGameStopped?.Invoke();
                return true;
            } catch (Exception e)
            {
                Console.Error("GameSystem", e);
                FatalError(e);
                return false;
            }
        }
        
        public static DisposableAction WatchVirtualWorldAsync(Func<VirtualWorld, Signal, UniTaskVoid> action)
        {
            return m_ObservableVirtualWorld.WatchAsync(action);
        }
        
        public static UniTask WhenStart(Signal cancelSignal = default)
        {
            var tsc = SafeUniTaskCompletionSource.Create();
            DisposableAction unregister = default;
            Action OnResolve = default;
            OnResolve = () =>
            {
                unregister.Dispose();
                OnGameScenesActivated -= OnResolve;
                tsc.TrySetResult();
            };
            OnGameScenesActivated += OnResolve;
            
            unregister = cancelSignal.Register(OnCancel);
            return tsc.Task;

            void OnCancel()
            {
                unregister.Dispose();
                OnGameScenesActivated -= OnResolve;
                tsc.TrySetCanceled();
            }

            return tsc.Task;
        }
        
        public static async UniTask ActivateScenes(AssetReference baseScene, AssetReference gameScene = null, bool trySeamless = false)
        {
            try
            {
                if (!trySeamless || _CurrentBaseSceneRef != baseScene)
                {
                    // Unload previous base scene
                    _CurrentBaseSceneInstance = default;
                    await _CurrentBaseSceneSummon.DisposeAsync();
                }
                
                if (!trySeamless || (_CurrentGameSceneRef != null && _CurrentGameSceneRef != gameScene))
                {
                    _CurrentGameSceneInstance = default;
                    await _CurrentGameSceneSummon.DisposeAsync();
                }
                _CurrentGameSceneRef = gameScene;
                _CurrentBaseSceneRef = baseScene;
            
                // Load base scene
                _CurrentBaseSceneSummon = ResourcesSystem.LoadScene(_CurrentBaseSceneRef, LoadSceneMode.Additive);
            
                // Load game scene
                _CurrentBaseSceneSummon = ResourcesSystem.LoadScene(_CurrentGameSceneRef, LoadSceneMode.Additive);
            
                var (baseSceneInstance, gameSceneInstance) = await UniTask.WhenAll(_CurrentBaseSceneSummon.ToUniTask(), _CurrentGameSceneSummon.ToUniTask());
                _CurrentBaseSceneInstance = baseSceneInstance;
                _CurrentGameSceneInstance = gameSceneInstance;
                
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