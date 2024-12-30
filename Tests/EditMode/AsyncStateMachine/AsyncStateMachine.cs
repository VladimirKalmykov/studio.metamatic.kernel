using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.TestTools;

namespace Metamatic.Tests
{
    [TestFixture]
    public class AsyncStateMachineTests
    {
        private enum State
        {
            Idle,
            Running,
            Jumping,
            Falling,
            Landing
        }
        [UnityTest]
        public IEnumerator SimpleTransitionBetweenTwoStates() => UniTask.ToCoroutine(async () =>
        {
            var actualState = State.Idle;
            var actualStatus = new List<AsyncStateMachine<State>.TransitionStatus>();
            var cancelled = false;
            await using var stateMachine = new AsyncStateMachine<State>(async transition =>
            {
                try
                {
                    /* Wait a bit */
                    await UniTask.Delay(10);
                    actualState = transition.state;
                    await transition.WaitNextState();
                    throw new Exception("Must be unreachable");
                } catch (Exception e)
                {
                    cancelled = true;
                }
            });
            using var transitionStatusListener = stateMachine.Debug_SubscribeTransitionStatusChanged(status =>
            {
                actualStatus.Add(status);
            });

            await stateMachine.PushLegacy(State.Running);
            await UniTask.Delay(10);
            Assert.AreEqual(State.Running, actualState);
            Assert.AreEqual(2, actualStatus.Count, "Expected two status changes");
            Assert.AreEqual(AsyncStateMachine<State>.TransitionStatus.FromPrevToCurrentProcessing, actualStatus[0], "Expected first status to be processing");
            Assert.AreEqual(AsyncStateMachine<State>.TransitionStatus.FromPrevToCurrentCompleted, actualStatus[1], "Expected first status to be processed");
            Debug.Log("Completed");
        });
        
        [UnityTest]
        public IEnumerator TransitionBetweenThreeStates() => UniTask.ToCoroutine(async () =>
        {
            var actualState = State.Idle;
            var actualStatus = new List<AsyncStateMachine<State>.TransitionStatus>();
            var progress = 0;
            await using var stateMachine = new AsyncStateMachine<State>(async transition =>
            {
                try
                {
                    progress++;
                    /* Wait a bit */
                    await UniTask.Delay(10);
                    actualState = transition.state;
                    progress++;
                    var nextState = await transition.WaitNextState();
                    progress++;
                }
                catch (Exception e)
                {
                    // ignored
                }
            });
            using var transitionStatusListener = stateMachine.Debug_SubscribeTransitionStatusChanged(status =>
            {
                actualStatus.Add(status);
            });

            await stateMachine.PushLegacy(State.Running);
            await stateMachine.PushLegacy(State.Falling);
            await UniTask.Delay(10);
            Assert.AreEqual(5, progress, "Progress should be 5");
            Assert.AreEqual(State.Falling, actualState);
            Assert.AreEqual(7, actualStatus.Count, "Expected six status changes");
            Assert.AreEqual(AsyncStateMachine<State>.TransitionStatus.FromPrevToCurrentProcessing, actualStatus[0], "Expected first status to be processing");
            Assert.AreEqual(AsyncStateMachine<State>.TransitionStatus.FromPrevToCurrentCompleted, actualStatus[1], "Expected first status to be processed");
            Assert.AreEqual(AsyncStateMachine<State>.TransitionStatus.FromCurrentToNextProcessing, actualStatus[2], "Expected first status to be completing");
            Assert.AreEqual(AsyncStateMachine<State>.TransitionStatus.FromCurrentToNextCompleted, actualStatus[3], "Expected first status to be completing");
            Assert.AreEqual(AsyncStateMachine<State>.TransitionStatus.Idle, actualStatus[4], "Expected first finalized with Idle status");
            Assert.AreEqual(AsyncStateMachine<State>.TransitionStatus.FromPrevToCurrentProcessing, actualStatus[5], "Expected first status to be processing");
            Assert.AreEqual(AsyncStateMachine<State>.TransitionStatus.FromPrevToCurrentCompleted, actualStatus[6], "Expected first status to be processed");
            Debug.Log("Completed");
        });
        
        
        [UnityTest]
        public IEnumerator TransitionQueue() => UniTask.ToCoroutine(async () =>
        {
            var statesHistory = new List<State>();
            var transitionHistory = new List<(State, State, State)>();
            var actualStatus = new List<AsyncStateMachine<State>.TransitionStatus>();
            await using var stateMachine = new AsyncStateMachine<State>(async transition =>
            {
                try
                {
                    /* Wait a bit */
                    await UniTask.Delay(10);
                    statesHistory.Add(transition.state);
                    var nextState = await transition.WaitNextState();
                    await UniTask.Delay(10);
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                }
                finally
                {
                    transitionHistory.Add((transition.prevState, transition.state, transition.nextState));
                }
            });
            using var transitionStatusListener = stateMachine.Debug_SubscribeTransitionStatusChanged(status =>
            {
                actualStatus.Add(status);
            });

            await stateMachine.PushLegacy(State.Running); // Running immediately
            await stateMachine.PushLegacy(State.Falling); // Add to queue as next state
            await stateMachine.PushLegacy(State.Jumping); // Add to queue
            await stateMachine.PushLegacy(State.Landing); // Add to queue
            Assert.AreEqual(4, statesHistory.Count);
            Assert.AreEqual(State.Running, statesHistory[0]);
            Assert.AreEqual(State.Falling, statesHistory[1]);
            Assert.AreEqual(State.Jumping, statesHistory[2]);
            Assert.AreEqual(State.Landing, statesHistory[3]);
            
            Assert.AreEqual(3, transitionHistory.Count);
            Assert.AreEqual((State.Idle, State.Running, State.Falling), transitionHistory[0]);
            Assert.AreEqual((State.Running, State.Falling, State.Jumping), transitionHistory[1]);
            Assert.AreEqual((State.Falling, State.Jumping, State.Landing), transitionHistory[2]);
            
            Debug.Log("Completed");
        });
        
        [UnityTest]
        public IEnumerator ExternalCancel() => UniTask.ToCoroutine(async () =>
        {
            var statesHistory = new List<State>();
            var transitionHistory = new List<(State, State, State)>();
            var actualStatus = new List<AsyncStateMachine<State>.TransitionStatus>();
            await using var stateMachine = new AsyncStateMachine<State>(async transition =>
            {
                try
                {
                    /* Wait a bit */
                    await UniTask.Delay(1000, cancellationToken: transition.cancellationSignal);
                    statesHistory.Add(transition.state);
                    var nextState = await transition.WaitNextState();
                    await UniTask.Delay(10, cancellationToken: transition.cancellationSignal);
                }
                finally
                {
                    transitionHistory.Add((transition.prevState, transition.state, transition.nextState));
                }
            });
            using var transitionStatusListener = stateMachine.Debug_SubscribeTransitionStatusChanged(status =>
            {
                actualStatus.Add(status);
            });
            Exception exception = null;
            var cancellationTokenSource = SignalSource.Create();
            UniTask.Delay(300).ContinueWith(() =>
            {
                cancellationTokenSource.TryRaise();
            }).Forget();
            try {
                await stateMachine.PushLegacy(State.Running, cancellationTokenSource.Signal);
            } catch (Exception e)
            {
                exception = e;
            }
            
            Assert.AreEqual(0, statesHistory.Count, "No states should be added");
            Assert.AreEqual(1, transitionHistory.Count, "Only one transition should be added");
            /* Exception should be thrown and it should be OperationCanceledException */
            Assert.NotNull(exception);
            Assert.AreEqual(typeof(OperationCanceledException), exception.GetType());
        });
        
        [UnityTest]
        public IEnumerator ExternalCancelForDeferredState() => UniTask.ToCoroutine(async () =>
        {
            var statesHistory = new List<State>();
            var transitionHistory = new List<(State, State, State)>();
            var actualStatus = new List<AsyncStateMachine<State>.TransitionStatus>();
            await using var stateMachine = new AsyncStateMachine<State>(async transition =>
            {
                try
                {
                    /* Wait a bit */
                    await UniTask.Delay(1000, cancellationToken: transition.cancellationSignal);
                    statesHistory.Add(transition.state);
                    var nextState = await transition.WaitNextState();
                    await UniTask.Delay(10, cancellationToken: transition.cancellationSignal);
                }
                finally
                {
                    transitionHistory.Add((transition.prevState, transition.state, transition.nextState));
                }
            });
            using var transitionStatusListener = stateMachine.Debug_SubscribeTransitionStatusChanged(status =>
            {
                actualStatus.Add(status);
            });
            Exception exception = null;
            var cancellationTokenSource = SignalSource.Create();
            UniTask.Delay(300).ContinueWith(() =>
            {
                cancellationTokenSource.TryRaise();
            }).Forget();
            stateMachine.PushLegacy(State.Running);
            try {
                await stateMachine.PushLegacy(State.Falling, cancellationTokenSource.Signal);
            } catch (Exception e)
            {
                exception = e;
            }
            
            Assert.AreEqual(0, statesHistory.Count, "No state should be added, because first transition must be cancelled by next transition. And next transition must be cancelled by external cancellation");
            Assert.AreEqual(2, transitionHistory.Count, "Two transitions should be added");
            /* Exception should be thrown and it should be OperationCanceledException */
            Assert.NotNull(exception);
            Assert.AreEqual(typeof(OperationCanceledException), exception.GetType());
        });
        
        [UnityTest]
        public IEnumerator CatchError() => UniTask.ToCoroutine(async () =>
        {
            Exception finalException = null;
            try
            {
                var statesHistory = new List<State>();
                var transitionHistory = new List<(State, State, State)>();
                var actualStatus = new List<AsyncStateMachine<State>.TransitionStatus>();
                await using var stateMachine = new AsyncStateMachine<State>(async transition =>
                {
                    try
                    {
                        /* Wait a bit */
                        await UniTask.Delay(10, cancellationToken: transition.cancellationSignal);
                        throw new Exception("Manual Error");
                    }
                    finally
                    {
                        transitionHistory.Add((transition.prevState, transition.state, transition.nextState));
                    }
                });
                using var transitionStatusListener = stateMachine.Debug_SubscribeTransitionStatusChanged(status =>
                {
                    actualStatus.Add(status);
                });
                Exception exception = null;

                try
                {
                    await stateMachine.PushLegacy(State.Running);
                }
                catch (Exception e)
                {
                    exception = e;
                }

                Assert.AreEqual(0, statesHistory.Count, "No state should be added, because of throwing error");
                Assert.AreEqual(1, transitionHistory.Count, "One transition should be added");
                /* Exception should be thrown and it should be OperationCanceledException */
                Assert.NotNull(exception);
                Assert.AreEqual("Manual Error", exception.Message);
            } catch (Exception e)
            {
                finalException = e;
            }
            Assert.NotNull(finalException);
        });

        [UnityTest]
        public IEnumerator OnTransitionStep() => UniTask.ToCoroutine(async () =>
        {
            var transitionStepsHistory = new List<(State, State)>();
            await using var stateMachine = new AsyncStateMachine<State>(async transition =>
            {
                await UniTask.Delay(10);
                await transition.WaitNextState();
                Debug.Log("Next state is specified");
            });
            
            stateMachine.OnTransitionStep += (from, to) =>
            {
                transitionStepsHistory.Add((from, to));
            };
            
            await stateMachine.PushLegacy(State.Running);
            await stateMachine.PushLegacy(State.Falling);
            await stateMachine.PushLegacy(State.Jumping);
            
            Assert.AreEqual(3, transitionStepsHistory.Count);
            Assert.AreEqual((State.Idle, State.Running), transitionStepsHistory[0]);
            Assert.AreEqual((State.Running, State.Falling), transitionStepsHistory[1]);
            Assert.AreEqual((State.Falling, State.Jumping), transitionStepsHistory[2]);
        });
        
        [UnityTest]
        public IEnumerator CompleteTransitionWithoutWaitingNext() => UniTask.ToCoroutine(async () =>
        {
            var transitionStepsHistory = new List<(State, State)>();
            await using var stateMachine = new AsyncStateMachine<State>(async transition =>
            {
                await UniTask.Delay(10);
            });
            
            stateMachine.OnTransitionStep += (from, to) =>
            {
                transitionStepsHistory.Add((from, to));
            };
            
            await stateMachine.PushLegacy(State.Running);
            
            Assert.AreEqual(1, transitionStepsHistory.Count);
            Assert.AreEqual((State.Idle, State.Running), transitionStepsHistory[0]);
        });
    }
}