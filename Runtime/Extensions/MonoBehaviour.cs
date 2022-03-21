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

        public static void ReleaseGameObject(this MonoBehaviour gameObject)
        {
            var releasable = gameObject.GetComponent<Releasable>();
            if (releasable != null)
            {
                releasable.Release();
            }
            else
            {
                Object.Destroy(gameObject);
            }
        }

        // public static CancellationTokenSource CreateCancellationTokenSource(this MonoBehaviour gameObject, CancellationToken parentCancellationToken)
        // {
        //     var cancellationTokenSource = new CancellationTokenSource();
        //     
        // }
    }
}