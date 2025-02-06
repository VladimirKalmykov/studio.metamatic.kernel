using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public readonly struct AsyncSummonObsolete<T>: IPolyDisposable where T: MonoBehaviour
    {
        private readonly Guid m_InstanceId;
        private readonly AsyncSummonerObsolete<T> m_AsyncSummonerObsolete;
        
        public UniTask<T> instance => m_AsyncSummonerObsolete.GetInstance(m_InstanceId);
        
        public AsyncSummonObsolete(AsyncSummonerObsolete<T> asyncSummonerObsolete, Guid instanceId)
        {
            m_AsyncSummonerObsolete = asyncSummonerObsolete;
            m_InstanceId = instanceId;
        }

        public void Dispose()
        {
            m_AsyncSummonerObsolete.Banish(m_InstanceId, true).Forget();
        }

        public UniTask DisposeAsync()
        {
            return m_AsyncSummonerObsolete.Banish(m_InstanceId);
        }
    }
    public abstract class AsyncSummonerObsolete<T> where T: MonoBehaviour
    {
        private readonly Dictionary<Guid, (SignalSource cancellationSignalSource, SignalSource completeSignal, T instance, Exception exception)> m_Instances = new ();
        private readonly Dictionary<Guid, SignalSource> m_AbortSignalSources = new ();
        public AsyncSummonObsolete<T> Summon()
        {
            var cancellationSignalSource = SignalSource.Create();
            var completeSignalSource = SignalSource.Create();
            var task = OnSummon(cancellationSignalSource.Signal);
            var instanceId = Guid.NewGuid();
            m_Instances.Add(instanceId, (cancellationSignalSource, completeSignalSource, default, null));
            InternalAwaitInstance(instanceId, task, cancellationSignalSource, completeSignalSource).Forget();
            return new AsyncSummonObsolete<T>(this, instanceId);
        }

        public async UniTask Banish(Guid guid, bool sync = false)
        {
            if (m_Instances.Remove(guid, out var data))
            {
                if (sync)
                {
                    data.cancellationSignalSource.TryRaise();
                }
                if (data.completeSignal.IsRaised && data.exception == null)
                {
                    m_AbortSignalSources[guid] = data.cancellationSignalSource;
                    try
                    {
                        await OnBanish(data.instance, data.cancellationSignalSource.Signal);
                    }
                    catch (OperationCanceledException)
                    {
                        // Ignore
                    }
                    catch (Exception e)
                    {
                        Console.Exception("Summoner", e);
                    }
                    finally
                    {
                        m_AbortSignalSources.Remove(guid);
                    }
                } else {
                    data.cancellationSignalSource.TryRaise();
                }
            } else if (m_AbortSignalSources.TryGetValue(guid, out var abortSignalSource))
            {
                abortSignalSource.TryRaise();
            }
        }

        private async UniTaskVoid InternalAwaitInstance(Guid instanceId, UniTask<T> task, SignalSource cancellationSignalSource, SignalSource completeSignalSource)
        {
            try
            {
                var instance = await task;
                m_Instances[instanceId] = (cancellationSignalSource, completeSignalSource, instance, null);
            }
            catch (Exception e)
            {
                m_Instances[instanceId] = (cancellationSignalSource, completeSignalSource, default, e);
            }
            finally
            {
                completeSignalSource.TryRaise();
            }
        }
        
        public async UniTask<T> GetInstance(Guid instanceId)
        {
            (SignalSource cancellationSignalSource, SignalSource completeSource, T instance, Exception exception) data = m_Instances[instanceId];
            if (data.completeSource.IsRaised)
            {
                if (data.exception != null)
                {
                    throw data.exception;
                }
                return data.instance;
            }

            await data.completeSource.Signal;

            var instance = await GetInstance(instanceId);
            return instance;
        }

        public abstract UniTask<T> OnSummon(Signal cancellationSignal);
        public abstract UniTask OnBanish(T instance, Signal abortSignal);
    }
}