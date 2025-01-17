namespace Metamatic
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    public partial class Dispatcher
    {
        public class Announcement : IDisposable
        {
            private static readonly ParamLessPool<Announcement> Pool = new ParamLessPool<Announcement>();
            private CancellationTokenSource m_CancellationTokenSource;
            public object Item;

            public CancellationToken ReleaseToken
            {
                get
                {
                    if (m_CancellationTokenSource == null)
                    {
                        m_CancellationTokenSource = new CancellationTokenSource();
                    }

                    return m_CancellationTokenSource.Token;
                }
            }

            public static Announcement Create(object item)
            {
                var announcement = Pool.Get();
                announcement.m_CancellationTokenSource = null;
                announcement.Item = item;
                return announcement;
            }

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

            public void Dispose()
            {
                if (m_CancellationTokenSource != null)
                {
                    m_CancellationTokenSource.Cancel();
                    m_CancellationTokenSource.Dispose();
                }
            }
        }

        public class ObservableLockedDictionaryContainer : IDisposable
        {
            private static readonly ParamLessPool<ObservableLockedDictionaryContainer> Pool =
                new ParamLessPool<ObservableLockedDictionaryContainer>();

            private readonly Dictionary<object, Announcement> m_Items = new();
            private readonly DeferLock m_Lock = new();
            public event Action<object, Announcement> OnAdd;

            public static ObservableLockedDictionaryContainer Create()
            {
                var container = Pool.Get();
                return container;
            }

            public bool isLocked => m_Lock.IsLocked;

            public UniTask<DisposableAction> Lock(Signal cancellationSignal)
            {
                return m_Lock.Lock(cancellationSignal);
            }

            public DisposableAction Subscribe(Action<object, Announcement> action)
            {
                OnAdd += action;
                return new DisposableAction(() => OnAdd -= action);
            }

            public void GetEach(Action<object, Announcement> action)
            {
                foreach (var item in m_Items)
                {
                    action(item.Key, item.Value);
                }
            }

            public bool ContainsKey(object key)
            {
                return m_Items.ContainsKey(key);
            }

            public Announcement Get(object key)
            {
                return m_Items[key];
            }

            public void Add(object key, Announcement item)
            {
                m_Items.Add(key, item);
                OnAdd?.Invoke(key, item);
            }

            public void Remove(object key)
            {
                var item = m_Items[key];
                m_Items.Remove(key);
                item.Dispose();
            }

            public IEnumerable<T> GetItems<T>() where T : notnull
            {
                foreach (var announcement in m_Items.Values)
                {
                    if (announcement.Item is T tItem)
                    {
                        yield return tItem;
                    }
                }
            }

            public void Dispose()
            {
                m_Lock.Reset();
                foreach (var item in m_Items)
                {
                    item.Value.Dispose();
                }

                m_Items.Clear();
                Pool.Return(this);
                GC.SuppressFinalize(this);
            }
        }

        private Dictionary<Type, ObservableLockedDictionaryContainer> m_Announcements = new();

        public DisposableAction Announce<T>(T item, bool useRuntimeType = false) where T : notnull
        {
            var type = useRuntimeType ? item.GetType() : typeof(T);
            if (!m_Announcements.TryGetValue(type, out var dict))
            {
                dict = ObservableLockedDictionaryContainer.Create();
                m_Announcements[type] = dict;
            }

            var key = (object)item;

            if (dict.isLocked)
            {
                var cts = SignalSource.Create();
                Internal_AddAnnouncement<T>(dict, key, item, cts.Signal).Forget();

                return new DisposableAction(() =>
                {
                    cts.TryRaise();
                    Internal_Retract(dict, key).Forget();
                });
            }
            else
            {
                dict.Add(key, Announcement.Create(item));
            }

            return new DisposableAction(() => { Internal_Retract(dict, key).Forget(); });
        }

        public DisposableAction ObserveAnnouncementEach<T>(Func<T, Signal, UniTaskVoid> action)
            where T : notnull
        {
            if (!m_Announcements.TryGetValue(typeof(T), out var dict))
            {
                dict = ObservableLockedDictionaryContainer.Create();
                m_Announcements[typeof(T)] = dict;
            }

            var cancellationTokenSources = ParamLessPool<List<SignalSource>>.GetShared();
            var registrations = ParamLessPool<List<DisposableAction>>.GetShared();

            dict.GetEach((key, announcement) =>
            {
                var cts = SignalSource.CreateLinked(announcement.ReleaseToken);
                cancellationTokenSources.Add(cts);
                var token = cts.Signal;
                DisposableAction registration = default;
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

            var dispose = dict.Subscribe((key, announcement) =>
            {
                var cts = SignalSource.CreateLinked(announcement.ReleaseToken);
                cancellationTokenSources.Add(cts);
                var token = cts.Signal;
                DisposableAction registration = default;
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
                    cts.TryRaise();
                }

                dispose.Dispose();
                cancellationTokenSources.Clear();
                registrations.Clear();
                ParamLessPool<List<SignalSource>>.ReturnShared(cancellationTokenSources);
                ParamLessPool<List<DisposableAction>>.ReturnShared(registrations);
            });
        }

        public IEnumerable<T> GetAnnouncements<T>() where T : notnull
        {
            if (m_Announcements.TryGetValue(typeof(T), out var dict))
            {
                return dict.GetItems<T>();
            }
            else
            {
                return Enumerable.Empty<T>();
            }
        }
        
        public DisposableAction SubscribeAnnouncements<T>(Action<T> action) where T : notnull
        {
            if (!m_Announcements.TryGetValue(typeof(T), out var dict))
            {
                dict = ObservableLockedDictionaryContainer.Create();
                m_Announcements[typeof(T)] = dict;
            }

            // Обработка существующих анонсов
            dict.GetEach((key, announcement) =>
            {
                action((T)announcement.Item);
            });

            // Подписка на будущие анонсы
            var dispose = dict.Subscribe((key, announcement) =>
            {
                action((T)announcement.Item);
            });

            return dispose;
        }
        
        public DisposableAction SubscribeAnnouncements<T>(Action<T> action, Func<T, bool> condition) where T : notnull
        {
            if (!m_Announcements.TryGetValue(typeof(T), out var dict))
            {
                dict = ObservableLockedDictionaryContainer.Create();
                m_Announcements[typeof(T)] = dict;
            }

            // Обработка существующих анонсов
            dict.GetEach((key, announcement) =>
            {
                T item = (T)announcement.Item;
                if (condition(item))
                {
                    action(item);
                }
            });

            // Подписка на будущие анонсы
            var dispose = dict.Subscribe((key, announcement) =>
            {
                T item = (T)announcement.Item;
                if (condition(item))
                {
                    action(item);
                }
            });

            return dispose;
        }
        
        public UniTask<T> WaitForAnnouncement<T>(Signal signal) where T : notnull
        {
            var completionSource = AutoResetUniTaskCompletionSource<T>.Create();

            if (!m_Announcements.TryGetValue(typeof(T), out var dict))
            {
                dict = ObservableLockedDictionaryContainer.Create();
                m_Announcements[typeof(T)] = dict;
            }

            // Проверка существующих анонсов
            bool found = false;
            dict.GetEach((key, announcement) =>
            {
                if (!found)
                {
                    T item = (T)announcement.Item;
                    found = true;
                    completionSource.TrySetResult(item);
                }
            });

            if (found)
            {
                return completionSource.Task;
            }

            // Подписка на будущие анонсы
            DisposableAction dispose = default;
            dispose = dict.Subscribe((key, announcement) =>
            {
                T item = (T)announcement.Item;
                if (completionSource.TrySetResult(item))
                {
                    dispose.Dispose();
                }
            });

            // Обработка отмены
            signal.Register(() =>
            {
                if (completionSource.TrySetCanceled())
                {
                    dispose.Dispose();
                }
            });

            return completionSource.Task;
        }

        public UniTask<T> WaitForAnnouncement<T>(Signal signal, Func<T, bool> condition) where T : notnull
        {
            var completionSource = AutoResetUniTaskCompletionSource<T>.Create();

            if (!m_Announcements.TryGetValue(typeof(T), out var dict))
            {
                dict = ObservableLockedDictionaryContainer.Create();
                m_Announcements[typeof(T)] = dict;
            }

            // Проверка существующих анонсов
            bool found = false;
            dict.GetEach((key, announcement) =>
            {
                if (!found)
                {
                    T item = (T)announcement.Item;
                    if (condition(item))
                    {
                        found = true;
                        completionSource.TrySetResult(item);
                    }
                }
            });

            if (found)
            {
                return completionSource.Task;
            }

            // Подписка на будущие анонсы
            DisposableAction dispose = default;
            dispose = dict.Subscribe((key, announcement) =>
            {
                T item = (T)announcement.Item;
                if (condition(item))
                {
                    if (completionSource.TrySetResult(item))
                    {
                        dispose.Dispose();
                    }
                }
            });

            // Обработка отмены
            signal.Register(() =>
            {
                if (completionSource.TrySetCanceled())
                {
                    dispose.Dispose();
                }
            });

            return completionSource.Task;
        }

        // Внутренние методы
        private async UniTaskVoid Internal_AddAnnouncement<T>(ObservableLockedDictionaryContainer dict, object key,
            T item, Signal cancellationSignal) where T : notnull
        {
            try
            {
                using (await dict.Lock(cancellationSignal))
                {
                    dict.Add(key, Announcement.Create(item));
                }
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
        }

        private async UniTaskVoid Internal_Retract(ObservableLockedDictionaryContainer dict, object key)
        {
            try
            {
                using (await dict.Lock(Signal.None))
                {
                    if (dict.ContainsKey(key))
                    {
                        var item = dict.Get(key);
                        item.Return();
                        dict.Remove(key);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
    }

}