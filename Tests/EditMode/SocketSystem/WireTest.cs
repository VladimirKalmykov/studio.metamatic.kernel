using System;
using NUnit.Framework;

namespace Metamatic
{
    struct State
    {
        public int value;
    }
    public class WireTests
    {
        [Test]
        public void SingleWire()
        {
            var wire = new Wire<int>(0);
            var vfs = 0;
            wire.Subscribe((ref int value) =>
            {
                vfs = value;
            });
            wire.SetData(1);
            Assert.AreEqual(1, vfs);
            Assert.AreEqual(1, wire.data);
            wire.SetData((ref int currentValue) => currentValue + 2);
            Assert.AreEqual(3, vfs);
            Assert.AreEqual(3, wire.data);
        }

        [Test]
        public void TwoWires()
        {
            var wire1 = new Wire<int>(15);
            var wire2 = new Wire<int>();
            wire1.ConnectOutput(wire2);
            Assert.AreEqual(15, wire2.data);
        }
        
        [Test]
        public void TenWires()
        {
            var wire1 = new Wire<int>(15);
            var wire2 = new Wire<int>();
            var wire3 = new Wire<int>();
            var wire4 = new Wire<int>();
            var wire5 = new Wire<int>();
            var wire6 = new Wire<int>();
            var wire7 = new Wire<int>();
            var wire8 = new Wire<int>();
            var wire9 = new Wire<int>();
            var wire10 = new Wire<int>();
            wire1.ConnectOutput(wire2);
            wire2.ConnectOutput(wire3);
            wire3.ConnectOutput(wire4);
            wire4.ConnectOutput(wire5);
            wire5.ConnectOutput(wire6);
            wire6.ConnectOutput(wire7);
            wire7.ConnectOutput(wire8);
            wire8.ConnectOutput(wire9);
            wire9.ConnectOutput(wire10);
            Assert.AreEqual(15, wire10.data);
            var vfs = 0;
            wire10.Subscribe((ref int data) =>
            {
                vfs = data;
            });
            Assert.AreEqual(15, vfs);
            wire1.SetData(443);
            Assert.AreEqual(443, vfs);
        }
        
        [Test]
        public void TwoWiresWithReconnect()
        {
            var wire1 = new Wire<int>(15);
            var wire2 = new Wire<int>();
            wire1.ConnectOutput(wire2);
            Assert.AreEqual(15, wire2.data);
            wire1.SetData(16);
            Assert.AreEqual(16, wire2.data);
            wire2.DisconnectInput();
            Assert.AreEqual(0, wire2.data);
            wire1.ConnectOutput(wire2);
            Assert.AreEqual(16, wire2.data);
            wire1.SetData(17);
            Assert.AreEqual(17, wire2.data);
        }
        
        [Test]
        public void TwoWiresWithReconnectAndReconnectEvent()
        {
            var wire1 = new Wire<int>(15);
            var wire2 = new Wire<int>();
            var reconnectCount = 0;
            wire2.OnReconnect += () =>
            {
                reconnectCount++;
            };
            Assert.AreEqual(0, reconnectCount);
            wire1.ConnectOutput(wire2);
            Assert.AreEqual(1, reconnectCount);
            Assert.AreEqual(15, wire2.data);
            wire1.SetData(16);
            Assert.AreEqual(16, wire2.data);
            wire2.DisconnectInput();
            Assert.AreEqual(0, wire2.data);
            Assert.AreEqual(2, reconnectCount);
            wire1.ConnectOutput(wire2);
            Assert.AreEqual(16, wire2.data);
            Assert.AreEqual(3, reconnectCount);
            wire1.SetData(17);
            Assert.AreEqual(17, wire2.data);
            Assert.AreEqual(3, reconnectCount);
        }
        
        [Test]
        public void TwoStepConnectionChangeSource() {
            var wire1 = new Wire<int>(15);
            var wire2 = new Wire<int>(33);
            var wire3 = new Wire<int>();
            var vfs = 0;
            wire3.Subscribe((ref int data) =>
            {
                vfs = data;
            });
            Assert.AreEqual(0, vfs);
            wire3.ConnectInput(wire1);
            Assert.AreEqual(15, wire3.data);
            Assert.AreEqual(15, vfs);
            wire3.ConnectInput(wire2);
            Assert.AreEqual(33, wire3.data);
            Assert.AreEqual(33, vfs);
        }
    }
}