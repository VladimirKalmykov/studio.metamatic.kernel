using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace Metamatic.Actions
{
    public class Dispatcher
    {
        #region Global
        private static DispatcherComponent _globalDispatcher;

        public static Dispatcher global
        {
            get => (_globalDispatcher ??= new GameObject("__GLOBALDISPATCHER__").AddComponent<DispatcherComponent>()).dispatcher;
        }
        /// <summary>
        /// Find dispatcher in parent components
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public Dispatcher FindInParent(GameObject gameObject)
        {
            var dc = gameObject.GetComponentInParent<DispatcherComponent>();
            return dc != null ? dc.dispatcher : null;
        }
        #endregion
        #region Properties
        private struct Announcement<T> where T : notnull
        {
            public T source;
            public Announcement(T source)
            {
                this.source = source;
            }
        }
        private struct Retraction<T> where T : notnull
        {
            public T source;
            public Retraction(T source)
            {
                this.source = source;
            }
        }
       
        private readonly Dictionary<Type, List<Delegate>> m_EventListeners = new();
        private readonly Dictionary<Type, List<object>> m_Announcements = new();
        private readonly Dictionary<Type, Dictionary<Type, Dictionary<object, RegistrySource<object>>>> m_RegistryTypesKeys = new();
        private readonly Dictionary<(object id, Type type), (object value, bool isActive, List<IDispatcher.EventHandler<object>> handlers1, List<IDispatcher.EventHandler<object>> handlers2)> m_Registry = new();
        private bool m_IsRegistrationProcessing = false;
        private DeferLock m_RegistrationLock = new();
        #endregion
        
        #region Events
        public void Emit<T>(T source) where T : notnull
        {
            Type eventType = typeof(T);
            List<Delegate> handlers;

            if (m_EventListeners.TryGetValue(eventType, out handlers))
            {
                var safeHandlers = handlers.ToArray();
                foreach (var handler in safeHandlers)
                {
                    ((IDispatcher.EventHandler<T>)handler).Invoke(source);
                }
            }
        }

        public DisposableAction Subscribe<T>(IDispatcher.EventHandler<T> handler) where T : notnull
        {
            Type eventType = typeof(T);
            List<Delegate> handlers;
            if (!m_EventListeners.TryGetValue(eventType, out handlers))
            {
                handlers = new List<Delegate>();
                m_EventListeners[eventType] = handlers;
            }

            handlers.Add(handler);

            return new DisposableAction(() => Unsubscribe(handler));
        }

        public void Unsubscribe<T>(IDispatcher.EventHandler<T> handler) where T : notnull
        {
            Type eventType = typeof(T);
            List<Delegate> handlers;
            if (m_EventListeners.TryGetValue(eventType, out handlers))
            {
                handlers.Remove(handler);
                if (handlers.Count == 0)
                {
                    m_EventListeners.Remove(eventType);
                }
            }
        }

        public UniTask<T> WhenEmitted<T>(IDispatcher.Condition<T> condition, CancellationToken cancellationToken) where T : notnull
        {
            var tcs = new UniTaskCompletionSource<T>();
            UniTask<T> task = tcs.Task;
            System.Action releaseSubscription = null;
            CancellationTokenRegistration cancellationTokenRegistration = cancellationToken.Register(() =>
            {
                releaseSubscription?.Invoke();
                tcs.TrySetCanceled(cancellationToken);
            });
            releaseSubscription = Subscribe<T>(source =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                if (condition(source))
                {
                    releaseSubscription?.Invoke();
                    cancellationTokenRegistration.Dispose();
                    tcs.TrySetResult(source);
                }
            });
            return task;
        }
        #endregion
        #region Announcements
        public DisposableAction Announce<T>(T source) where T : notnull
        {
            if (!m_Announcements.TryGetValue(typeof(T), out var announcements))
            {
                announcements = new List<object>();
                m_Announcements[typeof(T)] = announcements;
            }
            announcements.Add(source);
            Emit(new Announcement<T>(source));
            return new DisposableAction(() => Retract(source));
        }

        public void Retract<T>(T source) where T : notnull
        {
            if (m_Announcements.TryGetValue(typeof(T), out var announcements))
            {
                announcements.Remove(source);
                if (announcements.Count == 0)
                {
                    m_Announcements.Remove(typeof(T));
                }
                Emit(new Retraction<T>(source));
            }
        }

        public IReadOnlyList<T> GetAnnounced<T>() where T : notnull
        {
            if (m_Announcements.TryGetValue(typeof(T), out var announcements))
            {
                return announcements.Cast<T>().ToList();
            }
            return new List<T>();
        }

        public UniTask<T> WhenAnnounce<T>(IDispatcher.Condition<T> condition, CancellationToken cancellationToken) where T : notnull
        {
            var tcs = new UniTaskCompletionSource<T>();
            UniTask<T> task = tcs.Task;
            System.Action releaseSubscription = null;
            CancellationTokenRegistration cancellationTokenRegistration = cancellationToken.Register(() =>
            {
                releaseSubscription?.Invoke();
                tcs.TrySetCanceled(cancellationToken);
            });
            releaseSubscription = SubscribeAnnouncements<T>(sources =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                foreach (var source in sources)
                {
                    if (condition(source))
                    {
                        releaseSubscription?.Invoke();
                        cancellationTokenRegistration.Dispose();
                        tcs.TrySetResult(source);
                        return;
                    }
                }
            });
            return task;
        }

        public DisposableAction SubscribeAnnouncements<T>(IDispatcher.AnnouncementsHandler<T> handler) where T : notnull
        {
            handler(GetAnnounced<T>().ToArray());
            return Subscribe<Announcement<T>>(announcement =>
            {
                handler(new []{announcement.source});
            });
        }

        public DisposableAction SubscribeRetractions<T>(IDispatcher.RetractionsHandler<T> handler) where T : notnull
        {
            return Subscribe<Retraction<T>>(retraction =>
            {
                handler(retraction.source);
            });
        }

        #endregion
        #region Registry
        public IDisposable Register<TValue>(object id, TValue source) where TValue : notnull
        {
            using (m_RegistrationLock.Lock())
            {
                var valueType = typeof(TValue);
                if (!m_Registry.TryGetValue((id, valueType), out var registry))
                {
                    registry = (default, false, new List<IDispatcher.EventHandler<object>>(),
                        new List<IDispatcher.EventHandler<object>>());
                }

                registry.value = source;
                registry.isActive = true;
                (registry.handlers1, registry.handlers2) = (registry.handlers2, registry.handlers1);
                registry.handlers1.Clear();
                registry.handlers1.AddRange(registry.handlers2);
                foreach (var handler in registry.handlers2)
                {
                    handler(source);
                }

                m_Registry[(id, valueType)] = registry;
                
                return new DisposableCallback(() =>
                {
                    if (m_Registry.TryGetValue((id, valueType), out var reg))
                    {
                        reg.value = default;
                        reg.isActive = false;
                        if (reg.handlers1.Count == 0)
                        {
                            m_Registry.Remove((id, valueType));
                        }
                        else
                        {
                            m_Registry[(id, valueType)] = reg;
                        }
                    }
#if DEBUG
                    else
                    {
                        throw new System.Exception("Registry key not found: " + id);
                    }
#endif
                });
            }
        }

        public IDisposable Register<TValue>(TValue source) where TValue : notnull
        {
            return Register(this, source);
        }

        public DisposableAction SubscribeRegistry<TValue>(object id, IDispatcher.EventHandler<TValue> handler, bool skipInitial = false)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            OnRegisteredInternal(id, handler, skipInitial, cancellationTokenSource.Token).Forget();

            return new DisposableAction(() =>
            {
                cancellationTokenSource.Cancel();
            });
        }

        private async UniTaskVoid OnRegisteredInternal<TValue>(object id, IDispatcher.EventHandler<TValue> handler,
            bool skipInitial = false, CancellationToken cancellationToken = default)
        {
            await m_RegistrationLock;
            if (cancellationToken.IsCancellationRequested) return;
            try
            {
                var valueType = typeof(TValue);
                if (!m_Registry.TryGetValue((id, valueType), out var registry))
                {
                    registry = (default, false, new List<IDispatcher.EventHandler<object>>(),
                        new List<IDispatcher.EventHandler<object>>());
                    m_Registry[(id, valueType)] = registry;
                }

                IDispatcher.EventHandler<object> handlerWrapper = (source) =>
                {
                    handler(source == null ? default : (TValue)source);
                };

                if (!skipInitial)
                {
                    handler(registry.value == null ? default : (TValue)registry.value);
                }

                registry.handlers1.Add(handlerWrapper);

                cancellationToken.Register(() =>
                {
                    if (m_Registry.TryGetValue((id, valueType), out var reg))
                    {
                        reg.handlers1.Remove(handlerWrapper);
                        if (!reg.isActive && reg.handlers1.Count == 0)
                        {
                            m_Registry.Remove((id, valueType));
                        }
                    }
#if DEBUG
                    else
                    {
                        throw new System.Exception("Registry key not found: " + id);
                    }
#endif
                });
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
        
        public int GetRegistrySize()
        {
            return m_Registry.Count;
        }

        public DisposableAction SubscribeRegistry<TValue>(IDispatcher.EventHandler<TValue> handler, bool skipInitial = false) where TValue : notnull
        {
            return SubscribeRegistry(this, handler, skipInitial);
        }
        
        /// <summary>
        /// Wait for registry for specific key and type to be changed
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <returns></returns>
        public UniTask<TValue> WhenRegistryChange<TKey, TValue>(TKey id, CancellationToken cancellationToken = default) where TValue : notnull
        {
            var tcs = new UniTaskCompletionSource<TValue>();
            UniTask<TValue> task = tcs.Task;
            System.Action releaseSubscription = null;
            CancellationTokenRegistration cancellationTokenRegistration = cancellationToken.Register(() =>
            {
                releaseSubscription?.Invoke();
                tcs.TrySetCanceled(cancellationToken);
            });
            releaseSubscription = SubscribeRegistry<TValue>(id, source =>
            {
                releaseSubscription?.Invoke();
                cancellationTokenRegistration.Dispose();
                tcs.TrySetResult(source);
            }, true);
            return task;
        }
        
        /// <summary>
        /// Wait for registry for specific key and type to be registered with not null value
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <returns></returns>
        public UniTask<TValue> WhenRegistered<TKey, TValue>(TKey id, CancellationToken cancellationToken = default, float timeout = 0f) where TValue : notnull
        {
            var tcs = new UniTaskCompletionSource<TValue>();
            UniTask<TValue> task = tcs.Task;
            System.Action releaseSubscription = null;
            CancellationTokenSource timeoutCancellationTokenSource = null;
            CancellationTokenRegistration cancellationTokenRegistration = cancellationToken.Register(() =>
            {
                timeoutCancellationTokenSource?.Cancel();
                releaseSubscription?.Invoke();
                tcs.TrySetCanceled(cancellationToken);
            });

            releaseSubscription = SubscribeRegistry<TValue>(id, source =>
            {
                timeoutCancellationTokenSource?.Cancel();
                if (cancellationToken.IsCancellationRequested || source == null)
                {
                    return;
                }
                releaseSubscription?.Invoke();
                cancellationTokenRegistration.Dispose();
                tcs.TrySetResult(source);
            });

            // Добавляем таймаут
            if (timeout > 0f)
            {
                UniTask.Delay(TimeSpan.FromSeconds(timeout), cancellationToken: cancellationToken).ContinueWith(() =>
                {
                    if (!task.Status.IsCompleted())
                    {
                        releaseSubscription?.Invoke();
                        cancellationTokenRegistration.Dispose();
                        tcs.TrySetException(new TimeoutException("Operation timed out."));
                    }
                }).Forget();
            }

            return task;
        }

        public UniTask<TValue> WhenRegistered<TValue>(CancellationToken cancellationToken = default, float timeout = 0f) where TValue : notnull
        {
            return WhenRegistered<Dispatcher, TValue>(this, cancellationToken, timeout);
        }

        public UniTask<TValue> WhenRegistryChange<TValue>(CancellationToken cancellationToken = default) where TValue : notnull
        {
            return WhenRegistryChange<Dispatcher, TValue>(this, cancellationToken);
        }
        
        public TValue GetRegisteredObject<TKey, TValue>(TKey id) where TValue : notnull
        {
            var valueType = typeof(TValue);
            if (m_Registry.TryGetValue((id, valueType), out var registry))
            {
                return (TValue)registry.Item1;
            }
            
            return default;
        }
        
        public T GetRegisteredObject<T>() where T : notnull
        {
            return GetRegisteredObject<Dispatcher, T>(this);
        }
        
        public bool HasRegistry<TKey, TValue>(TKey id) where TValue : notnull
        {
            var valueType = typeof(TValue);
            
            if (m_Registry.TryGetValue((id, valueType), out var registry))
            {
                return registry.Item1 != null;
            }
            
            return false;
        }
        
        public bool HasRegistry<T>() where T : notnull
        {
            return HasRegistry<Dispatcher, T>(this);
        }

        #endregion
    }
}