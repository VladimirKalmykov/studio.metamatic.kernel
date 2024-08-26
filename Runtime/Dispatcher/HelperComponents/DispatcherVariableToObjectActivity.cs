namespace Metamatic
{
    using System;
    using UnityEngine;

    public class DispatcherVariableToObjectActivity : MonoBehaviour
    {
        public string variableName;

        public bool fallbackValue;
        private Action m_ActionOnDestroy;
        void Start()
        {
            m_ActionOnDestroy = Dispatcher.global.SubscribeVariable<bool>(variableName, OnValueChange, fallbackValue);
        }
    
        private void OnValueChange(bool value)
        {
            gameObject.SetActive(value);
        }

        private void OnDestroy()
        {
            m_ActionOnDestroy?.Invoke(); 
        }
    }
}