using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public interface IPoolRules
    {
        /// <summary>
        /// Should return a unique key for the combination of the pool rule and its payload.
        /// </summary>
        /// <returns></returns>
        public object GetKey();
    }
    public interface IPoolConfig<T>: IPoolRules
    {
        public T OnCreate();
        public void OnGet(T instance);
        public void OnReturn(T instance);
        public void OnDestroy(T instance);
    }
}