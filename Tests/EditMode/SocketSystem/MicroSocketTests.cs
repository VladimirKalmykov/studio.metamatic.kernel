using System;
using NUnit.Framework;

namespace Metamatic
{
    struct State
    {
        public int value
    }
    public class MicroSocketTests
    {
        [Test]
        public void MicroSocketConnectOut()
        {
            State state = new State();
            var socket1 = new MicroSocket<State>();
            var socket2 = new MicroSocket<State>();
            socket1.Connect(socket2);
            socket1.writable.value = 1;
            Assert.AreEqual(socket2.state.value, 1);
        }
    }
}