using System;
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
        public void PerformanceSubscribeTest()
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
        public void SubscribeAndRegister()
        {
            var dispatcher = new Dispatcher();
            var x = 0;
            using var subscription = dispatcher.OnRegistered<int>(value => x = value);
            dispatcher.Register(1);
            Assert.AreEqual(1, x);
        }
        
        [Test]
        public void RegisterAndThenSubscribe()
        {
            var dispatcher = new Dispatcher();
            var x = 0;
            var release = dispatcher.Register(2);
            var subscription = dispatcher.OnRegistered<int>(value => x = value);
            Assert.AreEqual(2, x);
            Assert.AreEqual(1, dispatcher.GetRegistrySize());
            subscription.Dispose();
            Assert.AreEqual(1, dispatcher.GetRegistrySize());
            release.Dispose();
            Assert.AreEqual(0, dispatcher.GetRegistrySize());
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
        
        /// <summary>
        /// Performance test for dispatcher shows that it is x6 faster than event in a creation of 100000 subscribers
        /// The normal result for dispatcher is 0.6-0.9 ms for emit event for 100000 subscribers
        /// </summary>
        [Test]
        public void PerformanceRegistryTest()
        {
            var dispatcher = new Dispatcher();
            var COUNTS = 1000;
            /* Create million registrations */
            /* Create tool to measure time */
            var watch = System.Diagnostics.Stopwatch.StartNew();
           
            for (var i = 0; i < COUNTS; i++)
            {
                dispatcher.Register(i, i);
            }
            
            watch.Stop();
            /* Get performance time */
            var elapsedMs = watch.Elapsed.TotalMilliseconds;
            Debug.Log($"Elapsed time: {elapsedMs} ms");
          
            Assert.Less(elapsedMs, 2);
        }
        
        /// <summary>
        /// Performance test for dispatcher shows that it is x6 faster than event in a creation of 100000 subscribers
        /// The normal result for dispatcher is 0.6-0.9 ms for emit event for 100000 subscribers
        /// </summary>
        [Test]
        public void PerformanceSubscribeRegistryTest()
        {
            var dispatcher = new Dispatcher();
            var COUNTS = 1000;
            /* Create million registrations */
            /* Create tool to measure time */
            var watch = System.Diagnostics.Stopwatch.StartNew();
            IDispatcher.EventHandler<int> stub = (value) => { };
            var randomInt = new System.Random();
            for (var i = 0; i < COUNTS; i++)
            {
                dispatcher.OnRegistered(i, stub);
            }
            
            watch.Stop();
            /* Get performance time */
            var elapsedMs = watch.Elapsed.TotalMilliseconds;
            Debug.Log($"Elapsed time: {elapsedMs} ms");
            Assert.AreEqual(dispatcher.GetRegistrySize(), 1000);
            Assert.Less(elapsedMs, 1);
        }
    }
}