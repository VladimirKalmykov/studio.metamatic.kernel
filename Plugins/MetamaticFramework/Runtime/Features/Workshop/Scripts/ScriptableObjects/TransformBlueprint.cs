using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Metamatic.Framework.Workshop
{
    [UnityEngine.CreateAssetMenu(fileName = "TransformBlueprint", menuName = "Metamatic/Workshop/Blueprints/Transform")]
    public class TransformBlueprint: ScriptableBlueprint<Transform>
    {
        /// <summary>
        /// The name of the object that will be displayed in the workshop (not for the user).
        /// </summary>
        [SerializeField] private string m_BlueprintName;
        [SerializeField] private string m_DisplayName;
        [SerializeField] private Transform m_Prefab;
        [PreviewField]
        [SerializeField] private Sprite m_Preview;

        public override string blueprintName => m_BlueprintName;

        public override UniTask<Transform> GetPrefab(CancellationSignal cancellationSignal = default)
        {
            return UniTask.FromResult(m_Prefab);
        }

        public override UniTask<Transform> ProduceObject(Vector3 position = default, Quaternion rotation = default,
            CancellationSignal cancellationSignal = default)
        {
            var instance = Instantiate(m_Prefab, position, rotation);
            return UniTask.FromResult(instance);
        }

        public override void ReturnObject(Transform gameObject)
        {
            Destroy(gameObject);
        }

        public override UniTask<string> GetDisplayName(CancellationSignal cancellationSignal = default)
        {
            return UniTask.FromResult(m_DisplayName);
        }

        public override UniTask<Texture2D> GetPreview(CancellationSignal cancellationSignal = default)
        {
            return UniTask.FromResult(m_Preview.texture);
        }
    }
}