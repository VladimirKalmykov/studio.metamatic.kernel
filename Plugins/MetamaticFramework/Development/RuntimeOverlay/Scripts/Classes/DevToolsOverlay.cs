using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Metamatic.Framework.Workshop;
using Metamatic.UIToolKit;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Metamatic.UIToolKit.VisualElements;

namespace Metamatic.Framework
{
    using Cysharp.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary>
    /// Singleton class that manages the overlay and all the tools that are used in the overlay.
    /// </summary>
    public class DevToolsOverlay : AsyncBehaviour
    {
        public enum PresenceType
        {
            Hidden,
            Visible,
            Interactive
        }
        
        [Serializable]
        public class KeyCombination
        {
            public Key mainKey; // Основная клавиша
            public bool ctrl; // Модификатор CTRL
            public bool shift; // Модификатор SHIFT
            public bool alt; // Модификатор ALT
        }

        #region Static

        /// <summary>
        /// Provides observable access to the instance of the DevToolsOverlay.
        /// </summary>
        private static Observable<DevToolsOverlay> m_Instance = new();

        /// <summary>
        /// Get instance of the DevToolsOverlay asynchronously.
        /// </summary>
        /// <param name="signal"></param>
        /// <returns></returns>
        public static UniTask<DevToolsOverlay> GetInstance(Signal signal) =>
            m_Instance.WaitUntilValueIsNotNull(signal);

        /**
         * Contains value that form the overlay presence: Hidden, Visible, Interactive.
         * Hidden - overlay is not visible.
         * Visible - overlay is visible, but not interactive.
         * Interactive - overlay is visible and interactive.
         */
        private static readonly Observable<PresenceType> m_ObservablePresence = new();
        
        // TODO: Remove m_IsVisibleObservable and m_IsInteractiveObservable and use m_ObservablePresence instead.

        public static int Version = 1;

        /// <summary>
        /// Class that holds the persistent state of the overlay.
        /// </summary>
        [Serializable]
        [Version("0.1")]
        public class PersistentState
        {
            public int version = Version;

            public SerializableDictionary<string, PanelElement.UserPreferences> panelPreferences =
                new SerializableDictionary<string, PanelElement.UserPreferences>();

            public List<string> activeWidgets = new List<string>();
        }

        public struct WidgetInstanceMetadata
        {
            public readonly Widget Instance;
            public readonly PanelElement Panel;

            public WidgetInstanceMetadata(Widget instance, PanelElement panel)
            {
                Instance = instance;
                Panel = panel;
            }
        }

        public class WidgetBlueprint
        {
            public readonly Type type;
            public readonly MetamaticWidgetAttribute metadata;
            public readonly Observable<bool> isEnabled = new(false);
            public readonly List<WidgetInstanceMetadata> Instances = new();
            public readonly string widgetName;

            public WidgetBlueprint(Type type, MetamaticWidgetAttribute metadata)
            {
                this.type = type;
                this.metadata = metadata;

                // Split widget name by slashes
                var path = metadata.widgetName.Split('/');
                widgetName = path[path.Length - 1];
            }
        }

        public class CursorToolBlueprint
        {
            public readonly Type Type;
            public readonly MetamaticCursorToolAttribute Metadata;
            private CursorTool m_Instance;
            public CursorTool Instance => m_Instance ??= Instantiate();
            
            public CursorToolBlueprint(Type type, MetamaticCursorToolAttribute metadata)
            {
                this.Type = type;
                this.Metadata = metadata;
            }
            
            public CursorTool Instantiate()
            {
                return Activator.CreateInstance(Type) as CursorTool;
            }
        }

        #endregion

        #region Fields
        [Tooltip("Presence of the overlay on start up")]
        [SerializeField] private PresenceType m_PresenceOnStart;

        [SerializeField] private KeyCombination m_ToggleKeyCombination = new KeyCombination()
        {
            ctrl = true,
            mainKey = Key.F9
        };
        [SerializeField] private KeyCombination m_ToggleInteractiveKeyCombination = new KeyCombination()
        {
            shift = true,
            mainKey = Key.F9
        };

        #endregion

        private readonly ObservableList<CursorToolBlueprint> m_CursorTools = new();
        private readonly HashSet<string> m_CursorToolNames = new();
        private readonly Dictionary<string, WidgetBlueprint> m_Widgets = new();

        public UIDocument uiDocument { get; private set; }

        private ScrollView m_WoodsPanelContent;
        private readonly Observable<DevToolsOverlay> m_OverlayContext = new Observable<DevToolsOverlay>();
        private readonly Observable<RuntimeCore> m_CoreContext = new();
        private readonly Observable<PersistentState> m_PersistentState = new();
        private readonly Observable<string> m_CursorText = new();
        public VisualElement root { get; private set; }
        public RuntimeCore core => RuntimeCore.Instance;
        public CameraService CameraService { get; private set; }
        private VisualElement m_CursorTextElement;
        private SideFlyoutPanel m_TopSideFlyoutPanel;

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
        private SignalSource m_CursorToolActivationCancellationSignalSource;

        private readonly HashSet<VisualElement> m_HoveredPanels = new();
        public readonly Observable<bool> IsHoveringOverPanel = new(false);
        
        // Cached services
        private CursorService m_CursorService;
        private InputService m_InputService;

        /* References to visual elements */
        private CriticalNotification m_CriticalNotification;
        private Menu m_Menu;

        protected override async UniTask Run(Ready ready, Signal signal)
        {
            try
            {
                m_Instance.Value = this;
                m_OverlayContext.Value = this;
                m_CoreContext.Value = core;
                
                /* Load persistent state */
                var loadPersistentStateTask = LoadPersistentState(signal);
                
                /* Get CursorService */
                try
                {
                    m_CursorService = await Service.Resolve<CursorService>(signal, 5f);
                } catch (TimeoutException e)
                {
                    Console.Exception("MetamaticFramework", "Overlay", "Overlay requires CursorService to be run");
                    return;
                } catch (Exception e)
                {
                    Console.Exception("MetamaticFramework", "Overlay", e);
                    return;
                }
                
                // Resolve input service
                try
                {
                    m_InputService = await Service.Resolve<InputService>(signal, 5f);
                } catch (TimeoutException e)
                {
                    Console.Exception("MetamaticFramework", "Overlay", "Overlay requires InputService to be run");
                    return;
                } catch (Exception e)
                {
                    Console.Exception("MetamaticFramework", "Overlay", e);
                    return;
                }
                
                /*
                 * Apply default presence
                 * TODO: Load presence from persistent state
                 */
                m_ObservablePresence.Value = m_PresenceOnStart;
                

                /* Wait for persistent state to load */
                await loadPersistentStateTask;

                // Subscribe camera service
                using var watchCameraService = Service.WatchServiceAsync(typeof(CameraService), OnCameraServiceReady);
                
                // Prepare UIDocument and add styles
                uiDocument = GetComponent<UIDocument>();
                uiDocument.rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("Uss/MetamaticOverlay"));
                uiDocument.rootVisualElement.styleSheets.Add(UIToolKitCore.styleSheet);
                uiDocument.rootVisualElement.styleSheets.Add(UIToolKitCore.tailwindStyleSheet);
                uiDocument.rootVisualElement.AddToClassList(UIToolKitCore.DarkEnoughClassName);

                /* Create context provider */
                var contextProvider = new VisualElementContextProvider();
                contextProvider.AddContext(m_OverlayContext);
                contextProvider.AddContext(m_CoreContext);
                uiDocument.rootVisualElement.Add(contextProvider);
                root = contextProvider;
                root.AddToClassList(UIToolKitCore.Tailwind.PositionRelative);
                root.AddToClassList(UIToolKitCore.Tailwind.WidthFull);
                root.AddToClassList(UIToolKitCore.Tailwind.HeightFull);
                
                // Add critical notification visual element
                m_CriticalNotification = new CriticalNotification();
                root.Add(m_CriticalNotification);
                
                // Create cursor tools
                var cursorToolClasses = CursorTool.GetClasses();
                foreach (var cursorToolInfo in cursorToolClasses)
                {
                    RegisterCursorTool(new CursorToolBlueprint(cursorToolInfo.type, cursorToolInfo.metadata));
                }
                
                // Watch active cursor tool
                using var watchCursorTool = m_ActiveCursorTool.WatchAsync(InternalActivateCursorTool);

                // Build menu tree and widgets
                BuildMenuTreeAndWidgets();

                /* Activate those widgets that marked as activated in persistent state */
                foreach (var widget in m_Widgets)
                {
                    if (m_PersistentState.Value.activeWidgets.Contains(widget.Key))
                    {
                        ActivateWidget(widget.Value);
                    }
                }

                /* Clean up: Remove from persistent state widgets that are not in the list */
                var beforeCount = m_PersistentState.Value.activeWidgets.Count;
                m_PersistentState.Value.activeWidgets.RemoveAll(x => !m_Widgets.ContainsKey(x));
                
                /* Save widgets state to persistent state */
                if (beforeCount != m_PersistentState.Value.activeWidgets.Count)
                {
                    SavePersistentState();
                }
                
                // Create top side flyout panel
                m_TopSideFlyoutPanel = new SideFlyoutPanel(SideFlyoutPanel.Position.Top, true);
                
                // Add toggle button
                var toggleButton = new SwitchButton<PresenceType>(m_ObservablePresence);
                toggleButton.AddChoice("Hidden", PresenceType.Hidden);
                toggleButton.AddChoice("Visible", PresenceType.Visible);
                toggleButton.AddChoice("Interactive", PresenceType.Interactive);
                m_TopSideFlyoutPanel.Add(toggleButton);
                
                root.Add(m_TopSideFlyoutPanel);
                
                // Subscribe to presence changes
                using var presenceSubscription = m_ObservablePresence.WatchAsync(OnPresenceChange);
                // Subscribe to hover panel changes
                using var watchHoveringOverPanel = IsHoveringOverPanel.WatchAsync(OnHoveringOverPanelChanged);
                await ready();
            }
            catch (Exception e)
            {
                Console.Exception("MetamaticFramework", "Overlay", e);
            }
        }
        
        private async UniTaskVoid OnCameraServiceReady(Service service, Signal signal)
        {
            if (service is CameraService cameraService)
            {
                CameraService = cameraService;
            }
            await signal;
            CameraService = null;
        }

        private async UniTaskVoid OnPresenceChange(PresenceType presenceType, Signal signal)
        {
            uiDocument.rootVisualElement.style.display = presenceType == PresenceType.Hidden ? DisplayStyle.None : DisplayStyle.Flex;
            var isInteractive = presenceType == PresenceType.Interactive;
            uiDocument.rootVisualElement.SetPickingModeRecursively(isInteractive ? PickingMode.Position : PickingMode.Ignore); 
            
            if (isInteractive)
            {
                m_InputService.DeactivateInput();
                using (m_CursorService.SetCursor(new CursorService.CursorConfig(true)))
                {
                    await signal;
                }
            }
            else
            {
                m_ActiveCursorTool.Value = null;
                m_InputService.ActivateInput();
                uiDocument.rootVisualElement.Focus();
            }

            await signal;
        }

        private void BuildMenuTreeAndWidgets()
        {
            var menuItemMethods = MetamaticMenuItemAttribute.GetMenuItemMethods();
            var widgetClasses = Widget.GetWidgetClasses();

            var widgetSortIndex = 0;
            foreach (var widgetInfo in widgetClasses)
            {
                var widgetConfig = new WidgetBlueprint(widgetInfo.type, widgetInfo.metadata);
                m_Widgets.Add(widgetInfo.Item2.widgetName, widgetConfig);
                menuItemMethods.Add(new MenuItemConfig($"Widgets/{widgetInfo.Item2.widgetName}", () =>
                {
                    if (widgetConfig.isEnabled.value)
                    {
                        DeactivateWidget(widgetConfig);
                    }
                    else
                    {
                        ActivateWidget(widgetConfig);
                    }
                }, ++widgetSortIndex, widgetConfig.isEnabled));
            }

            m_Menu = new Menu();

            // Add menu items from MenuItem attributes
            foreach (var menuMethod in menuItemMethods)
            {
                var current = m_Menu;
                var path = menuMethod.Name.Split('/');
                for (var i = 0; i < path.Length; i++)
                {
                    var isLast = i == path.Length - 1;
                    var menuItem = current.Select(path[i]);
                    if (menuItem == null)
                    {
                        if (isLast)
                        {
                            menuItem = current.CreateMenuItem(path[i], menuMethod.boolState, menuMethod.Action);
                        }
                        else
                        {
                            menuItem = current.CreateMenuItem(path[i]);
                        }
                    }

                    if (!isLast)
                    {
                        current = !menuItem.HasSubMenu ? menuItem.AddSubMenu() : menuItem.SubMenu.value;
                    }
                }
            }
            
            var menu = new MenuElement(MenuElement.Orientation.Horizontal, m_Menu);
            root.Add(menu);
        }
        
        private async UniTaskVoid OnHoveringOverPanelChanged(bool hovering, Signal signal)
        {
            if (hovering)
            {
                /* Set default visible cursor to override any custom cursor (eg.created by cursor tool) */
                using var showCursor = m_CursorService.SetCursor(new CursorService.CursorConfig(true));
                await signal;
            }
        }

        private void Update()
        {
            if (IsToggleKeyCombinationPressed())
            {
                ToggleOverlay();
            } else if (IsToggleInteractiveKeyCombinationPressed())
            {
                if (m_ObservablePresence.value != PresenceType.Interactive)
                {
                    m_ObservablePresence.value = PresenceType.Interactive;
                }
                else
                {
                    m_ObservablePresence.value = PresenceType.Visible;
                }
            }
        }

        private bool IsToggleKeyCombinationPressed()
        {
            bool isMainKeyPressed = Keyboard.current[m_ToggleKeyCombination.mainKey].wasPressedThisFrame;
            bool isCtrlPressed = !m_ToggleKeyCombination.ctrl || Keyboard.current[Key.LeftCtrl].isPressed ||
                                 Keyboard.current[Key.RightCtrl].isPressed;
            bool isShiftPressed = !m_ToggleKeyCombination.shift || Keyboard.current[Key.LeftShift].isPressed ||
                                  Keyboard.current[Key.RightShift].isPressed;
            bool isAltPressed = !m_ToggleKeyCombination.alt || Keyboard.current[Key.LeftAlt].isPressed ||
                                Keyboard.current[Key.RightAlt].isPressed;

            return isMainKeyPressed && isCtrlPressed && isShiftPressed && isAltPressed;
        }

        private bool IsToggleInteractiveKeyCombinationPressed()
        {
            bool isMainKeyPressed = Keyboard.current[m_ToggleInteractiveKeyCombination.mainKey].wasPressedThisFrame;
            bool isCtrlPressed = !m_ToggleInteractiveKeyCombination.ctrl || Keyboard.current[Key.LeftCtrl].isPressed ||
                                 Keyboard.current[Key.RightCtrl].isPressed;
            bool isShiftPressed = !m_ToggleInteractiveKeyCombination.shift || Keyboard.current[Key.LeftShift].isPressed ||
                                  Keyboard.current[Key.RightShift].isPressed;
            bool isAltPressed = !m_ToggleInteractiveKeyCombination.alt || Keyboard.current[Key.LeftAlt].isPressed ||
                                Keyboard.current[Key.RightAlt].isPressed;

            return isMainKeyPressed && isCtrlPressed && isShiftPressed && isAltPressed;
        }

        private void ToggleOverlay()
        {
            switch (m_ObservablePresence.value)
            {
                case PresenceType.Hidden:
                    m_ObservablePresence.value = PresenceType.Visible;
                    break;
                case PresenceType.Visible:
                case PresenceType.Interactive:
                    m_ObservablePresence.value = PresenceType.Hidden; // TODO: Maybe we should remember the last state of the overlay and restore it.
                    break;
            }
        }
        
        public void ToggleInteractiveMode()
        {
            if (m_ObservablePresence.value != PresenceType.Interactive)
            {
                m_ObservablePresence.value = PresenceType.Interactive;
            }
            else
            {
                m_ObservablePresence.value = PresenceType.Visible;
            }
        }

        private void ActivateWidget(WidgetBlueprint blueprint)
        {
            // Instantiate widget by type
            var widgetInstance = Activator.CreateInstance(blueprint.type) as Widget;

            if (widgetInstance == null)
            {
                Console.Exception("MetamaticFramework", "Overlay",
                    $"Failed to instantiate widget {blueprint.type.Name}");
                return;
            }

            var preferences = widgetInstance.GetDefaultPreferences();
            /* Create panel for widget */
            var panel = CreatePanel(blueprint.metadata.widgetName, blueprint.widgetName, new PanelElement.Constraints()
            {
                DefaultPreferences = new PanelElement.UserPreferences()
                {
                    Position = preferences.Position,
                    Size = preferences.Size
                },
                MaxSize = preferences.MaxSize,
                MinSize = preferences.MinSize,
                AutoHeight = true,
                AutoWidth = false
            }, onClose: () => { DeactivateWidget(blueprint); });
            root.Add(panel);


            // Add widget to panel
            panel.AddContent(widgetInstance);

            // Add widget to list
            blueprint.Instances.Add(new WidgetInstanceMetadata(widgetInstance, panel));

            // Make list active
            blueprint.isEnabled.Value = true;

            // Save persistent state
            m_PersistentState.Value.activeWidgets.Add(blueprint.metadata.widgetName);
            SavePersistentState();
        }

        private void DeactivateWidget(WidgetBlueprint blueprint)
        {
            foreach (var metadata in blueprint.Instances)
            {
                metadata.Panel.Remove(metadata.Instance);
                // Remove panel
                root.Remove(metadata.Panel);
            }

            blueprint.Instances.Clear();
            blueprint.isEnabled.Value = false;

            // Save persistent state
            m_PersistentState.Value.activeWidgets.Remove(blueprint.metadata.widgetName);
            SavePersistentState();
        }
        
        public void Notify(string message, float duration)
        {
            m_CriticalNotification.Show(message, duration);
        }
        
        public DisposableAction Notify(string message, CriticalNotification.MessageType messageType)
        {
            return m_CriticalNotification.Show(message, messageType);
        }

        public PanelElement CreatePanel(string name, string title, PanelElement.Constraints constraints = default,
            Action onClose = null)
        {
            var panel = new PanelElement(name, title, constraints, onClose: onClose);
            return panel;
        }

        public void SetCursorText(string text)
        {
            m_CursorText.Value = text;
        }

        public PanelElement.UserPreferences GetPanelPreferences(string panelName,
            PanelElement.UserPreferences defaultPreferences)
        {
            /* Search panel in persistent state */
            if (m_PersistentState.value.panelPreferences.ContainsKey(panelName))
            {
                return m_PersistentState.value.panelPreferences[panelName];
            }

            return defaultPreferences;
        }

        public void SetPanelPreferences(string panelName, PanelElement.UserPreferences preferences)
        {
            m_PersistentState.value.panelPreferences[panelName] = preferences;
            SavePersistentState();
        }

        public async UniTask LoadPersistentState(Signal signal)
        {
            var localStorage = Repository.localStorageLegacy;
            var state = await localStorage.TryGet("metamatic-framework/overlay", () => new PersistentState(),
                signal);
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
            Repository.localStorageLegacy.Set("metamatic-framework/overlay", m_PersistentState.Value, CancellationToken.None).Forget();
        }

        public DisposableAction RegisterCursorTool(CursorToolBlueprint cursorTool)
        {
            var pathName = cursorTool.Metadata.PathName;
            var instance = cursorTool.Instantiate();
            if (m_CursorToolNames.Contains(pathName))
            {
                Debug.LogWarning($"Cursor tool with path name {pathName} already exists.");
                return DisposableAction.Empty;
            }

            m_CursorToolNames.Add(pathName);
            m_CursorTools.Add(cursorTool);

            return new DisposableAction(() =>
            {
                if (m_ActiveCursorTool.Value == pathName)
                {
                    m_CursorToolActivationCancellationSignalSource.TryRaise();
                }

                m_CursorToolNames.Remove(pathName);
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

        public DisposableAction ObserveCursorTools(Func<CursorToolBlueprint, int, Signal, UniTaskVoid> handler)
        {
            return m_CursorTools.WatchEachLifecycle(handler);
        }

        /// <summary>
        /// Active wood. Activated wood will receive mouse events.
        /// </summary>
        /// <param name="cursorTool"></param>
        /// <param name="signal"></param>
        public void ActivateCursorTool(string pathName)
        {
            m_ActiveCursorTool.Value = pathName;
        }

        public void ToggleActivate(string pathName)
        {
            if (m_ActiveCursorTool.Value == pathName)
            {
                m_ActiveCursorTool.Value = null;
            }
            else
            {
                m_ActiveCursorTool.Value = pathName;
            }
        }
        
        /// <summary>
        /// Subscribe to active cursor tool changes.
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public DisposableAction SubscribeToActiveCursorTool(Action<string> handler)
        {
            return m_ActiveCursorTool.Watch(handler);
        }

        private async UniTaskVoid InternalActivateCursorTool(string pathName, Signal cancellationSignal)
        {
            if (pathName == null)
            {
                return;
            }

            EnhancedTouchSupport
                .Enable(); // TODO: Maybe we should use some singleton service for doing this, because it's not a good idea to enable/disable EnhancedTouchSupport in different places.
            var cursorTool = m_CursorTools.FirstOrDefault(x => x.Metadata.PathName == pathName);
            if (cursorTool == null)
            {
                Console.Exception("MetamaticFramework", "RuntimeCore", $"Cursor tool with name {pathName} not found.");
                return;
            }

            var instance = cursorTool.Instance;
            instance.OnActivate(this, cancellationSignal).Forget();
            using var watchPointerEvents = PlayerLoop.EachFrame(() => TrackPointer(instance));
            await cancellationSignal;
            EnhancedTouchSupport
                .Disable(); // TODO: Maybe we should use some singleton service for doing this, because it's not a good idea to enable/disable EnhancedTouchSupport in different places.
        }

        /// <summary>
        /// Track pointer position and send events to the active cursor tool.
        /// </summary>
        private void TrackPointer(CursorTool cursor)
        {
            Vector2 screenPosition = Vector2.zero;
            if (IsHoveringOverPanel.value)
            {
                /* Ignore cursor tool if hovering over panel */
                return;
            }

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