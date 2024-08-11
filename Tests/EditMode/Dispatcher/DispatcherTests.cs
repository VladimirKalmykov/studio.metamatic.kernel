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
            var COUNTS = 10000;
            /* Create million subscribers */
            var x = 0;
            /* Create tool to measure time */
            var watch = System.Diagnostics.Stopwatch.StartNew();
            for (var i = 0; i < COUNTS; i++)
            {
                dispatcher.Subscribe<int>((value) => { x++; });
            }
            watch.Stop();
            /* Invoke event */
            
            dispatcher.Emit(1); // tedt
            
            /* Get performance time */
            var elapsedMs = watch.Elapsed.TotalMilliseconds;
            Debug.Log($"Elapsed time: {elapsedMs} ms (x: {x})");
          
            Assert.AreEqual(x, COUNTS);
            Assert.Less(elapsedMs, 3);
        }
        
        /// <summary>
        /// Performance test for dispatcher shows that it is x6 faster than event in a creation of 100000 subscribers
        /// The normal result for dispatcher is 0.6-0.9 ms for emit event for 100000 subscribers
        /// </summary>
        [Test]
        public void PerformanceEmitTest()
        {
           var dispatcher = new Dispatcher();
           var COUNTS = 10000;
           /* Create million subscribers */
           var x = 0;
           /* Create tool to measure time */
           
           for (var i = 0; i < COUNTS; i++)
           {
               dispatcher.Subscribe<int>((value) => { x++; });
           }
          
           /* Invoke event */
           var watch = System.Diagnostics.Stopwatch.StartNew();
           dispatcher.Emit(1); // tedt
           watch.Stop();
           /* Get performance time */
           var elapsedMs = watch.Elapsed.TotalMilliseconds;
           Debug.Log($"Elapsed time: {elapsedMs} ms (x: {x})");
          
           Assert.AreEqual(x, COUNTS);
           Assert.Less(elapsedMs, 1);
        }
        
        [Test]
        public void ChangeSubscriptionsDuringEvaluatingEvent()
        {
            var dispatcher = new Dispatcher();
            var x = 0;
            var y = 0;
            var z = 0;
            var subscriptionXY = dispatcher.Subscribe<int>((value) =>
            {
                x += value;
                dispatcher.Subscribe<int>((value) => y += value);
            });
            var subscriptionZ = dispatcher.Subscribe<int>((value) =>
            {
                z += value;
            });
            dispatcher.Emit(1);
            Assert.AreEqual(1, x, "Expected x to be 1 after first emit, but it was not.");
            Assert.AreEqual(0, y, "Expected y to be 0 after first emit, because it subscribes during emit, but it was not.");
            Assert.AreEqual(1, z, "Expected z to be 1 after first emit, but it was not.");

            dispatcher.Emit(2);
            Assert.AreEqual(3, x, "Expected x to be 3 after second emit, but it was not.");
            Assert.AreEqual(2, y, "Expected y to be 2 after second emit, but it is first emit for it.");
            Assert.AreEqual(3, z, "Expected z to be 3 after second emit, but it was not.");
            
            dispatcher.Emit(3);
            Assert.AreEqual(6, x, "Expected x to be 6 after third emit, but it was not.");
            Assert.AreEqual(8, y, "Expected y to be 8 after third emit, because at already has been subscribed twice, but it was not.");
            Assert.AreEqual(6, z, "Expected z to be 6 after third emit, but it was not.");
        }
        
        [Test]
        public void DeferSubscribeRightAfterUnsubscribe()
        {
            var dispatcher = new Dispatcher();
            var x = 0;
            IDisposable disposable = default;
            IDisposable mainDisposable = default;
            mainDisposable = dispatcher.Subscribe<int>((value) =>
            {
                mainDisposable?.Dispose();
                dispatcher.Subscribe<int>((value) =>
                {
                    x++;
                });
            });
            dispatcher.Emit(1);
            Assert.AreEqual(0, x, "Fist emit should not increase x, because it is unsubscribed.");
            dispatcher.Emit(2);
            Assert.AreEqual(1, x, "Second emit should increase x, because it is subscribed.");
        }
    
        [Test]
        public void SubscribeAndRegister()
        {
            var dispatcher = new Dispatcher();
            var x = 0;
            using var subscription = dispatcher.SubscribeRegistry<int>(value => x = value);
            dispatcher.Register(1);
            Assert.AreEqual(1, x);
        }
        
        [Test]
        public void RegisterAndThenSubscribe()
        {
            var dispatcher = new Dispatcher();
            var x = 0;
            var release = dispatcher.Register(2);
            var subscription = dispatcher.SubscribeRegistry<int>(value => x = value);
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
            var COUNTS = 10000;
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
          
            Assert.Less(elapsedMs, 14);
        }
        
        /// <summary>
        /// Performance test for dispatcher shows that it is x6 faster than event in a creation of 100000 subscribers
        /// The normal result for dispatcher is 0.6-0.9 ms for emit event for 100000 subscribers
        /// </summary>
        [Test]
        public void PerformanceSubscribeRegistryTest()
        {
            var dispatcher = new Dispatcher();
            var COUNTS = 10000;
            /* Create million registrations */
            /* Create tool to measure time */
            Action<int> stub = (value) => { };
            var randomInt = new System.Random();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            for (var i = 0; i < COUNTS; i++)
            {
                dispatcher.SubscribeRegistry(1, stub);
            }
            watch.Stop();
            /* Get performance time */
            var elapsedMs = watch.Elapsed.TotalMilliseconds;
            Debug.Log($"Elapsed time: {elapsedMs} ms");
            Assert.AreEqual(1, dispatcher.GetRegistrySize());
            Assert.Less(elapsedMs, 6);
        }

        
    }
}