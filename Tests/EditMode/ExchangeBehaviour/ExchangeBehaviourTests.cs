using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using Metamatic.StorageDriver;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Metamatic.Tests
{
    [TestFixture]
    public class ExchangeBehaviourTests
    {
        [UnityTest]
        public IEnumerator TestSimpleReceive() => UniTask.ToCoroutine(async () =>
        {
            var state = 0;
            await using var exchange = ExchangeBehaviour.Run(async (exchanger, token) =>
            {
                var receivedValue = await exchanger.Receive<int>();
                state = receivedValue;
            });
            Assert.AreEqual(0, state);
            exchange.Send(1);
            // await UniTask.Delay(100);
            Assert.AreEqual(1, state);
        });
        
        [UnityTest]
        public IEnumerator TestSimpleSend() => UniTask.ToCoroutine(async () =>
        {
            var state = 0;
            await using var exchange = ExchangeBehaviour.Run(async (exchanger, token) =>
            {
                exchanger.Send(1);
            });
            Assert.AreEqual(0, state);
            state = await exchange.Receive<int>();
            // await UniTask.Delay(100);
            Assert.AreEqual(1, state);
        });
        
        [UnityTest]
        public IEnumerator TestSimpleDuplex() => UniTask.ToCoroutine(async () =>
        {
            var innerState = 0;
            var outerState = 0;
            await using var exchange = ExchangeBehaviour.Run(async (exchanger, token) =>
            {
                innerState = await exchanger.Duplex<int, int>(33);
            });
            Assert.AreEqual(0, innerState);
            Assert.AreEqual(0, outerState);
            outerState = await exchange.Duplex<int, int>(44);
            // await UniTask.Delay(100);
            Assert.AreEqual(44, innerState);
            Assert.AreEqual(33, outerState);
        });
        
        [UnityTest]
        public IEnumerator TestDoubleDuplex() => UniTask.ToCoroutine(async () =>
        {
            var innerState = 0;
            var outerState = 0;
            await using var exchange = ExchangeBehaviour.Run(async (exchanger, token) =>
            {
                innerState = await exchanger.Duplex<int, int>(33);
                innerState = await exchanger.Duplex<int, int>(67);
            });
            Assert.AreEqual(0, innerState);
            Assert.AreEqual(0, outerState);
            outerState = await exchange.Duplex<int, int>(44);
            // await UniTask.Delay(100);
            Assert.AreEqual(44, innerState);
            Assert.AreEqual(33, outerState);
            outerState = await exchange.Duplex<int, int>(56);
            Assert.AreEqual(56, innerState);
            Assert.AreEqual(67, outerState);
        });
        
        [UnityTest]
        public IEnumerator TestInnerReceiveThenDuplex() => UniTask.ToCoroutine(async () =>
        {
            var innerState = 0;
            var outerState = 0;
            await using var exchange = ExchangeBehaviour.Run(async (exchanger, token) =>
            {
                innerState = await exchanger.Receive<int>();
                innerState = await exchanger.Duplex<int, int>(67);
                throw new System.Exception("This should not be reached");
            });
            Assert.AreEqual(0, innerState);
            Assert.AreEqual(0, outerState);
            outerState = await exchange.Duplex<int, int>(44);
            // await UniTask.Delay(100);
            Assert.AreEqual(44, innerState);
            Assert.AreEqual(67, outerState);
        });
        
        [UnityTest]
        public IEnumerator TestOuterReceiveThenDuplex() => UniTask.ToCoroutine(async () =>
        {
            var innerState = 0;
            var outerState = 0;
            await using var exchange = ExchangeBehaviour.Run(async (exchanger, token) =>
            {
                innerState = await exchanger.Duplex<int, int>(67);
            });
            Assert.AreEqual(0, innerState);
            Assert.AreEqual(0, outerState);
            outerState = await exchange.Receive<int>();
            Assert.AreEqual(0, innerState);
            Assert.AreEqual(67, outerState);
            exchange.Duplex<int, int>(66);
            Assert.AreEqual(66, innerState);
            Assert.AreEqual(67, outerState);
        });
        
        [UnityTest]
        public IEnumerator TestTypelessSendReceiveDuplex() => UniTask.ToCoroutine(async () =>
        {
            var innerState = 0;
            var outerState = 0;
            await using var exchange = ExchangeBehaviour.Run(async (exchanger, token) =>
            {
                await exchanger.Receive();
                innerState++;
                await exchanger.Duplex();
                innerState++;
                exchanger.Send();
                innerState++;
            });
            Assert.AreEqual(0, innerState);
            Assert.AreEqual(0, outerState);
            exchange.Send();
            outerState++;
            Assert.AreEqual(1, innerState);
            Assert.AreEqual(1, outerState);
            await exchange.Duplex();
            outerState++;
            Assert.AreEqual(3, innerState, "Inner state should be 3 because of the two sync increments");
            Assert.AreEqual(2, outerState, "Outer state should be 2");
            await exchange.Receive();
            outerState++;
            Assert.AreEqual(3, innerState, "Inner state should be 3");
            Assert.AreEqual(3, outerState, "Outer state should be 3");
        });
        
        [UnityTest]
        public IEnumerator TestTypelessSendReceiveDuplexWithTimers() => UniTask.ToCoroutine(async () =>
        {
            var innerState = 0;
            var outerState = 0;
            await using var exchange = ExchangeBehaviour.Run(async (exchanger, token) =>
            {
                await exchanger.Receive();
                innerState++;
                /* Wait for 100 ms */
                await UniTask.Delay(100, cancellationToken: token);
                await exchanger.Duplex();
                innerState++;
                exchanger.Send();
                innerState++;
            });
            Assert.AreEqual(0, innerState);
            Assert.AreEqual(0, outerState);
            exchange.Send();
            outerState++;
            Assert.AreEqual(1, innerState);
            Assert.AreEqual(1, outerState);
            /* Wait for 100 ms */
            await UniTask.Delay(100);
            await exchange.Duplex();
            outerState++;
            Assert.AreEqual(3, innerState, "Inner state should be 3 because of the two sync increments");
            Assert.AreEqual(2, outerState, "Outer state should be 2");
            await exchange.Receive();
            outerState++;
            Assert.AreEqual(3, innerState, "Inner state should be 3");
            Assert.AreEqual(3, outerState, "Outer state should be 3");
        });

        [UnityTest]
        public IEnumerator TestThrowIfDoubleSendFromInsideWithoutAccept() => UniTask.ToCoroutine(async () =>
        {
            var wasError = false;
            try
            {
                await using var test = ExchangeBehaviour.Run(async (exchanger, token) =>
                {
                    exchanger.Send(1);
                    exchanger.Send(2);
                });
            } catch (System.Exception e)
            {
                wasError = true;
                Assert.AreEqual("Output completion source is already resolved", e.Message);
            }
            Assert.IsTrue(wasError, "Must throw an exception");
        });
        
        [UnityTest]
        public IEnumerator TestThrowIfDoubleSendFromOutputWithoutAccept() => UniTask.ToCoroutine(async () =>
        {
            var wasError = false;
            try
            {
                await using var test = ExchangeBehaviour.Run(async (exchanger, token) =>
                {
                    
                });
                test.Send(1);
                test.Send(2);
            } catch (System.Exception e)
            {
                wasError = true;
                Assert.AreEqual("Input completion source is already resolved", e.Message);
            }
            Assert.IsTrue(wasError, "Must throw an exception");
        });
        
        [UnityTest]
        public IEnumerator TestThrowCancelIfBehaviourHasBeenDisposed() => UniTask.ToCoroutine(async () =>
        {
            var wasError = false;
            var errorIsCancellation = false;
            try
            {
                var test = ExchangeBehaviour.Run(async (exchanger, token) =>
                {
                    
                });
                /* Dispose after 100 ms */
                UniTask.Delay(100).ContinueWith(() =>
                {
                    test.Dispose();
                });
                await test.Receive();
            } catch (System.Exception e)
            {
                wasError = true;
                errorIsCancellation = e is OperationCanceledException;
            }
            Assert.IsTrue(wasError, "Must throw an exception");
            Assert.IsTrue(errorIsCancellation, "Must throw a cancellation exception");
        });
        
        [UnityTest]
        public IEnumerator TestThrowCancelInsideIfBehaviourHasBeenDisposed() => UniTask.ToCoroutine(async () =>
        {
            var wasError = false;
            var errorIsCancellation = false;
            var test = ExchangeBehaviour.Run(async (exchanger, token) =>
            {
                try
                {
                    await exchanger.Receive();
                } catch (System.Exception e)
                {
                    wasError = true;
                    if (e is OperationCanceledException)
                    {
                        errorIsCancellation = true;
                    }
                }
            });
            /* Dispose after 100 ms */
            UniTask.Delay(100).ContinueWith(() =>
            {
                test.Dispose();
            });
            await test.task;
            Assert.IsTrue(wasError, "Must throw an exception");
            Assert.IsTrue(errorIsCancellation, "Must throw a cancellation exception");
        });
        
        [UnityTest]
        public IEnumerator TestCanBeDisposedAsync() => UniTask.ToCoroutine(async () =>
        {
            var wasError = false;
            var errorIsCancellation = false;
            var valueAfterDelay = 0;
            {
                await using var test = ExchangeBehaviour.Run(async (exchanger, token) =>
                {
                    try
                    {
                        await exchanger.Receive();
                    }
                    catch (System.Exception e)
                    {
                        wasError = true;
                        if (e is OperationCanceledException)
                        {
                            errorIsCancellation = true;
                        }
                    }
                    finally
                    {
                        if (exchanger.isDisposedAsync)
                        {
                            await UniTask.Delay(100);
                            valueAfterDelay = 1;
                        }
                        else
                        {
                            valueAfterDelay = 2;
                        }
                    }
                });
            }
            Assert.IsTrue(wasError, "Must throw an exception");
            Assert.IsTrue(errorIsCancellation, "Must throw a cancellation exception");
            Assert.AreEqual(1, valueAfterDelay, "Value after delay must be 1");
        });
    }
}