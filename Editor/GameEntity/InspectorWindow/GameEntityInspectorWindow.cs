using System.Linq;
using Metamatic.Extensions;
namespace Metamatic {
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UIElements;
    using UnityEditor;
    using System.IO;

    class GameEntityInspectorWindow : EditorWindow {
        public class MarkdownCompilerElement: VisualElement {
            public class PharagraphElement: VisualElement {
                public PharagraphElement(): base() {
                    this.style.marginBottom = 20;
                }
            }
            public class HeadingElement: VisualElement {
                private readonly Dictionary<int, int> depthToFontSize = new Dictionary<int, int> {
                    {1, 24},
                    {2, 20},
                    {3, 18},
                    {4, 16},
                    {5, 14},
                    {6, 12}
                };
                public HeadingElement(int depth): base() {
                    this.style.fontSize = depthToFontSize[depth];
                    this.style.unityFontStyleAndWeight = FontStyle.Bold;
                    this.style.marginBottom = 12;
                }
            }

            public class StrongElement: VisualElement {
                public StrongElement(): base() {
                    this.style.unityFontStyleAndWeight = FontStyle.Bold;
                }
            }
            public class EmphasisElement: VisualElement {
                public EmphasisElement(): base() {
                    this.style.unityFontStyleAndWeight = FontStyle.Italic;
                }
            }
            public class RichTextElement: VisualElement {
                public string text;
                public RichTextElement(string richText): base() {
                    this.text = richText;
                    
                    var imgui = new UnityEngine.UIElements.IMGUIContainer(() => {
                        GUIStyle style = new GUIStyle ();
                        style.wordWrap = true;
                        style.richText = true;
                        style.fontSize = (int)this.parent.style.fontSize.value.value;
                        if (this.parent.style.unityFontStyleAndWeight == FontStyle.Bold) {
                            style.fontStyle = FontStyle.Bold;
                        }
                        
                        GUILayout.Label(this.text, style);
                    });
                    this.Add(imgui);
                }
            }
            public class ImageElement: VisualElement {
                public string path = null;
                public string alt = null;
                public ImageElement(string path, string alt = ""): base() {
                    this.path = path;
                    this.alt = alt;

                    var image = new UnityEngine.UIElements.Image();
                    image.scaleMode = ScaleMode.ScaleAndCrop;
                    image.style.alignSelf = Align.FlexStart;
                    var fn = Path.GetFileNameWithoutExtension(path);
                    var sprite = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

                    if (sprite != null) {
                        image.image = sprite;
                    }
                    this.Add(image);
                }
            }

            public class CodeElement: VisualElement {
                public string value = "";
                public string lang = "";

                public CodeElement(string value, string lang = null) {
                    this.value = value;
                    this.lang = lang;

                    this.style.paddingTop = 12;
                    this.style.paddingRight = 12;
                    this.style.paddingBottom = 12;
                    this.style.paddingLeft = 12;
                    this.style.backgroundColor = new StyleColor(new Color32(15, 17, 27, 255));
                    this.style.color = new StyleColor(new Color32(166, 172, 205, 255));
                    this.style.marginBottom = 24;

                    var text = new TextElement() {
                        text = value
                    };
                    this.Add(text);
                }
            }

            private string sourcePath = null;
            public MarkdownCompilerElement(string sourceText, string sourcePath): base() {
                this.AddToClassList("MarkdownDocument"); /// 123
                var ast = new MarkdownDocument(sourceText);
                this.sourcePath = sourcePath;

                RenderNodes(this, ast.root.children);
            }

            VisualElement RenderNodes(VisualElement root, IEnumerable<MarkdownDocument.Node> nodes) {
                foreach(var node in nodes) {
                    VisualElement last = root.childCount > 0 ? root.Children().Last() : null;
                    if (node is MarkdownDocument.Pharagraph pharagraphNode) {
                        /* Pharagraph */
                        root.Add(RenderNodes(new PharagraphElement(), pharagraphNode.children));
                    } else if (node is MarkdownDocument.HeadingNode headingNode) {
                        /* Headling */
                        root.Add(RenderNodes(new HeadingElement(headingNode.depth), headingNode.children));
                    } else if (node is MarkdownDocument.StrongNode strongNode) {
                        if (last is RichTextElement && strongNode.children.Count > 0 && strongNode.children[0] is MarkdownDocument.TextNode strongTextNode) {
                            ((RichTextElement)last).text = ((RichTextElement)last).text + $"<b>{strongTextNode.value}</b>";
                        } else {
                            root.Add(RenderNodes(new StrongElement(), strongNode.children));
                        }
                    } else if (node is MarkdownDocument.EmphasisNode emphasisNode) {
                        if (last is RichTextElement && emphasisNode.children.Count > 0 && emphasisNode.children[0] is MarkdownDocument.TextNode strongTextNode) {
                            ((RichTextElement)last).text = ((RichTextElement)last).text + $"<i>{strongTextNode.value}</i>";
                        } else {
                            root.Add(RenderNodes(new EmphasisElement(), emphasisNode.children));
                        }
                    } else if (node is MarkdownDocument.InlineCodeNode inlineCodeNode) {
                        if (last is RichTextElement) {
                            ((RichTextElement)last).text = ((RichTextElement)last).text + $"<color=#712893><i>{inlineCodeNode.value}</i></color>";
                        } else {
                            root.Add(new RichTextElement(inlineCodeNode.value));
                        }
                    } else if (node is MarkdownDocument.TextNode textNode) {
                        if (last is RichTextElement) {
                            ((RichTextElement)last).text = ((RichTextElement)last).text + textNode.value;
                        } else {
                            root.Add(new RichTextElement(textNode.value));
                        }
                    } else if (node is MarkdownDocument.ImageNode imageNode) {
                        root.Add(new ImageElement(GetRelativePath(imageNode.url), imageNode.alt));
                    } else if (node is MarkdownDocument.CodeNode codeNode) {
                        root.Add(new CodeElement(codeNode.value, codeNode.lang));
                    }
                }

                return root;
            }

            string GetRelativePath(string relativePath) {
                return MiscUtils.NormalizePath(Path.Combine(Path.GetDirectoryName(sourcePath), relativePath));
            }
        }
        event System.Action OnNextUpdate;
        GameObject selectedGameObject {
            get {
                return Selection.activeGameObject;
            }
        }
        // [ShowInInspector, InlineEditor(InlineEditorObjectFieldModes.CompletelyHidden)]
        UnityEditor.Editor mainGameEntityEditor;
        GameEntity mainGameEntity;
        // [ShowInInspector, ReadOnly, CustomValueDrawer("TitleDrawer"), PropertyOrder(-10)]
        string windowTitle;
        string windowDescription;
        Texture2D windowPicture;

        string guideMarkdownText;
        string guideMarkdownPath;

        [MenuItem ("Window/Game Entity inspector")]
        public static void ShowWindow () {
            EditorWindow.GetWindow(typeof(GameEntityInspectorWindow), false, "Game Entity");
        }

        void OnEnable() {
            SelectionChangeHandler();
            Selection.selectionChanged += SelectionChangeHandler;
            var root = this.rootVisualElement;
            root.styleSheets.Add(this.LoadRelativeAsset<StyleSheet>("styles.uss"));
            var scrollView = new UnityEngine.UIElements.ScrollView() {
                // verticalScrollerVisibility = ScrollerVisibility.Auto
            };
            root.Add(scrollView);

            scrollView.Add(RenderHeader());
            scrollView.Add(RenderTabs());
        }

        VisualElement RenderHeader() {
            var header = new VisualElement();
            this.LoadRelativeAsset<VisualTreeAsset>("Header.uxml").CloneTree(header);
            var iconTexture = this.LoadRelativeAsset<Texture2D>("Assets/GameEntityIcon.png");
            var iconImage = header.Query<Image>("MainIcon").First();
            iconImage.image = iconTexture;
            var label = header.Query<Label>("Title").First();
            label.text = windowTitle;
            var description = header.Query<TextElement>("Description").First();
            description.text = windowDescription;
            description.style.whiteSpace = WhiteSpace.Normal;
            var tools = header.Query<VisualElement>("Tools").First();
            RenderTools(tools);
            // header.Add(description);1 2

            /* Add picture */
            
            // header.Add(pictureContainer);
            // 12345u 4

            return header;
        }

        TabElement m_PreviewTabElement;
        TabElement m_InspecorGUITabElement;
        TabElement m_DocsTabElement;
        VisualElement RenderTabs() {
            var root = new VisualElement();
            this.LoadRelativeAsset<VisualTreeAsset>("Tabs.uxml").CloneTree(root);
            var tabTemplate = this.LoadRelativeAsset<VisualTreeAsset>("Tab.uxml");
            var flatTabTemplate = this.LoadRelativeAsset<VisualTreeAsset>("FlatTab.uxml");
            var tabs = root.Query<VisualElement>("Tabs").First();

            /* Preview tab */
            m_PreviewTabElement = new TabElement("Preview", this.LoadRelativeAsset<Texture2D>("Assets/InspectorIcon.png"), RenderPreview(), false);
            tabs.Add(m_PreviewTabElement);

            /* Inspector GUI tab */
            m_InspecorGUITabElement = new TabElement("Inspector", this.LoadRelativeAsset<Texture2D>("Assets/InspectorIcon.png"), RenderInlineEditor(), false);
            tabs.Add(m_InspecorGUITabElement);

            /* Documentation tab x */
            m_DocsTabElement = new TabElement("Docs", this.LoadRelativeAsset<Texture2D>("Assets/DocsIcon.png"), RenderGuide(), expanded: false);
            tabs.Add(m_DocsTabElement);

            return root;
        }

        UnityEngine.UIElements.Image m_PreviewPictureElement;
        VisualElement RenderPreview() {
            var pictureContainer = new VisualElement();
            pictureContainer.AddToClassList("PreviewContainer");
            m_PreviewPictureElement = new UnityEngine.UIElements.Image() {
                name = "Picture"
            };
            m_PreviewPictureElement.scaleMode = ScaleMode.ScaleAndCrop;
            m_PreviewPictureElement.style.alignSelf = Align.FlexStart;
            m_PreviewPictureElement.image = windowPicture;
            pictureContainer.Add(m_PreviewPictureElement);

            return pictureContainer;
        }

        VisualElement m_InlineEditorElement;
        VisualElement RenderInlineEditor() {
            m_InlineEditorElement = new VisualElement() {
                name = "InlineEditor"
            };
            m_InlineEditorElement.AddToClassList("InlineEditor");
            m_InlineEditorElement.Add(RenderIMGUI());
            return m_InlineEditorElement;
        }

        private GameEntity cached_MainGameEntity = null;
        void Update() {
            OnNextUpdate?.Invoke();
            /* Update title */
            this.rootVisualElement.Query<Label>("Title").First().text = windowTitle;
            /* Update description */
            this.rootVisualElement.Query<TextElement>("Description").First().text = windowDescription;
            /* Update prevbiw */
            m_PreviewPictureElement.image = windowPicture;
            m_PreviewTabElement.style.display = windowPicture == null ? DisplayStyle.None : DisplayStyle.Flex;
            /* Update inline editor */
            if (this.mainGameEntity != cached_MainGameEntity) {
                m_InlineEditorElement.Clear();
                m_InlineEditorElement.Add(RenderIMGUI());
                m_InspecorGUITabElement.style.display = mainGameEntity == null ? DisplayStyle.None : DisplayStyle.Flex;
            }
            
            /* Update guide */
            UpdateGuide();
            /* Update tools */
            UpdateTools(this.rootVisualElement.Query<VisualElement>("Tools").First());
        }

#if UNITY_EDITOR
        private static string TitleDrawer(string title, GUIContent label) {
            var style = new GUIStyle();
            style.fontSize = 30;
            GUILayout.Label(title, style);
            return title;
        }

        // [PropertySpace(SpaceBefore = 20), Button(ButtonSizes.Medium), ShowIf("mainGameEntity")]
        void OpenInCodeEditor() {
           var members = mainGameEntity.GetType().GetMembers();
           var so = new SerializedObject(mainGameEntity);
           var script = so.FindProperty("m_Script");
           var scriptName = script.objectReferenceValue.name;
           var assetPath = AssetDatabase.GetAssetPath(script.objectReferenceValue);
           AssetDatabase.OpenAsset(script.objectReferenceValue);
        }
#endif

        void OnDisable() {
            Selection.selectionChanged -= SelectionChangeHandler;
             this.rootVisualElement.Clear();
        }

        VisualElement RenderIMGUI() {
            cached_MainGameEntity = mainGameEntity;
            VisualElement root = new UnityEngine.UIElements.IMGUIContainer(() => {
                if (this.mainGameEntityEditor != null) {
                    this.mainGameEntityEditor.OnInspectorGUI();
                }
            });
            return root;
        }

        string cached_GuideMarkDownText = "";
        VisualElement m_GuideContainerElement;
        VisualElement RenderGuide() {
            m_GuideContainerElement = new VisualElement() {
                name = "Guide"
            };
            m_GuideContainerElement.Add(new Label("No docs"));
            
            return m_GuideContainerElement;
        }

        VisualElement RenderTools(VisualElement root) {
             var createGameEntityButton = new Button(() => {
                CreateGameEntityClass();
            }) {
                text = "Create GameEntity class",
                name = "CreateClassButton"
            };
            root.Add(createGameEntityButton);
            UpdateTools(root);

            return root;
        }

        void UpdateGuide() {
            if (cached_GuideMarkDownText != guideMarkdownText) {
                m_GuideContainerElement.Clear();
                cached_GuideMarkDownText = guideMarkdownText;
                if (guideMarkdownText.Length > 0) {
                    var document = new MarkdownCompilerElement(guideMarkdownText, guideMarkdownPath);
                    m_GuideContainerElement.Add(document);
                } else {
                    m_GuideContainerElement.Add(new Label("No docs"));
                }
            }
            m_DocsTabElement.style.display = guideMarkdownText.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
        }

        void UpdateTools(VisualElement root) {
            if (mainGameEntity == null) {
               root.Query<Button>("CreateClassButton").First().style.display = DisplayStyle.Flex;
            } else {
               root.Query<Button>("CreateClassButton").First().style.display = DisplayStyle.None;
            }
        }

        void CreateGameEntityClass() {
            GameObject selected = Selection.activeObject as GameObject;

            if (selected != null) {
                var scriptName = selected.name.Replace(" ","_").Replace("-", "_")+"GameEntity";
                var initialPath = "Assets/"+scriptName+".cs";
                var ext = ".cs";
                var path = initialPath;
                int version = 0;
                while ( File.Exists(path) == true ){
                    path = $"{Path.GetFileNameWithoutExtension(initialPath)}{++version}{ext}";
                }

                using (StreamWriter outfile = 
                 new StreamWriter(path))
                 {
                        outfile.WriteLine("using UnityEngine;");
                        outfile.WriteLine("using Metamatic;");
                        outfile.WriteLine("");
                        outfile.WriteLine($"[GameEntity.Summary(\"{scriptName}\", \"\")]");
                        outfile.WriteLine("public class "+scriptName+" : GameEntity {");
                        outfile.WriteLine("}");
                }

                AssetDatabase.Refresh();

                System.Action addComponentAfterCompilingAction = null;
                addComponentAfterCompilingAction = () => {
                    OnNextUpdate -= addComponentAfterCompilingAction;
                    selected.AddComponent(System.Type.GetType(name));
                    SelectionChangeHandler();
                };

                OnNextUpdate += addComponentAfterCompilingAction;
            }

            
        }

        void SelectionChangeHandler() {
            mainGameEntityEditor = null;
            guideMarkdownText = "";
            guideMarkdownPath = "";
            windowTitle = "No Game Entity Provided";
            windowDescription = "";
            windowPicture = null;

            if (selectedGameObject != null) {
                mainGameEntity = selectedGameObject.GetComponent<GameEntity>();
                if (mainGameEntity != null) {
                    mainGameEntityEditor = Editor.CreateEditor(mainGameEntity);
                    windowTitle = mainGameEntity.gameObject.name;
                    var attributes =  mainGameEntity.GetType().GetCustomAttributes(false);

                     foreach (object attribute in attributes) {
                          /* Determine title and description */
                          var summary = attribute as GameEntity.SummaryAttribute;
                          if (summary != null) {
                              windowTitle = summary.title != "" ? summary.title : mainGameEntity.gameObject.name;
                              windowDescription = summary.description;
                          }

                          /* Determine picture */
                          var picture = attribute as GameEntity.PictureAttribute;
                          if (picture != null && picture.path != "") {
                              var realPath = picture.relative ? GetRealPath(picture.path) : picture.path;
                              windowPicture = AssetDatabase.LoadAssetAtPath<Texture2D>(realPath);
                          }

                          /* Determine markdown guide */
                          var markdown = attribute as GameEntity.MarkdownGuideAssetPath;
                          if (markdown != null && markdown.path != "") {
                              var realPath = markdown.relative ? GetRealPath(markdown.path) : markdown.path;
                              var markdownText = AssetDatabase.LoadAssetAtPath<TextAsset>(realPath);
                              if (markdownText != null) {
                                  guideMarkdownText = markdownText.text;
                                  guideMarkdownPath = realPath;
                              }
                          }
                     }
                }
            }
        }

        string GetRealPath(string relativePath) {
            MonoScript ms = MonoScript.FromMonoBehaviour( mainGameEntity );
            string directoryName = Path.GetDirectoryName(AssetDatabase.GetAssetPath(ms));
            return MiscUtils.NormalizePath(Path.Combine(directoryName, relativePath));
        }
    }

    public class TabElement: VisualElement {
        public bool expanded = false;
        VisualElement m_Tab;
        VisualElement m_TabBody;
        public TabElement(string label, Texture icon, VisualElement content, bool padded = true, bool expanded = true) {
            this.expanded = expanded;
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/studio.metamatic.kernel/Editor/GameEntity/InspectorWindow/Tab.uxml");
            template.CloneTree(this);
            m_Tab = this.Query<VisualElement>("Tab").First();
            if (padded) {
                m_Tab.AddToClassList("Padded");
            }
            m_TabBody = this.Query<VisualElement>("TabBody").First();
            m_TabBody.Add(content);
            this.Query<VisualElement>("TabHeader").First().Query<Label>("Label").First().text = label;
            this.Query<VisualElement>("TabHeader").First().Query<Image>("Icon").First().image = icon;
            this.Query<VisualElement>("TabHeader").First().RegisterCallback<MouseDownEvent>(HandleClick);

            Render();
        }

        void HandleClick(MouseDownEvent e) {
            expanded = !expanded;
            Render();
        }

        void Render() {
            if (expanded) {
                m_Tab.AddToClassList("Expanded");
                m_TabBody.style.display = DisplayStyle.Flex;
            } else {
                m_Tab.RemoveFromClassList("Expanded");
                m_TabBody.style.display = DisplayStyle.None;
            }
        }
    }
}