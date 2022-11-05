using System;

namespace Metamatic.Actions
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public abstract class Dispatcher : MonoBehaviour
    {
        public bool enableLogging = false;
        public virtual void Dispatch(Action action)
        {
            if (enableLogging)
            {
                Debug.Log($"<color=cyan>ACTION</color>: {action.GetDebugMessage()}");
            }
            OnDispatch(action);
        }

        public virtual void OnDispatch(Action action)
        {
            switch (action)
            {
                // Apply effect to the current target
                case ApplyEffectAction aea:
                    aea.effect.Apply(this, aea.props);
                    break;
                case EffectExitAction eea:
                    // Skip
                    break;
                default:
                    throw new Exception($"Action type {action.GetType().Name} is out of range for {GetType().Name}");
            }
        }
    }
}