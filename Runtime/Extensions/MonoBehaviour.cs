using System.Threading;
using UnityEngine;

namespace Metamatic {
    public static partial class ExtensionMethods {
        /* Get nearest parent component except self */
        public static T GetParentComponent<T>(this MonoBehaviour gameObject) {
            if (gameObject.transform.parent is null) {
                return default(T);
            }

            T component = (T)gameObject.transform.parent.gameObject.GetComponentInParent<T>();

            if (component == null) {
                return default(T);
            }

            return (T)component;
        }

        // public static CancellationTokenSource CreateCancellationTokenSource(this MonoBehaviour gameObject, CancellationToken parentCancellationToken)
        // {
        //     var cancellationTokenSource = new CancellationTokenSource();
        //     
        // }
    }
}