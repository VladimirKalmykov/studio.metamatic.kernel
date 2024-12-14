using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public class MotelTPoolConfig<T>: IPoolConfig<MotelT<T>>
    {
        private readonly object[] m_Dependencies;
        public MotelTPoolConfig(object[] dependencies)
        {
            m_Dependencies = dependencies;
        }
        public object GetKey()
        {
            return (GetType(), typeof(T));
        }

        public MotelT<T> OnCreate()
        {
            return new MotelT<T>(dependencies: new object[] { this });
        }

        public void OnGet(MotelT<T> instance)
        {
            // Do nothing
        }

        public void OnReturn(MotelT<T> instance)
        {
            instance.Reset();
        }

        public void OnDestroy(MotelT<T> instance)
        {
            instance.DisposeAsync().Forget();
        }
    }
}