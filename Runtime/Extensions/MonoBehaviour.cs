using UnityEngine;

namespace Metamatic {
    public static partial class ExtensionMethods {
        /* Get nearest parent component except self */
        public static T GetParentComponent<T>(this MonoBehaviour gameObject) {
            if (gameObject.transform.parent is null) {
                return default(T);
            }

            var component = (T)gameObject.transform.parent.gameObject.GetComponentInParent<T>();

            if (component == null) {
                return default(T);
            }

            return (T)component;
        }
        
        public static T TryAddComponent<T>(this GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }
        
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            if (!gameObject.TryGetComponent<T>(out var component))
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }
        
        public static T GetOrAddComponent<T>(this MonoBehaviour behaviour) where T : Component
        {
            if (!behaviour.gameObject.TryGetComponent<T>(out var component))
            {
                component = behaviour.gameObject.AddComponent<T>();
            }
            return component;
        }
    }
}