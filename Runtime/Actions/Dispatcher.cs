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
    
    class AnnounceBehaviourActionLegacy<T>: BehaviourActionLegacy<T>
    where T: Behaviour
    {
        
        public AnnounceBehaviourActionLegacy(T source) : base(source)
        {
            
        }
    }
    
    class ConcealBehaviourActionLegacy<T>: BehaviourActionLegacy<T>
    where T: Behaviour
    {
        public ConcealBehaviourActionLegacy(T source) : base(source)
        {
            
        }
    }
    
    [DisallowMultipleComponent]
    public class Dispatcher : MonoBehaviour
    {
        
        public bool enableLogging = false;
        
        #region Legacy
        public delegate bool LegacyActionHandler(ActionLegacy actionLegacy);
        private readonly List<LegacyActionHandler> m_LegacyActionHandlers = new List<LegacyActionHandler>();
        private Dictionary<Type, List<LegacyActionHandler>> m_ActionHandlersByType = new Dictionary<Type, List<LegacyActionHandler>>();
        #endregion
        
        public delegate bool ActionHandler(Action action);
        private readonly Dictionary<System.Type, List<Delegate>> m_EventListeners = new Dictionary<System.Type, List<Delegate>>();
        
        private static Dispatcher _globalDispatcher;
        private TypesRegistry m_Registry = new TypesRegistry();

        public static Dispatcher global
        {
            get => _globalDispatcher ??= new GameObject("__GLOBALDISPATCHER__").AddComponent<Dispatcher>();
        }
        public virtual void DispatchLegacy(ActionLegacy actionLegacy)
        {
            if (enableLogging)
            {
                Debug.Log($"<color=cyan>ACTION</color>: {actionLegacy.GetDebugMessage()}");
            }
            OnDispatchLegacy(actionLegacy);
        }

        public virtual void OnDispatchLegacy(ActionLegacy actionLegacy)
        {
            switch (actionLegacy)
            {
                // Apply effect to the current target
                case ApplyEffectActionLegacy aea:
                    Debug.LogWarning("ApplyEffectAction is deprecated. Use direct all effect Apply method instead: effect.Apply(behaviour, props)");
                    aea.effect.Apply(this, aea.props);
                    break;
                case EffectExitActionLegacy eea:
                    Debug.LogWarning("EffectExitAction is deprecated. Avoid using it.");
                    // Skip
                    break;
                default:
                    // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                    var actionHandled = false;
                    foreach (var ah in m_LegacyActionHandlers)
                    {
                        if (ah(actionLegacy))
                        {
                            actionHandled = true;
                        }
                    }
                    /*
                     * Invoke listeners from m_ActionHandlersByType
                     */
                    if (m_ActionHandlersByType.TryGetValue(actionLegacy.GetType(), out var list)) {
                        foreach (var ah in list)
                        {
                            if (ah(actionLegacy))
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
        /// Subscribe to instant action
        /// </summary>
        /// <param name="handler"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public System.Action Subscribe<T>(ActionHandler handler)
        {
            if (!m_EventListeners.TryGetValue(typeof(T), out var list))
            {
                m_EventListeners.Add(typeof(T), list = new List<Delegate>());
            }
            list.Add(handler);
            return () =>
            {
                list.Remove(handler);
            };
        }
        
        public void Dispatch<T>(T action) where T : Action
        {
            if (enableLogging)
            {
                Debug.Log($"<color=cyan>ACTION</color>: {action.GetDebugMessage()}");
            }

            if (m_EventListeners.TryGetValue(typeof(T), out var list))
            {
                foreach (var handler in list)
                {
                    ((System.Action<T>)handler)?.Invoke(action);
                }
            }

            action.Clear();
        }
        
        /// <summary>
        /// Subscribe to the action events. Return unsubscriber
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public System.Action SubscribeLegacy(LegacyActionHandler handler)
        {
            m_LegacyActionHandlers.Add(handler);
            return () =>
            {
                m_LegacyActionHandlers.Remove(handler);
            };
        }

        /**
         * Subscribe to specified action type. Return unsubscriber
         */
        public System.Action SubscribeLegacy<T>(System.Action<T> handler) where T : ActionLegacy
        {
            if (!m_ActionHandlersByType.TryGetValue(typeof(T), out var list))
            {
                list = new List<LegacyActionHandler>();
                m_ActionHandlersByType.Add(typeof(T), list);
            }
            var actionHandler = new LegacyActionHandler(delegate(ActionLegacy action)
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

        public Task<T> WhenEmit<T>(CancellationToken cancellationToken) where T : ActionLegacy
        {
            var result = new Promise<T>((resolve, reject, self) =>
            {
                var unsubscribe = SubscribeLegacy(delegate(ActionLegacy action)
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
            DispatchLegacy(new AnnounceBehaviourActionLegacy<T>(source));
        }
        
        /**
         * Conceal behaviour from the dispatcher. I will be removed from the registry. The ConcealBehaviourAction will be dispatched.
         */
        public void Conceal<T>(T source) where T: Behaviour
        {
            DispatchLegacy(new ConcealBehaviourActionLegacy<T>(source));
            if (m_Registry.TryGetValue(typeof(T), out var list))
            {
                list.Remove(source);
            }
            DispatchLegacy(new ConcealBehaviourActionLegacy<T>(source));
        }
        
        /// <summary>
        /// Get all announced behaviours of specified type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IReadOnlyList<T> GetAnnounced<T>() where T: Behaviour
        {
            if (m_Registry.TryGetValue(typeof(T), out var list))
            {
                return list.OfType<T>().ToArray();
            }

            return new T[0];
        }
        
        /// <summary>
        /// Subscribe to the announced behaviours of specified type. Return unsubscriber
        /// </summary>
        /// <param name="handler"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>Unsubscriber</returns>
        public System.Action SubscribeAnnounced<T>(System.Action<T[]> handler) where T: Behaviour
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
            var unsubscribe = SubscribeLegacy(delegate(ActionLegacy action)
            {
                if (action is AnnounceBehaviourActionLegacy<T> aba)
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
        
        /// <summary>
        /// Subscribe to the concealed behaviours of specified type. Return unsubscriber
        /// </summary>
        /// <param name="handler"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>Unsubscriber</returns>
        public System.Action SubscribeConcealed<T>(System.Action<T> handler) where T: Behaviour
        {
            var unsubscribe = SubscribeLegacy(delegate(ActionLegacy action)
            {
                if (action is ConcealBehaviourActionLegacy<T> aba)
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