using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Metamatic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Metamatic.Magic
{
    /// <summary>
    /// Allows to add magic woods to the scene.
    /// </summary>
    public static class MagicMag
    {
        private static readonly ManagedList<MagicWood> Woods2D = new ManagedList<MagicWood>();
        private static readonly HashSet<string> Wood2DNames = new HashSet<string>();
        private static readonly MicroState<MagicWood> ActiveWood = new MicroState<MagicWood>(null);
        private static readonly MicroState<string> CursorLabel = new MicroState<string>(string.Empty);
        private static CancellationSignalSource _ActivationCancellationSignalSource;
        private static Vector2 m_PreviousPointerPosition;
        public static DisposableAction RegisterMagicWood(MagicWood wood2D)
        {
            if (Wood2DNames.Contains(wood2D.Name))
            {
                Debug.LogWarning($"Wood with name {wood2D.Name} already exists.");
                return DisposableAction.Empty;
            }
            Wood2DNames.Add(wood2D.Name);
            Woods2D.Add(wood2D);
            return new DisposableAction(() =>
            {
                Wood2DNames.Remove(wood2D.Name);
                Woods2D.Remove(wood2D);
            });
        }

        public static DisposableAction ObserveWoods(Func<MagicWood, CancellationSignal, UniTaskVoid> handler)
        {
            return Woods2D.Observe(handler);
        }
        
        public static DisposableAction SubscribeToCursorLabel(Action<string> handler)
        {
            return CursorLabel.Subscribe(handler);
        }
        
        public static void SetCursorLabel(string label)
        {
            CursorLabel.Value = label;
        }
        
        /// <summary>
        /// Active wood. Activated wood will receive mouse events.
        /// </summary>
        /// <param name="wood"></param>
        /// <param name="signal"></param>
        public static void ActivateWood(MagicWood wood, CancellationSignal signal)
        {
            ActiveWood.Value = wood;
            _ActivationCancellationSignalSource.TryCancel();
            _ActivationCancellationSignalSource = CancellationSignalSource.CreateLinked(signal);
            InternalActiveWoodProcess(wood, _ActivationCancellationSignalSource.Signal).Forget();
            wood.OnActivate(_ActivationCancellationSignalSource.Signal).Forget();
        }
        
        public static DisposableAction SubscribeToActiveWood(Action<MagicWood> handler)
        {
            return ActiveWood.Subscribe(handler);
        }
        
        public static async UniTaskVoid InternalActiveWoodProcess(MagicWood wood, CancellationSignal cancellationSignal)
        {
            EnhancedTouchSupport.Enable(); // TODO: Maybe we should use some singleton service for doing this, because it's not a good idea to enable/disable EnhancedTouchSupport in different places.
            using (PlayerLoop.EachFrame(TrackPointer))
            {
                await cancellationSignal;
            }
            EnhancedTouchSupport.Disable(); // TODO: Maybe we should use some singleton service for doing this, because it's not a good idea to enable/disable EnhancedTouchSupport in different places.
        }

        private static void TrackPointer()
        {
            var wood = ActiveWood.Value;
            Vector2 screenPosition = Vector2.zero;

            // Проверяем, есть ли тачскрин и активные тачи
            if (Touchscreen.current != null && UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count > 0)
            {
                // Используем первый активный тач
                var touch = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0];
                screenPosition = touch.screenPosition;

                if (touch.phase == TouchPhase.Began)
                {
                    wood.OnPress(screenPosition);
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    wood.OnRelease(screenPosition);
                }
                else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    if (screenPosition != m_PreviousPointerPosition)
                    {
                        wood.OnMove(screenPosition);
                        m_PreviousPointerPosition = screenPosition;
                    }
                }
            }
            else if (Mouse.current != null)
            {
                // Работаем с мышью
                screenPosition = Mouse.current.position.ReadValue();

                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    wood.OnPress(screenPosition);
                }
                if (Mouse.current.leftButton.wasReleasedThisFrame)
                {
                    wood.OnRelease(screenPosition);
                }
                if (screenPosition != m_PreviousPointerPosition)
                {
                    wood.OnMove(screenPosition);
                    m_PreviousPointerPosition = screenPosition;
                }
            }
        }
    }
}