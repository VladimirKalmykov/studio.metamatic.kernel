using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public partial class Dispatcher
    {
        private readonly Dictionary<Type, List<object>> m_AnnouncementsLegacy = new();
        #region Announcements
        /// <summary>
        /// Announce object
        /// </summary>
        /// <param name="source"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public DisposableAction AnnounceLegacy<T>(T source) where T : notnull
        {
            if (!m_AnnouncementsLegacy.TryGetValue(typeof(T), out var announcements))
            {
                announcements = new List<object>();
                m_AnnouncementsLegacy[typeof(T)] = announcements;
            }
            announcements.Add(source);
            Emit(new Announcement<T>(source));
            return new DisposableAction(() => RetractLgacy(source));
        }
        
        /// <summary>
        /// Remove announcement from the dispatcher
        /// </summary>
        /// <param name="source"></param>
        /// <typeparam name="T"></typeparam>
        public void RetractLgacy<T>(T source) where T : notnull
        {
            if (m_AnnouncementsLegacy.TryGetValue(typeof(T), out var announcements))
            {
                announcements.Remove(source);
                if (announcements.Count == 0)
                {
                    m_AnnouncementsLegacy.Remove(typeof(T));
                }
                Emit(new Retraction<T>(source));
            }
        }

        public IReadOnlyList<T> GetAnnouncementsLegacy<T>() where T : notnull
        {
            if (m_AnnouncementsLegacy.TryGetValue(typeof(T), out var announcements))
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
            releaseSubscription = SubscribeAnnouncementsLegacy<T>(sources =>
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

        public DisposableAction SubscribeAnnouncementsLegacy<T>(IDispatcher.AnnouncementsHandler<T> handler) where T : notnull
        {
            handler(GetAnnouncementsLegacy<T>().ToArray());
            return Subscribe<Announcement<T>>(announcement =>
            {
                handler(new []{announcement.source});
            });
        }

        public DisposableAction SubscribeRetractionsLegacy<T>(IDispatcher.RetractionsHandler<T> handler) where T : notnull
        {
            return Subscribe<Retraction<T>>(retraction =>
            {
                handler(retraction.source);
            });
        }

        #endregion
    }
}