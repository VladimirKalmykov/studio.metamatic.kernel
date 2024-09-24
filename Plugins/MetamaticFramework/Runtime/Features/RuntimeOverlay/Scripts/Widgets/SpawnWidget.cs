using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Framework
{
    public class SpawnWidget: Widget
    {
        public SpawnWidget(Overlay overlay) : base(overlay)
        {
        }

        public override async UniTaskVoid OnInitializeWidget(CancellationSignal signal)
        {
            /* Create spawn panel */
            var panel = overlay.CreatePanel("panels/spawn", "Spawn", new Panel.Constraints()
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
            await signal;
            overlay.root.Remove(panel);
        }
    }
}