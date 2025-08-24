using Cysharp.Threading.Tasks;
using Metamatic.Systems;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Metamatic.Framework.Workshop
{
    [UnityEngine.CreateAssetMenu(fileName = "EntityBlueprint", menuName = "Metamatic/Workshop/Blueprints/Entity")]
    public class EntityBlueprint: ScriptableBlueprint<Entity>, IObjectBlueprint
    {
        [SerializeField] private string m_DisplayName;
        
        [PreviewField]
        [SerializeField] private Sprite m_Preview;

        [SerializeField] private int m_InitialCapacity = 0;

        public void Awake()
        {
            
        }

        public override UniTask<string> GetDisplayName(Signal cancellationSignal = default)
        {
            return UniTask.FromResult(m_DisplayName);
        }

        public override UniTask<Texture2D> GetPreview(Signal cancellationSignal = default)
        {
            return UniTask.FromResult(m_Preview.texture);
        }
    }
}