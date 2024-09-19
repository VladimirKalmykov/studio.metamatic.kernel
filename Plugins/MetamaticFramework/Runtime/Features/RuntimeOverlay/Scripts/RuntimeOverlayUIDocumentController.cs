namespace Metamatic.Framework
    {
        using Cysharp.Threading.Tasks;
        using UnityEngine;
        using UnityEngine.UIElements;
        using Cursor = UnityEngine.Cursor;

        public class RuntimeOverlayUIDocumentController : AsyncBehaviour
        {
            private UIDocument m_UIDocument;
            private ScrollView m_WoodsPanelContent;
            private VisualTreeAsset m_SpellCardTemplate;
            private Texture2D m_CursorTexture;
            private MicroState<CursorTool> m_SelectedWood = new MicroState<CursorTool>(null);

            protected override async UniTask Run(Ready ready, CancellationSignal signal)
            {
                m_UIDocument = GetComponent<UIDocument>();
                m_SpellCardTemplate = Resources.Load<VisualTreeAsset>("SpellCard");
                m_CursorTexture = Resources.Load<Texture2D>("MagicWoodCursor");
                InitWoodsPanel();
                using var selectedWoodSubscription =
                    SequentialHandler<CursorTool>.Subscribe(m_SelectedWood.Subscribe, OnObserveSelectedWood);

                /* Subscribe to Woods data */
                using var observeWoods = MagicMag.ObserveWoods(OnObserveWood);

                await ready();
            }

            private async UniTaskVoid OnObserveSelectedWood(CursorTool wood, CancellationSignal signal)
            {
                if (wood == null)
                {
                    return;
                }

                //Cursor.visible = false;
                Cursor.SetCursor(m_CursorTexture, Vector2.zero, CursorMode.Auto);

                MagicMag.ActivateWood(wood, signal);

                await signal;
                //Cursor.visible = true;
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }

            private async UniTaskVoid OnObserveWood(CursorTool wood, CancellationSignal signal)
            {
                /* Add wood to WoodsPanel */
                m_SpellCardTemplate.CloneTree(m_WoodsPanelContent);
                var spellCard = m_WoodsPanelContent.Query<VisualElement>("SpellCard").Last();
                spellCard.Q<Label>("Title").text = wood.DisplayName;
                /* Handle wood click */
                spellCard.RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.button == 0)
                    {
                        m_SelectedWood.value = wood;
                    }
                });
                await signal;
                if (m_SelectedWood.value == wood)
                {
                    m_SelectedWood.value = null;
                }

                /* Remove wood from WoodsPanel */
                spellCard.RemoveFromHierarchy();
            }

            private void InitWoodsPanel()
            {
                VisualTreeAsset woodsPanel = Resources.Load<VisualTreeAsset>("Panel");
                /* Add panel to UIDocument */
                woodsPanel.CloneTree(m_UIDocument.rootVisualElement);
                /* Get Panel from UIDocument */
                VisualElement panel = m_UIDocument.rootVisualElement.Q<VisualElement>("Panel");
/* Set absolute position to 0 */
                panel.style.position = Position.Absolute;
                panel.style.left = 0;
                panel.style.top = 0;

                var header = panel.Q<VisualElement>("Header");
/* Make panel draggable by header */
                var startMousePosition = Vector2.zero;
                var startPanelPosition = Vector2.zero;

                m_WoodsPanelContent = panel.Q<ScrollView>("Content");

                header.RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.button == 0)
                    {
                        // Используем глобальную позицию мыши для точности
                        startMousePosition = evt.mousePosition;
                        startPanelPosition = new Vector2(panel.resolvedStyle.left, panel.resolvedStyle.top);
                        header.CaptureMouse();
                    }
                });

                header.RegisterCallback<MouseMoveEvent>(evt =>
                {
                    if (header.HasMouseCapture())
                    {
                        // Рассчитываем разницу с использованием глобальной позиции мыши
                        var diff = evt.mousePosition - startMousePosition;
                        panel.style.left = startPanelPosition.x + diff.x;
                        panel.style.top = startPanelPosition.y + diff.y;
                    }
                });

                header.RegisterCallback<MouseUpEvent>(evt =>
                {
                    if (evt.button == 0)
                    {
                        header.ReleaseMouse();
                    }
                });
            }
        }
    }