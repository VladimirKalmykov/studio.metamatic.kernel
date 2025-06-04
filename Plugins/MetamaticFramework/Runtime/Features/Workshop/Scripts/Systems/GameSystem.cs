using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
        public enum GameLoadingType
        {
            Breaking,
            Seamless
        }
        public const string LimbPlaceAddress = "LIMB";
        public const string SystemPlaceAddress = "SYSTEM";
        
        #region Constants
        public const string ConfigSkipGameIntroKeyName = "Metamatic_ConfigSkipGameIntro";
        public const string ConfigSkipMainMenuOnStartKeyName = "Metamatic_ConfigSkipMainMenuOnStart";
        public const string ConfigForceStartChapterGuid = "Metamatic_ConfigForceStartChapterGuid";
        public const string ConfigForceStartCheckpointId = "Metamatic_ConfigForceStartCheckpointId";
        #endregion
        public enum GameEvent
        {
            IntroSceneComplete,
        }
        
        #region Public API

        public static VirtualWorld virtualWorld => VirtualWorld.Current;
        public static Observable<GameAppStage> m_ObservableAppStage = new Observable<GameAppStage>(GameAppStage.Stopped);
        public static Observable<string> m_ActivePlaceAddress = new Observable<string>();
        public static bool isGameRunning { get; private set; }
        public static bool isGameLoaded { get; private set; }
        public static GameLoadingType loadingType { get; private set; }
        public static event Action<bool> OnBeforeLoadGameScenes;
        public static event Action OnGameScenesActivated;
        /// <summary>
        /// Event is raised when the game is ready to start (scenes are loaded).
        /// </summary>
        public static event Action OnGameReady;
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

        private static SignalSource _StopGameSignalSource;
        private static SceneInstance _CurrentBaseSceneInstance;
        private static SceneInstance _CurrentGameSceneInstance;
        public static event Action<Exception> OnFatalError;
        
        public static string activePlaceAddress => m_ActivePlaceAddress.Value;
        
        private static SignalSource m_StopGameSignalSource = default;

        static GameSystem()
        {
            
        }
        
        public static bool IsPlaceActive(string placeAddress)
        {
            return m_ActivePlaceAddress.Value == placeAddress;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void HandleExceptionsFrom(UniTask task)
        {
            InternalHandleExceptions(task).Forget();
        }
        
        private static async UniTaskVoid InternalHandleExceptions(UniTask task)
        {
            try
            {
                await task;
            }
            catch (Exception e)
            {
                Console.Error("GameSystem", e);
                FatalError(e);
            }
        }

        /// <summary>
        /// Returns the address of the place where the game object is located.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public static string GetCurrentPlaceAddress()
        {
            return m_ActivePlaceAddress.Value ?? LimbPlaceAddress;
        }
        
        public static DisposableAction WatchPlaceActivationAsync(Func<string, Signal, UniTaskVoid> action)
        {
            return m_ActivePlaceAddress.WatchAsync(action);
        }

        public static void FatalError(Exception error)
        {
            Console.Error("GameSystem", error);
            OnFatalError?.Invoke(error); // TOOD: And what then?
        }
        
        public static void FatalError(string message)
        {
            var error = new Exception(message);
            Console.Error("GameSystem", error);
            OnFatalError?.Invoke(error); // TOOD: And what then?
            TryStopGame().Forget();
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
                m_StopSignalSource.Dispose();
            });
        }
        
        public static DisposableAction WatchGameAppStageAsync(Func<GameAppStage, Signal, UniTaskVoid> action)
        {
            return m_ObservableAppStage.WatchAsync(action);
        }

        /// <summary>
        /// Start game from the scratch.
        /// </summary>
        /// <param name="placeAddress"></param>
        /// <param name="seamless"></param>
        /// <param name="cancellationSignal"></param>
        /// <returns></returns>
        public static async UniTask<bool> LaunchGame(string placeAddress, bool seamless)
        {
            if (isGameRunning && !seamless)
            {
                return false;
            }
            m_ObservableAppStage.uniqValue = GameAppStage.Loading;
            try
            {
                _StopGameSignalSource.TryRaise();
                _StopGameSignalSource = SignalSource.Create();
                OnGameIsLoading?.Invoke();
                var initializationResult = await InternalInitializeGame(placeAddress, seamless, _StopGameSignalSource.Signal);
                if (!initializationResult)
                {
                    return false;
                }
                isGameLoaded = true;
                
                
                OnGameLoaded?.Invoke();
                await Service.InternalPerformPlaceLoadedStageAsync(placeAddress, _StopGameSignalSource.Signal);
                m_ObservableAppStage.uniqValue = GameAppStage.Running;
            } catch (Exception e)
            {
                Console.Error("GameSystem", e);
                FatalError(e);
                m_ObservableAppStage.uniqValue = GameAppStage.Error;
                return false;
            }
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
        public static async UniTask<bool> LoadGame(string addressOrGuid, object worldData, bool seamless = false)
        {
            try
            {
                if (isGameRunning && !seamless)
                {
                    return false;
                }
                // Re-create stop game signal source
                _StopGameSignalSource.TryRaise();
                _StopGameSignalSource = SignalSource.Create();
                OnGameIsLoading?.Invoke();
                m_ObservableAppStage.uniqValue = GameAppStage.Loading;
                var initializationResult = await InternalInitializeGame(addressOrGuid, seamless, _StopGameSignalSource.Signal);
                if (!initializationResult)
                {
                    return false;
                }
                await LoadWorld(worldData);
                isGameLoaded = true;
                
                OnGameLoaded?.Invoke();
                await Service.InternalPerformPlaceLoadedStageAsync(addressOrGuid, _StopGameSignalSource.Signal);
                m_ObservableAppStage.uniqValue = GameAppStage.Running;
                return true;
            } catch (Exception e)
            {
                Console.Error("GameSystem", e);
                FatalError(e);
                m_ObservableAppStage.uniqValue = GameAppStage.Error;
                return false;
            }
        }

        public static async UniTask LoadWorld(object data)
        {
            await virtualWorld.LoadFromWorld(data);
        }
        
        private static async UniTask<bool> InternalInitializeGame(string addressOrGuid, bool seamless, Signal cancellationSignal)
        {
            try
            {
                if (!seamless)
                {
                    // Re-create virtual world if not seamless
                    VirtualWorld.CreateNewCurrent();
                }
                m_ActivePlaceAddress.Value = null;
                isGameRunning = true;
                isGameLoaded = false;
                using var pending = PendingSystem.CreateCriticalPending("Initialize place scenes");
                OnBeforeLoadGameScenes?.Invoke(seamless);
                
                // Show load game scene
                // Launch the game async
                using var summonChapter = ResourcesSystem.Summon<Place>(addressOrGuid);
                var chapter = await summonChapter.Wait(default);
                
                // Activate scenes
                if (!seamless || _CurrentBaseSceneRef?.AssetGUID != chapter.baseScene.AssetGUID)
                {
                    _CurrentBaseSceneInstance = default;
                    // Unload previous base scene
                    await _CurrentBaseSceneSummon.DisposeAsync();
                    _CurrentBaseSceneRef = chapter.baseScene;
                    // Load base scene
                    _CurrentBaseSceneSummon = ResourcesSystem.LoadScene(_CurrentBaseSceneRef, LoadSceneMode.Single);
                }
                
                if (!seamless || (_CurrentGameSceneRef?.AssetGUID != chapter.gameScene.AssetGUID))
                {
                    _CurrentGameSceneInstance = default;
                    await _CurrentGameSceneSummon.DisposeAsync();
                    _CurrentGameSceneRef = chapter.gameScene;
                    // Load game scene
                    _CurrentGameSceneSummon = ResourcesSystem.LoadScene(_CurrentGameSceneRef, LoadSceneMode.Additive);
                }
            
                var (baseSceneInstance, gameSceneInstance) = await UniTask.WhenAll(_CurrentBaseSceneSummon.ToUniTask(), _CurrentGameSceneSummon.ToUniTask());
                _CurrentBaseSceneInstance = baseSceneInstance;
                _CurrentGameSceneInstance = gameSceneInstance;
                
                m_ActivePlaceAddress.Value = addressOrGuid;
                
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

        public static async UniTask<bool> TryStopGame()
        {
            try
            {
                if (!isGameRunning)
                {
                    return false;
                }

                // Pause game
                Time.timeScale = 0;

                isGameRunning = false;
                m_ObservableAppStage.uniqValue = GameAppStage.Unloading;

                _StopGameSignalSource.TryRaise();
                isGameLoaded = false;
                OnBeforeGameStop?.Invoke();
                /* Emit stop game event to all services */
                Service.EmitStopGameEvent();
                VirtualWorld.DisposeCurrent();
                m_ActivePlaceAddress.Value = null;
                await UniTask.Yield();

                Console.Debug("UnloadGame", "Ensure that all spawned object are dismissed");

                // Unload scenes
                await UniTask.WhenAll(_CurrentBaseSceneSummon.DisposeAsync(), _CurrentGameSceneSummon.DisposeAsync());

                Console.Debug("UnloadGame", "Ensure that all scenes are unloaded");

                OnGameStopped?.Invoke();
                m_ObservableAppStage.uniqValue = GameAppStage.Stopped;
                return true;
            }
            catch (Exception e)
            {
                // Unload all scenes
                await _CurrentBaseSceneSummon.DisposeAsync();
                await _CurrentGameSceneSummon.DisposeAsync();
                Console.Error("GameSystem", e);
                FatalError(e);
                m_ObservableAppStage.uniqValue = GameAppStage.Error;

                return false;
            }
            finally
            {
                Time.timeScale = 1;
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DisposableAction WatchVirtualWorldAsync(Func<VirtualWorld, Signal, UniTaskVoid> action)
        {
            return VirtualWorld.WatchVirtualWorldAsync(action);
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
        #endregion
        
    }
}