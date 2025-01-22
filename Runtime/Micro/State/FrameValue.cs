using System;
using UnityEngine;

namespace Metamatic
{
    /// <summary>
    /// Allows to cache the value for the frame. The value is updated only once per frame.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FrameValue<T>
    {
        private int m_LastFrameId = -1;
        private T m_CachedValue;

        public Func<T> getter { get; set; }
        public T value => GetValue();

        private static T DefaultGetter() => default;

        public FrameValue(Func<T> getter)
        {
            this.getter = getter;
        }

        public FrameValue()
        {
            getter = DefaultGetter;
        }

        private T GetValue()
        {
            // Get Unity frame Id
            var frameId = Time.frameCount;
            if (m_LastFrameId != frameId)
            {
                m_LastFrameId = frameId;
                m_CachedValue = getter();
            }
            return m_CachedValue;
        }
        
        public void Reset()
        {
            m_LastFrameId = -1;
            m_CachedValue = default;
            getter = DefaultGetter;
        }
        
        public static implicit operator T(FrameValue<T> frameValue)
        {
            return frameValue.value;
        }
        
        
        public static explicit operator FrameValue<T>(Func<T> value)
        {
            return new FrameValue<T>(value);
        }
    }
}