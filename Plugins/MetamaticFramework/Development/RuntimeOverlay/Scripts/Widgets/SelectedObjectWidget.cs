using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic.Framework.Widgets
{
    [MetamaticWidget("Metamatic/Selected Object", null, WidgetApplicability.Viewport)]
    public class SelectedObjectWidget: Widget
    {
        private VisualTreeAsset m_SelectedObjectTemplate;
        private VisualElement m_Container;
        private Overlay m_Overlay;
        public SelectedObjectWidget()
        {
            m_Container = new VisualElement();
            m_Container.AddToClassList("VerticalItemsContainer");
            Add(m_Container);
        }
        public override async UniTask OnPanelAttachedAsync(CancellationSignal cancellationSignal)
        {
            m_Overlay = this.GetContext<Overlay>().Value;
            m_SelectedObjectTemplate = Resources.Load<VisualTreeAsset>("SelectableItem");
            
            
            /* Subscribe to selected objects */
            using var selectedObjectSubscription = m_Overlay.core.WatchEverySelectedObjectLifecycle(OnObserveSelectedObject);

            await cancellationSignal;
        }

        public override Preferences GetDefaultPreferences()
        {
            return new Preferences(new UnityEngine.Vector2(0, 0), new UnityEngine.Vector2(200, 0), new UnityEngine.Vector2(200, 0), new UnityEngine.Vector2(0, 0));
        }
        
        private async UniTaskVoid OnObserveSelectedObject(GameObject selectedObject, int index, CancellationSignal signal)
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
                m_Overlay.core.DeselectObject(selectedObject);
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
                m_Overlay.root.Add(boundsRect);
                boundsRect.style.position = Position.Absolute;
                /* Create update loop to update bounds rect for selected object */
                trackBounds = PlayerLoop.EachFrame(() =>
                {
                    if (selectedObject == null || m_Overlay.CameraService == null || m_Overlay.CameraService.camera == null)
                    {
                        boundsRect.style.display = DisplayStyle.None;
                        return;
                    }
                    var bounds = renderer.bounds;
                    
                    /* Convert world bounds to screen space */
                    var camera = m_Overlay.CameraService.camera;
                    var screenMin = camera.WorldToScreenPoint(bounds.min);
                    var screenMax = camera.WorldToScreenPoint(bounds.max);
                    
                    var panelBoundsMin = RuntimePanelUtils.ScreenToPanel(m_Overlay.root.panel, new Vector2(screenMin.x, Screen.height - screenMin.y));
                    var panelBoundsMax = RuntimePanelUtils.ScreenToPanel(m_Overlay.root.panel, new Vector2(screenMax.x, Screen.height - screenMax.y));
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
                m_Overlay.root.Remove(boundsRect);
            }
        }
    }
}