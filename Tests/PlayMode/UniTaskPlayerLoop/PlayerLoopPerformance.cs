using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Tests.PlayMode.UniTaskPlayerLoop
{
    [TestFixture]
    public class PlayerLoopPerformance
    {
        [UnityTest]
        public IEnumerator TestDelaySeparatedTo10Ms() => UniTask.ToCoroutine(async () =>
        {
            var start = System.DateTime.Now;
            for (var i = 0; i < 5000; i++)
            {
                await UniTask.Delay(10);
            }
            var end = System.DateTime.Now;
            
            var elapsed = end - start;
            Assert.Less(elapsed.TotalMilliseconds, 5000, $"Expected less than 500 ms, but got {elapsed.TotalMilliseconds}");
        });
        
        [UnityTest]
        public IEnumerator TestDelay() => UniTask.ToCoroutine(async () =>
        {
            var start = System.DateTime.Now;
            await UniTask.Delay(5000);
            var end = System.DateTime.Now;
            
            var elapsed = end - start;
            Assert.Less(elapsed.TotalMilliseconds, 5000, $"Expected less than 5000 ms, but got {elapsed.TotalMilliseconds}");
        });
        
        [UnityTest]
        public IEnumerator TestYield() => UniTask.ToCoroutine(async () =>
        {
            var start = System.DateTime.Now;
            for (var i = 0; i < 500; i++)
            {
                await UniTask.Yield();
            }
            var end = System.DateTime.Now;
            
            var elapsed = end - start;
            Assert.Less(elapsed.TotalMilliseconds, 500, $"Expected less than 500 ms, but got {elapsed.TotalMilliseconds}");
        });
    }
}