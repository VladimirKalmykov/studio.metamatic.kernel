using System;

namespace Metamatic
{
    /// <summary>
    /// Allows to combine two handlers into one
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    public readonly struct CombineHandlers<T1, T2>: IDisposable
    {
        private readonly CombineHandlersImpl<T1, T2> m_Impl;
        private readonly int version;
        public CombineHandlers(Action<T1, T2> handler, bool waitBoth = true)
        {
            var impl = CombineHandlersImpl<T1, T2>.Get();
            impl.Handler = handler;
            impl.WaitBoth = waitBoth;
            version = impl.version;
            m_Impl = impl;
        }

        public void Handler1(T1 value)
        {
            if (m_Impl?.version == version)
            {
                m_Impl.SetValue1(value);
            }
        }

        public void Handler2(T2 value)
        {
            if (m_Impl?.version == version)
            {
                m_Impl.SetValue2(value);
            }
        }

        public void Dispose()
        {
            if (m_Impl != null && m_Impl.version == version)
            {
                CombineHandlersImpl<T1, T2>.Return(m_Impl);
            }
        }
    }

    internal class CombineHandlersImpl<T1, T2>: IResettable
    {
        private T1 m_Value1;
        private T2 m_Value2;
        private bool m_Value1Set;
        private bool m_Value2Set;
        public Action<T1, T2> Handler;
        public bool WaitBoth;
        public static CombineHandlersImpl<T1, T2> Get()
        {
            return ParamLessPool<CombineHandlersImpl<T1, T2>>.GetShared();
        }

        public static void Return(CombineHandlersImpl<T1, T2> instance)
        {
            ParamLessPool<CombineHandlersImpl<T1, T2>>.ReturnShared(instance);
        }

        public void SetValue1(T1 value)
        {
            m_Value1 = value;
            m_Value1Set = true;
            if (!WaitBoth || (m_Value1Set && m_Value2Set))
            {
                Handler?.Invoke(m_Value1, m_Value2);
            }
        }

        public void SetValue2(T2 value)
        {
            m_Value2 = value;
            m_Value2Set = true;
            if (!WaitBoth || (m_Value1Set && m_Value2Set))
            {
                Handler?.Invoke(m_Value1, m_Value2);
            }
        }

        public int version { get; private set; } = 1;
        public void Reset()
        {
            unchecked
            {
                version++;
            }

            Handler = null;
            m_Value1Set = false;
            m_Value2Set = false;
            m_Value1 = default;
            m_Value2 = default;
        }
    }
}