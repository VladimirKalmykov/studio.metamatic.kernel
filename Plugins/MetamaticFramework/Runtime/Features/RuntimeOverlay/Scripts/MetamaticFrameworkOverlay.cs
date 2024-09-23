using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Metamatic.Framework.CursorTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Metamatic.Framework
{
    using Cysharp.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.UIElements;
    using Cursor = UnityEngine.Cursor;

    public class MetamaticFrameworkOverlay : AsyncBehaviour
    {
        public static int Version = 1;

        [Serializable]
        [Version("0.1")]
        public class PersistentState
        {
            public int version = Version;

            public SerializableDictionary<string, Panel.UserPreferences> panelPreferences =
                new SerializableDictionary<string, Panel.UserPreferences>();
        }

        private readonly ObservableList<CursorTool> m_CursorTools = new();
        private readonly HashSet<string> m_CursorToolNames = new();

        public UIDocument uiDocument { get; private set; }

        private ScrollView m_WoodsPanelContent;
        private readonly Observable<MetamaticFrameworkOverlay> m_Context = new Observable<MetamaticFrameworkOverlay>();
        private readonly Observable<PersistentState> m_PersistentState = new();
        private readonly Observable<string> m_CursorText = new();
        public VisualElement root { get; private set; }
        public RuntimeCore core => RuntimeCore.Instance;
        public CameraService CameraService { get; private set; }
        private VisualElement m_CursorTextElement;

        /// <summary>
        /// Helper variable to store the previous pointer position.
        /// </summary>
        private Vector2 m_PreviousPointerPosition;

        /// <summary>
        /// Observable active cursor tool.
        /// </summary>
        private readonly Observable<string> m_ActiveCursorTool = new();

        /// <summary>
        /// Signal source for active cursor tool. It will be canceled when the active cursor tool is changed.
        /// </summary>
        private CancellationSignalSource m_CursorToolActivationCancellationSignalSource;
        private readonly HashSet<VisualElement> m_HoveredPanels = new();
        public readonly Observable<bool> IsHoveringOverPanel = new(false);

        protected override async UniTask Run(Ready ready, CancellationSignal signal)
        {
            m_Context.Value = this;
            /* Load persistent state */
            await LoadPersistentState();
            
            CameraService = await Service.Resolve<CameraService>(signal);

            uiDocument = GetComponent<UIDocument>();
            uiDocument.rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("Uss/MetamaticOverlay"));

            /* Create context provider */
            var contextProvider = new VisualElementContextProvider();
            contextProvider.AddContext(m_Context);
            uiDocument.rootVisualElement.Add(contextProvider);
            root = contextProvider;

            /* Add system tools */
            RegisterCursorTool(new SelectorCursorTool());
            SequentialHandler<string>.Subscribe(m_ActiveCursorTool.Watch, InternalActivateCursorTool);

            /* Create cursor tools panel */
            using var toolsWidget = new CursorToolsWidget(this);
            using var selectedObjectWidget = new SelectedObjectWidget(this);
            using var spellsWidget = new SpellsWidget(this);
            using var spellWidget = new SpellWidget(this);

            await ready();
        }

        public Panel CreatePanel(string name, string title, Panel.Constraints constraints = default)
        {
            var panel = new Panel(name, title, constraints);
            return panel;
        }

        public void SetCursorText(string text)
        {
            m_CursorText.Value = text;
        }

        public Panel.UserPreferences GetPanelPreferences(string panelName, Panel.UserPreferences defaultPreferences)
        {
            /* Search panel in persistent state */
            if (m_PersistentState.value.panelPreferences.ContainsKey(panelName))
            {
                return m_PersistentState.value.panelPreferences[panelName];
            }

            return defaultPreferences;
        }

        public void SetPanelPreferences(string panelName, Panel.UserPreferences preferences)
        {
            m_PersistentState.value.panelPreferences[panelName] = preferences;
            SavePersistentState();
        }

        public async UniTask LoadPersistentState()
        {
            var localStorage = Repository.LocalStorage;
            var state = await localStorage.TryGet("metamatic-framework/overlay", () => new PersistentState(),
                CancellationToken.None);
            if (state != null)
            {
                m_PersistentState.Value = state;
            }
            else
            {
                m_PersistentState.Value = new PersistentState();
            }
        }

        public void SavePersistentState()
        {
            var localStorage = Repository.LocalStorage;
            localStorage.Set("metamatic-framework/overlay", m_PersistentState.Value, CancellationToken.None).Forget();
        }

        public DisposableAction RegisterCursorTool(CursorTool cursorTool)
        {
            if (m_CursorToolNames.Contains(cursorTool.Name))
            {
                Debug.LogWarning($"Wood with name {cursorTool.Name} already exists.");
                return DisposableAction.Empty;
            }

            m_CursorToolNames.Add(cursorTool.Name);
            m_CursorTools.Add(cursorTool);

            if (m_ActiveCursorTool.Value == cursorTool.Name)
            {
                Activate(cursorTool);
            }

            return new DisposableAction(() =>
            {
                if (m_ActiveCursorTool.Value == cursorTool.Name)
                {
                    m_CursorToolActivationCancellationSignalSource.TryCancel();
                }

                m_CursorToolNames.Remove(cursorTool.Name);
                m_CursorTools.Remove(cursorTool);
            });
        }
        
        public void AddHoveredPanel(VisualElement panel)
        {
            m_HoveredPanels.Add(panel);
            if (m_HoveredPanels.Count() == 1)
            {
                IsHoveringOverPanel.Value = true;
            }
        }
        
        public void RemoveHoveredPanel(VisualElement panel)
        {
            m_HoveredPanels.Remove(panel);
            if (m_HoveredPanels.Count == 0)
            {
                IsHoveringOverPanel.Value = false;
            }
        }

        public DisposableAction ObserveCursorTools(Func<CursorTool, CancellationSignal, UniTaskVoid> handler)
        {
            return m_CursorTools.Observe(handler);
        }

        /// <summary>
        /// Active wood. Activated wood will receive mouse events.
        /// </summary>
        /// <param name="cursorTool"></param>
        /// <param name="signal"></param>
        public void Activate(CursorTool cursorTool)
        {
            m_ActiveCursorTool.Value = cursorTool.Name;
        }

        public void ToggleActivate(CursorTool cursorTool)
        {
            if (m_ActiveCursorTool.Value == cursorTool.Name)
            {
                m_ActiveCursorTool.Value = null;
            }
            else
            {
                m_ActiveCursorTool.Value = cursorTool.Name;
            }
        }

        public DisposableAction SubscribeToActiveCursorTool(Action<string> handler)
        {
            return m_ActiveCursorTool.Watch(handler);
        }

        private async UniTaskVoid InternalActivateCursorTool(string name, CancellationSignal cancellationSignal)
        {
            if (name == null)
            {
                return;
            }

            EnhancedTouchSupport
                .Enable(); // TODO: Maybe we should use some singleton service for doing this, because it's not a good idea to enable/disable EnhancedTouchSupport in different places.
            var cursorTool = m_CursorTools.FirstOrDefault(x => x.Name == name);
            if (cursorTool == null)
            {
                Console.Exception("MetamaticFramework", "RuntimeCore", $"Cursor tool with name {name} not found.");
                return;
            }

            cursorTool.OnActivate(this, cancellationSignal).Forget();
            using (PlayerLoop.EachFrame(() => TrackPointer(cursorTool)))
            {
                await cancellationSignal;
            }

            EnhancedTouchSupport
                .Disable(); // TODO: Maybe we should use some singleton service for doing this, because it's not a good idea to enable/disable EnhancedTouchSupport in different places.
        }

        /// <summary>
        /// Track pointer position and send events to the active cursor tool.
        /// </summary>
        private void TrackPointer(CursorTool cursor)
        {
            Vector2 screenPosition = Vector2.zero;

            // Проверяем, есть ли тачскрин и активные тачи
            if (Touchscreen.current != null && UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count > 0)
            {
                // Используем первый активный тач
                var touch = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0];
                screenPosition = touch.screenPosition;

                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    cursor.OnPress(screenPosition, new InputModifiers()); // TODO: Support input modifiers for touch
                }
                else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                         touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    cursor.OnRelease(screenPosition);
                }
                else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                         touch.phase == UnityEngine.InputSystem.TouchPhase.Stationary)
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
                    var shiftIsPressed = Keyboard.current.shiftKey.isPressed;
                    var altIsPressed = Keyboard.current.altKey.isPressed;
                    var controlIsPressed = Keyboard.current.ctrlKey.isPressed;
                    cursor.OnPress(screenPosition, new InputModifiers()
                    {
                        Alt = altIsPressed,
                        Control = controlIsPressed,
                        Shift = shiftIsPressed
                    });
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
    }
}