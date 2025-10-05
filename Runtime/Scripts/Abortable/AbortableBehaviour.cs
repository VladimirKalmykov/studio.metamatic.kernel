using System;
using UnityEngine;

namespace Metamatic
{
    /// <summary>
    /// Allows to observe game object as IAbortable and use it as a dependency for abortable operations.
    /// You can use AbortableObserver to observe the game object as IAbortable and dispose wrapper IPolyDisposable when the game object is aborted.
    /// E.g. if your operation is async disposable, but it strongly depends on the game object, you can track the game object state and abort the operation when the game object is aborted.
    /// </summary>
    /// <example>
    /// <![CDATA[
    /// var abortable1 = gameObject1.GetOrAddComponent<AbortableBehaviour>();
    /// var abortable2 = gameObject2.GetOrAddComponent<AbortableBehaviour>();
    /// const withAbortables = AbortableObserver.CreateWrapper(abortable1, abortable2, abortable3);
    /// await using var transferEnergyBetweenGameObjects = withAbortables(TransferEnergyBetweenGameObjects(energy, gameObject1, gameObject2))
    /// /* transferEnergyBetweenGameObjects is aborted immediately if any of the game objects is aborted, otherwise it will be disposed asynchronously when it is needed */
    /// ]]>
    /// </example>
    public class AbortableBehaviour: MonoBehaviour, IAbortable
    {
        public bool isAborted { get; private set; }
        public event Action OnAborted;
        public void Abort()
        {
            if (isAborted) return;
            isAborted = true;
            OnAborted?.Invoke();
        }

        public DisposableAction AddAbortedListenerOnce(Action action)
        {
            OnAborted += action;
            return DisposableAction.Create(() => OnAborted -= action);
        }

        private void OnDestroy()
        {
            Abort();
        }
    }
}