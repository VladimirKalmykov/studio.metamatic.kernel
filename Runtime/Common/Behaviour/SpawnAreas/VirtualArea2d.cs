using System;
using System.Collections.Generic;
using UnityEngine;

namespace Metamatic
{
    public abstract class VirtualArea2d: VirtualArea
    {
        public class Activator
        {
            private event Action OnIntersection;
            private readonly Func<Vector2, bool> m_Contains;
            public Activator(Func<Vector2, bool> contains)
            {
                m_Contains = contains;
            }
            public DisposableAction SubscribeIntersections(Action action)
            {
                OnIntersection += action;
                action();
                return new DisposableAction(() =>
                {
                    OnIntersection -= action;
                });
            }

            public void TriggerIntersection()
            {
                OnIntersection?.Invoke();
            }
            
            /// <summary>
            /// Returns if the position is inside the activator's area
            /// </summary>
            /// <param name="position"></param>
            /// <returns></returns>
            public bool Contains(Vector2 position)
            {
                return m_Contains(position);
            }
        }
        public event Action<Activator> onActivated;
        public event Action<Activator> onDeactivated;
        
        private List<Activator> m_Activators = new List<Activator>();
        public bool overlapped => m_Activators.Count > 0;
        /// <summary>
        /// Area size of the shape of the area
        /// </summary>
        public abstract float areaSize { get; }
        /// <summary>
        /// Get random position on the area
        /// </summary>
        /// <returns></returns>
        public abstract Vector2 GetRandomPosition(bool local = false);
        /// <summary>
        /// Get random position on the area that is inside the given rect
        /// </summary>
        /// <param name="otherRect"></param>
        /// <returns></returns>
        public abstract Vector2 GetRandomPosition(Rect otherRect);
        /// <summary>
        /// If the area overlaps with the given rect
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public abstract bool Overlaps(Rect rect);
        public abstract float OverlapsAreaSize(Rect rect);
        /// <summary>
        /// Activate area with activator
        /// </summary>
        /// <param name="activator"></param>
        /// <returns></returns>
        public DisposableAction Activate(Activator activator)
        {
            m_Activators.Add(activator);
            onActivated?.Invoke(activator);
            return new DisposableAction(() => Deactivate(activator));
        }
        /// <summary>
        /// Disable area with activator
        /// </summary>
        /// <param name="activator"></param>
        public void Deactivate(Activator activator)
        {
            #if DEBUG
            if (!m_Activators.Contains(activator))
            {
                throw new System.Exception("Activator is not activated for this area.");
            }
            #endif
            onDeactivated?.Invoke(activator);
            m_Activators.Remove(activator);
        }
        public DisposableAction SubscribeActivated(Action<Activator> action)
        {
            onActivated += action;
            return new DisposableAction(() => onActivated -= action);
        }
        public DisposableAction SubscribeDeactivated(Action<Activator> action)
        {
            onDeactivated += action;
            return new DisposableAction(() => onDeactivated -= action);
        }
    }
}