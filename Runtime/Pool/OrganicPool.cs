using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    /// <summary>
    /// Organic pool is a kind of UnityObjectPool that has a lifetime. When the lifetime is reached, the pool will start to remove objects from the pool until the pool size is less than or equal to the capacity.
    /// Organic pool has to public constructor, to create organic pool, use Pool.GetComponentPool or Pool.GetGameObjectPool.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class OrganicPool<T>: UnityObjectPool<T> where T: UnityEngine.Object {
        
        private CancellationTokenSource m_LifetimeCancellationTokenSource = new ();
        private float m_Lifetime;
        private int m_InquiryCount;
        private int m_CountInPoolAtCountdownStart = 0;
        private bool m_IsCountingDown = false;
        
        internal OrganicPool(Func<T> onCreate, Action<T> onGet = null, Action<T> onRelease = null, Action<T> onDestroy = null, int capacity = 0, float lifetime = 30f) : base(onCreate, onGet, onRelease, onDestroy, capacity)
        {   
            m_Lifetime = lifetime;
        }
        
        public DisposableAction Inquire(string id)
        {
            m_InquiryCount++;
            bool disposed = false;
            return new DisposableAction(() =>
            {
                if (disposed) return;
                disposed = true;
                m_InquiryCount--;
                ReleaseTemporaryConfig(id, m_InquiryCount == 0);
                if (m_InquiryCount == 0)
                {
                    m_LifetimeCancellationTokenSource.Cancel();
                    ReadyToRelease();
                }
            });
        }
        
        public override T Get()
        {
            m_CountInPoolAtCountdownStart--;
            T obj = base.Get();
            return obj;
        }
        
        public override void Return(T obj)
        {
            m_CountInPoolAtCountdownStart++;
            base.Return(obj);
            /* Start the lifetime countdown */
            StartCountdown();
        }
        
        private async void StartCountdown()
        {
            if (m_IsCountingDown) return;
            m_CountInPoolAtCountdownStart = objectsInPool.Count;
            m_IsCountingDown = true;
            var shouldRestart = false;
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(m_Lifetime),
                    cancellationToken: m_LifetimeCancellationTokenSource.Token);
                if (objectsInPool.Count > capacity)
                {
                    if (m_CountInPoolAtCountdownStart <= 0) return;
                    var countToDelete = Mathf.Min(objectsInPool.Count - capacity, m_CountInPoolAtCountdownStart);
                    /* Remove countToDelete objects from the pool */
                    var countToDeleteClamped = Mathf.Clamp(countToDelete, 0, objectsInPool.Count - capacity);
                    for (int i = 0; i < countToDeleteClamped; i++)
                    {
                        T obj = objectsInPool.Pop();
                        Destroy(obj);
                    }

                    if (objectsInPool.Count > capacity)
                    {
                        /* Restart the countdown */
                        shouldRestart = true;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                /* Ignore */
            }
            catch (Exception e)
            {
                Debug.LogError($"Countdown error: {e.Message}");
                Debug.LogError(e);
            }
            finally
            {
                m_IsCountingDown = false;
                if (shouldRestart)
                {
                    StartCountdown();
                }
            }
        }
    }
}