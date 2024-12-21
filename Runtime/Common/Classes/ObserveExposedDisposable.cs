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
    public class ObserveExposedDisposable: IDisposable, IExposedDisposedSync
    {
        private static ParamLessPool<ObserveExposedDisposable> _Pool = new(10);
        private bool m_IsDisposed;
        public bool isDisposedSync => m_IsDisposed;
        public event Action OnDisposedSync;
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
            m_OnFire = onFire;
            foreach (var d in disposables)
            {
                if (d.Equals(null))
                {
                    Fire();
                    break;
                }
                if (d is IAbortable uniTaskAsyncDisposableAbortable)
                {
                    if (uniTaskAsyncDisposableAbortable.isAborted)
                    {
                        Fire();
                        break;
                    }
                    uniTaskAsyncDisposableAbortable.OnAborted += Fire;
                    OnDisposedSync += () =>
                    {
                        if (!uniTaskAsyncDisposableAbortable.Equals(null))
                        {
                            uniTaskAsyncDisposableAbortable.OnAborted -= Fire;
                        }
                    };
                }
                if (d is IExposedDisposedAsync exposedAsyncDisposed)
                {
                    /* If the object supports async disposal, we should fire only on async dispose event, unlike the sync dispose event in IExposedDisposed */
                    if (exposedAsyncDisposed.isDisposedAsync)
                    {
                        Fire();
                        break;
                    }
                    exposedAsyncDisposed.OnDisposedAsync += FireAsync;
                    OnDisposedSync += () =>
                    {
                        if (!exposedAsyncDisposed.Equals(null))
                        {
                            exposedAsyncDisposed.OnDisposedAsync -= FireAsync;
                        }
                    };
                } else if (d is IExposedDisposedSync exposedDisposed)
                {
                    if (exposedDisposed.isDisposedSync)
                    {
                        Fire();
                        break;
                    }
                    exposedDisposed.OnDisposedSync += Fire;
                    OnDisposedSync += () =>
                    {
                        if (!exposedDisposed.Equals(null))
                        {
                            exposedDisposed.OnDisposedSync -= Fire;
                        }
                    };
                }
                else
                {
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
                        Debug.Log($"[MICRO-OPERATION] ObserveNotExposed: Fire because of null");
                        Fire();
                        return true;
                    }
                }
                return m_IsDisposed;
            }, PlayerLoopTiming.EarlyUpdate);
        }

        private void Fire()
        {
            m_OnFire?.Invoke();
            m_OnFire = null;
            Dispose();
        }

        private void FireAsync(Exception exception)
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
            OnDisposedSync?.Invoke();
            OnDisposedSync = null;
            m_NotExposedDisposedDependencies.Clear();
            _Pool.Return(this);
        }
        
        public static implicit operator Action(ObserveExposedDisposable self) => self.Dispose;
    }
}