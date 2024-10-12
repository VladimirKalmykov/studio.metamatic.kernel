using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public class MindThreadsProcessor: IDisposable
    {
        #region Constants
        private const float WeightingNoUpdateFrequency = 3f;
        private const float WeightingLowUpdateFrequency = 2f;
        private const float WeightingMediumUpdateFrequency = 1f;
        private const float WeightingHighUpdateFrequency = 0.2f;
        private const float LogicNoUpdateFrequency = 2f;
        private const float LogicLowUpdateFrequency = 1f;
        private const float LogicMediumUpdateFrequency = 0.5f;
        private const float LogicHighUpdateFrequency = 0.05f;
        #endregion
        /// <summary>
        /// Contains currently active thread.
        /// </summary>
        private IMindThread m_ActiveThread;
        /// <summary>
        /// Should be assigned by the constructor parameter.
        /// It shows the level of detail of the object. Depends on it the AI updates are done.
        /// </summary>
        private ObservableReadOnly<LODLevel> m_LODLevel;
        
        private readonly CancellationSignalSource m_DisposeSignalSource;
        private CancellationSignalSource m_PlaybackCancellationSignalSource;
        private float m_WeightingUpdateFrequency;
        private float m_LogicUpdateFrequency;
        private float m_LastLogicUpdateTime;
        
        public MindThreadsProcessor(IMindThread mindThread, ObservableReadOnly<LODLevel> lodLevel)
        {
            m_ActiveThread = mindThread;
            m_LODLevel = lodLevel;
            m_DisposeSignalSource = CancellationSignalSource.Create();
        }

        /// <summary>
        /// Start thinking process
        /// </summary>
        public void Run()
        {
            m_PlaybackCancellationSignalSource = CancellationSignalSource.CreateLinked(m_DisposeSignalSource.Signal);
            AsyncInit(m_PlaybackCancellationSignalSource.Signal).Forget();
        }

        /// <summary>
        /// Stop thinking process
        /// </summary>
        public void Stop()
        {
            m_PlaybackCancellationSignalSource.TryCancel();
        }

        private async UniTaskVoid AsyncInit(CancellationSignal cancellationSignal)
        {
            m_LastLogicUpdateTime = Time.time;
            // Subscribe to the LOD level changes
            using var lodLevelSubscription = m_LODLevel.Watch(OnLODLevelChanged);
            await using var eventLoop = StartEventLoop();
            // Call Weighting initially
            await m_DisposeSignalSource.Signal;
        }
        
        /// <summary>
        /// Create event loop that will run the AI logic depending on the LOD level.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private MicroOperation StartEventLoop()
        {
            return MicroOperation.Call(async (CancellationSignal signal) =>
            {
                m_LastLogicUpdateTime = Time.time;
                PlayerLoopHelper.AddAction(PlayerLoopTiming.Update, new LoopItem(() =>
                {
                    // ReSharper disable once AccessToModifiedClosure
                    if (Time.time - m_LastLogicUpdateTime >= m_LogicUpdateFrequency)
                    {
                        m_LastLogicUpdateTime = Time.time;
                        // Update the active thread
                        m_ActiveThread.Update();
                    }
                    return signal.IsCancellationRequested;
                }));
                while (!signal.IsCancellationRequested)
                {
                    // Weighting must be done on post late update
                    await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
                    signal.ThrowIfCancellationRequested();
                    m_ActiveThread.Weighting();
                    await PlayerLoop.Delay(m_WeightingUpdateFrequency, timing: PlayerLoopTiming.FixedUpdate, signal: signal);
                }
            });
        }
        
        private void OnLODLevelChanged(LODLevel lodLevel)
        {
            switch (lodLevel)
            {
                case LODLevel.No:
                    m_WeightingUpdateFrequency = WeightingNoUpdateFrequency;
                    m_LogicUpdateFrequency = LogicNoUpdateFrequency;
                    break;
                case LODLevel.Low:
                    m_WeightingUpdateFrequency = WeightingLowUpdateFrequency;
                    m_LogicUpdateFrequency = LogicLowUpdateFrequency;
                    break;
                case LODLevel.Medium:
                    m_WeightingUpdateFrequency = WeightingMediumUpdateFrequency;
                    m_LogicUpdateFrequency = LogicMediumUpdateFrequency;
                    break;
                case LODLevel.High:
                    m_WeightingUpdateFrequency = WeightingHighUpdateFrequency;
                    m_LogicUpdateFrequency = LogicHighUpdateFrequency;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        public void Dispose()
        {
            m_DisposeSignalSource.TryCancel();
        }
    }
}