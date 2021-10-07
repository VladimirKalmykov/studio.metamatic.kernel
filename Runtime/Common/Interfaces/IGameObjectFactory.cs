namespace Metamatic {
    using UnityEngine;

    public interface IFactory<T>: IGroup<T> {
        T Pull(bool fromPool);
        T[] PullMany(int count, bool fromPool);
        System.Action Prepare(int count);
    }

    public interface IGameObjectFactory: IFactory<GameObject> {}
}