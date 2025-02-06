using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metamatic.SocketSystem
{
    public abstract class SocketComponent : MonoBehaviour
    {
    }

    public abstract class SocketComponent<T> : SocketComponent
    {
        private static readonly ObservableList<SocketComponent<T>> m_Sockets = new();
        private SignalSource m_LifecycleSignalSource;
        private SignalSource m_DisconnectSignalSource;
        private readonly List<PlugRequest> m_ConnectRequests = new();

        public static DisposableAction WatchEachLifecycle(Func<SocketComponent<T>, int, Signal, UniTaskVoid> handler)
        {
            return m_Sockets.WatchEachLifecycle(handler);
        }

        public delegate UniTask Plug(T property, Signal cancellationSignal);

        public readonly struct PlugRequest
        {
            public readonly Plug Plug;
            public readonly float Weight;
            public readonly SignalSource RemoveSignalSource;

            public PlugRequest(Plug plug, float weight, SignalSource removeSignalSource)
            {
                Plug = plug;
                Weight = weight;
                RemoveSignalSource = removeSignalSource;
            }

            public override int GetHashCode() => Plug.GetHashCode();
        }

        [ShowInInspector, ReadOnly]
        public PlugRequest connectedPlug { get; private set; }

        public void OnEnable()
        {
            m_Sockets.Add(this);
            m_LifecycleSignalSource = SignalSource.Create();
        }

        public void OnDisable()
        {
            m_Sockets.Remove(this);

            m_LifecycleSignalSource.TryRaise();
            m_LifecycleSignalSource = default;
            
            if (connectedPlug.Plug != null)
            {
                m_DisconnectSignalSource.TryRaise();
                connectedPlug = default;
                m_DisconnectSignalSource = default;
            }

            // Отменяем все запросы в очереди
            foreach (var request in m_ConnectRequests)
            {
                request.RemoveSignalSource.TryRaise();
            }
            m_ConnectRequests.Clear();
        }

        public DisposableAction TryConnect(Plug plug, float weight = 0f)
        {
            if (!isActiveAndEnabled)
                return DisposableAction.Empty;

#if DEBUG
            if (connectedPlug.Plug == plug)
            {
                Debug.LogError($"Plug {plug} is already connected.");
                return DisposableAction.Empty;
            }
#endif
            bool allowToConnect = connectedPlug.Plug == null || weight > connectedPlug.Weight;
            var removeSignalSource = SignalSource.Create();
            var plugRequest = new PlugRequest(plug, weight, removeSignalSource);

            if (allowToConnect)
            {
                InternalConnect(plugRequest).Forget();
            }
            else
            {
                m_ConnectRequests.Add(plugRequest);
            }

            return new DisposableAction(() =>
            {
                removeSignalSource.TryRaise();
                if (connectedPlug.Plug == plug)
                {
                    Disconnect(plugRequest);
                }
                m_ConnectRequests.RemoveAll(r => r.Plug == plug);
            });
        }

        private async UniTaskVoid InternalConnect(PlugRequest plugRequest)
        {
            if (connectedPlug.Plug != null)
            {
                Disconnect(connectedPlug);
            }

            connectedPlug = plugRequest;
            try
            {
                m_DisconnectSignalSource = SignalSource.CreateLinked(plugRequest.RemoveSignalSource.Signal);
                var socketValue = await GetSocketValue(m_DisconnectSignalSource.Signal);
                await plugRequest.Plug(socketValue, m_DisconnectSignalSource.Signal);
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                Disconnect(plugRequest);
            }
        }

        private void Disconnect(PlugRequest plugRequest)
        {
            if (connectedPlug.Plug != plugRequest.Plug)
                return;

            m_DisconnectSignalSource.TryRaise();
            m_DisconnectSignalSource = default;
            connectedPlug = default;

            if (m_ConnectRequests.Count > 0)
            {
                var nextRequest = m_ConnectRequests[0];
                m_ConnectRequests.RemoveAt(0);
                InternalConnect(nextRequest).Forget();
            }
        }

        public IDisposable Watch(Func<T, Signal, UniTask> handler)
        {
            if (!m_LifecycleSignalSource.isAlive || !isActiveAndEnabled)
                return DisposableAction.Empty;

            var watchSignalSource = SignalSource.Create();
            var compositeSignal = SignalSource.CreateLinked(watchSignalSource.Signal, m_LifecycleSignalSource.Signal);

            async UniTaskVoid WatchTask()
            {
                using (compositeSignal)
                {
                    try
                    {
                        T value = await GetSocketValue(compositeSignal.Signal);
                        await handler(value, compositeSignal.Signal);
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
            }

            WatchTask().Forget();
            return new DisposableAction(() => watchSignalSource.TryRaise());
        }

        public abstract UniTask<T> GetSocketValue(Signal cancellationSignal);
    }
}