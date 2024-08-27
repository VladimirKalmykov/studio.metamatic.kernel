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
            private CancellationTokenSource m_CancellationTokenSource;
            public object Item;
            public CancellationToken ReleaseToken => m_CancellationTokenSource.Token;
            public static Announcement Create(object item)
            {
                var announcement = Pool.Get();
                announcement.m_CancellationTokenSource = new CancellationTokenSource();
                announcement.Item = item;
                return announcement;
            }

            public void Dispose()
            {
                m_CancellationTokenSource.Cancel();
                m_CancellationTokenSource.Dispose();
                Pool.Return(this);
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
            }
        }
        
        private Dictionary<Type, Dictionary<object, ObservableLockedDictionaryContainer>> m_Announcements = new();
        
        /// <summary>
        /// Announce object
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public DisposableAction Announce<T>(object key, T item) where T : notnull
        {
            if (!m_Announcements.TryGetValue(typeof(T), out var announcements))
            {
                announcements = new Dictionary<object, ObservableLockedDictionaryContainer>();
                m_Announcements[typeof(T)] = announcements;
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
                    Internal_Retract<T>(dict, id).Forget();
                });
            }
            else
            {
                dict.Add(id, Announcement.Create(item));
            }

            return new DisposableAction(() =>
            {
                Internal_Retract<T>(dict, id).Forget();
            });
        }
        
        /// <summary>
        /// Subscribe to each announced object of type T with a key
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
        /// Subscribe to each announced object of type T with a key and a condition
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
        
        private async UniTaskVoid Internal_Retract<T>(ObservableLockedDictionaryContainer dict, string id) where T : notnull
        {
            try
            {
                using (await dict.Lock(CancellationToken.None))
                {
                    var item = dict.Get(id);
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