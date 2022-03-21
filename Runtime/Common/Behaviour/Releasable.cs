using UnityEngine;

namespace Metamatic
{
    public class Releasable : MonoBehaviour
    {
        public virtual void Release()
        {
            Destroy(gameObject);
        }
    }
}