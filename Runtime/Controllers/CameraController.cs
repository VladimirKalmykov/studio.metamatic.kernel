using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

namespace Metamatic
{
    using System;
    using Unity.Cinemachine;
    using UnityEngine;

    [RequireService(typeof(CameraService))]
    [RequireComponent(typeof(Camera))]
    public class CameraController : AsyncBehaviour
    {
        private CinemachineBrain m_CinemachineBrain;

        public CinemachineBrain cinemachineBrain
        {
            get
            {
                if (m_CinemachineBrain) m_CinemachineBrain = GetComponent<CinemachineBrain>();
                return m_CinemachineBrain;
            }
        }
        public new Camera camera { get; private set; }

       

        protected override async UniTask Run(Ready ready, Signal signal)
        {
            camera = GetComponent<Camera>();
            var cameraService = await Service.Resolve<CameraService>(signal);
            using var announcing = cameraService.AnnounceCameraController(this);
            await ready();
        }
    }

}