using UnityEngine;

namespace Metamatic
{
    public class NewGameObjectPoolConfig: IPoolConfig<GameObject>
    {
        public object GetKey()
        {
            return GetType();
        }

        public GameObject OnCreate()
        {
            return new GameObject();
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