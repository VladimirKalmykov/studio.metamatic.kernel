using System;

namespace Metamatic
{
    public class EffectCast : IDisposable
    {
        protected SignalSource CancellationSignalSource;
        /// <summary>
        /// Cast cancellation signal. Will be triggered when the spell is disposed
        /// </summary>
        public Signal cancellationSignal => CancellationSignalSource.Signal;
        protected bool IsFromPool;
        protected bool IsDisposed;
        
        public virtual void Dispose()
        {
            if (IsDisposed)
            {
                Console.Warning("EffectCast", "Already disposed");
                return;
            }
            IsDisposed = true;
            CancellationSignalSource.TryRaise();
        }
    }
    
    public class EffectCast<T>: EffectCast where T : struct
    {
        private static readonly ParamLessPool<EffectCast<T>> Pool = new();
        public static EffectCast<T> Get()
        {
            var effectCast = Pool.Get();
            effectCast.IsFromPool = true;
            effectCast.IsDisposed = false;
            return effectCast;
        }
        private static void Return(EffectCast<T> effectCast)
        {
            effectCast.IsFromPool = false;
            Pool.Return(effectCast);
        }
        public T data { get; set; }

        public EffectCast()
        {
            this.data = default;
            CancellationSignalSource = SignalSource.Create();
        }

        public override void Dispose()
        {
            base.Dispose();
            if (IsFromPool)
            {
                Return(this);
            }
        }
    }
}