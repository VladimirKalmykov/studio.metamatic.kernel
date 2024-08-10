using System;
using System.Runtime.CompilerServices;
using System.Threading;
using NUnit.Framework;

namespace Tests.EditMode.Experiments.UniTaskVoid
{
    public class DelayedAwaitable: INotifyCompletion
    {
        private readonly int m_DelayMilliseconds;
        private event Action OnComplete;
        private bool m_IsCompleted;

        public DelayedAwaitable(int delayMilliseconds)
        {
            m_DelayMilliseconds = delayMilliseconds;
            new Timer(_ =>
            {
                m_IsCompleted = true;
                OnComplete?.Invoke();
            }, null, delayMilliseconds, Timeout.Infinite);
        }
        
        // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
        // ReSharper disable once InconsistentNaming
        public bool IsCompleted => m_IsCompleted;

        public void OnCompleted(Action continuation)
        {
            if (m_IsCompleted)
            {
                continuation();
            }
            else
            {
                OnComplete += continuation;
            }
        }
        
        public void GetResult() { }
        
        public DelayedAwaitable GetAwaiter() => this;
    }
    
    
    [TestFixture]
    public class UniTaskVoidTests
    {
        [Test]
        public void HowAsyncLambdaFunctionsReturnUniTaskVoid()
        {
            void Tester(Func<Cysharp.Threading.Tasks.UniTask> fn)
            {
                var result = fn();
                /* Result type must be UniTaskVoid */
                Assert.AreEqual(typeof(Cysharp.Threading.Tasks.UniTaskVoid), result.GetType(), $"Expected UniTaskVoid, but got {result.GetType()}");
            }
            Tester(async () =>
            {
                await new DelayedAwaitable(200);
            });
        }
    }
}