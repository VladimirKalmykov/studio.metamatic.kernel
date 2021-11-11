using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    /// <summary>
    /// Allows to get access to the behaviour lifecycle events
    /// </summary>
    public class BehaviourTicker : MonoBehaviour
    {
        public event Action OnUpdate = () => { };
        public event Action OnLateUpdate = () => { };

        private void Update()
        {
            OnUpdate?.Invoke();
        }
        
        private void LateUpdate()
        {
            OnLateUpdate.Invoke();
        }

        private void OnDisable()
        {
            foreach (var aop in asyncOperations.ToArray())
            {
                aop.Cancel();
            }
        }

        private List<Promise> asyncOperations = new List<Promise>();
        
        public delegate void UpdateForCallback(float ratio);

        public delegate void UpdateForCallback<T>(ref T props, float ratio);
        /// <summary>
        /// Start update fragment for specified duration
        /// </summary>
        /// <param name="duration"></param>
        /// <param name="animationCallback"></param>
        /// <param name="realTime"></param>
        public Promise UpdateFor(float duration, UpdateForCallback animationCallback, bool realTime = false, CancellationToken cancellationToken = default)
        {
            var asyncOperation = new Promise((resolve, reject, self) =>
            {
                var startTime = realTime ? Time.unscaledTime : Time.time;
                void UpdateHandler()
                {
                    var time = realTime ? Time.unscaledTime : Time.time;
                    var ratio = Mathf.Clamp((time - startTime) / duration, 0f, 1f);
                    try {
                        animationCallback(ratio);
                    }
                    catch (Exception e) {
                        reject(e);
                    }

                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (ratio != 1f) return;
                    OnUpdate -= UpdateHandler;
                    resolve(null);
                }
                
                OnUpdate += UpdateHandler;
                
                self.OnCancel += new Action(() =>
                {
                    OnUpdate -= UpdateHandler;
                });
            }, cancellationToken);
            
            
            asyncOperations.Add(asyncOperation);
            asyncOperation.OnFinally += () =>
            {
                asyncOperations.Remove(asyncOperation);
            };

            return asyncOperation;
        }
        
        /// <summary>
        /// The same as untyped Promise but allows you to pass any props to the callback function
        /// </summary>
        /// <param name="duration"></param>
        /// <param name="props"></param>
        /// <param name="animationCallback"></param>
        /// <param name="realTime"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public Promise UpdateFor<T>(float duration, UpdateForCallback<T> animationCallback, T props, bool realTime = false)
        where T: notnull
        {
            var asyncOperation = new Promise((resolve, reject, self) =>
            {
                var startTime = realTime ? Time.unscaledTime : Time.time;
                void UpdateHandler()
                {
                    var time = realTime ? Time.unscaledTime : Time.time;
                    var ratio = Mathf.Clamp((time - startTime) / duration, 0f, 1f);
                    try {
                        animationCallback(ref props, ratio);
                    }
                    catch (Exception e) {
                        reject(e);
                    }

                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (ratio != 1f) return;
                    OnUpdate -= UpdateHandler;
                    resolve(null);
                }
                
                OnUpdate += UpdateHandler;
                
                self.OnCancel += new Action(() =>
                {
                    OnUpdate -= UpdateHandler;
                });
            });
            
            
            asyncOperations.Add(asyncOperation);
            asyncOperation.OnFinally += () =>
            {
                asyncOperations.Remove(asyncOperation);
            };

            return asyncOperation;
        }
    }
}