using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Metamatic{
    public static partial class Automation {
        public interface IRuntime {
            Task<object> Task {
                get;
            }
            void TryCancel();
            TaskAwaiter<object> GetAwaiter();
            event Action OnUpdate;
            event Action OnFixedUpdate;
            event Action OnComplete;
            event Action OnCancel;
            event Action OnFinally;
            void Cancel();
            TimeProgress timer {
                get;
            }
            int durationMilliseconds {
                get;
            }
        }
    }
}