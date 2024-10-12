using System;
using System.Runtime.CompilerServices;

namespace Metamatic
{
    /// <summary>
    /// Polymorphic time source.
    /// </summary>
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class TimeSource
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float DefaultTimeGetter()
        {
            return UnityEngine.Time.time;
        }
        public virtual float time => m_TimeGetter();
            
        private readonly Func<float> m_TimeGetter;
            
        public TimeSource(Func<float> timeGetter = null)
        {
            m_TimeGetter = timeGetter ?? DefaultTimeGetter;
        }
    }
}