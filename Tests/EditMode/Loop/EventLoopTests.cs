using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Metamatic.Tests
{
    [TestFixture]
    public class EventLoopTests
    {
        private enum Types
        {
            Alfa,
            Beta,
            Gamma
        }

        private float m_Time = 0;
        [Test]
        public void AddActionByType_ShowBeCalled()
        {
            m_Time = 0;
            var timeSource = new Loop.TimeSource(() => m_Time);
            var eventLoop = new EventLoop<Types>(timeSource);
            var results = new Dictionary<Types, List<float>>();
            using var loop1 = eventLoop.AddAction(Types.Alfa, delta =>
            {
                if (!results.ContainsKey(Types.Alfa))
                {
                    results[Types.Alfa] = new List<float>();
                }
                results[Types.Alfa].Add(delta);
            });
            using var loop2 = eventLoop.AddAction(Types.Beta, delta =>
            {
                if (!results.ContainsKey(Types.Beta))
                {
                    results[Types.Beta] = new List<float>();
                }
                results[Types.Beta].Add(delta);
            });
            using var loop3 = eventLoop.AddAction(Types.Gamma, delta =>
            {
                if (!results.ContainsKey(Types.Gamma))
                {
                    results[Types.Gamma] = new List<float>();
                }
                results[Types.Gamma].Add(delta);
            });

            m_Time = 0.5f;
            eventLoop.Run(Types.Alfa);
            eventLoop.Run(Types.Beta);
            eventLoop.Run(Types.Gamma);
            
            Assert.AreEqual(1, results[Types.Alfa].Count);
            Assert.AreEqual(1, results[Types.Beta].Count);
            Assert.AreEqual(1, results[Types.Gamma].Count);
            Assert.AreEqual(0.5f, results[Types.Alfa][0]);
            Assert.AreEqual(0.5f, results[Types.Beta][0]);
            Assert.AreEqual(0.5f, results[Types.Gamma][0]);
            
            m_Time = 1;
            eventLoop.Run(Types.Alfa);
            m_Time = 1.5f;
            eventLoop.Run(Types.Beta);
            m_Time = 2;
            eventLoop.Run(Types.Gamma);
            
            Assert.AreEqual(2, results[Types.Alfa].Count);
            Assert.AreEqual(2, results[Types.Beta].Count);
            Assert.AreEqual(2, results[Types.Gamma].Count);
            Assert.AreEqual(0.5, results[Types.Alfa][1]);
            Assert.AreEqual(1f, results[Types.Beta][1]);
            Assert.AreEqual(1.5f, results[Types.Gamma][1]);
        }
    }
}