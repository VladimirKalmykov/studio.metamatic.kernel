using System;
using Cysharp.Threading.Tasks;
using Metamatic.EditorTools;
using Metamatic.Framework.Workshop;
using Metamatic.UIToolKit;
using UnityEngine.UIElements;

namespace Metamatic.Framework.Editor.VirtualWorldEditorWIndow
{
    public class VirtualWorldInspectorWindow: AsyncEditorWindow
    {
        [UnityEditor.MenuItem ("Metamatic/Framework/Virtual World inspector")]
        public static void ShowWindow()
        {
            GetWindow(typeof(VirtualWorldInspectorWindow), false, "Virtual World Inspector");
        }
        private TableView<VirtualEntity> m_EntitiesTable;
        
        private VisualElement CreateRootElement()
        {
            var root = new VisualElement();
            /* Connect tailwind from metamatic uikit */
            root.styleSheets.Add(UIToolKitCore.tailwindStyleSheet);
            root.styleSheets.Add(UIToolKitCore.styleSheet);
            
            root.Add(CreateEntitiesSection());

            return root;
        }

        private async UniTaskVoid OnVirtualWorldChangeAsync(VirtualWorld virtualWorld, Signal cancellationSignal)
        {
            if (virtualWorld == null) return;
            m_EntitiesTable.Bind(virtualWorld.DebugObservableEntitiesList);
            await cancellationSignal;
            m_EntitiesTable.Bind(null);
        }

        private VisualElement CreateEntitiesSection()
        {
            var section = new BoxGroup("Entities");
            var cols = new TableView<VirtualEntity>.Col[4];
            cols[0] = new TableView<VirtualEntity>.Col(
                "Id",
                TableView<VirtualEntity>.Col.WidthType.Relative,
                0.5f,
                entity => new Label(entity.id));
            cols[1] = new TableView<VirtualEntity>.Col(
                "Place",
                TableView<VirtualEntity>.Col.WidthType.Relative,
                0.5f,
                entity => new Tracker<Label, VirtualEntity>(new Label(entity.placeAddress),entity, (label, virtualEntity) => label.text = virtualEntity.placeAddress));
            cols[2] = new TableView<VirtualEntity>.Col(
                "Matereialized",
                TableView<VirtualEntity>.Col.WidthType.Relative,
                0.5f,
                entity => new Tracker<Label, VirtualEntity>(new Label(entity.isMaterialized ? "Yes" : "No"),entity, (label, virtualEntity) => label.text = virtualEntity.isMaterialized ? "Yes" : "No"));
            cols[3] = new TableView<VirtualEntity>.Col(
                "Position",
                TableView<VirtualEntity>.Col.WidthType.Relative,
                0.5f,
                entity => new Tracker<Label, VirtualEntity>(new Label($"{entity.position.x:F1},{entity.position.y:F1},{entity.position.z:F1}"), entity, (label, virtualEntity) => label.text = $"{entity.position.x:F1},{entity.position.y:F1},{entity.position.z:F1}"));
                
            
            m_EntitiesTable = new TableView<VirtualEntity>(cols);
            section.Add(m_EntitiesTable);
            
            return section;
        }

        protected override async UniTaskVoid EnableAsync(Func<UniTask> ready, Signal signal)
        {
            using var watchWorld = GameSystem.WatchVirtualWorldAsync(OnVirtualWorldChangeAsync);
            await signal;
        }

        protected override void OnCreateGUI()
        {
            rootVisualElement.Add(CreateRootElement());
        }
    }
}