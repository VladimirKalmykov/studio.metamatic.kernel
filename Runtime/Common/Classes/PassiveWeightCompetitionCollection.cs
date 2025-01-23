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
    public class PassiveWeightCompetitionCollection<T>: IDisposable
    {
        private readonly Dictionary<string, (T, Func<float>)> m_Items = new();
        private readonly Observable<T> m_Best = new();
        public int count => m_Items.Count;
        private event Action m_OnEmpty;
        private event Action m_OnPopulated;

        public DisposableAction Add(T item, Func<float> getWeight)
        {
            var id = item.GetHashCode().ToString();
            m_Items.Add(id, (item, getWeight));
            Update();
            if (m_Items.Count == 1)
            {
                m_OnPopulated?.Invoke();
            }
            return new DisposableAction(() =>
            {
                m_Items.Remove(id);
                Update();
                if (m_Items.Count == 0)
                {
                    m_OnEmpty?.Invoke();
                }
            });
        }
        
        public T GetBest()
        {
            Update();
            return m_Best.Value;
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

        /// <summary>
        /// Watches the population of the collection. It triggers the action when the collection is populated and raise signal when it is empty.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public DisposableAction WatchPopulationAsync(Func<Signal, UniTaskVoid> action)
        {
            var signalSource = SignalSource.Create();
            m_OnPopulated += OnPopulated;
            m_OnEmpty += OnEmpty;
            
            return new DisposableAction(() =>
            {
                m_OnPopulated -= OnPopulated;
                m_OnEmpty -= OnEmpty;
                signalSource.Dispose();
            });
            
            void OnPopulated()
            {
                action(signalSource.Signal).Forget();
            }
            
            void OnEmpty()
            {
                signalSource.TryRaise();
            }
        }
        

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}