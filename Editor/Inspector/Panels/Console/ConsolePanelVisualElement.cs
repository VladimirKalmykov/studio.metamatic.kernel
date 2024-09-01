using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Metamatic.DevTools;
using Metamatic.EditorTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace Metamatic.EditorInspector
{
    public class ConsolePanelVisualElement: AsyncVisualElement
    {
        public class LogLine : VisualElement
        {
            private static VisualTreeAsset m_LineTemplate;
            private VisualElement m_Tags;
            private Label m_Content;
            private LogMetadata m_Metadata;
            private ConsolePanelVisualElement m_ConsolePanel;
            public LogLine(ConsolePanelVisualElement consolePanel, LogMetadata metadata): base()
            {
                m_ConsolePanel = consolePanel;
                m_Metadata = metadata;
                if (m_LineTemplate == null)
                {
                    m_LineTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/studio.metamatic.kernel/Editor/Inspector/Panels/Console/Line.uxml");
                }
                m_LineTemplate.CloneTree(this);
            
                /* Content */
                m_Content = this.Query<Label>("Content").First();
                m_Content.text = metadata.Text;
                
                /* Tags */
                m_Tags = this.Query<VisualElement>("Tags");
                m_Tags.Clear();
                
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

        public static void ToggleTagFilter(string tag)
        {
            if (TagsFilter.TryGetValue(tag, out var state))
            {
                if (state == 1)
                {
                    TagsFilter.Remove(tag);
                    OnTagsFilterChanged?.Invoke(Array.Empty<string>(), new[] { tag });
                }
            }
            else
            {
                TagsFilter[tag] = 1;
                OnTagsFilterChanged?.Invoke(new[] { tag }, Array.Empty<string>());
            }
            /* Save tags filter to preferences */
            PlayerPrefs.SetString("MetamaticConsoleTagsFilter", string.Join(",", TagsFilter.Keys));
        }
        
        public static void SetTagsFilter(string[] tags)
        {
            TagsFilter.Clear();
            foreach (var tag in tags)
            {
                TagsFilter[tag] = 1;
            }
            OnTagsFilterChanged?.Invoke(tags, Array.Empty<string>());
            /* Save tags filter to preferences */
            PlayerPrefs.SetString("MetamaticConsoleTagsFilter", string.Join(",", tags));
        }

        public static event Action<string[], string[]> OnTagsFilterChanged;
        public static Dictionary<string, Color> TagColors = new Dictionary<string, Color>
        {
            {"Log", new Color(0.2f, 0.2f, 0.2f)},
            {"Warning", new Color(0.66f, 0.37f, 0f)},
            {"Error", new Color(0.78f, 0.16f, 0.07f)},
        };
        
        public static VisualElement CreateTagElement(string tag)
        {
            if (!TagColors.TryGetValue(tag, out var color))
            {
                /* Generate random color with green and blue dominant */
                var isGreenDominant = Random.Range(0, 2) == 0;
                var firstNonDominant = Random.Range(41, 162);
                var secondNonDominant = Random.Range(41, 162);
                color = new Color((float)firstNonDominant / 255f, isGreenDominant ? (161f / 255f) : (float)secondNonDominant / 255f, isGreenDominant ? (float)secondNonDominant / 255f : (161f / 255f));
                TagColors[tag] = color;
            }
            var tagElement = new Label(tag);
            tagElement.AddToClassList("Tag");
            tagElement.style.backgroundColor = color;
            /* Detect click on tag */
            tagElement.RegisterCallback<MouseUpEvent>(evt =>
            {
                ToggleTagFilter(tag);
            });
            return tagElement;
        }
        
        private ScrollView m_Content;
        private VisualElement m_TagsFilterContainer;
        private VisualElement m_Details;
        private bool m_AutoScroll = true;
        private bool m_LockScroll = false;
        public MicroState<bool> DetailsVisible = new();
        private Dictionary<string, VisualElement> m_LogRepresentation = new Dictionary<string, VisualElement>();
        private string m_PrevSelectedLogId = "";
        /// <summary>
        /// Contains log id of currently selected log
        /// </summary>
        private MicroState<string> m_SelectedLogId = new("");
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
        
        public ConsolePanelVisualElement(CancellationToken cancellationToken) : base(cancellationToken)
        {
        }
        
        protected override async UniTaskVoid InitializeAsync(CancellationToken cancellationToken)
        {
            /* Set height 100% */
            style.height = new StyleLength(Length.Percent(100));
            
            /* Read preferences */
            var prefTags = PlayerPrefs.GetString("MetamaticConsole_TagsFilter", "");
            
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
            
            /* Search input */
            var searchInput = this.Query<UnityEngine.UIElements.TextField>("SearchInput");
            
            /* Content */
            m_Content = this.Query<ScrollView>("Content");
            
            /* Details */
            m_Details = this.Query<VisualElement>("Details");
            
            m_Content.RegisterCallback<PointerCaptureEvent>(evt => OnUserScrollStart());
            m_Content.RegisterCallback<WheelEvent>(evt => OnUserScrollStart());
            
            /* Get player loop update */
            using var playerLoopUpdate = AsyncUtils.PlayerLoopAction(FrameUpdate, cancellationToken: cancellationToken);
            /* Details visible state */
            using var detailsVisibleListener = DetailsVisible.Subscribe(OnDetailsVisibleChanged);
            /* Listen for selected log changes */
            using var selectedLogListener = m_SelectedLogId.Subscribe(ApplySelectedLog);
           
            
            /* Listen tags filter changes */
            OnTagsFilterChanged += TagsFilterChanged;
            
            /* Apply preferred tags filter to GUI */
            if (prefTags.Length > 0)
            {
                SetTagsFilter(prefTags.Split(','));
            }
            
            /* Subscribe to log events */
            using var logEventListener = StaticLogsCollector.SubscribeToLogHistory(OnLogReceived);
            EditorGUI.hyperLinkClicked += EditorGUI_HyperLinkClicked;

            await cancellationToken;
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
                var richText = FormatStackTraces(log.StackTrace);

                Vector2 scrollPos = Vector2.zero;
                var imguiContainer = new IMGUIContainer(() =>
                {
                    GUIStyle guiStyle = new GUIStyle ();
                    guiStyle.richText = true;
                    guiStyle.normal.textColor = Color.white;
                    var options = new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true) };

                    scrollPos = EditorGUILayout.BeginScrollView(scrollPos, true, true);
                    EditorGUILayout.SelectableLabel(richText, guiStyle, options);
                    
                    EditorGUILayout.EndScrollView();
                });
                imguiContainer.style.height = new StyleLength(Length.Percent(100));
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
            var lines = stackTrace.Split('\n');
            foreach (var line in lines)
            {
                var match = line.IndexOf("(at ", StringComparison.Ordinal);
                if (match >= 0)
                {
                    var start = match + 4;
                    var end = line.IndexOf(")", start + 4, StringComparison.Ordinal);
                    if (end > start)
                    {
                        var path = line.Substring(start, end - start);
                        var parts = path.Split(':');
                        if (parts.Length != 2)
                        {
                            formatted += line + "\n";
                            continue;
                        }
                        var filename = parts[0];
                        var lineNumber = parts[1];
                        // <color=#40a0ff><link="href='./Packages/studio.metamatic.kernel/Runtime/Services/InputService.cs' line='104'">./Packages/studio.metamatic.kernel/Runtime/Services/InputService.cs:104</link></color>
                        var clickablePath = $"<color=#40a0ff><link=\"href='{filename}' line='{lineNumber}'\">{path}</link></color>";
                        /* Replace with clickable path using start and end indexes */
                        var nextLine = line.Remove(start, end - start);
                        nextLine = nextLine.Insert(start, clickablePath);
                        formatted += nextLine + "\n";
                        continue;
                    }
                }
                formatted += line + "\n";
            }

            return formatted;
        }
        
        private void TagsFilterChanged(string[] added, string[] removed)
        {
            var logHistory = StaticLogsCollector.logHistory;
            foreach (var log in logHistory)
            {
                if (TagsFilter.Count > 0)
                {
                    var hasTags = false;
                    foreach (var tag in log.Tags)
                    {
                        if (TagsFilter.TryGetValue(tag, out var state))
                        {
                            if (state == 1)
                            {
                                hasTags = true;
                                break;
                            }
                        }
                    }

                    if (!hasTags)
                    {
                        m_LogRepresentation[log.Id].style.display = DisplayStyle.None;
                    }
                    else
                    {
                        m_LogRepresentation[log.Id].style.display = DisplayStyle.Flex;
                    }
                }
                else
                {
                    m_LogRepresentation[log.Id].style.display = DisplayStyle.Flex;
                }
            }
            
            m_TagsFilterContainer.Clear();
            foreach (var tag in TagsFilter.Keys)
            {
                var tagElement = CreateTagElement(tag);
                m_TagsFilterContainer.Add(tagElement);
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
            /* Filter log by tags */
            bool hasTags = true;
            if (TagsFilter.Count > 0)
            {
                hasTags = false;
                foreach (var tag in logMetadata.Tags)
                {
                    if (TagsFilter.TryGetValue(tag, out var state))
                    {
                        if (state == 1)
                        {
                            hasTags = true;
                            break;
                        }
                    }
                }
            }
            var element = new LogLine(this, logMetadata);
            m_LogRepresentation[logMetadata.Id] = element;
            m_Content.Add(element);
            if (!hasTags)
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