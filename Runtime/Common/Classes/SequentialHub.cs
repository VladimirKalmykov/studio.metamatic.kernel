using System;
using System.Threading;

namespace Metamatic
{
    public class SequentialHub : IDisposable
    {
        protected CancellationTokenSource m_CancellationTokenSource;
        private bool m_Disposed;

        ~SequentialHub()
        {
            Dispose();
        }
        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_CancellationTokenSource?.Cancel();
            GC.SuppressFinalize(this);
        }
    }
    public class SequentialHub<T1, T2>: SequentialHub
    {
        public delegate void Handler(T1 value1, T2 value2, CancellationToken cancellationToken);
        private Handler m_Handler;
        private bool m_Latch;
        private T1 m_Value1;
        private T2 m_Value2;
        private bool m_Value1Set;
        private bool m_Value2Set;
        
        public SequentialHub(Handler handler, bool latch = true): base()
        {
            m_Handler = handler;
            m_Latch = latch;
        }
        public void Handle1(T1 obj)
        {
            m_Value1 = obj;
            m_Value1Set = true;
            CheckCompletion();
        }
        
        public void Handle2(T2 obj)
        {
            m_Value2 = obj;
            m_Value2Set = true;
            CheckCompletion();
        }
        
        private void CheckCompletion()
        {
            if (!m_Latch || (m_Value1Set && m_Value2Set))
            {
                m_CancellationTokenSource?.Cancel();
                m_CancellationTokenSource = new CancellationTokenSource();
                m_Handler(m_Value1, m_Value2, m_CancellationTokenSource.Token);
            }
        }
    }
}