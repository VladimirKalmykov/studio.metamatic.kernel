using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    /// <summary>
    /// Class for observing the disposal of the exposed and not exposed disposables.
    /// Objects that implement IExposedDisposed will be listening for disposal.
    /// Objects that do not implement IExposedDisposed will be observed every frame until they are disposed.
    /// Try to avoid using not exposed disposables, because it will be observed every frame.
    /// </summary>
    public class ObserveExposedDisposable: IDisposable, IExposedDisposed
    {
        private Action m_OnFire;
        private readonly object[] m_NotExposedDisposedDependencies;
        public ObserveExposedDisposable(object[] disposables, Action onFire, bool trackNotExposed = true)
        {
            m_NotExposedDisposedDependencies = new object[disposables.Length];
            var notExposedCount = 0;
            m_OnFire = onFire;
            foreach (var d in disposables)
            {
                if (d.Equals(null))
                {
                    Fire();
                    break;
                }
                if (d is IExposedDisposed exposedDisposed)
                {
                    if (exposedDisposed.isDisposed)
                    {
                        Fire();
                        break;
                    }
                    exposedDisposed.OnDisposed += Fire;
                    OnDisposed += () =>
                    {
                        if (!exposedDisposed.Equals(null))
                        {
                            exposedDisposed.OnDisposed -= Fire;
                        }
                    };
                }
                else
                {
                    notExposedCount++;
                    m_NotExposedDisposedDependencies[m_NotExposedDisposedDependencies.Length] = d;
                }
            }
            
            Array.Resize(ref m_NotExposedDisposedDependencies, notExposedCount);

            if (trackNotExposed)
            {
                ObserveNotExposed().Forget();
            }
        }

        private async UniTaskVoid ObserveNotExposed()
        {
            await UniTask.WaitUntil(() =>
            {
                foreach (var d in m_NotExposedDisposedDependencies)
                {
                    if (d == null || d.Equals(null))
                    {
                        Fire();
                        return true;
                    }
                }
                return m_IsDisposed;
            });
        }

        private void Fire()
        {
            m_OnFire?.Invoke();
            m_OnFire = null;
            Dispose();
        }
        
        public void Dispose()
        {
            m_IsDisposed = true;
            OnDisposed?.Invoke();
        }
        
        private bool m_IsDisposed;
        public bool isDisposed => m_IsDisposed;
        public event Action OnDisposed;
        
        public static implicit operator Action(ObserveExposedDisposable self) => self.Dispose;
    }
}