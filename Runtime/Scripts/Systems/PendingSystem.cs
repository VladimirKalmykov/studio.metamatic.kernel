using System;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public readonly struct PendingStats
    {
        private readonly InternalPending m_InternalPending;
        public string comment => m_InternalPending.Comment;
        
        public PendingStats(InternalPending internalPending)
        {
            m_InternalPending = internalPending;
        }
    }
    public readonly struct Pending: IDisposable
    {
        // ReSharper disable once InconsistentNaming
        public static Pending Empty => new Pending(null);
        private readonly InternalPending m_InternalPending;
        private readonly int m_Version;

        public Pending(InternalPending internalPending)
        {
            m_InternalPending = internalPending;
            m_Version = internalPending?.version ?? -1;
        }
        
        private bool IsValid()
        {
            return m_InternalPending != null && m_InternalPending.version == m_Version;
        }

        public void Dispose()
        {
            if (!IsValid()) return;
            m_InternalPending.Release();
        }
    }
    public class InternalPending: IResettable
    {
        public string Comment;
        public event Action OnRelease;
        public int version { get; private set; }
        public void Initialize(string comment)
        {
            Comment = comment;
        }
        public void Release()
        {
            OnRelease?.Invoke();
        }

        public void Reset()
        {
            Comment = null;
            OnRelease = null;
            version++;
        }
    }
    
    public class PendingTracker
    {
        private readonly SignedIndexedHashPool<InternalPending> m_Pendings = new (30);
        public int pendingCount => m_Pendings.Count;
        public Observable<bool> m_PendingStatus = new Observable<bool>();
        public event Action OnPendingStateChanged;
        public Pending CreatePending(string comment = null)
        {
            var newPending = ParamLessPool<InternalPending>.GetShared();
            newPending.Initialize(comment);
            var token = m_Pendings.Add(newPending);
            OnPendingStateChanged?.Invoke();
            m_PendingStatus.uniqValue = true;
            newPending.OnRelease += () =>
            {
                m_Pendings.Remove(token);
                OnPendingStateChanged?.Invoke();
                m_PendingStatus.uniqValue = m_Pendings.Count > 0;
                ParamLessPool<InternalPending>.ReturnShared(newPending);
            };
            return new Pending(newPending);
        }

        public DisposableAction WatchPending(Action<bool> handler)
        {
            return m_PendingStatus.Watch(handler);
        }
        
        public DisposableAction WatchPendingAsync(Func<bool, Signal, UniTaskVoid> handler) {
            return m_PendingStatus.WatchAsync(handler);
        }

        /// <summary>
        /// You get all pending stats as array at initial call and every time when pending state changed.
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public DisposableAction WatchPendingStatsAsArray(Action<PendingStats[]> handler)
        {
            handler(m_Pendings.ToArray().Select(p => new PendingStats(p)).ToArray());

            OnPendingStateChanged += OnStatsChange;

            return DisposableAction.Create(() =>
            {
                OnPendingStateChanged -= OnStatsChange;
            });
            
            void OnStatsChange()
            {
                handler(m_Pendings.ToArray().Select(p => new PendingStats(p)).ToArray());
            }
        }
    }
    
    /// <summary>
    /// Pending system is responsible for tracking pending operations.
    /// You can use in loading screen, or in any other place where you need to track pending operations (e.g. right bottom side of screen like in Witcher 3)
    /// </summary>
    public class PendingSystem
    {
        private static readonly PendingTracker CommonPendingTracker = new PendingTracker();
        private static readonly PendingTracker CriticalPendingTracker = new PendingTracker();
        public static Pending CreatePending(string comment = null)
        {
            return CommonPendingTracker.CreatePending(comment);
        }
        
        /// <summary>
        /// Crate critical pending that should be trigger for displaying loading screen.
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        public static Pending CreateCriticalPending(string comment = null)
        {
            return CriticalPendingTracker.CreatePending(comment);
        }
        
        public static DisposableAction WatchPending(Action<bool> handler)
        {
            return CommonPendingTracker.WatchPending(handler);
        }
        
        public static DisposableAction WatchCriticalPending(Action<bool> handler)
        {
            return CriticalPendingTracker.WatchPending(handler);
        }
        
        public static DisposableAction WatchPendingStatsAsArray(Action<PendingStats[]> handler)
        {
            return CommonPendingTracker.WatchPendingStatsAsArray(handler);
        }
        
        public static DisposableAction WatchCriticalPendingStatsAsArray(Action<PendingStats[]> handler)
        {
            return CriticalPendingTracker.WatchPendingStatsAsArray(handler);
        }
    }
}