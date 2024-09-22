using System;
using System.Threading;
using UnityEngine.Serialization;

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
                public SerializableDictionary<string, Panel.UserPreferences> panelPreferences = new SerializableDictionary<string, Panel.UserPreferences>();
            }

            public UIDocument uiDocument { get; private set; }

            private ScrollView m_WoodsPanelContent;
            private VisualTreeAsset m_SpellCardTemplate;
            private Texture2D m_CursorTexture;
            private readonly Observable<MetamaticFrameworkOverlay> m_Context = new Observable<MetamaticFrameworkOverlay>();
            private readonly Observable<PersistentState> m_PersistentState = new();
            public VisualElement root { get; private set; }

            #region References
            #endregion

            protected override async UniTask Run(Ready ready, CancellationSignal signal)
            {
                m_Context.Value = this;
                /* Load persistent state */
                await LoadPersistentState();
                
                uiDocument = GetComponent<UIDocument>();
                uiDocument.rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("Uss/MetamaticOverlay"));
                
                /* Create context provider */
                var contextProvider = new VisualElementContextProvider();
                contextProvider.AddContext(m_Context);
                uiDocument.rootVisualElement.Add(contextProvider);
                root = contextProvider;
                
                /* Create cursor tools panel */
                using var toolsWidget = new CursorToolsWidget(this);

                await ready();
            }

            public Panel CreatePanel(string name, string title, Panel.Constraints constraints = default)
            {
                var panel = new Panel(name, title, constraints);
                return panel;
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
                var state = await localStorage.TryGet("metamatic-framework/overlay", () => new PersistentState(), CancellationToken.None);
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
        }
    }