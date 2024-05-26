using Metamatic.Actions;
using NUnit.Framework;
using UnityEngine;

namespace Metamatic
{
    public class DispatcherTests
    {
        private struct TestStruct
        {
            public int value;
        }
        private event System.Action<int> OnEvent;
        
        /// <summary>
        /// Performance test for dispatcher shows that it is x6 faster than event in a creation of 100000 subscribers
        /// The normal result for dispatcher is 0.6-0.9 ms for emit event for 100000 subscribers
        /// </summary>
        [Test]
        public void PerformanceTest()
        {
           var dispatcher = new Dispatcher();
           var COUNTS = 100000;
           /* Create million subscribers */
           var x = 0;
           /* Create tool to measure time */
           
           for (var i = 0; i < COUNTS; i++)
           {
               dispatcher.Subscribe<int>((value) => { x++; });
           }
          
           /* Invoke event */
           var watch = System.Diagnostics.Stopwatch.StartNew();
           dispatcher.Emit(1);
           watch.Stop();
           /* Get performance time */
           var elapsedMs = watch.Elapsed.TotalMilliseconds;
           Debug.Log($"Elapsed time: {elapsedMs} ms (x: {x})");
          
           Assert.AreEqual(x, COUNTS);
           Assert.Less(elapsedMs, 1);
        }
        
        [Test]
        public void PerformanceTestForStructs()
        {
            var dispatcher = new Dispatcher();
            var COUNTS = 100000;
            /* Create million subscribers */
            var x = 0;
            /* Create tool to measure time */
           
            for (var i = 0; i < COUNTS; i++)
            {
                dispatcher.Subscribe<TestStruct>((value) => { x++; });
            }
          
            /* Invoke event */
            var watch = System.Diagnostics.Stopwatch.StartNew();
            dispatcher.Emit(new TestStruct() { value = 1 });
            watch.Stop();
            /* Get performance time */
            var elapsedMs = watch.Elapsed.TotalMilliseconds;
            Debug.Log($"Elapsed time: {elapsedMs} ms (x: {x})");
          
            Assert.AreEqual(x, COUNTS);
            Assert.Less(elapsedMs, 1);
        }
    }
}