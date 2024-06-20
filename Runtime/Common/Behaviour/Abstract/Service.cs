using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public abstract class Service: AsyncBehaviour
    {
        private static readonly Dictionary<Type, Service> _Services = new ();
        private static event Action<Service> _HandleServiceAdded;
        private static event Action<Service> _HandleServiceRemoved;
        protected sealed override void RunAsync(CancellationToken cancellationToken)
        {
            var type = this.GetType();
            if (_Services.ContainsKey(type))
            {
                throw new InvalidOperationException($"Service {type} already added");
            }
            _Services.Add(type, this);
            _HandleServiceAdded?.Invoke(this);
            cancellationToken.Register(() =>
            {
                _Services.Remove(this.GetType());
                _HandleServiceRemoved?.Invoke(this);
            });
            RunService(cancellationToken);
        }
        
        /// <summary>
        /// Resolve service by type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static UniTask<T> Resolve<T>(CancellationToken cancellationToken = default, float timeout = 0f) where T : Service
        {
            if (_Services.TryGetValue(typeof(T), out var service))
            {
                return UniTask.FromResult((T) service);
            }
            var tcs = new UniTaskCompletionSource<T>();
            var task = tcs.Task;
            Action<Service> handler = null;
            handler = s =>
            {
                if (s.GetType() == typeof(T))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        tcs.TrySetCanceled();
                        return;
                    }
                    _HandleServiceAdded -= handler;
                    tcs.TrySetResult((T) s);
                }
            };
            _HandleServiceAdded += handler;
            var cancellationTokenRegistration = cancellationToken.Register(() =>
            {
                _HandleServiceAdded -= handler;
                tcs.TrySetCanceled();
            });
            
            if (timeout > 0f)
            {
                UniTask.Delay(TimeSpan.FromSeconds(timeout), cancellationToken: cancellationToken).ContinueWith(() =>
                {
                    if (!task.Status.IsCompleted())
                    {
                        _HandleServiceAdded -= handler;
                        cancellationTokenRegistration.Dispose();
                        tcs.TrySetException(new TimeoutException("Operation timed out."));
                    }
                }).Forget();
            }
            
            return tcs.Task;
        }

        public static DisposableAction SubscribeService<T>(Action<T> service) where T : Service
        {
            Action<Service> handler = null;
            handler = s =>
            {
                if (s.GetType() == typeof(T))
                {
                    service((T)s);
                }
            };
            _HandleServiceAdded += handler;
            return new DisposableAction(() => _HandleServiceAdded -= handler);
        }

        protected abstract void RunService(CancellationToken cancellationToken);
    }
}