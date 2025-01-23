using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    /// <summary>
    /// Allows to add items with weights and get the best one. You can pass Loop to transfer control of the update to specific loop.
    /// </summary>
    /// <note>
    /// Don't be misled by the word "competitive". It's not about multi threads competition, it's about the best item selection by dynamic weight. Collection is not thread safe.
    /// </note>
    /// <typeparam name="T"></typeparam>
    public class ActiveWeightCompetitionCollection<T>: IDisposable
    {
        private readonly Dictionary<string, (T, Func<float>)> m_Items = new();
        private Observable<T> m_Best = new();
        private ILoop m_Loop;
        public int count => m_Items.Count;
        private event Action OnDisposed;

        public ActiveWeightCompetitionCollection()
        {
            
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="loop">Custom loop (if null it uses default UniTaskLoop) </param>
        public ActiveWeightCompetitionCollection(Func<T, Signal, UniTaskVoid> action = null, Loop loop = null)
        {
            m_Loop = loop == null ? new UniTaskLoop() : loop;
            OnDisposed += m_Loop.AddAction(Update);
            if (action != null)
            {
                OnDisposed += m_Best.WatchAsync(action);
            }
        }
        
        ~ActiveWeightCompetitionCollection()
        {
            Console.Error("CompetitiveWeightsCollection", "Was not disposed");
        }

        public DisposableAction Add(T item, Func<float> getWeight)
        {
            var id = item.GetHashCode().ToString();
            m_Items.Add(id, (item, getWeight));
            Update();
            return new DisposableAction(() =>
            {
                m_Items.Remove(id);
                Update();
            });
        }
        
        public void Update()
        {
            var best = default(T);
            var bestWeight = float.MinValue;
            foreach (var (item, getWeight) in m_Items.Values)
            {
                var weight = getWeight();
                if (weight > bestWeight)
                {
                    best = item;
                    bestWeight = weight;
                }
            }
            m_Best.SetUniqValue(best);
        }
        
        public DisposableAction WatchBest(Action<T> action)
        {
            return m_Best.Watch(action);
        }
        
        public DisposableAction WatchAsyncBest(Func<T, Signal, UniTaskVoid> action)
        {
            return m_Best.WatchAsync(action);
        }

        public void Dispose()
        {
            OnDisposed?.Invoke();
            GC.SuppressFinalize(this);
        }
    }
}