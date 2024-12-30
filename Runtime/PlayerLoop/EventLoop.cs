using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public class EventLoop<T>: IEventLoop<T>
    where T: Enum
    {
        private readonly Loop[] m_Loops;
        private readonly TimeSource m_TimeSource;

        public EventLoop(TimeSource timeSource = null)
        {
            m_Loops = new Loop[Enum.GetValues(typeof(T)).Length];
            m_TimeSource = timeSource ?? new TimeSource();
            
            // Fill the array with loops
            for (var i = 0; i < m_Loops.Length; i++)
            {
                m_Loops[i] = new Loop(timeSource);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Loop GetLoop(T eventType)
        {
            return m_Loops[(int) (object) eventType];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Run(T eventType)
        {
            GetLoop(eventType).Run();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask WaitUntil(T eventType, Func<bool> action, Signal cancellationSignal = default)
        {
            return GetLoop(eventType).WaitUntil(action, cancellationSignal);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask WaitWhile(T eventType, Func<bool> action, Signal cancellationSignal = default)
        {
            return GetLoop(eventType).WaitWhile(action, cancellationSignal);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DisposableAction AddAction(T eventType, Action action)
        {
            return GetLoop(eventType).AddAction(action);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Loop.YieldAwaitable Yield(T eventType)
        {
            return GetLoop(eventType).Yield();
        }

        public UniTask Delay(T eventType, float seconds, Signal cancellationSignal = default)
        {
            return GetLoop(eventType).Delay(seconds, cancellationSignal);
        }
        
        public UniTask Delay(T eventType, TimeSpan timeSpan, Signal cancellationSignal = default)
        {
            return GetLoop(eventType).Delay(timeSpan, cancellationSignal);
        }

        public float time => m_TimeSource.time;

        #region Loop control
        /// <summary>
        /// Run all events in the single loop
        /// </summary>
        public void RunCycle()
        {
            foreach (var loop in m_Loops)
            {
                loop.Run();
            }
        }
        #endregion
    }
}