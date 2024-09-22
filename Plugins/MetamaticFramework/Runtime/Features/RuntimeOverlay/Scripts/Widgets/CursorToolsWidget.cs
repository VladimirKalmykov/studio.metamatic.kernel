using Cysharp.Threading.Tasks;
using Metamatic;
using Metamatic.Framework;
using UnityEngine;
using Widget = Metamatic.Framework.Widget;

namespace Metamatic.Framework
{
    public class CursorToolsWidget: Widget
    {
        
        private Panel m_CursorToolsPanel;
        public CursorToolsWidget(MetamaticFrameworkOverlay overlay) : base(overlay)
        {
            
        }

        public override async UniTaskVoid OnInitializeWidget(CancellationSignal signal)
        {
            m_CursorToolsPanel = overlay.CreatePanel("panels/cursor_tools", "Tools", new Panel.Constraints()
            {
                MaxSize = new Vector2(80, 0),
            });
            overlay.root.Add(m_CursorToolsPanel);
            
            /* Subscribe to cursor tools */
            using var cursorToolSubscription = RuntimeCore.Instance.ObserveCursorTools(OnObserverCursorTool);
            
            await signal;
        }
        
        private async UniTaskVoid OnObserverCursorTool(CursorTool cursorTool, CancellationSignal cancellationSignal)
        {
            // Here we can add cursor tool button to the panel
            await cancellationSignal;
        }
    }
}