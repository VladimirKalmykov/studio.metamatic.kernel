using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metamatic.SocketSystem
{
    public abstract class Socket: MonoBehaviour
    {
        
    }
    
    public abstract class Socket<TOutput>: MonoBehaviour
    {
        private static readonly ObservableList<Socket<TOutput>> m_Sockets = new ();
        public static DisposableAction WatchEachLifecycle(Func<Socket<TOutput>, CancellationSignal, UniTaskVoid> handler)
        {
            return m_Sockets.WatchEachLifecycle(handler);
        }
        public delegate UniTask Plug(TOutput property, CancellationSignal cancellationSignal);
        public readonly struct PlugRequest
        {
            public readonly Plug Plug;
            public readonly float Weight;
            
            /// <summary>
            /// This signal is called when the plug is removed from the socket totally.
            /// </summary>
            public readonly CancellationSignal RemoveSignal;
            
            public PlugRequest(Plug plug, float weight, CancellationSignal removeSignal)
            {
                this.Plug = plug;
                this.Weight = weight;
                RemoveSignal = removeSignal;
            }
            
            public override int GetHashCode()
            {
                return Plug.GetHashCode();
            }
        }
        [ShowInInspector, ReadOnly]
        public PlugRequest connectedPlug { get; private set; }
        private CancellationSignalSource m_DisconnectSignalSource;
        private readonly List<PlugRequest> m_ConnectRequests = new ();

        public void OnEnable()
        {
            m_Sockets.Add(this);
        }
        
        public void OnDisable()
        {
            m_Sockets.Remove(this);
        }
        
        
        public DisposableAction TryConnect(Plug plug, float weight = 0f)
        {
            #if DEBUG
            if (connectedPlug.Plug == plug)
            {
                Console.Exception($"Socket", $"Plug {plug} is already connected.");
                return DisposableAction.Empty;
            }
            #endif
            bool allowToConnect = true;
            if (connectedPlug.Plug != null)
            {
                /* Compare weights */
                allowToConnect = connectedPlug.Weight > weight;
            }
            var removeSignalSource = CancellationSignalSource.Create();
            var plugRequest = new PlugRequest(plug, weight, removeSignalSource.Signal);
            if (allowToConnect)
            {
                InternalConnect(plugRequest).Forget();
            }
            else
            {
                /* Add to queue */
                m_ConnectRequests.Add(plugRequest);
            }
            
            return new DisposableAction(() =>
            {
                removeSignalSource.TryCancel();
                Disconnect(connectedPlug);
                
                /* FInd and remove plug from queue */
                for (int i = 0; i < m_ConnectRequests.Count; i++)
                {
                    if (m_ConnectRequests[i].Plug == plug)
                    {
                        m_ConnectRequests.RemoveAt(i);
                        break;
                    }
                }
            });
        }

        private async UniTaskVoid InternalConnect(PlugRequest plugRequest)
        {
            if (connectedPlug.Plug != null)
            {
                /* Disconnect current plug */
                Disconnect(connectedPlug);
            }
            connectedPlug = plugRequest;
            try
            {
                var disconnectSignalSource = m_DisconnectSignalSource = CancellationSignalSource.CreateLinked(plugRequest.RemoveSignal);
                var socketValue = await GetSocketValue(disconnectSignalSource.Signal);
                await plugRequest.Plug(socketValue, disconnectSignalSource.Signal);
            } catch (OperationCanceledException)
            {
                // Ignore
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
        
        private void Disconnect(PlugRequest plugRequest)
        {
            if (connectedPlug.Plug == plugRequest.Plug)
            {
                /* Cancel connection */
                m_DisconnectSignalSource.TryCancel();
                /* Return connected plug to queue */
                m_ConnectRequests.Add(connectedPlug);
                connectedPlug = default;
            }
        }

        public abstract UniTask<TOutput> GetSocketValue(CancellationSignal cancellationSignal);
    }
}