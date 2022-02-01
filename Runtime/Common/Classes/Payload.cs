using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public class Payload
    {
        public readonly Type type;

        public Payload(Type type)
        {
            this.type = type;
        }
    }

    public class Payload<T> : Payload
    where T : notnull
    {
        public readonly T data;

        public Payload(in T data = default): base(typeof(T))
        {
            this.data = data;
        }
    }

    public class MutablePayload<T> : Payload
        where T : notnull
    {
        public T value { get; protected set; }
        public bool locked = false;
        private event Action<T> OnMutate;
        public MutablePayload(in T value = default, bool locked = false): base(typeof(T))
        {
            this.value = value;
            this.locked = locked;
        }

        ~MutablePayload()
        {
            Debug.Log("Mutable payload have released");
        }

        public void SetValue(in T value)
        {
            if (locked) return;
            this.value = value;
            OnMutate?.Invoke(value);
        }
        
        public void SetValue(Func<T, T> mutator)
        {
            SetValue(mutator(this.value));
        }

        public Task<T> WhenCondition(Func<T, bool> condition, CancellationToken cancellationToken)
        {
            var taskSource = new TaskCompletionSource<T>();
            Action<T> handler = null;
            CancellationTokenRegistration registration;
            handler = (value) =>
            {
                if (!condition(value)) return;
                registration.Dispose();
                OnMutate -= handler;
                taskSource.SetResult(value);
            };
            Action cancelHandler = () =>
            {
                registration.Dispose();
                OnMutate -= handler;
                taskSource.SetCanceled();
            };
            registration = cancellationToken.Register(cancelHandler);
            OnMutate += handler;
            handler(value);
            return taskSource.Task;
        }
    }
}