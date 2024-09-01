
using Sirenix.OdinInspector;

namespace Metamatic
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    public class CameraService : Service
    {   
        private readonly MicroState<CameraController> m_CameraController = new();
        private Dictionary<CameraController, IDisposable> m_CameraWeightSubscriptions = new();
        
        [ShowInInspector]
        public new Camera camera
        {
            get
            {
                if (m_CameraController.value == null) return null;
                return m_CameraController.value.camera;
            }
        }

        protected override async UniTask RunService(Ready ready, CancellationSignal signal)
        {
            using var cameraAnnounceSubscription =
                Dispatcher.global.SubscribeAnnouncementsLegacy<CameraController>(HandlerCamerasAnnounce);
            using var cameraRetractSubscription =
                Dispatcher.global.SubscribeRetractionsLegacy<CameraController>(HandlerCamerasRetract);
            await ready();
        }

        public UniTask<CameraController> WaitForCamera(CancellationSignal signal)
        {
            return m_CameraController.WaitUntilValueIsNotNull(signal);
        }

        public DisposableAction SubscribeToMainCamera(Action<CameraController> action)
        {
            return m_CameraController.Subscribe(action);
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