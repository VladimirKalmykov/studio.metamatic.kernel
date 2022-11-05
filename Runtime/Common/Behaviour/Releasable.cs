using UnityEngine;

namespace Metamatic
{
    public class Releasable : MonoBehaviour
    {
        public static void Release(GameObject gameObject)
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
        public virtual void Release()
        {
            Destroy(gameObject);
        }
    }
}