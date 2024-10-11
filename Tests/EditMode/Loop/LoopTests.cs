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
        private float m_Time;

        [SetUp]
        public void Setup()
        {
            var customTimeSource = new Loop.TimeSource(() => m_Time);
            m_Loop = new Loop(customTimeSource);
        }

        [Test]
        public void AddAction_ShouldExecuteAction()
        {
            m_Time = 0;
            m_Loop.Reset();
            List<float> deltaTimeList = new List<float>();
            var disposer = m_Loop.AddAction((deltaTime) =>
            {
                deltaTimeList.Add(deltaTime);
            });
            m_Time = 0;
            m_Loop.Update();
            m_Time = 1;
            m_Loop.Update();
            m_Time = 1.5f;
            m_Loop.Update();
            disposer.Dispose();
            m_Time = 2;
            m_Loop.Update();
            
            Assert.AreEqual(3, deltaTimeList.Count);
            Assert.AreEqual(0, deltaTimeList[0]);
            Assert.AreEqual(1, deltaTimeList[1]);
            Assert.AreEqual(0.5f, deltaTimeList[2]);
        }
        
        [Test]
        public void AddMultipleActions_ShouldExecuteActions()
        {
            m_Time = 0;
            m_Loop.Reset();
            List<float> deltaTimeList = new List<float>();
            var disposer1 = m_Loop.AddAction((deltaTime) =>
            {
                deltaTimeList.Add(deltaTime);
            });
            var disposer2 = m_Loop.AddAction((deltaTime) =>
            {
                deltaTimeList.Add(deltaTime);
            });
            m_Time = 0;
            m_Loop.Update();
            m_Time = 1;
            m_Loop.Update();
            m_Time = 1.5f;
            m_Loop.Update();
            disposer1.Dispose();
            disposer2.Dispose();
            m_Time = 2;
            m_Loop.Update();
            
            Assert.AreEqual(6, deltaTimeList.Count);
            Assert.AreEqual(0, deltaTimeList[0]);
            Assert.AreEqual(0, deltaTimeList[1]);
            Assert.AreEqual(1, deltaTimeList[2]);
            Assert.AreEqual(1, deltaTimeList[3]);
            Assert.AreEqual(0.5f, deltaTimeList[4]);
            Assert.AreEqual(0.5f, deltaTimeList[5]);
        }
        
        [Test]
        public void AddNewActionsDuringLoop_ShouldLockItWell()
        {
            m_Time = 0;
            m_Loop.Reset();
            List<float> deltaTimeList = new List<float>();
            DisposableAction disposer1 = default;
            DisposableAction disposer2 = default;
            disposer1 = m_Loop.AddAction((deltaTime) =>
            {
                deltaTimeList.Add(deltaTime);
                if (deltaTime == 1f)
                {
                    // Second action will not be executed
                    disposer2.Dispose();
                }
            });
            disposer2 = m_Loop.AddAction((deltaTime) =>
            {
                deltaTimeList.Add(deltaTime);
            });
            m_Time = 0;
            m_Loop.Update();
            m_Time = 1;
            m_Loop.Update();
            m_Time = 1.5f;
            m_Loop.Update();
            disposer1.Dispose();
            
            Assert.AreEqual(4, deltaTimeList.Count);
            Assert.AreEqual(0, deltaTimeList[0]);
            Assert.AreEqual(0, deltaTimeList[1]);
            Assert.AreEqual(1, deltaTimeList[2]);
            Assert.AreEqual(0.5f, deltaTimeList[3]);
        }
        
        [Test]
        public void WaitUntil_ShouldAutoRemoveAction()
        {
            m_Time = 0;
            m_Loop.Reset();
            List<float> deltaTimeList = new List<float>();
            m_Loop.WaitUntil((deltaTime) =>
            {
                deltaTimeList.Add(deltaTime);
                return deltaTime > 0.3f;
            }).Forget();
            m_Time = 0;
            m_Loop.Update();
            m_Time = 0.1f;
            m_Loop.Update();
            m_Time = 0.5f;
            m_Loop.Update();
            m_Time = 1f;
            m_Loop.Update();
            
            Assert.AreEqual(3, deltaTimeList.Count);
            Assert.AreEqual(0f, deltaTimeList[0]);
            Assert.AreEqual(0.1f, deltaTimeList[1]);
            Assert.AreEqual(0.4f, deltaTimeList[2]);
        }
        
        [Test]
        public void WaitWhile_ShouldAutoRemoveAction()
        {
            m_Time = 0;
            m_Loop.Reset();
            List<float> deltaTimeList = new List<float>();
            m_Loop.WaitWhile((deltaTime) =>
            {
                deltaTimeList.Add(deltaTime);
                return deltaTime < 0.3f;
            }).Forget();
            m_Time = 0;
            m_Loop.Update();
            m_Time = 0.1f;
            m_Loop.Update();
            m_Time = 0.5f;
            m_Loop.Update();
            m_Time = 1f;
            m_Loop.Update();
            
            Assert.AreEqual(3, deltaTimeList.Count);
            Assert.AreEqual(0f, deltaTimeList[0]);
            Assert.AreEqual(0.1f, deltaTimeList[1]);
            Assert.AreEqual(0.4f, deltaTimeList[2]);
        }

        [Test]
        public void WaitWhileTask_ShouldBeCompleted()
        {
            m_Time = 0;
            m_Loop.Reset();
           var whileTask = m_Loop.WaitWhile((deltaTime) => deltaTime < 0.3f);
           m_Time = 1f;
           m_Loop.Update();
           Assert.IsTrue(whileTask.Status == UniTaskStatus.Succeeded, "Task should be completed");
        }

        [UnityTest]
        public IEnumerator Yield_ShouldBeCompleted() => UniTask.ToCoroutine(async () =>
        {
            m_Time = 0;
            m_Loop.Reset();
            UniTask.Delay(1000).ContinueWith(() =>
            {
                m_Time = 1f;
                m_Loop.Update();
            }).Forget();
            await m_Loop.Yield();
            Assert.Pass();
        });
        
        [UnityTest]
        public IEnumerator YieldToUniTask_ShouldBeCompleted() => UniTask.ToCoroutine(async () =>
        {
            m_Time = 0;
            m_Loop.Reset();
            UniTask.Delay(1000).ContinueWith(() =>
            {
                m_Time = 1f;
                m_Loop.Update();
            }).Forget();
            await m_Loop.Yield().ToUniTask();
            Assert.Pass();
        });
        
        [UnityTest]
        public IEnumerator YieldToUniTaskWithCancellationToken_ShouldBeCancelled() => UniTask.ToCoroutine(async () =>
        {
            m_Time = 0;
            m_Loop.Reset();
            var cancellationSignalSource = CancellationSignalSource.Create();
            UniTask.Delay(500).ContinueWith(() =>
            {
                cancellationSignalSource.TryCancel();
            }).Forget();
            UniTask.Delay(1000).ContinueWith(() =>
            {
                m_Time = 1f;
                m_Loop.Update();
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