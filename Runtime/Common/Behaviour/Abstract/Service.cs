using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public abstract class Service: AsyncBehaviour
    {
        private static readonly Dictionary<Type, Service> _Services = new ();
        private static event Action<Service> _HandleServiceAdded;
        private static event Action<Service> _HandleServiceRemoved;
        private static bool _IsReady;
        private event Action OnReady;
        
        #region Fields
        [Tooltip("If true, service will be initialized only on demand")]
        [SerializeField] private bool m_InitializeOnDemand;
        #endregion
        
        protected async UniTask InternalServiceReady()
        {
            _IsReady = true;
            OnReady?.Invoke();
            await componentDisableCancellationToken;
        }
        public UniTask WhenReady(CancellationToken cancellationToken = default)
        {
            var tcs = new UniTaskCompletionSource();
            IDisposable readyListener = null;
            var cancellationTokenRegistration = cancellationToken.Register(() =>
            {
                readyListener?.Dispose();
                tcs.TrySetCanceled();
            });
            
            readyListener = SubscribeReady(() =>
            {
                readyListener?.Dispose();
                cancellationTokenRegistration.Dispose();
                tcs.TrySetResult();
            });

            return tcs.Task;
        }
        
        public DisposableAction SubscribeReady(Action action)
        {
            if (_IsReady)
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
        
        public UniTask Demand(CancellationToken cancellationToken)
        {
            Demand();
            return WhenReady(cancellationToken);
        }

        private bool m_Demanded;
        private UniTaskCompletionSource m_DemandCompletionSource;
        protected sealed override async void RunAsync(CancellationToken cancellationToken)
        {
            var type = this.GetType();
            if (_Services.ContainsKey(type))
            {
                throw new InvalidOperationException($"Service {type} already added");
            }
            _Services.Add(type, this);
            _HandleServiceAdded?.Invoke(this);
            cancellationToken.Register(() =>
            {
                m_DemandCompletionSource?.TrySetCanceled();
                m_DemandCompletionSource = null;
                _Services.Remove(this.GetType());
                _HandleServiceRemoved?.Invoke(this);
            });
            try
            {
                if (m_InitializeOnDemand)
                {
                    m_DemandCompletionSource = m_DemandCompletionSource ?? new UniTaskCompletionSource();
                    await m_DemandCompletionSource.Task;
                }

                await RunService(InternalServiceReady, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"Service {type} was canceled.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Service {type} failed to run: {e}");
            }
        }

        public static async UniTask<T> Resolve<T>(CancellationToken cancellationToken = default, float timeout = 60f)
            where T : Service
        {
            var service = await ResolveInternal<T>(cancellationToken, timeout);
            await service.Demand(cancellationToken);
            return service;
        }
        
        /// <summary>
        /// Resolve service by type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static UniTask<T> ResolveInternal<T>(CancellationToken cancellationToken = default, float timeout = 60f) where T : Service
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
                    if (cancellationToken.IsCancellationRequested)
                    {
                        tcs.TrySetCanceled();
                        return;
                    }
                    _HandleServiceAdded -= handler;
                    tcs.TrySetResult((T) s);
                }
            };
            _HandleServiceAdded += handler;
            var cancellationTokenRegistration = cancellationToken.Register(() =>
            {
                _HandleServiceAdded -= handler;
                tcs.TrySetCanceled();
            });
            
            if (timeout > 0f)
            {
                UniTask.Delay(TimeSpan.FromSeconds(timeout), cancellationToken: cancellationToken).ContinueWith(() =>
                {
                    if (!task.Status.IsCompleted())
                    {
                        _HandleServiceAdded -= handler;
                        cancellationTokenRegistration.Dispose();
                        tcs.TrySetException(new TimeoutException("Operation timed out."));
                    }
                }).Forget();
            }
            
            return tcs.Task;
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
            _HandleServiceAdded += handler;
            return new DisposableAction(() => _HandleServiceAdded -= handler);
        }
        
        protected delegate UniTask Ready();
        private bool m_InitializedOnDemand;

        protected abstract UniTask RunService(Ready ready, CancellationToken cancellationToken);
    }
}