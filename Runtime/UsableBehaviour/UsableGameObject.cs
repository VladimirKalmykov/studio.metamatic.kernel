using System;
using UnityEngine;

namespace Metamatic
{
    [Obsolete("Use UsableBehaviour instead", false)]
    [DisallowMultipleComponent]
    public abstract class UsableGameObject : UsableBehaviour
    {
        
    }

    [DisallowMultipleComponent]
    public abstract class UsableGameObject<TProps> : UsableBehaviour<TProps>
        where TProps: struct

    {

    }
}