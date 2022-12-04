using System;
using System.Collections.Generic;

namespace Metamatic
{
    public class Bank
    {
        
    }

    public class Contest<T>: IDisposable
    where T: ICompetitive
    {
        readonly struct SubscriptionKeeper
        {
            public readonly Action subcription;
            public readonly T value;
            public SubscriptionKeeper(Action subscription, T value) {
                this.subcription = subscription;
                this.value = value;
            }
        }
        private readonly List<T> m_Competitives = new List<T>();
        private Dictionary<T, Action> m_Subscriptions = new Dictionary<T, Action>();

        public delegate void SupplyHandler(T value);
        private readonly SupplyHandler m_Handler;
        private T m_BestSupply;
        public event Action OnDispose;

        public Contest(SupplyHandler handler)
        {
            this.m_Handler = handler;
        }

        public void Add(T offer)
        {
            m_Competitives.Add(offer);
            Action unsubscribe = null;
            unsubscribe = offer.SubscribeDispose(() =>
            {
                Remove(offer);
            });
            OnDispose += unsubscribe;
            m_Subscriptions[offer] = unsubscribe;
            UpdateSort();
        }
        
        /// <summary>
        /// Remove competitive element from the list. This operation also recalculate priority selection
        /// </summary>
        /// <param name="offer"></param>
        /// <exception cref="Exception"></exception>
        public void Remove(T offer)
        {
            if (!m_Competitives.Contains(offer))
            {
                throw new Exception("Supply doesn't belong to bank");
            }
            
            /* Unsubscribe dispose */
            OnDispose -= m_Subscriptions[offer];

            m_Competitives.Remove(offer);
            UpdateSort();
        }

        private void UpdateSort()
        {
            m_Competitives.Sort(SortSupply);
            var nextSupply = m_Competitives.Count > 0 ? m_Competitives[^1] : default;
            m_BestSupply = nextSupply;
            m_Handler(nextSupply);
        }

        private int SortSupply(T a, T b)
        {
            if (a.priority == b.priority)
            {
                return 0;
            }
            
            return a.priority > b.priority ? 1 : -1;
        }

        public void Dispose()
        {
            OnDispose?.Invoke();
        }
    }
}