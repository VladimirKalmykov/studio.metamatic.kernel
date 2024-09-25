using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic.Framework
{
    public class FunctionsWidget: Widget
    {
        private VisualElement m_Root;
        public FunctionsWidget(Overlay overlay) : base(overlay)
        {
        }

        public override async UniTaskVoid OnInitializeWidget(CancellationSignal signal)
        {
            /* Create spawn panel */
            var panel = overlay.CreatePanel("panels/functions", "Functions", new Panel.Constraints()
            {
                DefaultPreferences = new Panel.UserPreferences()
                {
                    Position = new Vector2(200, 200),
                    Size = new Vector2(200, 0)  
                },
                MaxSize = new Vector2(500, 600),
                MinSize = new Vector2(200, 40),
                AutoHeight = true,
                AutoWidth = false
            });

            overlay.root.Add(panel);
            
            /* Create vertical container */
            var contentContainer = new VisualElement();
            contentContainer.AddToClassList("VerticalItemsContainer");
            m_Root = contentContainer;
            panel.AddContent(contentContainer);
            
            /* Watch for custom functions */
            using var functionSubscription = overlay.core.WatchEachCustomFunctionLifecycle(OnCustomFunctionLifecycle);
            await signal;
            overlay.root.Remove(panel);
        }

        private async UniTaskVoid OnCustomFunctionLifecycle(RuntimeCore.CustomFunction customFunction,
            CancellationSignal cancellationSignal)
        {
            var button = new Button(customFunction.Invoke)
            {
                text = customFunction.Name
            };
            m_Root.Add(button);
            await cancellationSignal;
            m_Root.Remove(button);
        }
    }
}