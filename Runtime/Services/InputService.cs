namespace Metamatic
{
    using System;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.InputSystem;

    public class InputService : Service
    {
        [SerializeField] private PlayerInput m_PlayerInputPrefab;
        private ManagedList<PlayerInput> m_PlayerInputs;
        private CameraService m_CameraService;
        private event Action<int, InputAction.CallbackContext> m_OnPlayerActionTriggered;

        protected override async UniTask RunService(Ready ready, CancellationToken cancellationToken)
        {
            /* Prepare handler for player inputs */
            m_PlayerInputs = new ManagedList<PlayerInput>(HandlePlayerInput);
            var cameraService = m_CameraService = await Resolve<CameraService>();
            /* Subscribe to main camera */
            using var mainCameraSubscription = cameraService.SubscribeToMainCamera(HandleMainCamera);

            /* Make a deal with the player manager */
            var playerInputManager = gameObject.AddComponent<PlayerInputManager>();
            playerInputManager.joinBehavior = PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed;
            playerInputManager.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            playerInputManager.playerPrefab = m_PlayerInputPrefab.gameObject;
            playerInputManager.onPlayerJoined += OnPlayerJoined;
            playerInputManager.onPlayerLeft += OnPlayerLeft;

            await ready();
            playerInputManager.onPlayerJoined -= OnPlayerJoined;
            playerInputManager.onPlayerLeft -= OnPlayerLeft;
        }

        private async UniTask HandlePlayerInput(PlayerInput playerInput, CancellationToken cancellationToken)
        {
            /* Subscribe to player input actions */
            Action<InputAction.CallbackContext> handler = (InputAction.CallbackContext ctx) =>
            {
                m_OnPlayerActionTriggered?.Invoke(playerInput.playerIndex, ctx);
            };
            playerInput.onActionTriggered += handler;
            await cancellationToken;
            playerInput.onActionTriggered -= handler;
        }

        private void OnPlayerJoined(PlayerInput playerInput)
        {
            Debug.Log($"Player joined: {playerInput.playerIndex}");
            playerInput.camera = m_CameraService.camera;
            m_PlayerInputs.Add(playerInput);
        }

        private void OnPlayerLeft(PlayerInput playerInput)
        {
            Debug.Log($"Player left: {playerInput.playerIndex}");
            m_PlayerInputs.Remove(playerInput);
        }

        private void HandleMainCamera(CameraController mainCamera)
        {
            if (mainCamera == null)
                return;
            /* Apply camera to every element in m_PlayerInputs */
            foreach (var playerInput in m_PlayerInputs)
                playerInput.camera = mainCamera.camera;
        }

        public DisposableAction SubscribeToActionTriggered(Action<InputAction.CallbackContext> action)
        {
            Action<int, InputAction.CallbackContext> handler = (int playerIndex, InputAction.CallbackContext ctx) =>
            {
                action(ctx);
            };
            m_OnPlayerActionTriggered += handler;

            return new DisposableAction(() => m_OnPlayerActionTriggered -= handler);
        }

        public DisposableAction SubscribeToActionTriggered(int playerIndex,
            Action<InputAction.CallbackContext> action)
        {
            Action<int, InputAction.CallbackContext> handler = (index, ctx) =>
            {
                if (index == playerIndex)
                {
                    action(ctx);
                }
            };
            m_OnPlayerActionTriggered += handler;

            return new DisposableAction(() => m_OnPlayerActionTriggered -= handler);
        }

        public IDisposable SubscribeToActionOnCollider<T>(Action<T> callback, string actionName, InputActionPhase phase,
            int mask = -1)
            where T : Component
        {
            return MicroOperation.Run(async (operation) =>
            {
                var cameraService = await Resolve<CameraService>(operation.cancellationToken);
                using var listener = SubscribeToActionTriggered(ctx =>
                {
                    if (ctx.action.name != actionName || ctx.action.phase != phase)
                        return;
                    var mousePosition = Mouse.current.position.ReadValue();
                    var ray = cameraService.camera.ScreenPointToRay(mousePosition);
                    var intersection = Physics2D.GetRayIntersection(ray, Mathf.Infinity, mask);
                    if (intersection.collider == null) return;
                    if (intersection.collider.gameObject.GetComponent<T>() is { } target)
                    {
                        callback(target);
                    }
                });
                await operation.cancellationToken;
            });
        }

        public async UniTask<InputAction.CallbackContext> WhenAction<T>(string actionName,
            CancellationToken cancellationToken)
            where T : Component
        {
            var tcs = new UniTaskCompletionSource<InputAction.CallbackContext>();
            await using var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
            using var listener = SubscribeToActionTriggered((ctx) =>
            {
                if (ctx.action.name != actionName)
                    return;
                tcs.TrySetResult(ctx);
            });
            var target = await tcs.Task;
            return target;
        }

        public async UniTask<T> WhenActionOnCollider<T>(string actionName, int mask,
            CancellationToken cancellationToken)
            where T : Component
        {
            var tcs = new UniTaskCompletionSource<T>();
            await using var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
            using var listener = SubscribeToActionOnCollider((T target) => { tcs.TrySetResult(target); }, actionName,
                InputActionPhase.Started, mask);
            var target = await tcs.Task;
            return target;
        }
    }

}