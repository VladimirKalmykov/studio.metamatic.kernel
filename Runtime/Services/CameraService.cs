using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Metamatic.DevTools;
using Sirenix.OdinInspector;
using Unity.Cinemachine;

namespace Metamatic
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    public class CameraService : Service
    {
        public class ShakerSource : IDisposable
        {
            public static ShakerSource Create(float amplitude = 0f, float frequency = 0f)
            {
                var shaker = ParamLessPool<ShakerSource>.GetShared();
                shaker.Amplitude = amplitude;
                shaker.Frequency = frequency;
                shaker.m_IsReleased = false;
                return shaker;
            }

            public int version { get; private set; }
            public float Amplitude;
            public float Frequency;

            private bool m_IsReleased;

            public event Action OnReleased;

            public void Release()
            {
                m_IsReleased = true;
                OnReleased?.Invoke();
                OnReleased = null;

                unchecked
                {
                    version++;
                }

                ParamLessPool<ShakerSource>.ReturnShared(this);
            }

            public void Dispose()
            {
                if (!m_IsReleased)
                {
                    Console.Verbose("Shaker", "Shaker was not released before disposal.");
                }
            }
        }

        public readonly struct Shaker : IDisposable
        {
            private readonly ShakerSource m_Source;
            private readonly int m_Version;

            public Shaker(ShakerSource source)
            {
                this.m_Source = source;
                this.m_Version = source.version;
            }

            public void UpdateProperties(float amplitude, float frequency)
            {
                if (m_Source.version != m_Version) return;
                m_Source.Amplitude = amplitude;
                m_Source.Frequency = frequency;
            }

            public void Dispose()
            {
                if (m_Source.version != m_Version) return;
                m_Source.Release();
            }
        }
        

        private readonly Observable<CameraController> m_CameraController = new();
        private Dictionary<CameraController, IDisposable> m_CameraWeightSubscriptions = new();

        private readonly ObservableNullable<CinemachineVirtualCameraBase> m_ObservableCurrentVirtualCamera = new();

        /// <summary>
        /// Contains all active shakers that are currently shaking the camera.
        /// </summary>
        private HashSet<ShakerSource> m_Shakers = new();

        public Action<Signal> OnShakersActivated;
        public float shakeAmplitude { get; private set; }
        public float shakeFrequency { get; private set; }
        
        public (CinemachineVirtualCameraBase virtualCameraBase, bool isSet) currentVirtualCamera
        {
            get
            {
                if (m_ObservableCurrentVirtualCamera.hasValue)
                {
                    return (m_ObservableCurrentVirtualCamera.value, true);
                }
                return (null, false);
            }
        }

        [ShowInInspector]
        public new Camera camera
        {
            get
            {
                if (m_CameraController.value == null) return null;
                return m_CameraController.value.camera;
            }
        }
        
        private readonly ObservableNullable<Transform> m_FollowTargetObservable = new ObservableNullable<Transform>();

        protected override async UniTask RunService(Ready ready, Signal signal)
        {
#if DEBUG
            using var monitoringShakeAmplitude = Monitoring.Create("ImpactService.Shake", "amplidute", () => shakeAmplitude);
            using var monitoringShakeFrequency = Monitoring.Create("ImpactService.Shake", "frequency", () => shakeFrequency);
#endif
            var currentCamera = GetCurrentVirtualCamera();
            if (currentCamera is CinemachineVirtualCameraBase cinemachineCamera)
            {
                m_ObservableCurrentVirtualCamera.Value = cinemachineCamera;
            }
            
            CinemachineCore.CameraActivatedEvent.AddListener(OnCameraActivated);
            using var observeFollowTarget = m_FollowTargetObservable.Watch(OnFollowTargetChange);
            await ready();
            CinemachineCore.CameraActivatedEvent.RemoveListener(OnCameraActivated);
        }
        
        
        private void OnFollowTargetChange(Transform target, bool hasValue)
        {
            if (m_ObservableCurrentVirtualCamera.hasValue)
            {
                m_ObservableCurrentVirtualCamera.Value.Follow = target;
            }
        }
        
        private void OnCameraActivated(ICinemachineCamera.ActivationEventParams activationParams)
        {
            var incomingCamera = activationParams.IncomingCamera;
            if (incomingCamera is CinemachineVirtualCameraBase cinemachineCamera)
            {
                if (m_FollowTargetObservable.hasValue)
                {
                
                    cinemachineCamera.Follow = m_FollowTargetObservable.value;
                }
                m_ObservableCurrentVirtualCamera.Value = incomingCamera as CinemachineCamera;
            }
        }

        #region Public methods

        public UniTask<CameraController> WaitForCamera(Signal signal)
        {
            return m_CameraController.WaitUntilValueIsNotNull(signal);
        }

        public DisposableAction SubscribeToMainCamera(Action<CameraController> action)
        {
            return m_CameraController.Watch(action);
        }
        
        public DisposableAction WatchMainCameraAsync(Func<CameraController, Signal, UniTaskVoid> action)
        {
            return m_CameraController.WatchAsync(action);
        }
        
        public DisposableAction WatchVirtualCamera(Action<CinemachineVirtualCameraBase, bool> action)
        {
            return m_ObservableCurrentVirtualCamera.Watch(action);
        }
        
        public DisposableAction WatchVirtualCameraAsync(
            Func<CinemachineVirtualCameraBase, Signal, UniTaskVoid> action)
        {
            return m_ObservableCurrentVirtualCamera.WatchAsync(action);
        }

        public DisposableAction AnnounceCameraController(CameraController cameraController)
        {
            if (m_CameraController.value != null)
            {
                Console.Warning("CameraService", "Do not announce camera controller when there is already one announced.");
            }
            m_CameraController.SetValue(cameraController);

            return DisposableAction.Create(() =>
            {
                m_CameraController.SetValue(null);
            });
        }
        
        [UsedImplicitly]
        public void FollowTarget(Transform target)
        {
            Console.Info("CameraService", "FollowTarget", target?.name);
            if (target == null)
            {
                m_FollowTargetObservable.Clear();
                return;
            }
            m_FollowTargetObservable.value = target;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Camera GetCamera()
        {
            return camera;
        }

        public ICinemachineCamera GetCurrentVirtualCamera()
        {
            if (m_CameraController.value == null) return null;
            return m_CameraController.value.cinemachineBrain.ActiveVirtualCamera;
        }

        public Shaker Shake(float amplitude, float frequency)
        {
            var shakerSource = ShakerSource.Create(amplitude, frequency);
            m_Shakers.Add(shakerSource);
            TryStartShakeComposite().Forget();
            shakerSource.OnReleased += () => { m_Shakers.Remove(shakerSource); };
            return new Shaker(shakerSource);
        }

        public void ShakeFor(float amplitude, float frequency, float duration, float extinctionDuration, Signal signal)
        {
            var shaker = Shake(amplitude, frequency);
            UniTask.RunOnThreadPool(async () =>
            {
                try
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: signal);
                    if (extinctionDuration > 0f)
                    {
                        var startTime = Time.time;
                        await UniTask.WaitUntil(() =>
                        {
                            if (signal.IsCancellationRequested) return true;
                            var progress = (Time.time - startTime) / extinctionDuration;
                            shaker.UpdateProperties(amplitude * (1f - progress), frequency);
                            return Time.time - startTime >= extinctionDuration;
                        });
                    }
                }
                catch (OperationCanceledException)
                {
                    // ignored
                }
                finally
                {
                    await UniTask.SwitchToMainThread();
                    shaker.Dispose();
                }
            }, cancellationToken: signal, configureAwait: true).Forget();
        }

        #endregion

        private async UniTaskVoid TryStartShakeComposite()
        {
            if (m_Shakers.Count == 0) return;
            if (m_Shakers.Count == 1)
            {
                var css = SignalSource.Create();
                OnShakersActivated?.Invoke(css.Signal);
                var maxAmplitude = 0f;
                var maxFrequency = 0f;
                await UniTask.WaitUntil(() =>
                {
                    maxAmplitude = 0f;
                    maxFrequency = 0f;
                    foreach (var shaker in m_Shakers)
                    {
                        shakeAmplitude = Mathf.Max(shaker.Amplitude, maxAmplitude);
                        shakeFrequency = Mathf.Max(shaker.Frequency, maxFrequency);
                    }

                    return m_Shakers.Count == 0;
                });
                css.TryRaise();
            }
        }
    }
}