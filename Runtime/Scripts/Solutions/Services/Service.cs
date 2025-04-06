using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DevTools;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metamatic
{
    public abstract class Service: MonoBehaviour
    {
        #region Static Members
        private static readonly Dictionary<Type, Service> AwakedServices = new ();
        private static readonly Dictionary<Type, Service> EnabledServices = new ();
        private static readonly Dictionary<Type, List<Action<Service, bool>>> ServiceObservers = new ();
        
        // ReSharper disable once InconsistentNaming
        public static IReadOnlyDictionary<Type, Service> Services => EnabledServices;
        
        private static event Action<Type, Service> OnServiceRegistered; 
        private static event Action<Type, Service> OnServiceUnregistered;
        private static event Action<Service> ServiceAddedEvent;
        private static event Action<Service> ServiceRemovedEvent;
        public static event Action<Type, Exception> OnErrorInService;

        public static async UniTask<T> Resolve<T>(Signal signal = default, float timeout = 10f)
            where T : Service
        {
            #if DEBUG
            using var stagnationCheck = StagnationCatcher.Create(50f, () =>
            {
                Console.Warning("Stagnation", "Service", $"Service {typeof(T)} resolving is stuck");
            });
            #endif
            var service = await ResolveInternal<T>(signal, timeout);
            await service.Demand(signal);
            return service;
        }

        private static UniTask<T> ResolveInternal<T>(Signal signal = default, float timeout = 10f) where T : Service
        {
            if (EnabledServices.TryGetValue(typeof(T), out var service))
            {
                return UniTask.FromResult((T) service);
            }
            var tcs = new UniTaskCompletionSource<T>();
            var task = tcs.Task;
            Action<Service> handler = null;
            handler = s =>
            {
                if (s.GetType() == typeof(T))
                {
                    if (signal.IsCancellationRequested)
                    {
                        tcs.TrySetCanceled();
                        return;
                    }
                    ServiceAddedEvent -= handler;
                    tcs.TrySetResult((T) s);
                }
            };
            ServiceAddedEvent += handler;
            var cancellationTokenRegistration = signal.Register(() =>
            {
                ServiceAddedEvent -= handler;
                tcs.TrySetCanceled();
            });
            
            if (timeout > 0f)
            {
                return DelayTimeout(timeout, signal, task, tcs, handler, cancellationTokenRegistration);
            }
            
            return tcs.Task;
        }

        private static SignalSource _StopGameSignal;
        
        /// <summary>
        /// This method is called when game is completely load its data. At this stage all services can spawn their entities.
        /// </summary>
        public static async UniTask InternalPerformPlaceLoadedStageAsync(string placeAddress, Signal stopOrCancelSignalSource)
        {
            var tasksList = ParamLessPool<List<UniTask>>.GetShared();
            try
            {
                _StopGameSignal = SignalSource.Create();
                stopOrCancelSignalSource.Register(_StopGameSignal.TryRaise);

                foreach (var service in AwakedServices)
                {
                    try
                    {
                        #if DEBUG
                        tasksList.Add(CarryOnServiceLoadStageReady(service.Value, placeAddress, _StopGameSignal.Signal));
                        #else
                        tasksList.Add(new AsyncWorkWithParam<string>(service.Value.OnGameLoaded, placeAddress, m_StopGameSignal.Signal).Task);
                        #endif
                    }
                    catch (Exception e)
                    {
                        Console.Error("Service", "EmitGameLoadedEvent", e);
                    }
                }
                
                await UniTask.WhenAll(tasksList);
            }
            catch (OperationCanceledException)
            {
                Console.Warning("Service", "EmitGameLoadedEvent", "Game loading was canceled");
            }
            catch (Exception e)
            {
                Console.Error("Service", "EmitGameLoadedEvent", e);
            }
            finally
            {
                ParamLessPool<List<UniTask>>.ReturnShared(tasksList);
            }
        }

        private static async UniTask CarryOnServiceLoadStageReady(Service service, string placeAddress, Signal cancellationSignal)
        {
            using var serviceLoadPending = PendingSystem.CreateCriticalPending($"Service {service.name} load");
            try
            {
                var asyncWork = new AsyncWorkWithParam<string>(service.OnPlaceLoaded, placeAddress, cancellationSignal);
                await asyncWork;
            } catch (OperationCanceledException)
            {
                // Skip
            }
            catch (Exception e)
            {
                Console.Error("Service", "CarryOnServiceLoadStageReady", e);
            }
        }

        public static void EmitStopGameEvent()
        {
            _StopGameSignal.TryRaise();
            foreach (var service in AwakedServices)
            {
                service.Value.OnGameStop();
            }
        }
        
        private static async UniTask<T> DelayTimeout<T>(float timeout, Signal signal, UniTask<T> task, UniTaskCompletionSource<T> tcs, Action<Service> handler, DisposableAction cancellationTokenRegistration)
        {
            var delayTask = UniTask.Delay(TimeSpan.FromSeconds(timeout), cancellationToken: signal);
            var result = await UniTask.WhenAny(task, delayTask);
            if (result.hasResultLeft)
            {
                return result.result;
            }
            tcs.TrySetException(new TimeoutException($"Resolving service {typeof(T)} timed out"));
            throw new TimeoutException($"Resolving service {typeof(T)} timed out");
        }

        public static DisposableAction SubscribeService<T>(Action<T> service) where T : Service
        {
            Action<Service> handler = null;
            handler = s =>
            {
                if (s.GetType() == typeof(T))
                {
                    service((T)s);
                }
            };
            ServiceAddedEvent += handler;
            return new DisposableAction(() => ServiceAddedEvent -= handler);
        }
        
        public static DisposableAction SubscribeService(Type serviceType, Action<Service> service)
        {
            Action<Service> handler = null;
            handler = s =>
            {
                if (s.GetType() == serviceType)
                {
                    service(s);
                }
            };
            ServiceAddedEvent += handler;
            return new DisposableAction(() => ServiceAddedEvent -= handler);
        }
        
        public static DisposableAction WatchServices(Action<Service> service)
        {
            /* List already added services */
            foreach (var servicePair in EnabledServices)
            {
                service(servicePair.Value);
            }
            ServiceAddedEvent += service;
            return new DisposableAction(() => ServiceAddedEvent -= service);
        }

        public static DisposableAction WatchServiceAsync(Type serviceType, Func<Service, Signal, UniTaskVoid> handler)
        {
            SignalSource cancellationSignalSource = default;
            
            /* Get existing service */
            if (EnabledServices.TryGetValue(serviceType, out var service))
            {
                cancellationSignalSource = SignalSource.Create();
                handler(service, cancellationSignalSource.Signal).Forget();
            }

            if (!ServiceObservers.TryGetValue(serviceType, out var observers))
            {
                observers = new List<Action<Service, bool>>();
                ServiceObservers.Add(serviceType, observers);
            }
            
            observers.Add(OnServiceChanged);
            
            return DisposableAction.Create(() =>
            {
                observers.Remove(OnServiceChanged);
                cancellationSignalSource.TryRaise();
            });
            
            void OnServiceChanged(Service service, bool added)
            {
                if (added)
                {
                    OnServiceAdded(service);
                }
                else
                {
                    OnServiceRemoved(service);
                }
            }
            
            void OnServiceAdded(Service service)
            {
                if (service.GetType() != serviceType)
                {
                    return;
                }
                cancellationSignalSource = SignalSource.Create();
                handler(service, cancellationSignalSource.Signal).Forget();
            }
            void OnServiceRemoved(Service service)
            {
                if (service.GetType() != serviceType)
                {
                    return;
                }
                cancellationSignalSource.TryRaise();
            }
        }
        #endregion

        #region Instance Members
        private event Action OnValidateEvent;
        private event Action OnDrawGizmosEvent;
        private event Action OnReady;
        
        private SignalSource m_DisableSignalSource;
        private SignalSource m_DestroySignalSource;
        private bool m_IsReady;
        private bool m_Demanded;
        private bool m_InitializedOnDemand;
        private UniTaskCompletionSource m_DemandCompletionSource;
        private Pending m_Pending = Pending.Empty;
        
        public Signal disableSignal => m_DisableSignalSource.Signal;
        public Signal destroySignal => m_DestroySignalSource.Signal;
        
        #if UNITY_EDITOR
        [ShowIf("@m_MissedRequiredServices.Count > 0"), ShowInInspector, ReadOnly, InfoBox("Following required services are missed", InfoMessageType.Error), UsedImplicitly]
        [ListDrawerSettings(IsReadOnly = true, ShowFoldout = false)]
        private List<string> m_MissedRequiredServices = new();
        #endif
        
        [Tooltip("If true, service will be initialized only on demand")]
        [SerializeField] private bool m_InitializeOnDemand;

        #if DEBUG
        private Type[] m_RequiredServices;
        #endif
        
        protected delegate UniTask Ready();

        public DisposableAction SubscribeErrorInService(Action<Type, Exception> action)
        {
            OnErrorInService += action;
            return new DisposableAction(() => OnErrorInService -= action);
        }

        #if DEBUG
        private void OnRequiredServiceRegistered(Type type, Service service)
        {
            if (m_MissedRequiredServices.Contains(type.ToString()))
            {
                m_MissedRequiredServices.Remove(type.ToString());
            }
        }
        
        private void OnRequiredServiceUnregistered(Type type, Service service)
        {
            if (m_RequiredServices.Contains(type) && !m_MissedRequiredServices.Contains(type.ToString()))
            {
                m_MissedRequiredServices.Add(type.ToString());
            }
        }
        #endif
        
        protected virtual void OnEnable()
        {
            #if DEBUG
            #if UNITY_EDITOR
            m_MissedRequiredServices.Clear();
            #endif
            /* Validate are there all required by RequireServiceAttribute services have registered */
            var requiredServices = m_RequiredServices = RequireServiceAttribute.GetRequiredServices(this.GetType());
            /* Ensure that all required services are registered */
            foreach (var requiredService in requiredServices)
            {
                if (!AwakedServices.ContainsKey(requiredService))
                {
                    #if UNITY_EDITOR
                    m_MissedRequiredServices.Add(requiredService.ToString());
                    #endif
                    global::Metamatic.Console.Exception("Service", $"Service {requiredService} is required by {this.GetType()} but not registered");
                }
            }
            
            OnServiceRegistered += OnRequiredServiceRegistered;
            OnServiceUnregistered += OnRequiredServiceUnregistered;
            
            #endif

            #if UNITY_EDITOR
            if (Application.isPlaying)
            {
            #endif
                m_DisableSignalSource = SignalSource.Create();
                OnEnableAsync(disableSignal).Forget();
            #if UNITY_EDITOR
            }
            #endif
        }

        protected virtual void OnDisable()
        {
            #if DEBUG
            OnServiceUnregistered -= OnRequiredServiceUnregistered;
            OnServiceRegistered -= OnRequiredServiceRegistered;
            #endif
            #if UNITY_EDITOR
            if (Application.isPlaying)
            {
            #endif
            m_DisableSignalSource.TryRaise();
            #if UNITY_EDITOR
            }
            #endif
        }

        protected virtual void Awake()
        {
            m_DestroySignalSource = SignalSource.Create();
            var type = this.GetType();
            if (AwakedServices.ContainsKey(type))
            {
                throw new InvalidOperationException($"Service {type} already added");
            }
            Console.Log("Service", "Register", $"{type} by ${name}");
            AwakedServices.Add(type, this);
            OnServiceRegistered?.Invoke(type, this);
        }

        protected virtual void OnDestroy()
        {
            var type = this.GetType();
            AwakedServices.Remove(type);
            OnServiceUnregistered?.Invoke(type, this);
            #if UNITY_EDITOR
            if (Application.isPlaying)
            {
            #endif
            m_DestroySignalSource.TryRaise();
            #if UNITY_EDITOR
            }
            #endif
        }

        protected virtual UniTask OnPlaceLoaded(Action ready, string placeAddress, Signal stopGameSignal)
        {
            return UniTask.CompletedTask;
        }

        protected virtual void OnGameStop()
        {
            
        }

        #if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            OnValidateEvent?.Invoke();
        }

        protected virtual void OnDrawGizmos()
        {
            OnDrawGizmosEvent?.Invoke();
        }
        #endif

        private UniTask InternalServiceReady()
        {
            m_Pending.Dispose();
            m_IsReady = true;
            OnReady?.Invoke();
            return disableSignal.ToUniTask();
        }

        public UniTask WhenReady(Signal signal = default)
        {
            var tcs = SafeUniTaskCompletionSource.Create();
            IDisposable readyListener = null;
            var cancellationTokenRegistration = signal.Register(() =>
            {
                readyListener?.Dispose();
                tcs.TrySetCanceled();
            });
            
            readyListener = SubscribeReady(() =>
            {
                cancellationTokenRegistration.Dispose();
                readyListener?.Dispose();
                tcs.TrySetResult();
            });

            return tcs.Task;
        }
        
        public DisposableAction SubscribeReady(Action action)
        {
            if (m_IsReady)
            {
                action();
                return DisposableAction.Empty;
            }
            OnReady += action;
            return new DisposableAction(() => OnReady -= action);
        }
        
        public void Demand()
        {
            if (m_InitializeOnDemand && !m_Demanded)
            {
                m_Demanded = true;
                m_DemandCompletionSource = m_DemandCompletionSource ?? new UniTaskCompletionSource();
                m_DemandCompletionSource.TrySetResult();
            }
        }
        
        public UniTask Demand(Signal signal)
        {
            Demand();
            return WhenReady(signal);
        }

        protected async UniTaskVoid OnEnableAsync(Signal signal)
        {
            var type = this.GetType();
            if (EnabledServices.ContainsKey(type))
            {
                throw new InvalidOperationException($"Service {type} already added");
            }
            EnabledServices.Add(type, this);
            ServiceAddedEvent?.Invoke(this);
            
            var hasObservers = ServiceObservers.TryGetValue(type, out var observers);
            if (hasObservers)
            {
                foreach (var observer in observers)
                {
                    observer(this, true);
                }
            }
            signal.Register(() =>
            {
                m_DemandCompletionSource?.TrySetCanceled();
                m_DemandCompletionSource = null;
                EnabledServices.Remove(this.GetType());
                ServiceRemovedEvent?.Invoke(this);
                if (hasObservers)
                {
                    foreach (var observer in observers)
                    {
                        observer(this, false);
                    }
                }
            });
            try
            {
                if (m_InitializeOnDemand)
                {
                    m_DemandCompletionSource = m_DemandCompletionSource ?? new UniTaskCompletionSource();
                    await m_DemandCompletionSource.Task;
                }
                
                Debug.Log($"[Service] {type} is running");
                m_Pending = PendingSystem.CreatePending(name);
                await RunService(InternalServiceReady, signal);
            }
            catch (OperationCanceledException)
            {
                m_Pending.Dispose();
                Debug.Log($"Service {type} was canceled.");
            }
            catch (Exception e)
            {
                m_Pending.Dispose();
                OnErrorInService?.Invoke(type, e);
                #if METAMATIC_TERMINAL
                Terminal.LogError($"Service {type} failed to run: {e.Message}");
                #endif
                Debug.LogError($"Service {type} failed to run: {e}");
            }
        }
        
        protected DisposableAction SubscribeValidate(Action action)
        {
            OnValidateEvent += action;
            return new DisposableAction(() => OnValidateEvent -= action);
        }
        
        public DisposableAction SubscribeDrawGizmos(Action action)
        {
            OnDrawGizmosEvent += action;
            return new DisposableAction(() => OnDrawGizmosEvent -= action);
        }

        protected abstract UniTask RunService(Ready ready, Signal signal);
        #endregion
    }
}