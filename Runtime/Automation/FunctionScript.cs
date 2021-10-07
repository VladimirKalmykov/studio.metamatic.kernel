using System.Threading;
using System.Threading.Tasks;
using System;

namespace Metamatic{
    public static partial class Automation {
        public class FunctionScript<T>: Automation.Script {
            public delegate void ScriptFunction(Automation.Runtime runtime, T props);
            public delegate Task AsyncFunction(Automation.Runtime runtime, T props, CancellationToken token);
            new public Type propsType = typeof(T);
            /// <summary>
            /// Contains automation script function (lambda or method)
            /// </summary>
            protected ScriptFunction fn = null;
            protected AsyncFunction asyncFn = null;
            public FunctionScript(ScriptFunction fn): base() {
                this.fn = fn;
            }

            public FunctionScript(AsyncFunction asyncFn): base() {
                this.asyncFn = asyncFn;
            }

            public FunctionScript(ScriptFunction fn, Automation.Script.Config config): base(config) {
                this.fn = fn;
            }

             public FunctionScript(AsyncFunction asyncFn, Automation.Script.Config config): base(config) {
                this.asyncFn = asyncFn;
            }

            protected void OnRun(Automation.Runtime runtime, T props) {
                fn(runtime, props);
            }
        }

        public class FunctionScript: Automation.Script {
            public delegate void ScriptFunction(Automation.Runtime runtime);
            public delegate Task AsyncFunction(Automation.Runtime runtime, CancellationToken token);
            /// <summary>
            /// Contains automation script function (lambda or method)
            /// </summary>
            protected ScriptFunction fn = null;
            protected AsyncFunction asyncFn = null;
            public FunctionScript(ScriptFunction fn): base() {
                this.fn = fn;
            }

            public FunctionScript(AsyncFunction asyncFn): base() {
                this.asyncFn = asyncFn;
                this.timingMode = Automation.TimingMode.Endless;
            }

             public FunctionScript(ScriptFunction fn, Automation.Script.Config config): base(config) {
                this.fn = fn;
            }
            public FunctionScript(AsyncFunction asyncFn, Automation.Script.Config config): base(config) {
                this.asyncFn = asyncFn;
            }
            protected async void OnRun(Automation.Runtime runtime) {
                if (asyncFn != null) {
                    var cancellationTokenSource = new CancellationTokenSource();
                    bool isCancelledByTask = false;
                    bool isCancelledByRuntime = false;
                    runtime.OnCancel += () => {
                        if (!isCancelledByTask) {
                            isCancelledByRuntime = true;
                            cancellationTokenSource.Cancel();
                        }
                    };

                    try {
                        await asyncFn(runtime, cancellationTokenSource.Token);
                        runtime.TryComplete();
                    } catch(OperationCanceledException) {
                        isCancelledByTask = true;
                        if (!isCancelledByRuntime) {
                            runtime.TryCancel();
                        }
                    } catch (Exception e) {
                        runtime.ThrowException(e);
                    }
                } else {
                    fn(runtime);
                }
            }
        }
    }
}