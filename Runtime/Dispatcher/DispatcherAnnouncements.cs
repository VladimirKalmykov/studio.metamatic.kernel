using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Metamatic.Metamatic;
using UnityEngine;

namespace Metamatic
{
    public partial class Dispatcher
    {
        /// <summary>
        /// Reusable announcement object
        /// </summary>
        public class Announcement: IDisposable
        {
            private static readonly ParamLessPool<Announcement> Pool = new ParamLessPool<Announcement>();
            /// <summary>
            /// CancellationToken should be initialized only if it is required by observer to reduce count of allocated objects
            /// </summary>
            private CancellationTokenSource m_CancellationTokenSource;
            public object Item;

            public CancellationToken ReleaseToken
            {
                get
                {
                    /* Lazy initialization on demand */
                    if (m_CancellationTokenSource == null)
                    {
                        m_CancellationTokenSource = new CancellationTokenSource();
                    }
                    return m_CancellationTokenSource.Token;
                }
            }
            
            /// <summary>
            /// Get announcement object from the pool
            /// </summary>
            /// <param name="item"></param>
            /// <returns></returns>
            public static Announcement Create(object item)
            {
                var announcement = Pool.Get();
                announcement.m_CancellationTokenSource = null;
                announcement.Item = item;
                return announcement;
            }
            
            /// <summary>
            /// Return announcement object to the pool
            /// </summary>
            public void Return()
            {
                if (m_CancellationTokenSource != null)
                {
                    m_CancellationTokenSource.Cancel();
                    m_CancellationTokenSource.Dispose();
                    m_CancellationTokenSource = null;
                }
                Pool.Return(this);
            }
            
            /// <summary>
            /// Dispose will be called from the pool when the object is released (totally removed from pool)
            /// </summary>
            public void Dispose()
            {
                if (m_CancellationTokenSource != null)
                {
                    m_CancellationTokenSource.Cancel();
                    m_CancellationTokenSource.Dispose();
                }
            }
        }

        public class ObservableLockedDictionaryContainer: IDisposable
        {
            private static readonly ParamLessPool<ObservableLockedDictionaryContainer> Pool = new ParamLessPool<ObservableLockedDictionaryContainer>();
            private readonly LockedDictionary<string, Announcement> m_Items = new();
            private readonly DeferLock m_Lock = new();
            public event Action<string, Announcement> OnAdd;
            
            public static ObservableLockedDictionaryContainer Create()
            {
                var container = Pool.Get();
                return container;
            }
            
            public bool isLocked => m_Lock.IsLocked;
            public UniTask<DisposableAction> Lock(CancellationToken cancellationToken)
            {
                return m_Lock.Lock(cancellationToken);
            }
            
            ~ObservableLockedDictionaryContainer()
            {
                Debug.LogError("You should dispose ObservableLockedDictionaryContainer");
            }
            
            public DisposableAction Subscribe(Action<string, Announcement> action)
            {
                OnAdd += action;
                return new DisposableAction(() => OnAdd -= action);
            }
            
            public void GetEach(Action<string, Announcement> action)
            {
                foreach (var item in m_Items)
                {
                    action(item.Key, item.Value);
                }
            }
            
            public (string id, Announcement announcement) Find(Func<KeyValuePair<string, Announcement>, bool> predicate, Action<string, Announcement> action)
            {
                foreach (var item in m_Items)
                {
                    if (predicate(item))
                    {
                        return (item.Key, item.Value);
                    }
                }
                return (null, null);
            }
            
            public void Add(string key, Announcement item)
            {
                m_Items.Add(key, item);
                OnAdd?.Invoke(key, item);
            }
            
            public Announcement Get(string key)
            {
                return m_Items[key];
            }
            
            public void Remove(string key)
            {
                var item = m_Items[key];
                m_Items.Remove(key);
                item.Dispose();
            }

            public void Dispose()
            {
                m_Lock.Reset();
                /* Dispose all items */
                foreach (var item in m_Items)
                {
                    item.Value.Dispose();
                }
                m_Items.Clear();
                Pool.Return(this);
                GC.SuppressFinalize(this);
            }
        }
        
        private Dictionary<Type, Dictionary<object, ObservableLockedDictionaryContainer>> m_Announcements = new();

        /// <summary>
        /// Announce object
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <param name="useRuntimeType">Use derived type instead of compile-time type</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public DisposableAction Announce<T>(object key, T item, bool useRuntimeType = false) where T : notnull
        {
            var type = useRuntimeType ? item.GetType() : typeof(T);
            if (!m_Announcements.TryGetValue(type, out var announcements))
            {
                announcements = new Dictionary<object, ObservableLockedDictionaryContainer>();
                m_Announcements[type] = announcements;
            }

            if (!announcements.TryGetValue(key, out var dict))
            {
                dict = ObservableLockedDictionaryContainer.Create();
                announcements[key] = dict;
            }
            
            var id = Guid.NewGuid().ToString();

            if (dict.isLocked)
            {
                var cts = new CancellationTokenSource();
                Internal_AddAnnouncement<T>(dict, id, item, cts.Token).Forget();
                
                return new DisposableAction(() =>
                {
                    cts.Cancel();
                    cts.Dispose();
                    Internal_Retract(dict, id).Forget();
                });
            }
            else
            {
                dict.Add(id, Announcement.Create(item));
            }

            return new DisposableAction(() =>
            {
                Internal_Retract(dict, id).Forget();
            });
        }
        
        /// <summary>
        /// Shortcut for Announce that automatically relates to the dispatcher as a key
        /// </summary>
        /// <param name="item"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public DisposableAction Announce<T>(T item, bool useRuntimeType = false) where T : notnull
        {
            return Announce(this, item, useRuntimeType);
        }
        
        /// <summary>
        /// Subscribe to each announced object of type T with a key. Try to avoid of using this method, if you can use ObserveAnnouncementEach with a condition
        /// </summary>
        /// <param name="key"></param>
        /// <param name="action"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public DisposableAction ObserveAnnouncementEach<T>(object key, Func<T, CancellationToken, UniTaskVoid> action) where T : notnull
        {
            if (!m_Announcements.TryGetValue(typeof(T), out var announcements))
            {
                announcements = new Dictionary<object, ObservableLockedDictionaryContainer>();
                m_Announcements[typeof(T)] = announcements;
            }
            if (!announcements.TryGetValue(key, out var observableDict))
            {
                observableDict = ObservableLockedDictionaryContainer.Create();
                announcements[key] = observableDict;
            }

            var cancellationTokenSources = ParamLessPool<List<CancellationTokenSource>>.GetShared();
            var registrations = ParamLessPool<List<CancellationTokenRegistration>>.GetShared();
            /* Iterate through all items */
            observableDict.GetEach((id, announcement) =>
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(announcement.ReleaseToken);
                cancellationTokenSources.Add(cts);
                var token = cts.Token;
                CancellationTokenRegistration registration = default;
                registration = token.Register(() =>
                {
                    registration.Dispose();
                    registrations.Remove(registration);
                    cancellationTokenSources.Remove(cts);
                    cts.Dispose();
                });
                registrations.Add(registration);
                action((T)announcement.Item, token).Forget();
            });
            var dispose = observableDict.Subscribe((id, announcement) =>
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(announcement.ReleaseToken);
                cancellationTokenSources.Add(cts);
                var token = cts.Token;
                CancellationTokenRegistration registration = default;
                registration = token.Register(() =>
                {
                    registration.Dispose();
                    registrations.Remove(registration);
                    cancellationTokenSources.Remove(cts);
                    cts.Dispose();
                });
                registrations.Add(registration);
                action((T)announcement.Item, token).Forget();
            });
            return new DisposableAction(() =>
            {
                foreach (var registration in registrations)
                {
                    registration.Dispose();
                }
                foreach (var cts in cancellationTokenSources)
                {
                    cts.Cancel();
                    cts.Dispose();
                }
                dispose.Dispose();
                cancellationTokenSources.Clear();
                registrations.Clear();
                ParamLessPool<List<CancellationTokenSource>>.ReturnShared(cancellationTokenSources);
                ParamLessPool<List<CancellationTokenRegistration>>.ReturnShared(registrations);
            });
        }
        
        /// <summary>
        /// Shortcut for ObserveAnnouncementEach that automatically relates to the dispatcher as a key
        /// </summary>
        /// <param name="action"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public DisposableAction ObserveAnnouncementEach<T>(Func<T, CancellationToken, UniTaskVoid> action)
            where T : notnull
        {
            return ObserveAnnouncementEach(this, action);
        }
        
        /// <summary>
        /// Observe to each announced object of type T with a key and a condition. The handler accept object itself and cancellation token that will be cancelled when the object is retracted
        /// </summary>
        /// <param name="key"></param>
        /// <param name="condition"></param>
        /// <param name="action"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public DisposableAction ObserveAnnouncementConditional<T>(object key, Func<T, bool> condition, Func<T, CancellationToken, UniTaskVoid> action) where T : notnull
        {
            if (!m_Announcements.TryGetValue(typeof(T), out var announcements))
            {
                announcements = new Dictionary<object, ObservableLockedDictionaryContainer>();
                m_Announcements[typeof(T)] = announcements;
            }
            if (!announcements.TryGetValue(key, out var observableDict))
            {
                observableDict = ObservableLockedDictionaryContainer.Create();
                announcements[key] = observableDict;
            }

            var cancellationTokenSources = ParamLessPool<List<CancellationTokenSource>>.GetShared();
            var registrations = ParamLessPool<List<CancellationTokenRegistration>>.GetShared();
            /* Iterate through all items */
            observableDict.GetEach((id, announcement) =>
            {
                if (!condition((T)announcement.Item)) return;
                var cts = CancellationTokenSource.CreateLinkedTokenSource(announcement.ReleaseToken);
                cancellationTokenSources.Add(cts);
                var token = cts.Token;
                CancellationTokenRegistration registration = default;
                registration = token.Register(() =>
                {
                    registration.Dispose();
                    registrations.Remove(registration);
                    cancellationTokenSources.Remove(cts);
                    cts.Dispose();
                });
                registrations.Add(registration);
                action((T)announcement.Item, token).Forget();
            });
            var dispose = observableDict.Subscribe((id, announcement) =>
            {
                if (!condition((T)announcement.Item)) return;
                var cts = CancellationTokenSource.CreateLinkedTokenSource(announcement.ReleaseToken);
                cancellationTokenSources.Add(cts);
                var token = cts.Token;
                CancellationTokenRegistration registration = default;
                registration = token.Register(() =>
                {
                    registration.Dispose();
                    registrations.Remove(registration);
                    cancellationTokenSources.Remove(cts);
                    cts.Dispose();
                });
                registrations.Add(registration);
                action((T)announcement.Item, token).Forget();
            });
            return new DisposableAction(() =>
            {
                foreach (var registration in registrations)
                {
                    registration.Dispose();
                }
                foreach (var cts in cancellationTokenSources)
                {
                    cts.Cancel();
                    cts.Dispose();
                }
                dispose.Dispose();
                cancellationTokenSources.Clear();
                registrations.Clear();
                ParamLessPool<List<CancellationTokenSource>>.ReturnShared(cancellationTokenSources);
                ParamLessPool<List<CancellationTokenRegistration>>.ReturnShared(registrations);
            });
        }
        
        /// <summary>
        /// Shortcut for ObserveAnnouncementConditional that automatically relates to the dispatcher as a key
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="action"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public DisposableAction ObserveAnnouncementConditional<T>(Func<T, bool> condition, Func<T, CancellationToken, UniTaskVoid> action)
            where T : notnull
        {
            return ObserveAnnouncementConditional(this, condition, action);
        }
        
        /// <summary>
        /// Subscribe to each announced object of type T with a key without observing the object's lifecycle
        /// </summary>
        /// <param name="key"></param>
        /// <param name="action"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public DisposableAction SubscribeAnnouncement<T>(object key, Action<T> action) where T : notnull
        {
            if (!m_Announcements.TryGetValue(typeof(T), out var announcements))
            {
                announcements = new Dictionary<object, ObservableLockedDictionaryContainer>();
                m_Announcements[typeof(T)] = announcements;
            }
            if (!announcements.TryGetValue(key, out var observableDict))
            {
                observableDict = ObservableLockedDictionaryContainer.Create();
                announcements[key] = observableDict;
            }
            observableDict.GetEach((id, announcement) =>
            {
                action((T)announcement.Item);
            });
            return observableDict.Subscribe((id, announcement) =>
            {
                action((T)announcement.Item);
            });
        }
        
        /// <summary>
        /// Shortcut for SubscribeAnnouncement that automatically relates to the dispatcher as a key
        /// </summary>
        /// <param name="action"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public DisposableAction SubscribeAnnouncement<T>(Action<T> action)
            where T : notnull
        {
            return SubscribeAnnouncement(this, action);
        }
        
        /// <summary>
        /// Find an announcement object of type T with a key and a condition
        /// </summary>
        /// <param name="key"></param>
        /// <param name="selector"></param>
        /// <param name="result"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool FindAnnouncement<T>(object key, Func<T, bool> selector, out T result)
        {
            if (!m_Announcements.TryGetValue(typeof(T), out var announcements))
            {
                result = default;
                return false;
            }
            if (!announcements.TryGetValue(key, out var observableDict))
            {
                result = default;
                return false;
            }
            var (id, announcement) = observableDict.Find(pair =>
            {
                if (pair.Value.Item is T item)
                {
                    if (selector(item))
                    {
                        return true;
                    }
                }
                return false;
            }, (id, announcement) => { });
            if (id == null)
            {
                result = default;
                return false;
            }
            result = (T)announcement.Item;
            return true;
        }
        
        /// <summary>
        /// Shortcut for FindAnnouncement that automatically relates to the dispatcher as a key
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="result"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool FindAnnouncement<T>(Func<T, bool> selector, out T result)
        {
            return FindAnnouncement(this, selector, out result);
        }
        
        /// <summary>
        /// Get the first announcement object of type T with a key
        /// </summary>
        /// <param name="key"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetFirstOfAnnouncement<T>(object key)
        {
            if (!m_Announcements.TryGetValue(typeof(T), out var announcements))
            {
                return default;
            }
            if (!announcements.TryGetValue(key, out var observableDict))
            {
                return default;
            }
            var (id, announcement) = observableDict.Find(pair =>
            {
                if (pair.Value.Item is T item)
                {
                    return true;
                }
                return false;
            }, (id, announcement) => { });
            return id == null ? default : (T)announcement.Item;
        }
        
        /// <summary>
        /// Shortcut for GetFirstOfAnnouncement that automatically relates to the dispatcher as a key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetFirstOfAnnouncement<T>()
        {
            return GetFirstOfAnnouncement<T>(this);
        }
        
        #region Internal
        private async UniTaskVoid Internal_AddAnnouncement<T>(ObservableLockedDictionaryContainer dict, string id, T item, System.Threading.CancellationToken cancellationToken) where T : notnull
        {
            try
            {
                using (await dict.Lock(cancellationToken))
                {
                    dict.Add(id, Announcement.Create(item));
                }
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
        }
        
        private async UniTaskVoid Internal_Retract(ObservableLockedDictionaryContainer dict, string id)
        {
            try
            {
                using (await dict.Lock(CancellationToken.None))
                {
                    var item = dict.Get(id);
                    item.Return();
                    dict.Remove(id);
                }
            }
            catch (OperationCanceledException)
            {
                // ignored
            } catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
        #endregion
    }
}