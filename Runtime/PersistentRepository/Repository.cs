using System.Collections.Generic;
using UnityEngine;

namespace Metamatic
{
    public class Repository : MonoBehaviour
    {

        void Awake()
        {
            /*DB.Branch("master").GetObject("Player")
            );*/
        }
        private List<StateMachine> m_Stores = new List<StateMachine>();

        public StateMachine<TState> InitStore<TState>(TState defaultState)
        {
            var stateMachine = new StateMachine<TState>(defaultState);
            m_Stores.Add(stateMachine);
            return stateMachine;
        }

        private void LateUpdate()
        {
            m_Stores.ForEach(stateMachine => stateMachine.OnFrameEnd());
        }
    }
}