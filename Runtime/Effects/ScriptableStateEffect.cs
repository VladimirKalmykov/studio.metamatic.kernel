using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Effects
{
    public abstract class ScriptableStateEffect<TStateMachine, TState, TProps> : ScriptableEffect<TStateMachine, TProps>
        where TState: struct
        where TStateMachine: StatefulBehaviour<TState>
    {
        
    }
}