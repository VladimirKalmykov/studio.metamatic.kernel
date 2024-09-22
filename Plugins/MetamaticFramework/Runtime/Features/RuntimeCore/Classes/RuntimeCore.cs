using System;
using System.Collections.Generic;
using System.Linq;
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
    public class RuntimeCore
    {
        private static RuntimeCore _Instance;
        // ReSharper disable once InconsistentNaming
        public static RuntimeCore Instance => _Instance ??= new RuntimeCore();
        
        private readonly ManagedList<CursorTool> m_CursorTools = new();
        private readonly HashSet<string> m_CursorToolNames = new();
        
        #region Private observables
        /// <summary>
        /// Observable active cursor tool.
        /// </summary>
        private readonly Observable<CursorTool> m_ActiveCursorTool = new();
        /// <summary>
        /// Contains observable cursor label. It can be used to display some information that appears when the cursor is over some object.
        /// </summary>
        private readonly Observable<string> m_CursorLabel = new(string.Empty);
        #endregion
        
        #region Various fields
        /// <summary>
        /// Signal source for active cursor tool. It will be canceled when the active cursor tool is changed.
        /// </summary>
        private CancellationSignalSource m_CursorToolActivationCancellationSignalSource;
        /// <summary>
        /// Helper variable to store the previous pointer position.
        /// </summary>
        private Vector2 m_PreviousPointerPosition;
        private readonly HashSet<GameObject> m_SelectedObjects = new(); 
        #endregion
        
        #region Cached properties
        public Type[] SpellTypes { get; private set; }
        #endregion
        
        #region Initialization

        private RuntimeCore()
        {
            /* Find all structures that implement the ISpell interface */
            var spellTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(ISpell).IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);
            var enumerable = spellTypes as Type[] ?? spellTypes.ToArray();
            Console.Info("Metamatic Framework", $"Found {enumerable.Count()} spells");
            SpellTypes = enumerable.ToArray();
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            _Instance = new RuntimeCore();
        }
        #endregion
        
        #region Public methods
        public DisposableAction RegisterCursorTool(CursorTool cursorTool)
        {
            if (m_CursorToolNames.Contains(cursorTool.Name))
            {
                Debug.LogWarning($"Wood with name {cursorTool.Name} already exists.");
                return DisposableAction.Empty;
            }
            m_CursorToolNames.Add(cursorTool.Name);
            m_CursorTools.Add(cursorTool);
            return new DisposableAction(() =>
            {
                m_CursorToolNames.Remove(cursorTool.Name);
                m_CursorTools.Remove(cursorTool);
            });
        }

        public DisposableAction ObserveCursorTools(Func<CursorTool, CancellationSignal, UniTaskVoid> handler)
        {
            return m_CursorTools.Observe(handler);
        }
        
        public DisposableAction SubscribeToCursorLabel(Action<string> handler)
        {
            return m_CursorLabel.Subscribe(handler);
        }
        
        public void SetCursorLabel(string label)
        {
            m_CursorLabel.Value = label;
        }
        
        /// <summary>
        /// Active wood. Activated wood will receive mouse events.
        /// </summary>
        /// <param name="cursorTool"></param>
        /// <param name="signal"></param>
        public void Activate(CursorTool cursorTool, CancellationSignal signal)
        {
            m_ActiveCursorTool.Value = cursorTool;
            m_CursorToolActivationCancellationSignalSource.TryCancel();
            m_CursorToolActivationCancellationSignalSource = CancellationSignalSource.CreateLinked(signal);
            InternalActivateCursorTool(cursorTool, m_CursorToolActivationCancellationSignalSource.Signal).Forget();
            cursorTool.OnActivate(this, m_CursorToolActivationCancellationSignalSource.Signal).Forget();
        }
        
        public DisposableAction SubscribeToActiveCursorTool(Action<CursorTool> handler)
        {
            return m_ActiveCursorTool.Subscribe(handler);
        }
        
        public void SelectObject(GameObject gameObject)
        {
            m_SelectedObjects.Add(gameObject);
        }
        
        public void ToggleObjectSelection(GameObject gameObject)
        {
            if (m_SelectedObjects.Contains(gameObject))
            {
                m_SelectedObjects.Remove(gameObject);
            }
            else
            {
                m_SelectedObjects.Add(gameObject);
            }
        }
        #endregion
        
        #region Private methods
        private async UniTaskVoid InternalActivateCursorTool(CursorTool wood, CancellationSignal cancellationSignal)
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
        private void TrackPointer()
        {
            var cursor = m_ActiveCursorTool.Value;
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
                    if (screenPosition != m_PreviousPointerPosition)
                    {
                        cursor.ScreenPosition = screenPosition;
                        cursor.OnMove(screenPosition);
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
                    cursor.OnPress(screenPosition);
                }
                if (Mouse.current.leftButton.wasReleasedThisFrame)
                {
                    cursor.OnRelease(screenPosition);
                }
                if (screenPosition != m_PreviousPointerPosition)
                {
                    cursor.ScreenPosition = screenPosition;
                    cursor.OnMove(screenPosition);
                    m_PreviousPointerPosition = screenPosition;
                }
            }
        }
        #endregion
    }
}