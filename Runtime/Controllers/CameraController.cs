using Cysharp.Threading.Tasks;

namespace Metamatic
{
    using System;
    using Unity.Cinemachine;
    using UnityEngine;

    public class CameraController : AsyncBehaviour
    {
        [SerializeField] float m_Weight = 1.0f;
        public event Action<CameraController, float> onWeightChanged;
        private CinemachineBrain m_CinemachineBrain;

        public CinemachineBrain cinemachineBrain
        {
            get
            {
                if (m_CinemachineBrain == null) m_CinemachineBrain = GetComponent<CinemachineBrain>();
                return m_CinemachineBrain;
            }
        }
        public new Camera camera { get; private set; }

        public float weight
        {
            get => m_Weight;
            set
            {
                m_Weight = value;
                onWeightChanged?.Invoke(this, m_Weight);
            }
        }

        protected override async UniTask Run(Ready ready, Signal signal)
        {
            camera = GetComponent<Camera>();
            using var announce = Dispatcher.global.AnnounceLegacy(this);
            await ready();
        }

        public DisposableAction SubscribeToWeightChanged(Action<CameraController, float> action,
            bool skipInitial = false)
        {
            onWeightChanged += action;
            if (!skipInitial) action(this, m_Weight);
            return new DisposableAction(() => onWeightChanged -= action);
        }
    }

}