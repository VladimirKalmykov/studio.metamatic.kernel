namespace Metamatic {
    using System;
    using UnityEngine;

    // TODO: add pool system

    /* Template is a object object's provider, that uses pool pattern and can reproduce the same object meny times without intantiation */
    public abstract class ScriptableGameObjectFactory: ScriptableObject, IGameObjectFactory, IGroup<GameObject> {
        public virtual GameObject Pull() {
            return Pull(false);
        }
        /// <summary>
        /// Pull object from factory
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <returns></returns>
        public virtual GameObject Pull(bool fromPool = false) {
            // if (fromPool) {
            //     return ObjectPooler.GetInstance().GetPooledObject(this);
            // } else {
                var gameObject = Instantiate(GetGameObject());
                gameObject.SetActive(false);
                return gameObject;
            // }
        }

        public virtual GameObject[] PullMany(int count) {
            return PullMany(count, false);
        }

        public virtual GameObject[] PullMany(int count, bool fromPool) {
            // if (fromPool) {
                // return ObjectPooler.GetInstance().GetPooledObjects(this, count);
            // } else {
                GameObject[] objects = new GameObject[count];
                for (int i = 0; i < count; i++) {
                    var gameObject = Instantiate(GetGameObject());
                    gameObject.SetActive(false);
                    objects[i] = gameObject;
                }

                return objects;
            // }
        }
        
        /// <summary>
        /// Prepare N-count of object to fast access it (all objects from pool supplies disabled)
        /// </summary>
        /// <param name="count">Count of game objects</param>
        /// <returns>Despose action</returns>
        public Action Prepare(int count = 1) {
            return () => {};
            // return ObjectPooler.Prepare(this, count);
        }

        protected virtual GameObject GetGameObject() {
            return new GameObject("Empty GameObject");
        }
    }
}