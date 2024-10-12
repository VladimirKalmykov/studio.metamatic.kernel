using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public interface IMindThread: IDisposable
    {
        /// <summary>
        /// Thread doesn't removed when reached zero weight
        /// </summary>
        public bool longLived { get; set; }
        public UniTask Activate(CancellationSignal cancellationSignal);
        public void Update();
        public float GetWeight();
        public void Weighting();
    }
    public abstract class MindThread<TContext>: IMindThread
    {
        public readonly TContext Context;
        
        /// <summary>
        /// Children threads
        /// </summary>
        private List<IMindThread> m_ChildThreads;

        private List<IMindThread> childThreads => m_ChildThreads ??= ParamLessPool<List<IMindThread>>.GetShared();
        /// <summary>
        /// Contains currently active child thread.
        /// </summary>
        private Observable<IMindThread> m_ActiveChildThread;
        private Observable<IMindThread> activeChildThread => m_ActiveChildThread ??= ParamLessPool<Observable<IMindThread>>.GetShared();
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
        private bool m_Activated;
        public bool longLived { get; set; }
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
            childThreads.Add(thread);
            Weighting();
            return new DisposableAction(() =>
            {
                thread.Dispose();
                
                // Use trick to remove the thread from the list more efficiently
                var index = childThreads.IndexOf(thread);
                var last = childThreads.Count - 1;
                if (index != -1)
                {
                    childThreads[index] = childThreads[last];
                    childThreads.RemoveAt(last);
                }
                Weighting();
            });
        }

        public async UniTask Activate(CancellationSignal cancellationSignal)
        {
            if (!m_Activated)
            {
                var nextChildThreads = CreateChildrenThreads();
                if (nextChildThreads != null)
                {
                    foreach (var thread in nextChildThreads)
                    {
                        // All threads created by CreateLongLiveThreads are long lived
                        thread.longLived = true;
                        childThreads.Add(thread);
                    }
                }
                m_Activated = true;
            }
            using var watchActiveThread = activeChildThread.WatchLifecycle(OnActiveMindThreadChanged);
            InternalCallUserActive(cancellationSignal).Forget();
            await cancellationSignal;
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
            updateLoop.Update();
            // Update active child thread
            activeChildThread.value?.Update();
        }

        public abstract float GetWeight();

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
            
            foreach (var thread in threadsToRemove)
            {
                childThreads.Remove(thread);
            }
            threadsToRemove.Clear();

            WeightingPostProcessing(nextActiveThread).Forget();
        }
        
        private async UniTaskVoid WeightingPostProcessing(IMindThread nextActiveThread)
        {
            // Wait for the next frame to apply the changes.
            await UniTask.Yield(PlayerLoopTiming.EarlyUpdate);
            // Apply the changes. Replace the active thread and remove the threads that are not needed anymore.
            activeChildThread.SetUniqValue(nextActiveThread);
            if (nextActiveThread != null)
            {
                // Wait for the post late update to perform weighting in children threads.
                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
                nextActiveThread.Weighting();
            }
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