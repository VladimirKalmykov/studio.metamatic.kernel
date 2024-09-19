namespace Metamatic.Magic
{
    using Cysharp.Threading.Tasks;
using Metamatic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

public class WoodsMagUI : AsyncBehaviour
{
    private UIDocument m_UIDocument;
    private ScrollView m_WoodsPanelContent;
    private VisualTreeAsset m_WoodBlockTemplate;
    private Texture2D m_CursorTexture;
    private MicroState<MagicWood> m_SelectedWood = new MicroState<MagicWood>(null);

    protected override async UniTask Run(Ready ready, CancellationSignal signal)
    {
        m_UIDocument = GetComponent<UIDocument>();
        m_WoodBlockTemplate = Resources.Load<VisualTreeAsset>("WoodBlock");
        m_CursorTexture = Resources.Load<Texture2D>("MagicWoodCursor");
        InitWoodsPanel();
        using var selectedWoodSubscription = SequentialHandler<MagicWood>.Subscribe(m_SelectedWood.Subscribe, OnObserveSelectedWood);
        
        /* Subscribe to Woods data */
        using var observeWoods = MagicMag.ObserveWoods(OnObserveWood);
        
        await ready();
    }
    
    private async UniTaskVoid OnObserveSelectedWood(MagicWood wood, CancellationSignal signal)
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
    
    private async UniTaskVoid OnObserveWood(MagicWood wood, CancellationSignal signal)
    {
        /* Add wood to WoodsPanel */
        m_WoodBlockTemplate.CloneTree(m_WoodsPanelContent);
        var woodElement = m_WoodsPanelContent.Query<VisualElement>("WoodBlock").Last();
        woodElement.Q<Label>("Title").text = wood.DisplayName;
        /* Handle wood click */
        woodElement.RegisterCallback<MouseDownEvent>(evt =>
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
        woodElement.RemoveFromHierarchy();
    }

    private void InitWoodsPanel()
    {
        VisualTreeAsset woodsPanel = Resources.Load<VisualTreeAsset>("WoodsPanelExpanded");
        /* Add panel to UIDocument */
        woodsPanel.CloneTree(m_UIDocument.rootVisualElement);
        /* Get WoodsPanelExpanded from UIDocument */
        VisualElement woodsPanelExpanded = m_UIDocument.rootVisualElement.Q<VisualElement>("WoodsPanelExpanded");
/* Set absolute position to 0 */
        woodsPanelExpanded.style.position = Position.Absolute;
        woodsPanelExpanded.style.left = 0;
        woodsPanelExpanded.style.top = 0;

        var header = woodsPanelExpanded.Q<VisualElement>("Header");
/* Make panel draggable by header */
        var startMousePosition = Vector2.zero;
        var startPanelPosition = Vector2.zero;
        
        m_WoodsPanelContent = woodsPanelExpanded.Q<ScrollView>("Content");

        header.RegisterCallback<MouseDownEvent>(evt =>
        {
            if (evt.button == 0)
            {
                // Используем глобальную позицию мыши для точности
                startMousePosition = evt.mousePosition;
                startPanelPosition = new Vector2(woodsPanelExpanded.resolvedStyle.left, woodsPanelExpanded.resolvedStyle.top);
                header.CaptureMouse();
            }
        });

        header.RegisterCallback<MouseMoveEvent>(evt =>
        {
            if (header.HasMouseCapture())
            {
                // Рассчитываем разницу с использованием глобальной позиции мыши
                var diff = evt.mousePosition - startMousePosition;
                woodsPanelExpanded.style.left = startPanelPosition.x + diff.x;
                woodsPanelExpanded.style.top = startPanelPosition.y + diff.y;
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