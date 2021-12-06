using UnityEngine;

namespace Metamatic
{
    [DisallowMultipleComponent]
    public abstract class UsablePresentableView : UsableGameObject
    {
        [Tooltip("Independent presentables should not be activated within parent")]
        public bool independent = false;
    }
}