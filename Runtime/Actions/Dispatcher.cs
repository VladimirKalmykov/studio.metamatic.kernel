using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Metamatic.Actions
{
    using System.Collections.Generic;
    using UnityEngine;

    class TypesRegistry : Dictionary<Type, List<object>>
    {
        
    }
    
    class AnnounceBehaviourAction<T>: BehaviourAction<T>
    where T: Behaviour
    {
        
        public AnnounceBehaviourAction(T source) : base(source)
        {
            
        }
    }
    
    class ConcealBehaviourAction<T>: BehaviourAction<T>
    where T: Behaviour
    {
        public ConcealBehaviourAction(T source) : base(source)
        {
            
        }
    }
    
    [DisallowMultipleComponent]
    public class Dispatcher : MonoBehaviour
    {
        public delegate bool ActionHandler(Action action);
        public bool enableLogging = false;
        private readonly List<ActionHandler> m_ActionHandlers = new List<ActionHandler>();
        private Dictionary<Type, List<ActionHandler>> m_ActionHandlersByType = new Dictionary<Type, List<ActionHandler>>();
        private static Dispatcher _globalDispatcher;
        private TypesRegistry m_Registry = new TypesRegistry();

        public static Dispatcher global
        {
            get => _globalDispatcher ??= new GameObject("__GLOBALDISPATCHER__").AddComponent<Dispatcher>();
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
                    Debug.LogWarning("ApplyEffectAction is deprecated. Use direct all effect Apply method instead: effect.Apply(behaviour, props)");
                    aea.effect.Apply(this, aea.props);
                    break;
                case EffectExitAction eea:
                    Debug.LogWarning("EffectExitAction is deprecated. Avoid using it.");
                    // Skip
                    break;
                default:
                    // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                    var actionHandled = false;
                    foreach (var ah in m_ActionHandlers)
                    {
                        if (ah(action))
                        {
                            actionHandled = true;
                        }
                    }
                    /*
                     * Invoke listeners from m_ActionHandlersByType
                     */
                    if (m_ActionHandlersByType.TryGetValue(action.GetType(), out var list)) {
                        foreach (var ah in list)
                        {
                            if (ah(action))
                            {
                                actionHandled = true;
                            }
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
            m_ActionHandlers.Add(handler);
            return () =>
            {
                m_ActionHandlers.Remove(handler);
            };
        }

        /**
         * Subscribe to specified action type. Return unsubscriber
         */
        public System.Action Subscribe<T>(System.Action<T> handler) where T : Action
        {
            if (!m_ActionHandlersByType.TryGetValue(typeof(T), out var list))
            {
                list = new List<ActionHandler>();
                m_ActionHandlersByType.Add(typeof(T), list);
            }
            var actionHandler = new ActionHandler(delegate(Action action)
            {
                if (action is T action1)
                {
                    handler(action1);
                    return true;
                }

                return false;
            });
            list.Add(actionHandler);
            return () =>
            {
                if (m_ActionHandlersByType.TryGetValue(typeof(T), out var list))
                {
                    list.Remove(actionHandler);
                }
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
        
        /**
         * Announce behaviour to the dispatcher. I will be added to the registry. The AnnounceBehaviourAction will be dispatched.
         */
        public void Announce<T>(T source) where T: Behaviour
        {
            if (!m_Registry.TryGetValue(typeof(T), out var list))
            {
                list = new List<object>();
                m_Registry.Add(typeof(T), list);
            }
            list.Add(source);
            Dispatch(new AnnounceBehaviourAction<T>(source));
        }
        
        /**
         * Conceal behaviour from the dispatcher. I will be removed from the registry. The ConcealBehaviourAction will be dispatched.
         */
        public void Conceal<T>(T source) where T: Behaviour
        {
            Dispatch(new ConcealBehaviourAction<T>(source));
            if (m_Registry.TryGetValue(typeof(T), out var list))
            {
                list.Remove(source);
            }
            Dispatch(new ConcealBehaviourAction<T>(source));
        }
        
        public System.Action SubscribeAnnounced<T>(Action<T[]> handler) where T: Behaviour
        {
            if (!m_Registry.TryGetValue(typeof(T), out var list))
            {
                list = new List<object>();
                m_Registry.Add(typeof(T), list);
            }

            if (list.Count > 0)
            {
                handler(list.OfType<T>().ToArray());
            }
            var unsubscribe = Subscribe(delegate(Action action)
            {
                if (action is AnnounceBehaviourAction<T> aba)
                {
                    handler(new [] { aba.behaviour as T });
                    return true;
                }

                return false;
            });
            return () =>
            {
                unsubscribe();
            };
        }
        
        public System.Action SubscribeConcealed<T>(Action<T> handler) where T: Behaviour
        {
            var unsubscribe = Subscribe(delegate(Action action)
            {
                if (action is ConcealBehaviourAction<T> aba)
                {
                    handler(aba.behaviour as T);
                    return true;
                }

                return false;
            });
            return () =>
            {
                unsubscribe();
            };
        }
    }
}