using System.Runtime.CompilerServices;
using Metamatic.DevTools;
using Sirenix.OdinInspector;
using Unity.Cinemachine;

namespace Metamatic
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
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

        /// <summary>
        /// Contains all active shakers that are currently shaking the camera.
        /// </summary>
        private HashSet<ShakerSource> m_Shakers = new();

        public Action<Signal> OnShakersActivated;
        public float shakeAmplitude { get; private set; }
        public float shakeFrequency { get; private set; }

        [ShowInInspector]
        public new Camera camera
        {
            get
            {
                if (m_CameraController.value == null) return null;
                return m_CameraController.value.camera;
            }
        }

        protected override async UniTask RunService(Ready ready, Signal signal)
        {
            using var cameraAnnounceSubscription =
                Dispatcher.global.SubscribeAnnouncementsLegacy<CameraController>(HandlerCamerasAnnounce);
            using var cameraRetractSubscription =
                Dispatcher.global.SubscribeRetractionsLegacy<CameraController>(HandlerCamerasRetract);
#if DEBUG
            using var monitoringShakeAmplitude = Monitoring.Create("ImpactService.Shake", "amplidute", () => shakeAmplitude);
            using var monitoringShakeFrequency = Monitoring.Create("ImpactService.Shake", "frequency", () => shakeFrequency);
#endif
            await ready();
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

        private void HandlerCamerasAnnounce(CameraController[] announcedCameras)
        {
            foreach (var cam in announcedCameras)
            {
                m_CameraWeightSubscriptions[cam] = cam.SubscribeToWeightChanged(HandlerCameraWeightChanged);
            }

            RecalculateMainCamera();
        }

        private void HandlerCamerasRetract(CameraController retractedCamera)
        {
            if (m_CameraController.value == retractedCamera)
            {
                m_CameraController.value = null;
            }

            RecalculateMainCamera();
        }

        private void RecalculateMainCamera()
        {
            CameraController mainCamera = null;
            foreach (var cam in m_CameraWeightSubscriptions.Keys)
            {
                if (mainCamera == null || cam.weight > mainCamera.weight)
                {
                    mainCamera = cam;
                }
            }

            m_CameraController.value = mainCamera;
        }

        private void HandlerCameraWeightChanged(CameraController camera, float weight)
        {
            if (camera == m_CameraController.value && m_CameraWeightSubscriptions.Count == 1) return;
            RecalculateMainCamera();
        }
    }
}