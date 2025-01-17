using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Metamatic.EditorTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace Metamatic.EditorInspector
{
    public class ConsolePanelVisualElement: AsyncVisualElementLegacy
    {
        public class LogLine : VisualElement
        {
            private static VisualTreeAsset m_LineTemplate;
            private static Texture2D m_UnityLogo;
            private static Texture2D m_MetamaticLogo;
            private static Texture2D unityLogo
            {
                get
                {
                    if (m_UnityLogo == null)
                    {
                        m_UnityLogo = AssetDatabase.LoadAssetAtPath<Texture2D>(UnityLogoPath);
                    }
                    return m_UnityLogo;
                }
            }
            private static Texture2D metamaticLogo
            {
                get
                {
                    if (m_MetamaticLogo == null)
                    {
                        m_MetamaticLogo = AssetDatabase.LoadAssetAtPath<Texture2D>(MetamaticLogoPath);
                    }
                    return m_MetamaticLogo;
                }
            }
            private VisualElement m_Tags;
            private Label m_Content;
            private LogMetadata m_Metadata;
            private ConsolePanelVisualElement m_ConsolePanel;

            private const string MetamaticLogoPath =
                "Packages/studio.metamatic.kernel/Editor/Sprites/metamatic-logo-32.png";
            private const string UnityLogoPath = "Packages/studio.metamatic.kernel/Editor/Sprites/unity-logo-32.png";
            
            public LogLine(ConsolePanelVisualElement consolePanel, LogMetadata metadata): base()
            {
                m_ConsolePanel = consolePanel;
                m_Metadata = metadata;
                if (m_LineTemplate == null)
                {
                    m_LineTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/studio.metamatic.kernel/Editor/Inspector/Panels/Console/Line.uxml");
                }
                m_LineTemplate.CloneTree(this);
            
                /* Icon */
                var icon = this.Query<VisualElement>("Icon").First();
                icon.style.backgroundImage = metadata.IsBuiltInLog ? unityLogo : metamaticLogo;
                
                /* Content */
                m_Content = this.Query<Label>("Content").First();
                m_Content.text = metadata.Text;
                
                /* Tags */
                m_Tags = this.Query<VisualElement>("Tags");
                m_Tags.Clear();
                
                /* Timestamp */
                var timestamp = this.Query<Label>("TimeStamp").First();
                timestamp.text = metadata.TimeStamp.ToString("HH:mm:ss:fff");
                
                foreach (var tag in metadata.Tags)
                {
                    AddTag(tag);
                }
                
                m_Content.RegisterCallback<MouseDownEvent>(evt =>
                {
                    m_ConsolePanel.selectedLogId = m_ConsolePanel.selectedLogId == m_Metadata.Id ? "" : m_Metadata.Id;
                });
            }
        
            public void AddTag(string tag)
            {
           
                m_Tags.Add(ConsolePanelVisualElement.CreateTagElement(tag));
            }

            public void ClearTags()
            {
                m_Tags.Clear();
            }
        }
        private static StyleSheet m_StyleSheet;
        public static StyleSheet StyleSheet
        {
            get
            {
                if (m_StyleSheet == null)
                {
                    m_StyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(EditorGUIUtility.isProSkin
                        ? KDarkStylePath
                        : KLightStylePath);
                }

                return m_StyleSheet;
            }
        }
        public static Dictionary<string, int> TagsFilter = new Dictionary<string, int>();
        public static void ClearTagsFilter()
        {
            var tags = TagsFilter.Keys;
            TagsFilter.Clear();
            OnTagsFilterChanged?.Invoke(Array.Empty<string>(), tags.ToArray());
        }

        public static void ToggleTagFilter(string tag, int nextState)
        {
            if (TagsFilter.TryGetValue(tag, out var state))
            {
                if (state == nextState)
                {
                    TagsFilter.Remove(tag);
                    OnTagsFilterChanged?.Invoke(Array.Empty<string>(), new[] { tag });
                } else if (nextState == 0)
                {
                    TagsFilter[tag] = 0;
                    OnTagsFilterChanged?.Invoke(new[] { tag }, new[] { tag });
                }
                else
                {
                    TagsFilter[tag] = 1;
                    OnTagsFilterChanged?.Invoke(new[] { tag }, new[] { tag });
                }
            }
            else
            {
                TagsFilter[tag] = nextState;
                OnTagsFilterChanged?.Invoke(new[] { tag }, Array.Empty<string>());
            }
            /* Save tags filter to preferences */
            PersistentSaveTagsFilter();
        }
        
        private static void PersistentSaveTagsFilter()
        {
            /* Converts tags filter to string by template {tagName}:{value}, e.g tag1:1,tag2:0 */
            var tags = TagsFilter.Select(pair => $"{pair.Key}:{pair.Value}");
            PlayerPrefs.SetString("MetamaticConsoleTagsFilter", string.Join(",", tags));
        }
        
        private static void PersistentLoadTagsFilter()
        {
            TagsFilter.Clear();
            var prefTags = PlayerPrefs.GetString("MetamaticConsoleTagsFilter", "");
            if (prefTags.Length > 0)
            {
                var tags = prefTags.Split(',');
                foreach (var tag in tags)
                {
                    var parts = tag.Split(':');
                    if (parts.Length == 2)
                    {
                        TagsFilter[parts[0]] = int.Parse(parts[1]);
                    }
                }
            }
        }
        
        public static void SetTagsFilter(string[] tags)
        {
            TagsFilter.Clear();
            foreach (var tag in tags)
            {
                TagsFilter[tag] = 1;
            }
            OnTagsFilterChanged?.Invoke(tags, Array.Empty<string>());
            
            PersistentSaveTagsFilter();
        }

        public static event Action<string[], string[]> OnTagsFilterChanged;
        public static Dictionary<string, Color> TagColors = new Dictionary<string, Color>
        {
            {"Log", new Color(0.2f, 0.2f, 0.2f)},
            {"Warning", new Color(0.66f, 0.37f, 0f)},
            {"Error", new Color(0.78f, 0.16f, 0.07f)},
        };
        
        public static VisualElement CreateTagElement(string tag, bool disabled = false)
        {
            Color color = new Color(0.14f, 0.12f, 0.11f);
            
            if (!disabled && !TagColors.TryGetValue(tag, out color))
            {
                /* Generate random color with green and blue dominant */
                var isGreenDominant = Random.Range(0, 2) == 0;
                var firstNonDominant = Random.Range(41, 162);
                var secondNonDominant = Random.Range(41, 162);
                color = new Color(firstNonDominant / 255f, isGreenDominant ? (161f / 255f) : secondNonDominant / 255f, isGreenDominant ? secondNonDominant / 255f : (161f / 255f));
                TagColors[tag] = color;
            }
            var tagElement = new Label(tag);
            tagElement.AddToClassList("Tag");
            tagElement.style.backgroundColor = color;
            /* Detect click on tag */
            tagElement.RegisterCallback<MouseUpEvent>(evt =>
            {
                /* Detect right click */
                if (evt.button == 1)
                {
                    ToggleTagFilter(tag, 0);
                }
                else
                {
                    ToggleTagFilter(tag, 1);
                }
            });
            return tagElement;
        }
        
        private ScrollView m_Content;
        private VisualElement m_TagsFilterContainer;
        private VisualElement m_Details;
        private bool m_AutoScroll = true;
        private bool m_LockScroll = false;
        public Observable<bool> DetailsVisible = new();
        private Dictionary<string, VisualElement> m_LogRepresentation = new Dictionary<string, VisualElement>();
        private string m_PrevSelectedLogId = "";
        private string m_SearchState = "";
        /// <summary>
        /// Contains log id of currently selected log
        /// </summary>
        private Observable<string> m_SelectedLogId = new("");
        /// <summary>
        /// Currently selected log id
        /// </summary>
        public string selectedLogId
        {
            get => m_SelectedLogId.value;
            set
            {
                m_SelectedLogId.value = value;
            }
        }
        
        
        private const string KLightStylePath = "Packages/studio.metamatic.kernel/Editor/Inspector/Panels/Console/ConsoleDark.uss";
        private const string KDarkStylePath = "Packages/studio.metamatic.kernel/Editor/Inspector/Panels/Console/ConsoleDark.uss";
        
        public ConsolePanelVisualElement(Signal signal) : base(signal)
        {
        }
        
        protected override async UniTask InitializeAsync(Func<UniTask> ready, Signal signal)
        {
            /* Set height 100% */
            style.height = new StyleLength(Length.Percent(100));
            
            /* Read layout from uxml */
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/studio.metamatic.kernel/Editor/Inspector/Panels/Console/Console.uxml");
            template.CloneTree(this);
            styleSheets.Add(StyleSheet);
            
            /* Built-in Log Checkbox */
            var builtInLogCheckbox = this.Query<Toggle>("BuiltInLogCheckbox").First();
            builtInLogCheckbox.value = StaticLogsCollector.isBuiltInLogEnabled;
            builtInLogCheckbox.RegisterValueChangedCallback(evt =>
            {
                StaticLogsCollector.ChangeBuiltInLogEnable(!StaticLogsCollector.isBuiltInLogEnabled);
            });
            
            /* Tags filter container */
            m_TagsFilterContainer = this.Query<VisualElement>("TagsFilter");
            
            /* Clear button */
            var clearBottom = this.Query<Button>("ClearButton").First();
            clearBottom.clicked += ClearHistory;
            
           
            
            /* Content */
            m_Content = this.Query<ScrollView>("Content");
            
            /* Details */
            m_Details = this.Query<VisualElement>("Details");
            
            m_Content.RegisterCallback<PointerCaptureEvent>(evt => OnUserScrollStart());
            m_Content.RegisterCallback<WheelEvent>(evt => OnUserScrollStart());
            
            /* Get player loop update */
            using var playerLoopUpdate = AsyncUtils.PlayerLoopAction(FrameUpdate, cancellationToken: signal);
            /* Details visible state */
            using var detailsVisibleListener = DetailsVisible.Watch(OnDetailsVisibleChanged);
            /* Listen for selected log changes */
            using var selectedLogListener = m_SelectedLogId.Watch(ApplySelectedLog);
            
           
            
            /* Listen tags filter changes */
            OnTagsFilterChanged += TagsFilterChanged;
            
            /* Load tags filter from preferences */
            PersistentLoadTagsFilter();
            
            /* Apply tags filter */
            m_TagsFilterContainer.Clear();
            foreach (var tag in TagsFilter)
            {
                var tagElement = CreateTagElement(tag.Key, tag.Value == 0);
                m_TagsFilterContainer.Add(tagElement);
            }
            
            /* Subscribe to log events */
            using var logEventListener = StaticLogsCollector.SubscribeToLogHistory(OnLogReceived);
            EditorGUI.hyperLinkClicked += EditorGUI_HyperLinkClicked;
            
            /* Search input */
            var searchInput = this.Query<UnityEngine.UIElements.TextField>("SearchInput").First();
            searchInput.RegisterValueChangedCallback(evt =>
            {
                m_SearchState = evt.newValue;
                ApplyLogsListFilter();
            });
            
            /* Subscribe built-in log flag */
            using var builtInSubscription = StaticLogsCollector.builtInLogEnabled.Watch(value =>
            {
                builtInLogCheckbox.value = value;
                ApplyLogsListFilter();
            });
            
            /* Command input */
            var commandsHistory = new List<string>();
            var commandHistoryCursor = 0;
            var commandInput = this.Query<UnityEngine.UIElements.TextField>("CommandInput").First();
            commandInput.RegisterCallback<KeyDownEvent>(evt =>
            {
                /* If arrow up or down pressed navigate through commands history */
                if (evt.keyCode == KeyCode.UpArrow)
                {
                    if (commandHistoryCursor > 0)
                    {
                        commandHistoryCursor--;
                        commandInput.value = commandsHistory[commandHistoryCursor];
                    }
                }
                else if (evt.keyCode == KeyCode.DownArrow)
                {
                    if (commandHistoryCursor < commandsHistory.Count - 1)
                    {
                        commandHistoryCursor++;
                        commandInput.value = commandsHistory[commandHistoryCursor];
                    }
                }
                
                
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    var rawCommand = commandInput.value;
                    commandsHistory.Add(rawCommand);
                    commandHistoryCursor = commandsHistory.Count;
                    commandInput.value = "";
                    var parts = rawCommand.Split(' ', 2);
                    Console.ExecuteCommand(parts[0], parts.Length > 1 ? parts[1] : "");
                    evt.StopImmediatePropagation();  // Останавливаем дальнейшее распространение события
                }
            });

            await ready();
            EditorGUI.hyperLinkClicked -= EditorGUI_HyperLinkClicked;
        }

        private static void EditorGUI_HyperLinkClicked(EditorWindow window, HyperLinkClickedEventArgs args)
        {
            Debug.Log($"Hyperlink clicked: {args}");
        }

        private static void OpenFileInIde(string filePath, int lineNumber)
        {
            // Открываем файл в IDE
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath);
            if (asset != null)
            {
                AssetDatabase.OpenAsset(asset, lineNumber);
            }
            else
            {
                Debug.LogWarning($"Failed to open file: {filePath}");
            }
        }
        
        
        
        
        private void OnDetailsVisibleChanged(bool visible)
        {
            m_Details.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
        
        private void ApplySelectedLog(string id)
        {
            if (m_LogRepresentation.TryGetValue(m_PrevSelectedLogId, out var prevElement))
            {
                prevElement.Q<VisualElement>("Line").RemoveFromClassList("Selected");
            }
            m_PrevSelectedLogId = id;
            if (id.Length > 0)
            {
                var element = m_LogRepresentation[id];
                element.Q<VisualElement>("Line").AddToClassList("Selected");
                
                DetailsVisible.value = true;
                var log = StaticLogsCollector.logHistory.FirstOrDefault(l => l.Id == id);
                m_Details.Clear();
                var richText = $"{log.Text}\n{FormatStackTraces(log.StackSource.GetStackTrace())}";

                Vector2 scrollPos = Vector2.zero;
                var imguiContainer = new IMGUIContainer(() =>
                {
                    var scrollOptions = new GUILayoutOption[] {GUILayout.Height(290) };
                    scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, true, scrollOptions);
                    GUIStyle guiStyle = new GUIStyle
                    {
                        richText = true,
                        normal =
                        {
                            textColor = Color.white
                        },
                        wordWrap = true
                    };
                    var guiContent = new GUIContent(richText);
                    var size = guiStyle.CalcHeight(guiContent, EditorGUIUtility.currentViewWidth);
                    var textOptions = new GUILayoutOption[] {GUILayout.Height(size) };
                    EditorGUILayout.SelectableLabel(richText, guiStyle, textOptions);
                    
                    EditorGUILayout.EndScrollView();
                });
                m_Details.Add(imguiContainer);
            }
            else
            {
                DetailsVisible.value = false;
            }
        }

        private string FormatStackTraces(string stackTrace)
        {
            /* Find paths and replace them with clickable links */
            var formatted = "";
            var edgeSymbolIndex = 0;
            formatted = stackTrace;
            while (true)
            {
                //d__47:MoveNext () (at D:/Gamedev/Projects/ClickTower/ClickTower6000/Assets/ClickTower2024/Scripts/Controllers/StrongPumpkin/StrongPumpkinMechanicsCharacterController.cs:456)
                var match = formatted.IndexOf("(at ", edgeSymbolIndex, StringComparison.Ordinal);
                if (match >= 0)
                {
                    edgeSymbolIndex = match + 4;
                    var start = match + 4;
                    var end = formatted.IndexOf(")", start + 4, StringComparison.Ordinal);
                    if (end > start)
                    {
                        var path = formatted.Substring(start, end - start);
                        var parts = path.Split(':');
                        if (parts.Length >= 2)
                        {
                            /* Filename should be formed by joining all parts except last */
                            var filename = string.Join(":", parts.Take(parts.Length - 1));
                            var lineNumber = parts[^1];
                            // <color=#40a0ff><link="href='./Packages/studio.metamatic.kernel/Runtime/Services/InputService.cs' line='104'">./Packages/studio.metamatic.kernel/Runtime/Services/InputService.cs:104</link></color>
                            var clickablePath = $"<color=#40a0ff><link=\"href='{filename}' line='{lineNumber}'\">{path}</link></color>";
                            /* Replace with clickable path using start and end indexes */
                            var nextLine = formatted.Remove(start, end - start);
                            formatted = nextLine.Insert(start, clickablePath);
                            edgeSymbolIndex = start + clickablePath.Length;
                        }
                        
                    }
                }
                else
                {
                    break;
                }
            }

            return formatted;
        }
        
        private void TagsFilterChanged(string[] added, string[] removed)
        {
            ApplyLogsListFilter();
            
            m_TagsFilterContainer.Clear();
            foreach (var tag in TagsFilter)
            {
                var tagElement = CreateTagElement(tag.Key, tag.Value == 0);
                m_TagsFilterContainer.Add(tagElement);
            }
        }
        
        /// <summary>
        /// Apply all filters to log list (tags, search, built-in toggle)
        /// </summary>
        private void ApplyLogsListFilter()
        {
            var logHistory = StaticLogsCollector.logHistory;
            var filtersContainsSoloTags = TagsFilter.Count > 0 && TagsFilter.Values.Contains(1);
            foreach (var log in logHistory)
            {
                var visibleBySoloTags = true;
                var hiddenBySkipTags = false;
                if (TagsFilter.Count > 0)
                {
                    visibleBySoloTags = !filtersContainsSoloTags;
                    foreach (var tagFilter in TagsFilter)
                    {
                        if (log.Tags.Contains(tagFilter.Key))
                        {
                            if (tagFilter.Value == 0)
                            {
                                hiddenBySkipTags = true;
                                break;
                            }
                            if (tagFilter.Value == 1)
                            {
                                visibleBySoloTags = true;
                            }
                        }
                    }
                }
                
                var summaryVisible = visibleBySoloTags && !hiddenBySkipTags;
                var builtInFilter = StaticLogsCollector.isBuiltInLogEnabled || !log.IsBuiltInLog;

                var visibleBySearch = builtInFilter && summaryVisible && (m_SearchState.Length == 0 || log.Text.Contains(m_SearchState));
                
                if (summaryVisible && builtInFilter && visibleBySearch)
                {
                    m_LogRepresentation[log.Id].style.display = DisplayStyle.Flex;
                }
                else
                {
                    m_LogRepresentation[log.Id].style.display = DisplayStyle.None;
                }
            }
        }

        private void ClearHistory()
        {
            selectedLogId = "";
            StaticLogsCollector.ClearLogHistory();
            m_Content.Clear();
            m_LogRepresentation.Clear();
        }

        private void OnUserScrollStart()
        {
            m_AutoScroll = false;
            if (!m_LockScroll)
            {
                m_LockScroll = true;
                m_Content.schedule.Execute(() => m_LockScroll = false).StartingIn(250);
            }
        }

        private void OnLogReceived(LogMetadata logMetadata)
        {
            var filtersContainsSoloTags = TagsFilter.Count > 0 && TagsFilter.Values.Contains(1);
            var visibleBySoloTags = true;
            var hiddenBySkipTags = false;
            if (TagsFilter.Count > 0)
            {
                visibleBySoloTags = !filtersContainsSoloTags;
                foreach (var tagFilter in TagsFilter)
                {
                    if (logMetadata.Tags.Contains(tagFilter.Key))
                    {
                        if (tagFilter.Value == 0)
                        {
                            hiddenBySkipTags = true;
                            break;
                        }
                        if (tagFilter.Value == 1)
                        {
                            visibleBySoloTags = true;
                        }
                    }
                }
            }
            
            var summaryVisible = visibleBySoloTags && !hiddenBySkipTags;
            var builtInFilter = StaticLogsCollector.isBuiltInLogEnabled || !logMetadata.IsBuiltInLog;

            var visibleBySearch = builtInFilter && summaryVisible && (m_SearchState.Length == 0 || logMetadata.Text.Contains(m_SearchState));

            var element = new LogLine(this, logMetadata);
            m_LogRepresentation[logMetadata.Id] = element;
            m_Content.Add(element);
            if (!visibleBySearch)
            {
                element.style.display = DisplayStyle.None;
            }
        }
        
        private bool IsScrolledToBottom()
        {
            float scrollHeight = m_Content.scrollOffset.y;
            float contentHeight = m_Content.contentContainer.resolvedStyle.height;
            float visibleHeight = m_Content.resolvedStyle.height;

            return Mathf.Approximately(contentHeight, visibleHeight + scrollHeight);
        }
        
        private void ScrollToBottom()
        {
            m_Content.scrollOffset = new Vector2(0, m_Content.contentContainer.resolvedStyle.height - m_Content.resolvedStyle.height);
        }

        private void FrameUpdate()
        {
            if (m_AutoScroll)
            {
                ScrollToBottom();
            }
            else if (!m_LockScroll)
            {
                m_AutoScroll = IsScrolledToBottom();
            }
        }
    }
}