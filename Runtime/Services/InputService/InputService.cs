using Sirenix.OdinInspector;

namespace Metamatic
{
    using System;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.InputSystem;

    public class InputService : Service
    {
        public class ActionMap
        {
            public string Name;
            public InputActionMap InputActionMap;
        }
        
        [SerializeField] private PlayerInput m_PlayerInputPrefab;
        private ObservableList<PlayerInput> m_PlayerInputs;
        private CameraService m_CameraService;
        private event Action<int, InputAction.CallbackContext> m_OnPlayerActionTriggered;
        
        #region Events
        public event Action<PlayerInput> OnPlayerJoined;
        public event Action<PlayerInput> OnPlayerLeft;
        #endregion

        protected override async UniTask RunService(Ready ready, CancellationSignal signal)
        {
            /* Prepare handler for player inputs */
            using var playerInputs = m_PlayerInputs = new ObservableList<PlayerInput>(HandlePlayerInput);
            var cameraService = m_CameraService = await Resolve<CameraService>(signal);
            /* Subscribe to main camera */
            using var mainCameraSubscription = cameraService.SubscribeToMainCamera(HandleMainCamera);

            /* Make a deal with the player manager */
            var playerInputManager = gameObject.AddComponent<PlayerInputManager>();
            playerInputManager.joinBehavior = PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed;
            playerInputManager.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            playerInputManager.playerPrefab = m_PlayerInputPrefab.gameObject;
            playerInputManager.onPlayerJoined += InternalOnPlayerJoined;
            playerInputManager.onPlayerLeft += InternalOnPlayerLeft;

            await ready();
            playerInputManager.onPlayerJoined -= InternalOnPlayerJoined;
            playerInputManager.onPlayerLeft -= InternalOnPlayerLeft;
        }
        
        public DisposableAction CheckoutActionMap(string actionMapName, Action<PlayerInput> action)
        {
            throw new NotImplementedException();
        }

        private async UniTask HandlePlayerInput(PlayerInput playerInput, CancellationSignal cancellationSignal)
        {
            /* Subscribe to player input actions */
            Action<InputAction.CallbackContext> handler = (InputAction.CallbackContext ctx) =>
            {
                m_OnPlayerActionTriggered?.Invoke(playerInput.playerIndex, ctx);
            };
            playerInput.onActionTriggered += handler;
            await cancellationSignal;
            playerInput.onActionTriggered -= handler;
        }
        
        public DisposableAction SubscribePlayerJoined(int playerIndex, Action<PlayerInput> action)
        {
            Action<PlayerInput> handler = (PlayerInput playerInput) =>
            {
                if (playerInput.playerIndex == playerIndex)
                {
                    action(playerInput);
                }
            };
            foreach (var playerInput in m_PlayerInputs)
            {
                if (playerInput.playerIndex == playerIndex)
                {
                    action(playerInput);
                }
            }
            OnPlayerJoined += handler;

            return new DisposableAction(() => OnPlayerJoined -= handler);
        }
        
        public DisposableAction SubscribePlayerLeft(int playerIndex, Action<PlayerInput> action)
        {
            Action<PlayerInput> handler = (PlayerInput playerInput) =>
            {
                if (playerInput.playerIndex == playerIndex)
                {
                    action(playerInput);
                }
            };
            OnPlayerLeft += handler;

            return new DisposableAction(() => OnPlayerLeft -= handler);
        }
        
        public UniTask<PlayerInput> WaitUntilPlayerJoined(int playerIndex, CancellationToken cancellationToken)
        {
            var tcs = new UniTaskCompletionSource<PlayerInput>();
            DisposableAction subscription = default;
            subscription = SubscribePlayerJoined(playerIndex, (playerInput) =>
            {
                // ReSharper disable once AccessToModifiedClosure
                subscription.Dispose();
                tcs.TrySetResult(playerInput);
            });
            return tcs.Task;
        }

        private void InternalOnPlayerJoined(PlayerInput playerInput)
        {
            global::Metamatic.Console.Info("Player", $"Joined: {playerInput.playerIndex}");
            
            playerInput.camera = m_CameraService.camera;
            m_PlayerInputs.Add(playerInput);
            OnPlayerJoined?.Invoke(playerInput);
        }

        private void InternalOnPlayerLeft(PlayerInput playerInput)
        {
            Console.Info("Player", $"Left: {playerInput.playerIndex}");
            m_PlayerInputs.Remove(playerInput);
            OnPlayerLeft?.Invoke(playerInput);
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
            return DisposableOperation.Run(async (operation) =>
            {
                var cameraService = await Resolve<CameraService>(operation.cancellationSignal);
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
                await operation.cancellationSignal;
            }).ToIDisposable();
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

        /// <summary>
        /// Get player input actions scheme associated with the specific player index.
        /// </summary>
        /// <returns></returns>
        public InputActionAsset GetPlayerInputActionAsset(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= m_PlayerInputs.Count)
                return null;
            return m_PlayerInputs[playerIndex].actions;
        }
        
        /// <summary>
        /// Get player input control scheme associated with the specific player index.
        /// </summary>
        /// <param name="playerIndex"></param>
        /// <returns></returns>
        public string GetPlayerInputControlScheme(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= m_PlayerInputs.Count)
                return null;
            return m_PlayerInputs[playerIndex].currentControlScheme;
        }
        
        #region Debug
        [Button, HideInEditorMode]
        public void DeactivateInput()
        {
            foreach (var playerInput in m_PlayerInputs)
            {
                playerInput.DeactivateInput();
            }
        }
        
        [Button, HideInEditorMode]
        public void ActivateInput()
        {
            foreach (var playerInput in m_PlayerInputs)
            {
                playerInput.ActivateInput();
            }
        }
        #endregion
    }

}