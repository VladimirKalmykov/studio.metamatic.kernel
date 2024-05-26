using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic.Actions
{
    public class RegistrySource<T>: IDisposable
    where T : notnull
    {
        public class Token {}

        public T value { get; private set; } = default;
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
            value = subject;
            OnChange?.Invoke(subject);
            m_ValueToken = new Token();
            return m_ValueToken;
        }

        public bool Release(Token token)
        {
            if (m_ValueToken != token) return false;
            value = default;
            OnChange?.Invoke(default);
            m_ValueToken = null;
            return true;
        }
        
        /// <summary>
        /// Check if the registry source is empty and ready to be disposed
        /// </summary>
        /// <returns></returns>
        public bool isEmpty()
        {
            return value == null && OnChange == null;
        }

        ~RegistrySource()
        {
            Dispose();
        }
        
        public System.Action Subscribe(System.Action<T> handler, bool skipInitial = false)
        {
            OnChange += handler;
            if (!skipInitial && value != null)
                handler(value);
            return () =>
            {
                OnChange -= handler;
            };
        }
        
        public UniTask<T> WhenNotNull(CancellationToken cancellationToken)
        {
            if (value != null) return UniTask.FromResult(value);
            var tcs = new UniTaskCompletionSource<T>();
            System.Action<T> handler = null;
            CancellationTokenRegistration registration = default;
            handler = (v) =>
            {
                registration.Dispose();
                OnChange -= handler;
                tcs.TrySetResult(v);
            };
            OnChange += handler;
            registration = cancellationToken.Register(() =>
            {
                OnChange -= handler;
                tcs.TrySetCanceled();
            });
            return tcs.Task;
        }

        public void Dispose()
        {
            if (m_IsDisposed) return;
            m_IsDisposed = true;
            OnChange?.Invoke(default);
            OnRelease?.Invoke();
            OnChange = null;
            OnRelease = null;
            GC.SuppressFinalize(this);
        }
    }
    public struct Registry<T>: IDisposable
    where T : notnull
    {
        public T subject => m_Source == null ? default : m_Source.value;
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