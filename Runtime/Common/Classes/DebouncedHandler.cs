using System;

namespace Metamatic
{
    public class DebouncedHandler<T>: IDisposable
    {
        private float m_Delay;
        private Action<T> m_Action;
        private System.Threading.CancellationTokenSource m_CancellationTokenSource;
        public DebouncedHandler(float delay, Action<T> action)
        {
            m_Delay = delay;
            m_Action = action;
        }
        
        public void Handle(T value)
        {
            m_CancellationTokenSource?.Cancel();
            m_CancellationTokenSource = new System.Threading.CancellationTokenSource();
            AsyncUtils.ExecuteDelayed(() =>
            {
                m_Action(value);
            }, m_Delay, m_CancellationTokenSource.Token);
            m_Action(value);
        }
        
        public void Dispose()
        {
            m_CancellationTokenSource?.Cancel();
        }
    }
    
    public class DebouncedHandler: IDisposable
    {
        private float m_Delay;
        private Action m_Action;
        private System.Threading.CancellationTokenSource m_CancellationTokenSource;
        public DebouncedHandler(float delay, Action action)
        {
            m_Delay = delay;
            m_Action = action;
        }
        
        public void Handle()
        {
            m_CancellationTokenSource?.Cancel();
            m_CancellationTokenSource = new System.Threading.CancellationTokenSource();
            AsyncUtils.ExecuteDelayed(() =>
            {
                m_Action();
            }, m_Delay, m_CancellationTokenSource.Token);
        }
        
        public void Dispose()
        {
            m_CancellationTokenSource?.Cancel();
        }
    }
    
    public class DebouncedHandler<T1, T2>: IDisposable
    {
        private float m_Delay;
        private Action<T1, T2> m_Action;
        private System.Threading.CancellationTokenSource m_CancellationTokenSource;
        public DebouncedHandler(float delay, Action<T1, T2> action)
        {
            m_Delay = delay;
            m_Action = action;
        }
        
        public void Handle(T1 value1, T2 value2)
        {
            m_CancellationTokenSource?.Cancel();
            m_CancellationTokenSource = new System.Threading.CancellationTokenSource();
            AsyncUtils.ExecuteDelayed(() =>
            {
                m_Action(value1, value2);
            }, m_Delay, m_CancellationTokenSource.Token);
        }
        
        public void Dispose()
        {
            m_CancellationTokenSource?.Cancel();
        }
    }
}