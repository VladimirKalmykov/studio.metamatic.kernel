using System;

namespace Metamatic
{
    /// <summary>
    /// This state can be used to store any type of data within a state machine.
    /// Use can use Create method to get an instance from the pool. When it no longer needed, it will be returned to the pool automatically (because AsyncStateMachine will call Dispose on it).
    /// It also call dispose on the data if it implements IDisposable.
    /// <example>
    /// asyncStateMachine.PushState(GenericState.Create(new MyData()));
    /// </example>
    /// </summary>
    public class GenericState
    {
    }
    
    public class GenericState<TData>: GenericState, IDisposable
    {
        #region Static
        private static ParamLessPool<GenericState<TData>> _Pool = new();
        public static GenericState<TData> Create(TData data)
        {
            var instance = _Pool.Get();
            instance.Data = data;
            return instance;
        }
        #endregion
        public TData Data;
        public override string ToString()
        {
            return $"{typeof(TData).Name}";
        }

        public GenericState(): base()
        {
            
        }

        public void Dispose()
        {
            if (Data is IDisposable disposable)
            {
                disposable.Dispose();
            }
            _Pool.Return(this);
        }
    }
}