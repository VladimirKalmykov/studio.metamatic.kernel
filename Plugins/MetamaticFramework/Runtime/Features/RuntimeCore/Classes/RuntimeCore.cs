using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Metamatic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Metamatic.Framework
{
    /// <summary>
    /// Allows to add magic woods to the scene.
    /// </summary>
    public static class RuntimeCore
    {
        private static readonly ManagedList<CursorTool> CursorTools = new();
        private static readonly HashSet<string> CursorToolNames = new();
        
        #region Private observables
        /// <summary>
        /// Observable active cursor tool.
        /// </summary>
        private static readonly Observable<CursorTool> ActiveCursorTool = new();
        /// <summary>
        /// Contains observable cursor label. It can be used to display some information that appears when the cursor is over some object.
        /// </summary>
        private static readonly Observable<string> CursorLabel = new(string.Empty);
        #endregion
        
        /// <summary>
        /// Signal source for active cursor tool. It will be canceled when the active cursor tool is changed.
        /// </summary>
        private static CancellationSignalSource _CursorToolActivationCancellationSignalSource;
        /// <summary>
        /// Helper variable to store the previous pointer position.
        /// </summary>
        private static Vector2 _PreviousPointerPosition;
        
        #region Public methods
        public static DisposableAction RegisterCursorTool(CursorTool wood2D)
        {
            if (CursorToolNames.Contains(wood2D.Name))
            {
                Debug.LogWarning($"Wood with name {wood2D.Name} already exists.");
                return DisposableAction.Empty;
            }
            CursorToolNames.Add(wood2D.Name);
            CursorTools.Add(wood2D);
            return new DisposableAction(() =>
            {
                CursorToolNames.Remove(wood2D.Name);
                CursorTools.Remove(wood2D);
            });
        }

        public static DisposableAction ObserveCursors(Func<CursorTool, CancellationSignal, UniTaskVoid> handler)
        {
            return CursorTools.Observe(handler);
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
        /// <param name="cursorTool"></param>
        /// <param name="signal"></param>
        public static void Activate(CursorTool cursorTool, CancellationSignal signal)
        {
            ActiveCursorTool.Value = cursorTool;
            _CursorToolActivationCancellationSignalSource.TryCancel();
            _CursorToolActivationCancellationSignalSource = CancellationSignalSource.CreateLinked(signal);
            InternalActivateCursorTool(cursorTool, _CursorToolActivationCancellationSignalSource.Signal).Forget();
            cursorTool.OnActivate(_CursorToolActivationCancellationSignalSource.Signal).Forget();
        }
        
        public static DisposableAction SubscribeToActiveCursorTool(Action<CursorTool> handler)
        {
            return ActiveCursorTool.Subscribe(handler);
        }
        #endregion
        
        #region Private methods
        private static async UniTaskVoid InternalActivateCursorTool(CursorTool wood, CancellationSignal cancellationSignal)
        {
            EnhancedTouchSupport.Enable(); // TODO: Maybe we should use some singleton service for doing this, because it's not a good idea to enable/disable EnhancedTouchSupport in different places.
            using (PlayerLoop.EachFrame(TrackPointer))
            {
                await cancellationSignal;
            }
            EnhancedTouchSupport.Disable(); // TODO: Maybe we should use some singleton service for doing this, because it's not a good idea to enable/disable EnhancedTouchSupport in different places.
        }
        
        /// <summary>
        /// Track pointer position and send events to the active cursor tool.
        /// </summary>
        private static void TrackPointer()
        {
            var cursor = ActiveCursorTool.Value;
            Vector2 screenPosition = Vector2.zero;

            // Проверяем, есть ли тачскрин и активные тачи
            if (Touchscreen.current != null && UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count > 0)
            {
                // Используем первый активный тач
                var touch = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0];
                screenPosition = touch.screenPosition;

                if (touch.phase == TouchPhase.Began)
                {
                    cursor.OnPress(screenPosition);
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    cursor.OnRelease(screenPosition);
                }
                else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    if (screenPosition != _PreviousPointerPosition)
                    {
                        cursor.OnMove(screenPosition);
                        _PreviousPointerPosition = screenPosition;
                    }
                }
            }
            else if (Mouse.current != null)
            {
                // Работаем с мышью
                screenPosition = Mouse.current.position.ReadValue();

                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    cursor.OnPress(screenPosition);
                }
                if (Mouse.current.leftButton.wasReleasedThisFrame)
                {
                    cursor.OnRelease(screenPosition);
                }
                if (screenPosition != _PreviousPointerPosition)
                {
                    cursor.OnMove(screenPosition);
                    _PreviousPointerPosition = screenPosition;
                }
            }
        }
        #endregion
    }
}