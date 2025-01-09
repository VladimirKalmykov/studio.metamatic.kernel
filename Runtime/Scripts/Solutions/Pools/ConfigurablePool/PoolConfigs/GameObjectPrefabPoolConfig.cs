using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public class GameObjectConfigurablePoolConfig: IPoolConfig<GameObject>
    {
        public readonly GameObject Prefab;
        public GameObjectConfigurablePoolConfig(GameObject prefab)
        {
            Prefab = prefab;
        }
        public object GetKey()
        {
            return (Prefab, GetType());
        }

        public GameObject OnCreate()
        {
            return UnityEngine.Object.Instantiate(Prefab);
        }

        public void OnGet(GameObject instance)
        {
            instance.SetActive(true);
        }

        public void OnReturn(GameObject instance)
        {
            instance.SetActive(false);
        }

        public void OnDestroy(GameObject instance)
        {
            Object.Destroy(instance);
        }
    }
}