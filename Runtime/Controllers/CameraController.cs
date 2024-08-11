using Cysharp.Threading.Tasks;

namespace Metamatic
{
    using System;
    using System.Threading;
    using Metamatic.Actions;
    using UnityEngine;

    public class CameraController : AsyncBehaviour
    {
        [SerializeField] float m_Weight = 1.0f;
        public event Action<CameraController, float> onWeightChanged;
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

        protected override async UniTask Run(Ready ready, CancellationToken cancellationToken)
        {
            camera = GetComponent<Camera>();
            using var announce = Dispatcher.global.Announce(this);
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