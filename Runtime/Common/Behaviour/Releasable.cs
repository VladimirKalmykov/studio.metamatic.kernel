using UnityEngine;

namespace Metamatic
{
    public class Releasable : MonoBehaviour
    {
        public static void Release(GameObject gameObject)
        {
            var ofp = gameObject.GetComponent<ObjectFromPool>();
            if (ofp != null)
            {
                ofp.Release();
            }
            else
            {
                Object.Destroy(gameObject);
            }
        }
        public virtual void Release()
        {
            throw new System.NotImplementedException();
        }
    }
}