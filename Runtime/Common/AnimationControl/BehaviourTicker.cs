using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Metamatic
{
    /// <summary>
    ///  Allows to get access to the behaviour lifecycle events
    /// </summary>
    [ExecuteAlways]
    public class BehaviourTicker : MonoBehaviour
    {
        public static BehaviourTicker GetBehaviourTicker(Behaviour component, string name = null) {
            if (name == null) {
                var track = component.GetComponent<BehaviourTicker>();
                if (track == null) {
                    return component.gameObject.AddComponent<BehaviourTicker>();
                }
            } else {
                var tracks = component.GetComponents<BehaviourTicker>();
                foreach(var track in tracks) {
                    if (track.name == name) {
                        return track;
                    }
                }
            }

            var newTrack = component.gameObject.AddComponent<BehaviourTicker>();
            newTrack.name = name;
            return newTrack;
        }

        public new string name = "Default";
        public event Action OnUpdate = () => { };
        public event Action OnFixedUpdate = () => { };
        public event Action OnLateUpdate = () => { };

        private void Update()
        {
            OnUpdate?.Invoke();
        }

        public void FixedUpdate()
        {
            OnFixedUpdate?.Invoke();
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

        
        #region EveryFrame
        public enum FrameType
        {
            Update,
            FixedUpdate,
            LateUpdate
        }
        
        public IDisposable EveryFrame(Action callback, FrameType frameType = FrameType.Update)
        {
            Action disposeCallback = null;
            switch (frameType)
            {
                case FrameType.Update:
                    OnUpdate += callback;
                    disposeCallback = () =>
                    {
                        OnUpdate -= callback;
                    };
                    break;
                case FrameType.FixedUpdate:
                    OnFixedUpdate += callback;
                    disposeCallback = () =>
                    {
                        OnFixedUpdate -= callback;
                    };
                    break;
                case FrameType.LateUpdate:
                    OnLateUpdate += callback;
                    disposeCallback = () =>
                    {
                        OnLateUpdate -= callback;
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(frameType), frameType, null);
            }

            return new DisposableCallback(disposeCallback);
        }
        #endregion
    }
}