using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public class EventLoop<T>
    where T: Enum
    {
        private readonly Loop[] m_Loops;

        public EventLoop(Loop.TimeSource timeSource = null)
        {
            m_Loops = new Loop[Enum.GetValues(typeof(T)).Length];
            
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
            GetLoop(eventType).Update();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask WaitUntil(T eventType, Func<float, bool> action)
        {
            return GetLoop(eventType).WaitUntil(action);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask WaitWhile(T eventType, Func<float, bool> action)
        {
            return GetLoop(eventType).WaitWhile(action);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DisposableAction AddAction(T eventType, Action<float> action)
        {
            return GetLoop(eventType).AddAction(action);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Loop.YieldAwaitable Yield(T eventType)
        {
            return GetLoop(eventType).Yield();
        }
    }
}