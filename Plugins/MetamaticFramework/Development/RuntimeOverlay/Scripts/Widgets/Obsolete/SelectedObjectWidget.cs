using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic.Framework
{
    public class SelectedObjectWidget: WidgetLegacy
    {
        private VisualTreeAsset m_SelectedObjectTemplate;
        private VisualElement m_Container;
        public SelectedObjectWidget(Overlay overlay) : base(overlay)
        {
        }

        public override async UniTaskVoid OnInitializeWidget(CancellationSignal signal)
        {
            m_SelectedObjectTemplate = Resources.Load<VisualTreeAsset>("SelectableItem");
            var panel = overlay.CreatePanel("panels/selected_object", "Selected Object", new PanelElement.Constraints()
            {
                DefaultPreferences = new PanelElement.UserPreferences()
                {
                    Position = new Vector2(0, 0),
                    Size = new Vector2(200, 0)  
                },
                MaxSize = new Vector2(500, 600),
                MinSize = new Vector2(200, 40),
                AutoHeight = true,
                AutoWidth = false
            });
            
            m_Container = new VisualElement();
            m_Container.AddToClassList("VerticalItemsContainer");
            panel.AddContent(m_Container);
            
            overlay.root.Add(panel);
            
            /* Subscribe to selected objects */
            using var selectedObjectSubscription = overlay.core.SwitchOnEverySelectedObject(OnObserveSelectedObject);

            await signal;
            overlay.root.Remove(panel);
        }
        
        private async UniTaskVoid OnObserveSelectedObject(GameObject selectedObject, CancellationSignal signal)
        {
            if (selectedObject == null) return;
            
            m_SelectedObjectTemplate.CloneTree(m_Container);
            var item = m_Container.Query<VisualElement>("Item").Last();
            item.AddToClassList("Selected");
            var led = item.Query<VisualElement>("Led").First();
            
            var title = item.Query<Label>("Title").First();
            title.text = selectedObject.name;
            var indicator = item.Query<VisualElement>("Indicator").First();
            
            /* Capture click at indicator */
            void ClickHandler(MouseUpEvent evt)
            {
                overlay.core.DeselectObject(selectedObject);
            }
            indicator.RegisterCallback<MouseUpEvent>(ClickHandler);
            
            var renderer = selectedObject.GetComponent<Renderer>();
            VisualElement boundsRect = null;
            DisposableAction trackBounds = default;
            if (renderer != null)
            {
                /* Create bounds rect */
                boundsRect = new VisualElement();
                boundsRect.AddToClassList("Bounds");
                overlay.root.Add(boundsRect);
                boundsRect.style.position = Position.Absolute;
                /* Create update loop to update bounds rect for selected object */
                trackBounds = PlayerLoop.EachFrame(() =>
                {
                    if (selectedObject == null || overlay.CameraService == null || overlay.CameraService.camera == null)
                    {
                        boundsRect.style.display = DisplayStyle.None;
                        return;
                    }
                    var bounds = renderer.bounds;
                    
                    /* Convert world bounds to screen space */
                    var camera = overlay.CameraService.camera;
                    var screenMin = camera.WorldToScreenPoint(bounds.min);
                    var screenMax = camera.WorldToScreenPoint(bounds.max);
                    
                    var panelBoundsMin = RuntimePanelUtils.ScreenToPanel(overlay.root.panel, new Vector2(screenMin.x, Screen.height - screenMin.y));
                    var panelBoundsMax = RuntimePanelUtils.ScreenToPanel(overlay.root.panel, new Vector2(screenMax.x, Screen.height - screenMax.y));
                    var height = panelBoundsMin.y - panelBoundsMax.y;
                    boundsRect.style.left = panelBoundsMin.x;
                    boundsRect.style.top = panelBoundsMin.y - height;
                    boundsRect.style.width = panelBoundsMax.x - panelBoundsMin.x;
                    boundsRect.style.height = height;
                    boundsRect.style.display = DisplayStyle.Flex;
                });
            }
            
            
            await signal;
            trackBounds.Dispose();
            indicator.UnregisterCallback<MouseUpEvent>(ClickHandler);
            m_Container.Remove(item);
            if (boundsRect != null)
            {
                overlay.root.Remove(boundsRect);
            }
        }
    }
}