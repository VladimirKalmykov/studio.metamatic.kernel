using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace Metamatic.Actions
{
    public class Dispatcher : IDispatcher
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
        public IDisposable Register<TKey, TValue>(TKey id, TValue source) where TValue : notnull
        {
            var keyType = typeof(TKey);
            var valueType = typeof(TValue);
            if (!m_RegistryTypesKeys.TryGetValue(keyType, out var keyTypeDictionary))
            {
                m_RegistryTypesKeys[keyType] = keyTypeDictionary = new Dictionary<Type, Dictionary<object, RegistrySource<object>>>();
            }
            if (!keyTypeDictionary.TryGetValue(valueType, out var valueTypeDictionary))
            {
                keyTypeDictionary[valueType] = valueTypeDictionary = new SimplyObservableDictionary<object, RegistrySource<object>>();
            }
            if (!valueTypeDictionary.TryGetValue(id, out var registry))
            {
                valueTypeDictionary[id] = registry = new RegistrySource<object>();
            }
            var token = registry.Register(source);
            return new DisposableCallback(() =>
            {
                registry.Release(token);
                if (registry.isEmpty())
                {
                    valueTypeDictionary.Remove(id);
                    if (valueTypeDictionary.Count == 0)
                    {
                        keyTypeDictionary.Remove(typeof(TValue));
                        if (keyTypeDictionary.Count == 0)
                        {
                            m_RegistryTypesKeys.Remove(typeof(TKey));
                        }
                    }
                }
            });
        }

        public DisposableAction SubscribeRegistry<TKey, TValue>(TKey id, IDispatcher.EventHandler<TValue> handler) where TValue : notnull
        {
            var keyType = typeof(TKey);
            var valueType = typeof(TValue);
            if (!m_RegistryTypesKeys.TryGetValue(keyType, out var keyTypeDictionary))
            {
                m_RegistryTypesKeys[keyType] = keyTypeDictionary = new Dictionary<Type, Dictionary<object, RegistrySource<object>>>();
            }
            if (!keyTypeDictionary.TryGetValue(valueType, out var valueTypeDictionary))
            {
                keyTypeDictionary[valueType] = valueTypeDictionary = new SimplyObservableDictionary<object, RegistrySource<object>>();
            }
            if (!valueTypeDictionary.TryGetValue(id, out var registry))
            {
                valueTypeDictionary[id] = registry = new RegistrySource<object>();
            }
            System.Action<object> handlerWrapper = source =>
            {
                handler((TValue)source);
            };
            var releaseSubscription = registry.Subscribe(handlerWrapper);
            return new DisposableAction(() =>
            {
                releaseSubscription();
                if (registry.isEmpty())
                {
                    valueTypeDictionary.Remove(id);
                    if (valueTypeDictionary.Count == 0)
                    {
                        keyTypeDictionary.Remove(typeof(TValue));
                        if (keyTypeDictionary.Count == 0)
                        {
                            m_RegistryTypesKeys.Remove(typeof(TKey));
                        }
                    }
                }
            });
        }

        public IDisposable Register<TValue>(TValue source) where TValue : notnull
        {
            return Register(this, source);
        }

        public DisposableAction SubscribeRegistry<TValue>(IDispatcher.EventHandler<TValue> handler) where TValue : notnull
        {
            return SubscribeRegistry(this, handler);
        }

        public UniTask<TValue> WhenRegistered<TKey, TValue>(TKey id, CancellationToken cancellationToken = default) where TValue : notnull
        {
            var tcs = new UniTaskCompletionSource<TValue>();
            UniTask<TValue> task = tcs.Task;
            System.Action releaseSubscription = null;
            CancellationTokenRegistration cancellationTokenRegistration = cancellationToken.Register(() =>
            {
                releaseSubscription?.Invoke();
                tcs.TrySetCanceled(cancellationToken);
            });
            releaseSubscription = SubscribeRegistry<TKey, TValue>(id, source =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                releaseSubscription?.Invoke();
                cancellationTokenRegistration.Dispose();
                tcs.TrySetResult(source);
            });
            return task;
        }

        public UniTask<TValue> WhenRegistered<TValue>(CancellationToken cancellationToken = default) where TValue : notnull
        {
            return WhenRegistered<Dispatcher, TValue>(this, cancellationToken);
        }

        #endregion
    }
}