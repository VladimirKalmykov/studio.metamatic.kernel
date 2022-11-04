using UnityEngine;

namespace Metamatic.Effects
{
    public abstract class StateEffector<T, TState> : Effector<T>
        where TState : struct
        where T: StatefulBehaviour<TState>
    {
        
    }
}