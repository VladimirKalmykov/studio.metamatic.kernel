using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    
    public abstract class MindThread<TContext>: IMindThread
    {
        public static string RemoveMindThreadSuffix(string typeName)
        {
            const string suffix = "MindThread";
    
            // Проверяем, оканчивается ли строка на "MindThread"
            if (typeName.EndsWith(suffix))
            {
                // Убираем окончание "MindThread"
                return typeName.Substring(0, typeName.Length - suffix.Length);
            }

            // Возвращаем исходное имя, если "MindThread" нет в конце
            return typeName;
        }
        
        public string name => RemoveMindThreadSuffix(GetType().Name);
        public readonly TContext Context;
        /// <summary>
        /// Memoized weight
        /// </summary>
        private float m_MemoizedWeight;
        /// <summary>
        /// Children threads
        /// </summary>
        private List<IMindThread> m_ChildThreads;
        /// <summary>
        /// Reference to the root thread
        /// </summary>
        public IMindThread rootThread { get; set; }

        private List<IMindThread> childThreads => m_ChildThreads ??= ParamLessPool<List<IMindThread>>.GetShared();
        /// <summary>
        /// Contains currently active child thread.
        /// </summary>
        private Observable<IMindThread> m_ActiveChildThread;
        private Observable<IMindThread> activeChildThread => m_ActiveChildThread ??= ParamLessPool<Observable<IMindThread>>.GetShared();
        public IMindThread activeThread => activeChildThread.value;
        public IMindThread topThread { get; private set; }
        public IEnumerable<IMindThread> children => childThreads;
        public float currentWeight => m_MemoizedWeight;

        /// <summary>
        /// List of threads to remove (used in weighing)
        /// </summary>
        private List<IMindThread> m_ThreadsToRemove;
        private List<IMindThread> threadsToRemove => m_ThreadsToRemove ??= ParamLessPool<List<IMindThread>>.GetShared();
        /// <summary>
        /// Update loop
        /// </summary>
        private Loop m_UpdateLoop;
        public Loop updateLoop => m_UpdateLoop ??= new Loop();
        /// <summary>
        /// Determines if the thread is activated
        /// </summary>
        private bool m_InitiallyActivated;
        public bool longLived { get; set; }
        public bool isActive { get; set; }
        public event Action<IMindThread> OnLoopEnd;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context">Thread context</param>
        public MindThread(TContext context)
        {
            Context = context;
        }
        
        public DisposableAction AddChildThread(IMindThread thread)
        {
            thread.rootThread = this.rootThread ?? this;
            childThreads.Add(thread);
            Weighting();
            return new DisposableAction(() =>
            {
                RemoveChildThread(thread);
            });
        }
        
        public void RemoveChildThread(IMindThread thread, bool skipCheckActive = false)
        {
            var index = childThreads.IndexOf(thread);
            var last = childThreads.Count - 1;
            if (index != -1)
            {
                var childThread = childThreads[index];
                var isCurrent = skipCheckActive && childThread == activeChildThread.value;
                if (isCurrent)
                {
                    // If removing the active thread, set the active thread to null
                    activeChildThread.SetUniqValue(null);
                }
                childThread.Dispose();
                childThreads[index] = childThreads[last];
                childThreads.RemoveAt(last);
                if (isCurrent)
                {
                    Weighting();
                }
            }
        }

        public async UniTask Activate(CancellationSignal cancellationSignal)
        {
            if (!m_InitiallyActivated)
            {
                var nextChildThreads = CreateChildrenThreads();
                if (nextChildThreads != null)
                {
                    foreach (var thread in nextChildThreads)
                    {
                        // All threads created by CreateLongLiveThreads are long lived
                        thread.rootThread = this.rootThread ?? this;
                        thread.longLived = true;
                        childThreads.Add(thread);
                    }
                }
                m_InitiallyActivated = true;
            }
            isActive = true;
            using var watchActiveThread = activeChildThread.WatchLifecycle(OnActiveMindThreadChanged);
            InternalCallUserActive(cancellationSignal).Forget();
            await cancellationSignal;
            isActive = false;
        }

        private async UniTaskVoid InternalCallUserActive(CancellationSignal cancellationSignal)
        {
            try
            {
                await OnActivate(cancellationSignal);
            }
            catch (OperationCanceledException)
            {
                // Skip the exception
            } catch (Exception e)
            {
                Console.Exception("MindThread", "InternalCallUserActive", e);
            }
        }

        public float GetWeight()
        {
            var weight = UpdateWeight();
            m_MemoizedWeight = weight;
            return weight;
        }
        
        /// <summary>
        /// This method is called every N ms to update the mind thread.
        /// It can be called chaotically depends on LOD factor, so make sure to use a delta time.
        /// When the mind thread is not active, this method is not called.
        /// The lifecycle of the mind thread is managed by the MindThreadManager.
        /// First called OnActivate, then OnUpdate multiple times, then called cancellationSignal.
        /// </summary>
        /// <param name="deltaTime"></param>
        public void Update()
        {
            updateLoop.Run();
            // Update active child thread
            activeChildThread.value?.Update();
        }
        
        public abstract float UpdateWeight();

        /// <summary>
        /// Method returns the weight of the thread. The thread with the highest weight is activated.
        /// </summary>
        /// <returns></returns>
        
        

        /// <summary>
        /// This method is called once when the thread is first activated.
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<IMindThread> CreateChildrenThreads()
        {
            return null;
        }
        
        protected abstract UniTask OnActivate(CancellationSignal cancellationSignal);

        public void Weighting()
        {
            var activeThreadWeight = activeChildThread.value?.GetWeight() ?? 0;
            var nextActiveThread = activeChildThread.value;
            foreach (var thread in childThreads)
            {
                if (thread == nextActiveThread)
                {
                    continue;
                }
                var weight = thread.GetWeight();
                
                // If the weight is zero and the thread is not long lived, remove it.
                if (weight == 0 && !thread.longLived)
                {
                    threadsToRemove.Add(thread);
                    continue;
                }
                
                if (weight > activeThreadWeight)
                {
                    nextActiveThread = thread;
                    activeThreadWeight = weight;
                }
            }
            
            bool wasActiveThreadRemoved = false;
            foreach (var thread in threadsToRemove)
            {
                if (thread == activeChildThread.value)
                {
                    wasActiveThreadRemoved = true;
                }
                RemoveChildThread(thread, true);
            }
            threadsToRemove.Clear();

            WeightingPostProcessing(nextActiveThread, wasActiveThreadRemoved).Forget();
        }
        
        private async UniTaskVoid WeightingPostProcessing(IMindThread nextActiveThread, bool wasActiveThreadRemoved)
        {
            // If the active thread was not removed, wait for the next frame to apply the changes.
            if (!wasActiveThreadRemoved)
            {
                // Wait for the next frame to apply the changes.
                await UniTask.Yield(PlayerLoopTiming.EarlyUpdate);
            }
            // Otherwise, apply the changes immediately.
            
            // Apply the changes. Replace the active thread and remove the threads that are not needed anymore.
            activeChildThread.SetUniqValue(nextActiveThread);
            if (nextActiveThread != null)
            {
                // Wait for the post late update to perform weighting in children threads.
                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
                nextActiveThread.Weighting();
            }
            else
            {
                if (rootThread != null)
                {
                    rootThread?.LoopEnd(this);
                }
                else
                {
                    LoopEnd(this);
                }
            }
        }

        public void LoopEnd(IMindThread topThread)
        {
            if (rootThread != null)
            {
                rootThread.LoopEnd(topThread);
            }
            else
            {
                this.topThread = topThread;
                // Notify the root thread that the loop is ended at specified child thread.
                OnLoopEnd?.Invoke(topThread);
            }
        }
        
        /// <summary>
        /// Wait the moment when the bubbling of the loop reaches the top thread.
        /// </summary>
        /// <param name="cancellationSignal"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public UniTask WaitLoopEnd(CancellationSignal cancellationSignal = default)
        {
            var tsc = SafeUniTaskCompletionSource.Create();
            DisposableAction registration = default;
            void OnLoopEndHandler(IMindThread topThread)
            {
                OnLoopEnd -= OnLoopEndHandler;
                registration.Dispose();
                tsc.TrySetResult();
            }
            OnLoopEnd += OnLoopEndHandler;
            registration = cancellationSignal.Register(() =>
            {
                registration.Dispose();
                OnLoopEnd -= OnLoopEndHandler;
                tsc.TrySetCanceled();
            });
            return tsc.Task;
        }

        public DisposableAction WatchLoopEnd(Action<IMindThread> action)
        {
            OnLoopEnd += action;
            return new DisposableAction(() =>
            {
                OnLoopEnd -= action;
            });
        }

        /// <summary>
        /// Method is called when the active thread is changed. It activates the thread and observes its lifecycle.
        /// </summary>
        /// <param name="thread"></param>
        /// <param name="cancellationSignal"></param>
        private async UniTaskVoid OnActiveMindThreadChanged(IMindThread thread, CancellationSignal cancellationSignal)
        {
            if (thread == null)
            {
                return;
            }

            try
            {
                cancellationSignal.ThrowIfCancellationRequested();
                await thread.Activate(cancellationSignal);
            } catch (Exception e)
            {
                Console.Exception("MindThreadsEngine", "OnActiveThreadChanged", e);
            }
        }

        public void Dispose()
        {
            // Return the list to the pool
            if (m_ChildThreads != null)
            {
                /* Dispose child threads */
                foreach (var thread in m_ChildThreads)
                {
                    thread.Dispose();
                }
                
                m_ChildThreads.Clear();
                ParamLessPool<List<IMindThread>>.ReturnShared(m_ChildThreads);
            }
            
            if (m_ActiveChildThread != null)
            {
                // Return the observable to the pool
                ParamLessPool<Observable<IMindThread>>.ReturnShared(m_ActiveChildThread);
            }
            
            
            // Return the list to the pool
            if (m_ThreadsToRemove != null)
            {
                m_ThreadsToRemove.Clear();
                ParamLessPool<List<IMindThread>>.ReturnShared(m_ThreadsToRemove);
            }
            
            // Return update loop to the pool
            if (m_UpdateLoop != null)
            {
                m_UpdateLoop.Dispose();
                m_UpdateLoop = null;
            }
            
            GC.SuppressFinalize(this);
        }
    }
}