using Metamatic;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

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
            List<float> deltaTimeList = new List<float>();
            DisposableAction disposer1 = default;
            DisposableAction disposer2 = default;
            disposer1 = m_Loop.AddAction((deltaTime) =>
            {
                deltaTimeList.Add(deltaTime);
                if (deltaTime == 1f)
                {
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
            
            Assert.AreEqual(5, deltaTimeList.Count);
            Assert.AreEqual(0, deltaTimeList[0]);
            Assert.AreEqual(0, deltaTimeList[1]);
            Assert.AreEqual(1, deltaTimeList[2]);
            Assert.AreEqual(1, deltaTimeList[3]);
            Assert.AreEqual(0.5f, deltaTimeList[4]);
        }
    }
}