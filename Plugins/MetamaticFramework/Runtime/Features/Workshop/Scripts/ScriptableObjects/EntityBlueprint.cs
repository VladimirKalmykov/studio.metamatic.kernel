using Cysharp.Threading.Tasks;
using Metamatic;
using Metamatic.Framework.Workshop;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metamatic.Framework.Workshop
{
    [UnityEngine.CreateAssetMenu(fileName = "EntityBlueprint", menuName = "Metamatic/Workshop/Blueprints/Entity")]
    public class EntityBlueprint: ScriptableBlueprint<Entity>, IObjectBlueprint
    {
        private ConfigurablePool<Entity> m_Pool;

        public ConfigurablePool<Entity> pool
        {
            get
            {
                if (m_Pool == null)
                {
                    m_Pool = new ConfigurablePool<Entity>(new EntityPoolConfig(m_Prefab), m_InitialCapacity);
                }

                return m_Pool;
            }
        }
        public Summoner<Entity> summoner => pool.GetSummoner();
        /// <summary>
        /// The name of the object that will be displayed in the workshop (not for the user).
        /// </summary>
        [SerializeField] private string m_BlueprintName;
        [SerializeField] private string m_DisplayName;
        [SerializeField] private Entity m_Prefab;
        [PreviewField]
        [SerializeField] private Sprite m_Preview;

        [SerializeField] private int m_InitialCapacity = 0;

        public void Awake()
        {
            m_Pool = new ConfigurablePool<Entity>(new EntityPoolConfig(m_Prefab));
        }

        public override string blueprintName => m_BlueprintName;


        public override async UniTask<GameObject> GetPrefab(Signal cancellationSignal = default)
        {
            var typedPrefab = await GetPrefabTyped();
            return typedPrefab.gameObject;
        }

        public override async UniTask<GameObject> Spawn(Vector3 position = default, Quaternion rotation = default,
            Signal cancellationSignal = default)
        {
            var (instance, _) = await SpawnAutoReturnable(position, rotation, cancellationSignal);
            return instance.gameObject;
        }

        public override UniTask<Entity> GetPrefabTyped(Signal cancellationSignal = default)
        {
            return UniTask.FromResult(m_Prefab);
        }

        public override AsyncSummon<Entity> Summon()
        {
            return pool.Summon();
        }

        /// <summary>
        /// Spawn an instance that will be automatically returned to the pool when it is disposed.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="cancellationSignal"></param>
        /// <returns></returns>
        public UniTask<(Entity, DisposableBehaviour)> SpawnAutoReturnable(Vector3 position = default, Quaternion rotation = default,
            Signal cancellationSignal = default)
        {
            var instanceSummoner = pool.Summon();
            var instance = instanceSummoner.resource;
            var transform = instance.transform;
            transform.position = position;
            transform.rotation = rotation;
            var disposable = instance.GetOrAddComponent<DisposableBehaviour>();
            disposable.AddAsyncDisposeListenerOnce((e) =>
            {
                instanceSummoner.Dispose();
            });
            return UniTask.FromResult((instance, disposable));
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