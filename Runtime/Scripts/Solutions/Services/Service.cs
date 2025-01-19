using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DevTools;
using UnityEngine;

namespace Metamatic
{
    public abstract class Service: AsyncBehaviourBase
    {
        private static readonly Dictionary<Type, Service> _AwakedServices = new ();
        private static readonly Dictionary<Type, Service> _Services = new ();
        private static event Action<Service> HandleServiceAdded;
        private static event Action<Service> HandleServiceRemoved;
        
        public static event Action<Type, Exception> OnErrorInService;
        public DisposableAction SubscribeErrorInService(Action<Type, Exception> action)
        {
            OnErrorInService += action;
            return new DisposableAction(() => OnErrorInService -= action);
        }
        
        private event Action OnReady;
        private bool m_IsReady;
        
        #region Fields
        [Tooltip("If true, service will be initialized only on demand")]
        [SerializeField] private bool m_InitializeOnDemand;
        #endregion
        
        protected UniTask InternalServiceReady()
        {
            m_IsReady = true;
            OnReady?.Invoke();
            return componentDisableCancellationToken.ToUniTask();
        }
        public UniTask WhenReady(Signal signal = default)
        {
            var tcs = new UniTaskCompletionSource();
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
        
        /// <summary>
        /// Demand service initialization (only if InitializeOnDemand is true)
        /// </summary>
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

        private bool m_Demanded;
        private UniTaskCompletionSource m_DemandCompletionSource;
        protected sealed override async UniTaskVoid RunAsync(Signal signal)
        {
            var type = this.GetType();
            if (_Services.ContainsKey(type))
            {
                throw new InvalidOperationException($"Service {type} already added");
            }
            _Services.Add(type, this);
            HandleServiceAdded?.Invoke(this);
            signal.Register(() =>
            {
                m_DemandCompletionSource?.TrySetCanceled();
                m_DemandCompletionSource = null;
                _Services.Remove(this.GetType());
                HandleServiceRemoved?.Invoke(this);
            });
            try
            {
                if (m_InitializeOnDemand)
                {
                    m_DemandCompletionSource = m_DemandCompletionSource ?? new UniTaskCompletionSource();
                    await m_DemandCompletionSource.Task;
                }
                
                Debug.Log($"[Service] {type} is running");
                await RunService(InternalServiceReady, signal);
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"Service {type} was canceled.");
            }
            catch (Exception e)
            {
                OnErrorInService?.Invoke(type, e);
                #if METAMATIC_TERMINAL
                Terminal.LogError($"Service {type} failed to run: {e.Message}");
                #endif
                Debug.LogError($"Service {type} failed to run: {e}");
            }
        }
        
        public static async UniTask<T> Resolve<T>(Signal signal = default, float timeout = 10f)
            where T : Service
        {
            using var stagnationCheck = StagnationCatcher.Create(50f, () =>
            {
                Console.Warning("Stagnation", "Service", $"Service {typeof(T)} resolving is stuck");
            });
            var service = await ResolveInternal<T>(signal, timeout);
            await service.Demand(signal);
            return service;
        }
        
        /// <summary>
        /// Resolve service by type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static UniTask<T> ResolveInternal<T>(Signal signal = default, float timeout = 10f) where T : Service
        {
            if (_Services.TryGetValue(typeof(T), out var service))
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
                    HandleServiceAdded -= handler;
                    tcs.TrySetResult((T) s);
                }
            };
            HandleServiceAdded += handler;
            var cancellationTokenRegistration = signal.Register(() =>
            {
                HandleServiceAdded -= handler;
                tcs.TrySetCanceled();
            });
            
            if (timeout > 0f)
            {
                return DelayTimeout(timeout, signal, task, tcs, handler, cancellationTokenRegistration);
            }
            
            return tcs.Task;
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
            HandleServiceAdded += handler;
            return new DisposableAction(() => HandleServiceAdded -= handler);
        }
        
        protected delegate UniTask Ready();
        private bool m_InitializedOnDemand;

        protected abstract UniTask RunService(Ready ready, Signal signal);
        
        protected override async UniTaskVoid RunUntilDestroy(Signal signal)
        {
            #if DEBUG
            var type = this.GetType();
            if (_AwakedServices.ContainsKey(type))
            {
                throw new InvalidOperationException($"Service {type} already added");
            }
            _AwakedServices.Add(type, this);
            await signal;
            _AwakedServices.Remove(type);
            #endif
        }
    }
}