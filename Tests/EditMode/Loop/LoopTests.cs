using Metamatic;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.TestTools;

namespace Metamatic.Tests
{
    public class LoopTests
    {
        private Loop m_Loop;

        [SetUp]
        public void Setup()
        {
            m_Loop = new Loop();
        }

        [Test]
        public void AddAction_ShouldExecuteAction()
        {
            m_Loop.Reset();
            var callCount = 0;
            List<float> deltaTimeList = new List<float>();
            var disposer = m_Loop.AddAction(() =>
            {
                callCount++;
            });
            m_Loop.Run();
            m_Loop.Run();
            m_Loop.Run();
            disposer.Dispose();
            m_Loop.Run();
            
            Assert.AreEqual(3, callCount);
        }
        
        [Test]
        public void AddMultipleActions_ShouldExecuteActions()
        {
            m_Loop.Reset();
            var callCountA = 0;
            var callCountB = 0;
            var disposer1 = m_Loop.AddAction(() =>
            {
                callCountA++;
            });
            var disposer2 = m_Loop.AddAction(() =>
            {
                callCountB++;
            });
            m_Loop.Run();
            m_Loop.Run();
            m_Loop.Run();
            disposer1.Dispose();
            disposer2.Dispose();
            m_Loop.Run();
            
            Assert.AreEqual(3, callCountA);
            Assert.AreEqual(3, callCountB);
        }
        
        [Test]
        public void AddNewActionsDuringLoop_ShouldLockItWell()
        {
            m_Loop.Reset();
            var callCountA = 0;
            var callCountB = 0;
            DisposableAction disposer1 = default;
            DisposableAction disposer2 = default;
            disposer1 = m_Loop.AddAction(() =>
            {
                callCountA++;
                if (callCountA == 1)
                {
                    // Second action will not be executed
                    disposer2.Dispose();
                }
            });
            disposer2 = m_Loop.AddAction(() =>
            {
                callCountB++;
            });
            m_Loop.Run();
            m_Loop.Run();
            m_Loop.Run();
            disposer1.Dispose();
            
            Assert.AreEqual(3, callCountA);
            Assert.AreEqual(0, callCountB);
        }
        
        [Test]
        public void WaitUntil_ShouldAutoRemoveAction()
        {
            m_Loop.Reset();
            var callCount = 0;
            m_Loop.WaitUntil(() =>
            {
                callCount++;
                return callCount == 3;
            }).Forget();
            m_Loop.Run();
            m_Loop.Run();
            m_Loop.Run();
            m_Loop.Run();
            
            Assert.AreEqual(3, callCount);
        }
        
        [Test]
        public void WaitWhile_ShouldAutoRemoveAction()
        {
            m_Loop.Reset();
            var callCount = 0;
            m_Loop.WaitWhile(() =>
            {
                callCount++;
                return callCount < 3;
            }).Forget();
            m_Loop.Run();
            m_Loop.Run();
            m_Loop.Run();
            m_Loop.Run();
            
            Assert.AreEqual(3, callCount);
        }

        [Test]
        public void WaitWhileTask_ShouldBeCompleted()
        {
           m_Loop.Reset();
           var whileTask = m_Loop.WaitWhile(() => false);
           m_Loop.Run();
           Assert.IsTrue(whileTask.Status == UniTaskStatus.Succeeded, "Task should be completed");
        }

        [UnityTest]
        public IEnumerator Yield_ShouldBeCompleted() => UniTask.ToCoroutine(async () =>
        {
            m_Loop.Reset();
            UniTask.Delay(1000).ContinueWith(() =>
            {
                m_Loop.Run();
            }).Forget();
            await m_Loop.Yield();
            Assert.Pass();
        });
        
        [UnityTest]
        public IEnumerator YieldToUniTask_ShouldBeCompleted() => UniTask.ToCoroutine(async () =>
        {
            m_Loop.Reset();
            UniTask.Delay(1000).ContinueWith(() =>
            {
                m_Loop.Run();
            }).Forget();
            await m_Loop.Yield().ToUniTask();
            Assert.Pass();
        });
        
        [UnityTest]
        public IEnumerator YieldToUniTaskWithCancellationToken_ShouldBeCancelled() => UniTask.ToCoroutine(async () =>
        {
            m_Loop.Reset();
            var cancellationSignalSource = CancellationSignalSource.Create();
            UniTask.Delay(500).ContinueWith(() =>
            {
                cancellationSignalSource.TryCancel();
            }).Forget();
            UniTask.Delay(1000).ContinueWith(() =>
            {
                m_Loop.Run();
            }).Forget();
            try
            {
                await m_Loop.Yield().ToUniTask(cancellationSignalSource.Signal); 
                Assert.Fail("Should throw OperationCanceledException");
            } catch (OperationCanceledException)
            {
                Assert.Pass();
            }
        });
    }
}