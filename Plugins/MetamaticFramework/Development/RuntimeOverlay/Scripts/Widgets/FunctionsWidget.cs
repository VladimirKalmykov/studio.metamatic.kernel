using Cysharp.Threading.Tasks;
using UnityEngine.UIElements;

namespace Metamatic.Framework.Widgets
{
    [MetamaticWidget("Metamatic/Functions", null, WidgetApplicability.Viewport)]
    public class FunctionsWidget: Widget
    {
        public FunctionsWidget()
        {
            AddToClassList("VerticalItemsContainer");
            
        }
        public override async UniTask OnPanelAttachedAsync(CancellationSignal cancellationSignal)
        {
            var overlay = this.GetContext<Overlay>().Value;
            using var functionSubscription = overlay.core.WatchEachCustomFunctionLifecycle(OnCustomFunctionLifecycle);
            await cancellationSignal;
        }

        public override Preferences GetDefaultPreferences()
        {
            return new Preferences(new UnityEngine.Vector2(0, 0), new UnityEngine.Vector2(200, 0), new UnityEngine.Vector2(200, 0), new UnityEngine.Vector2(0, 0));
        }
        
        private async UniTaskVoid OnCustomFunctionLifecycle(RuntimeCore.CustomFunction customFunction, int index, CancellationSignal cancellationSignal)
        {
            var button = new Button(customFunction.Invoke)
            {
                text = customFunction.Name
            };
            Add(button);
            await cancellationSignal;
            Remove(button);
        }
    }
}