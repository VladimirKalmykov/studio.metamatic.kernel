using System;
using System.Threading;
using System.Threading.Tasks;

namespace Metamatic.Actions
{
    using System.Collections.Generic;
    using UnityEngine;

    public class Dispatcher : MonoBehaviour
    {
        public delegate bool ActionHandler(Action action);
        public bool enableLogging = false;
        private List<ActionHandler> actionHandlers = new List<ActionHandler>();
        private static Dispatcher m_GlobalDispatcher;

        public static Dispatcher global
        {
            get => m_GlobalDispatcher ??= new GameObject("__GLOBALDISPATCHER__").AddComponent<Dispatcher>();
        }
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
                    // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                    var actionHandled = false;
                    foreach (var ah in actionHandlers)
                    {
                        if (ah(action))
                        {
                            actionHandled = true;
                        }
                    }

                    // if (!actionHandled)
                    // {
                    //     throw new Exception($"Action type {action.GetType().Name} is out of range for {GetType().Name}");
                    // }
                    break;
            }
        }
        
        /// <summary>
        /// Subscribe to the action events. Return unsubscriber
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public System.Action Subscribe(ActionHandler handler)
        {
            actionHandlers.Add(handler);
            return () =>
            {
                actionHandlers.Remove(handler);
            };
        }

        public Task<T> WhenEmit<T>(CancellationToken cancellationToken) where T : Action
        {
            var result = new Promise<T>((resolve, reject, self) =>
            {
                var unsubscribe = Subscribe(delegate(Action action)
                {
                    if (action is T action1)
                    {
                        resolve(action1);
                    }

                    return false;
                });

                self.OnCancel += () =>
                {
                    unsubscribe();
                };
            }, cancellationToken);

            return result.task;
        }
    }
}