namespace Metamatic.Actions
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public abstract class Dispatcher : MonoBehaviour
    {
        public abstract void Dispatch(Action action);
    }
}