using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

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
        private static ParamLessPool<ObserveExposedDisposable> _Pool = new(10);
        private bool m_IsDisposed;
        public bool isDisposed => m_IsDisposed;
        public event Action OnDisposed;
        public static ObserveExposedDisposable Create(object[] disposables, Action onFire, bool trackNotExposed = true)
        {
            var instance = _Pool.Get();
            instance.m_IsDisposed = false;
            instance.Initialize(disposables, onFire, trackNotExposed);
            return instance;
        }
        private Action m_OnFire;
        private readonly List<object> m_NotExposedDisposedDependencies = new(5);
        public void Initialize(object[] disposables, Action onFire, bool trackNotExposed = true)
        {
            m_NotExposedDisposedDependencies.Clear();
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
                    m_NotExposedDisposedDependencies.Add(d);
                }
            }

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
            if (m_IsDisposed)
            {
                return;
            }
            m_IsDisposed = true;
            OnDisposed?.Invoke();
            OnDisposed = null;
            m_NotExposedDisposedDependencies.Clear();
            _Pool.Return(this);
        }
        
        public static implicit operator Action(ObserveExposedDisposable self) => self.Dispose;
    }
}