using System;
using UnityEngine;

namespace Metamatic
{
    public class InstanceOrigin : MonoBehaviour
    {
        public GameObject prefab;

        public static GameObject Clone(GameObject prefab)
        {
            if (prefab.gameObject.scene.name == null || prefab.gameObject.scene.name == prefab.name)
            {
                var activity = prefab.activeSelf;
                prefab.SetActive(false);
                var instance = Instantiate(prefab);
                prefab.SetActive(activity);
                var origin = instance.gameObject.GetComponent<InstanceOrigin>();
                if (origin == null)
                {
                    origin = instance.gameObject.AddComponent<InstanceOrigin>();
                }
                origin.prefab = prefab;

                return instance;
            }   
            else
            {
                var origin = prefab.gameObject.GetComponent<InstanceOrigin>();
                if (origin != null)
                {
                    return Clone(origin.prefab);
                }
                throw new Exception("You cannot instantiate non prefab");
            }
        }

        public static T Clone<T>(T prefab) where T: Component
        {
            return Clone(prefab.gameObject).GetComponent<T>();
        }

        public static void Release<T>(T instance) where T : Component
        {
            if (instance == null) return;
            Release(instance.gameObject);
        }
        
        public static void Release(GameObject instance)
        {
            if (instance == null) return;
            var objectFromPool = instance.GetComponent<ObjectFromPool>();
            if (objectFromPool != null)
            {
                objectFromPool.Release();
                return;
            }
            /* var origin = instance.GetComponent<InstanceOrigin>();
            if (origin != null)
            {
                UnityEngine.Object.Destroy(instance.gameObject);
            }
            else
            {
                instance.transform.SetParent(null);
            }*/
            UnityEngine.Object.Destroy(instance.gameObject);
        }
    }
}