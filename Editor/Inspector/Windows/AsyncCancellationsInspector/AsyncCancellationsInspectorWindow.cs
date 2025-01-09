using System;
using Cysharp.Threading.Tasks;
using Metamatic.EditorTools;
using Metamatic.Inspector.Windows.PoolsInspector;
using Metamatic.UIToolKit;
using UnityEditor;
using UnityEngine.UIElements;

namespace Metamatic.Inspector.Windows.AsyncCancellationsInspector
{
    public class AsyncCancellationsInspectorWindow: AsyncEditorWindow
    {
        [MenuItem ("Window/Metamatic/Async cancellations Inspector")]
        public static void ShowWindow () {
            GetWindow(typeof(AsyncCancellationsInspectorWindow), false, "Async cancellations inspector");
        }
        protected override UniTaskVoid EnableAsync(Func<UniTask> ready, Signal signal)
        {
            return new UniTaskVoid();
        }

        protected override void OnCreateGUI()
        {
            var table = new Table<CancellationSourceImpl>(new Table<CancellationSourceImpl>.Column[]
            {
                new()
                {
                    Header = "State",
                    Accessor = impl => new Tracker<Label>(new Label(impl.state.ToString()),
                        (label) => label.text = impl.state.ToString()),
                    Width = new StyleLength(new Length(50, LengthUnit.Percent))
                },
                new()
                {
                    Header = "Version",
                    Accessor = impl => new Tracker<Label>(new Label(impl.version.ToString()),
                        (label) => label.text = impl.version.ToString()),
                    Width = new StyleLength(new Length(50, LengthUnit.Percent))
                }
            }, CancellationSourceImpl.Instances);
            
            rootVisualElement.Add(table);
        }
    }
}