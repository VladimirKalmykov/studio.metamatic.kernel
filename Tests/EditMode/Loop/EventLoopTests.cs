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

        [Test]
        public void AddActionByType_ShowBeCalled()
        {
            var eventLoop = new EventLoop<Types>();
            var results = new Dictionary<Types, int>();
            using var loop1 = eventLoop.AddAction(Types.Alfa, () =>
            {
                if (!results.ContainsKey(Types.Alfa))
                {
                    results[Types.Alfa] = 0;
                }
                results[Types.Alfa]++;
            });
            using var loop2 = eventLoop.AddAction(Types.Beta, () =>
            {
                if (!results.ContainsKey(Types.Beta))
                {
                    results[Types.Beta] = 0;
                }
                results[Types.Beta]++;
            });
            using var loop3 = eventLoop.AddAction(Types.Gamma, () =>
            {
                if (!results.ContainsKey(Types.Gamma))
                {
                    results[Types.Gamma] = 0;
                }
                results[Types.Gamma]++;
            });

            eventLoop.Run(Types.Alfa);
            eventLoop.Run(Types.Beta);
            eventLoop.Run(Types.Gamma);
            
            Assert.AreEqual(1, results[Types.Alfa]);
            Assert.AreEqual(1, results[Types.Beta]);
            Assert.AreEqual(1, results[Types.Gamma]);
            
            eventLoop.Run(Types.Alfa);
            eventLoop.Run(Types.Beta);
            eventLoop.Run(Types.Gamma);
            
            Assert.AreEqual(2, results[Types.Alfa]);
            Assert.AreEqual(2, results[Types.Beta]);
            Assert.AreEqual(2, results[Types.Gamma]);
        }
    }
}