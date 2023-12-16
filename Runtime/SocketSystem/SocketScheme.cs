using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Metamatic
{
    [Serializable]
    public class SocketScheme
    {
        
    }
    
    [Serializable]
    public class SocketScheme<TOutputBundle, TInputBundle>: SocketScheme
    where TOutputBundle: TypeBundle, new()
    where TInputBundle: TypeBundle, new()
    {
        public static readonly TOutputBundle outputBundle = new TOutputBundle();
        public static readonly TInputBundle inputBundle = new TInputBundle();
        
        public IPin[] outputPins;
        public IPin[] inputPins;
        private bool loopbackMode { get; set; }
        
        private List<SocketScheme<TOutputBundle, TInputBundle>> m_Plugs = new List<SocketScheme<TOutputBundle, TInputBundle>>();
        
        #if DEBUG
        private List<string> debug_Error = new List<string>();
        #endif
        
        /// <summary>
        /// This schema is monotype. It means that output type is the same as input type. And there is only one type at all. Monotype schemas can be connected to each other in loopback mode and can be connected to other schemas in loop chain mode.
        /// </summary>
        public bool isMonoType => inputBundle.GetTypes().Length == 1 && outputBundle.GetTypes().Length == 1 && inputBundle.GetTypes()[0] == outputBundle.GetTypes()[0];

        public void Initialize(IPin[] outputPins, IPin[] inputPins, bool loopbackMode = false)
        {
            this.loopbackMode = loopbackMode;
            this.outputPins = outputPins;
            this.inputPins = inputPins;
            
            if (loopbackMode && isMonoType)
            {
                MakeLoopbackConnection();
            }
        }
        
        /// <summary>
        /// Connect plug to this socket. Inputs of this input pins to the outputs of the other input pins. Outputs of this output pins to the inputs of the other output pins.
        /// </summary>
        /// <param name="socketScheme"></param>
        public void ConnectPlug(SocketScheme<TOutputBundle, TInputBundle> socketScheme)
        {
            if (m_Plugs.Contains(socketScheme))
            {
                throw new InvalidOperationException("Can't connect the same plug twice");
            }
            if (m_Plugs.Count > 0)
            {
                if (inputBundle.GetTypes().Length == 0)
                {
                    /* Connect multiple plugs to inputless socket */
                    MakeDirectConnection(socketScheme);
                }
                else
                {
                    if (inputBundle.GetType() != outputBundle.GetType()) {
                        throw new InvalidOperationException("Can't connect more than one plug when input and output bundles are different");
                    }
                    if (inputBundle.GetTypes().Length > 1)
                    {
                        throw new InvalidOperationException("Can't connect more than one plug when input bundle has more than one type");
                    }
                    /* Make chain connection */
                    m_Plugs.Add(socketScheme);
                    MakeLoopChain();
                }
            }
            else
            {
                m_Plugs.Add(socketScheme);
                MakeDirectConnection(socketScheme);
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="socketScheme">Scheme to disconnect</param>
        /// <param name="doNotReconnectedOthers">Do not reconnected rest plugs and don't create loopback connection</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void DisconnectPlug(SocketScheme<TOutputBundle, TInputBundle> socketScheme, bool doNotReconnectedOthers = false)
        {
            if (!m_Plugs.Contains(socketScheme))
            {
                throw new InvalidOperationException("Can't disconnect plug that is not connected");
            }
            /* Unconnect plug */
            var thatInputPins = socketScheme.inputPins;
            var thisOutputPins = outputPins;
            
            foreach( var thatInput in thatInputPins)
            {
                thatInput.DisconnectOutput();
            }

            foreach (var thatOutput in thisOutputPins)
            {
                thatOutput.DisconnectOutput();
            }

            m_Plugs.Remove(socketScheme);

            if (doNotReconnectedOthers)
            {
                return;
            }

            Rebuild();
        }
        
        public void DisconnectAllPlugs(bool doNotRebuild = false)
        {
            foreach (var plug in m_Plugs)
            {
                DisconnectPlug(plug, true);
            }

            if (doNotRebuild)
            {
                return;
            }
            Rebuild();
        }

        public void Dispose()
        {
            DisconnectAllPlugs(true);
            if (isMonoType)
            {
                DisconnectLoopbackConnection();
            }
        }

        private void Rebuild()
        {
            if (m_Plugs.Count > 2)
            {
                /* Remake loop chain */
                MakeLoopChain();
            }
            else if (m_Plugs.Count > 0)
            {
                /* Remake direct connection */
                MakeDirectConnection(m_Plugs[0]);
            }
            else if (loopbackMode && isMonoType)
            {
                /* Connector local output pin to local input pin */
                MakeLoopbackConnection();
            }
        }
        
        private void MakeDirectConnection(SocketScheme<TOutputBundle, TInputBundle> socketScheme)
        {

            var thatInputPins = socketScheme.inputPins;
            var thisInputPins = inputPins;
            var inputTypes = inputBundle.GetTypes();
            for (var i = 0; i < inputTypes.Length; i++)
            {
                var thisPin = thisInputPins[i];
                var thatPin = thatInputPins[i];
                if (thisPin == null || thatPin == null)
                {
#if DEBUG
                    AddError("One of the pins is null");
#endif
                    continue;
                }
                thisPin.ConnectInput(thatPin);
            }

            var thatOutputPins = socketScheme.outputPins;
            var thisOutputPins = outputPins;
            var outputTypes = outputBundle.GetTypes();
            for (var i = 0; i < outputTypes.Length; i++)
            {
                var thisPin = thisOutputPins[i];
                var thatPin = thatOutputPins[i];
                if (thisPin == null || thatPin == null)
                {
#if DEBUG
                    AddError("One of the pins is null");
#endif
                    continue;
                }
                thatPin.ConnectInput(thisPin);
            }
        }

        private void MakeLoopChain()
        {
            for (var q = 0; q < m_Plugs.Count; q++) {
                var currentPlug = m_Plugs[q];
                if (q == 0)
                {
                    /* For the first plug connect this output pins output to the other output pins input */
                    var thatOutputPins = currentPlug.outputPins;
                    var thisOutputPins = outputPins;
                    var outputTypes = outputBundle.GetTypes();
                    for (var i = 0; i < outputTypes.Length; i++)
                    {
                        var thisPin = thisOutputPins[i];
                        var thatPin = thatOutputPins[i];
                        if (thisPin == null || thatPin == null)
                        {
#if DEBUG
                                AddError("Unable to make chain. One of the pins is null");
#endif
                            return;
                        }
                        thatPin.ConnectInput(thisPin);
                    }
                } 
                else
                {
                    /* For the rest connect that input pins output of previous plug to that output pins input */
                    var previousPlug = m_Plugs[q - 1];
                    var currentOutputPins = currentPlug.outputPins;
                    var previousInputPins = previousPlug.inputPins;
                    var outputTypes = outputBundle.GetTypes();
                    for (var i = 0; i < outputTypes.Length; i++)
                    {
                        var currentPin = currentOutputPins[i];
                        var previousPin = previousInputPins[i];
                        if (currentPin == null || previousPin == null)
                        {
#if DEBUG
                            AddError("One of the pins is null");
#endif
                            continue;
                        }
                        currentPin.ConnectInput(previousPin);
                    }
                    
                    if (q == m_Plugs.Count - 1)
                    {
                        /* For the last connect that input pins output to this input pins input */
                        var thatInputPins = currentPlug.inputPins;
                        var thisInputPins = inputPins;
                        var inputTypes = inputBundle.GetTypes();
                        for (var i = 0; i < inputTypes.Length; i++)
                        {
                            var thisPin = thisInputPins[i];
                            var thatPin = thatInputPins[i];
                            if (thisPin == null || thatPin == null)
                            {
#if DEBUG
                                AddError("Unable to make chain. One of the pins is null");
#endif
                                return;
                            }
                            thisPin.ConnectInput(thatPin);
                        }
                    }
                }
            }
        }

        private void MakeLoopbackConnection()
        {
            var thisOutputPins = outputPins;
            var thisInputPins = inputPins;
            
            thisInputPins[0].ConnectInput(thisOutputPins[0]);
        }

        private void DisconnectLoopbackConnection()
        {
            var thisOutputPins = outputPins;
            var thisInputPins = inputPins;
            thisInputPins[0].DisconnectInput();
            thisOutputPins[0].DisconnectOutput();
        }
        
        #if DEBUG
        private void AddError(string error)
        {
            debug_Error.Add(error);
        }
        
        
        private void ClearError()
        {
            debug_Error.Clear();
        }
        #endif
    }

    public class SocketScheme<TOutputBundle> : SocketScheme<TOutputBundle, TypeBundle>
        where TOutputBundle : TypeBundle, new()
    {
        
    }
}