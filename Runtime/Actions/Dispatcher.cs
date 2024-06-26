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
            get
            {
                #if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    throw new System.Exception("Global dispatcher is not available in edit mode");
                }
                #endif
                return (_globalDispatcher ??=
                    new GameObject("__GLOBALDISPATCHER__").AddComponent<DispatcherComponent>()).dispatcher;
            }
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
       
        private readonly Dictionary<Type, LockedList<Delegate>> m_EventListeners = new();
        private readonly Dictionary<Type, List<object>> m_Announcements = new();
        private readonly Dictionary<(object id, Type type), (object value, bool isActive, LockedList<IDispatcher.EventHandler<object>> handlers)> m_Registry = new();
        private readonly Dictionary<object, (object value, bool isActive, LockedList<IDispatcher.EventHandler<object>> handlers)> m_RegistryLite = new();
        private bool m_IsRegistrationProcessing = false;
        private DeferLock m_RegistrationLock = new();
        #endregion
        
        #region Events
        public void Emit<T>(T source) where T : notnull
        {
            Type eventType = typeof(T);
            LockedList<Delegate> handlers;

            if (m_EventListeners.TryGetValue(eventType, out handlers))
            {
                using (handlers.Lock())
                {
                    foreach (var handler in handlers)
                    {
                        ((IDispatcher.EventHandler<T>)handler).Invoke(source);
                    }
                }
            }
        }

        public DisposableAction Subscribe<T>(IDispatcher.EventHandler<T> handler) where T : notnull
        {
            Type eventType = typeof(T);
            LockedList<Delegate> handlers;
            if (!m_EventListeners.TryGetValue(eventType, out handlers))
            {
                handlers = new LockedList<Delegate>();
                m_EventListeners[eventType] = handlers;
            }

            if (handlers.isLocked)
            {
                var cancellationTokenSource = new CancellationTokenSource();
                // Здесь мне нужно дождаться когда handlers будет освобождeн (await handlers) и только потом вносить в него изменения
                // handlers содержит GetAwaiter() и OnCompleted(Action continuation), так что я могу его ожидать
                // Но явно использовать тут await handlers не получится, так как это не async метод
                // Поэтому мне нужно каким то образом дождаться handlers, но и не превращать Subscribe в async метод
                Subscribe_Deferred(handlers, handler, cancellationTokenSource.Token);
                return new DisposableAction(() => cancellationTokenSource.Cancel());
            }
            handlers.Add(handler);

            return new DisposableAction(() => Unsubscribe(handler));
        }
        
        private async void Subscribe_Deferred<T>(LockedList<Delegate> handlers, IDispatcher.EventHandler<T> handler, CancellationToken token) where T : notnull
        {
            await handlers;
            if (token.IsCancellationRequested) return;
            var dispose = Subscribe(handler);
            token.Register(() => dispose.Dispose());
        }

        private void Unsubscribe<T>(IDispatcher.EventHandler<T> handler) where T : notnull
        {
            Type eventType = typeof(T);
            LockedList<Delegate> handlers;
            if (m_EventListeners.TryGetValue(eventType, out handlers))
            {
                if (handlers.isLocked)
                {
                    Unsubscribe_Deferred(handlers, handler);
                    return;
                }
                handlers.Remove(handler);
                if (handlers.Count == 0)
                {
                    m_EventListeners.Remove(eventType);
                }
            }
        }
        
        private async void Unsubscribe_Deferred<T>(LockedList<Delegate> handlers, IDispatcher.EventHandler<T> handler) where T : notnull
        {
            await handlers;
            Unsubscribe(handler);
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

        public IReadOnlyList<T> GetAnnouncements<T>() where T : notnull
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
            handler(GetAnnouncements<T>().ToArray());
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
        
        public IDisposable Register<TValue>(object id, TValue source) where TValue : notnull
        {
            var valueType = typeof(TValue);
            if (!m_Registry.TryGetValue((id, valueType), out var registry))
            {
                registry = (source, true, new LockedList<IDispatcher.EventHandler<object>>());
            }
            else
            {
                if (registry.handlers.isLocked)
                {
                    var cancellationTokenSource = new CancellationTokenSource();
                    Register_Deferred(registry.handlers, id, source, cancellationTokenSource.Token);
                    return new DisposableAction(() => cancellationTokenSource.Cancel());
                }
                if (registry.isActive)
                {
                    throw new System.Exception($"Registry {id} of type {typeof(TValue).Name} already registered");
                }
                registry.value = source;
                registry.isActive = true;
            }
            
            using (registry.handlers.Lock())
            {
                foreach (var handler in registry.handlers)
                {
                    handler(source);
                }
                m_Registry[(id, valueType)] = registry;
            }
                
            return new DisposableAction(() =>
            {
                Unregister(id, source);
            });
        }
        
        private async void Register_Deferred<TValue>(LockedList<IDispatcher.EventHandler<object>> handlers, object id, TValue source, CancellationToken token) where TValue : notnull
        {
            await handlers;
            if (token.IsCancellationRequested) return;
            Register(id, source);
        }

        private void Unregister<TValue>(object id, TValue source) where TValue : notnull
        {
            var valueType = typeof(TValue);
            if (m_Registry.TryGetValue((id, valueType), out var reg))
            {
                if (reg.handlers.isLocked)
                {
                    Unregister_Deferred(reg.handlers, id, source);
                    return;
                }
                reg.value = default;
                reg.isActive = false;
                if (reg.handlers.Count == 0)
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
        }
        
        private async void Unregister_Deferred<TValue>(LockedList<IDispatcher.EventHandler<object>> handlers, object id, TValue source) where TValue : notnull
        {
            await handlers;
            Unregister(id, source);
        }

        public IDisposable Register<TValue>(TValue source) where TValue : notnull
        {
            return Register(this, source);
        }

        

        public DisposableAction SubscribeRegistry<TValue>(object id, IDispatcher.EventHandler<TValue> handler, bool skipInitial = false)
        {
            var valueType = typeof(TValue);
            if (!m_Registry.TryGetValue((id, valueType), out var registry))
            {
                registry = (default, false, new LockedList<IDispatcher.EventHandler<object>>());
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
                
            if (registry.handlers.isLocked)
            {
                var cancellationTokenSource = new CancellationTokenSource();
                SubscribeRegistry_Deferred<TValue>(registry.handlers, handler, cancellationTokenSource.Token);
                return new DisposableAction(() => cancellationTokenSource.Cancel());
            }

            using (registry.handlers.Lock())
            {
                registry.handlers.Add(handlerWrapper);
            }
            

            return new DisposableAction(() =>
            {
                UnsubscribeRegistry<TValue>(id, handlerWrapper);
            });
        }
        
        private void UnsubscribeRegistry<TValue>(object id, IDispatcher.EventHandler<object> handlerWrapper)
        {
            var valueType = typeof(TValue);
            if (m_Registry.TryGetValue((id, valueType), out var reg))
            {
                if (reg.handlers.isLocked)
                {
                    UnsubscribeRegistry_Deferred<TValue>(reg.handlers, id, handlerWrapper);
                    return;
                }
                reg.handlers.Remove(handlerWrapper);
                if (!reg.isActive && reg.handlers.Count == 0)
                {
                    m_Registry.Remove((id, valueType));
                }
            }
#if DEBUG
            else
            {
                throw new System.Exception($"Registry key '{id}' not found for type {valueType}");
            }
#endif
        }
        
        private async void UnsubscribeRegistry_Deferred<TValue>(LockedList<IDispatcher.EventHandler<object>> handlers, object id, IDispatcher.EventHandler<object> handlerWrapper)
        {
            await handlers;
            UnsubscribeRegistry<TValue>(id, handlerWrapper);
        }
        
        private async void SubscribeRegistry_Deferred<TValue>(LockedList<IDispatcher.EventHandler<object>> handlers, IDispatcher.EventHandler<TValue> handler, CancellationToken token)
        {
            await handlers;
            if (token.IsCancellationRequested) return;
            var dispose = SubscribeRegistry<TValue>(handler);
            token.Register(() => dispose.Dispose());
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
        
        public UniTask<TValue> WhenRegistered<TKey,TValue>(object key, CancellationToken cancellationToken = default, float timeout = 0f) where TValue : notnull
        {
            return WhenRegistered<Dispatcher, TValue>(key, cancellationToken, timeout);
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
        
        #region Request
        
        
        private readonly Dictionary<(Type, object), LockedList<IDispatcher.RequestHandler<object>>> m_RequestHandlers = new ();
        public async UniTask Request<T>(T eventObject, object eventName, CancellationToken token) where T : notnull
        {
            if (m_RequestHandlers.TryGetValue((typeof(T), eventName), out var handlers))
            {
                if (handlers.isLocked)
                {
                    await handlers;
                }
                if (handlers.Count > 0)
                {
                    using (handlers.Lock())
                    {
                        var tasks = new List<UniTask>();
                        foreach (var handler in handlers)
                        {
                            tasks.Add(handler(eventObject, token));
                        }
                        await UniTask.WhenAll(tasks);
                    }
                }
            }
        }
        
        public DisposableAction SubscribeRequest<T>(object eventName, IDispatcher.RequestHandler<T> handler) where T : notnull
        {
            /* Subscribe Request. But take in opinion that it can be locked */
            if (!m_RequestHandlers.TryGetValue((typeof(T), eventName), out var handlers))
            {
                handlers = new LockedList<IDispatcher.RequestHandler<object>>();
                m_RequestHandlers[(typeof(T), eventName)] = handlers;
            }
            
            IDispatcher.RequestHandler<object> handlerWrapper = (source, token) =>
            {
                return handler((T)source, token);
            };
            
            if (handlers.isLocked)
            {
                var cancellationTokenSource = new CancellationTokenSource();
                SubscribeRequest_Deferred(handlers, handlerWrapper, cancellationTokenSource.Token);
                return new DisposableAction(() => cancellationTokenSource.Cancel());
            }
            
            handlers.Add(handlerWrapper);
            
            return new DisposableAction(() =>
            {
                UnsubscribeRequest(eventName, handlerWrapper);
            });
        }
        
        public async void SubscribeRequest_Deferred<T>(LockedList<IDispatcher.RequestHandler<object>> handlers, IDispatcher.RequestHandler<T> handler, CancellationToken token)
        {
            await handlers;
            if (token.IsCancellationRequested) return;
            var dispose = SubscribeRequest<T>(handlers, handler);
            token.Register(() => dispose.Dispose());
        }
        
        public void UnsubscribeRequest(object eventName, IDispatcher.RequestHandler<object> handlerWrapper)
        {
            if (m_RequestHandlers.TryGetValue((handlerWrapper.Method.GetParameters()[0].ParameterType, eventName), out var handlers))
            {
                if (handlers.isLocked)
                {
                    UnsubscribeRequest_Deferred(handlers, eventName, handlerWrapper);
                    return;
                }
                handlers.Remove(handlerWrapper);
                if (handlers.Count == 0)
                {
                    m_RequestHandlers.Remove((handlerWrapper.Method.GetParameters()[0].ParameterType, eventName));
                }
            }
        }
        
        public async void UnsubscribeRequest_Deferred(LockedList<IDispatcher.RequestHandler<object>> handlers, object eventName, IDispatcher.RequestHandler<object> handlerWrapper)
        {
            await handlers;
            UnsubscribeRequest(eventName, handlerWrapper);
        }

        #endregion
    }
}