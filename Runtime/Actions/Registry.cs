using System;

namespace Metamatic.Actions
{
    public class RegistrySource<T>: IDisposable
    where T : class
    {
        public class Token {}
        public T Subject { get; private set; } = null;
        public event System.Action OnRelease;
        public event System.Action<T> OnChange;
        public Registry<T> Registry => new Registry<T>(this);
        private bool m_IsDisposed;
        private Token m_ValueToken = null;
        public RegistrySource()
        {
        }
        
        public Token Register(T subject)
        {
            Subject = subject;
            OnChange?.Invoke(subject);
            m_ValueToken = new Token();
            return m_ValueToken;
        }

        public bool Release(Token token)
        {
            if (m_ValueToken != token) return false;
            Subject = null;
            OnChange?.Invoke(null);
            m_ValueToken = null;
            return true;
        }
        
        /// <summary>
        /// Check if the registry source is empty and ready to be disposed
        /// </summary>
        /// <returns></returns>
        public bool isEmpty()
        {
            return Subject == null && OnChange == null;
        }

        ~RegistrySource()
        {
            Dispose();
        }
        
        public System.Action Subscribe(System.Action<T> handler)
        {
            OnChange += handler;
            handler(Subject);
            return () =>
            {
                OnChange -= handler;
            };
        }

        public void Dispose()
        {
            if (m_IsDisposed) return;
            m_IsDisposed = true;
            OnChange?.Invoke(null);
            OnRelease?.Invoke();
            OnChange = null;
            OnRelease = null;
            GC.SuppressFinalize(this);
        }
    }
    public struct Registry<T>: IDisposable
    where T : class
    {
        public T subject => m_Source == null ? default : m_Source.Subject;
        public bool isDisposed => m_Source == null;
        private RegistrySource<T> m_Source;

        public Registry(RegistrySource<T> source)
        {
            m_Source = source;
            m_Source.OnRelease += Release;
        }
        
        private void Release()
        {
            m_Source.OnRelease -= Release;
            m_Source = default;
        }

        public void Dispose()
        {
            if (m_Source == null) return;
            Release();
        }
    }
}