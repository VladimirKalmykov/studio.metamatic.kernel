using UnityEngine;
namespace Metamatic {
    public abstract class ScriptablePoolBase: ScriptableObject
    {
        public System.Action Prepare(int count) {
            OnPrewarm(count);
            return () => {
                OnPrewarmRelease(count);
            };
        }

        public void Release(int count) {
            OnPrewarm(count);
        }

        protected virtual void OnPrewarm(int count) {}
        protected virtual void OnPrewarmRelease(int count) {}
    }
}