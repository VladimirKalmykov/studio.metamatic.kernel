using System;
using System.Collections.Generic;
using Metamatic;
using NUnit.Framework;
using UnityEditor.VersionControl;
using UnityEngine;

namespace Tests.EditMode.SocketSystem
{
    public class TestSocketScheme: SocketScheme<TypeBundle<string>, TypeBundle<int>> {
        
    }
    public class MultiTypeSocketScheme: SocketScheme<TypeBundle<string, int, bool>, TypeBundle<float, byte>> {
        
    }

    public class InputLessSocketScheme : SocketScheme<TypeBundle<int, float>>
    {
        
    }
    public class MonoTypeSocketScheme: SocketScheme<TypeBundle<int>, TypeBundle<int>> {
        
    }
    public class SocketScheme: MonoBehaviour
    {
        [Test]
        public void SocketSchemeTypes()
        {
            var socketInputTypes = TestSocketScheme.inputBundle.GetTypes();
            Assert.AreEqual(1, socketInputTypes.Length);
            Assert.AreEqual(typeof(int), socketInputTypes[0]);
            var socketOutputTypes = TestSocketScheme.outputBundle.GetTypes();
            Assert.AreEqual(1, socketOutputTypes.Length);
            Assert.AreEqual(typeof(string), socketOutputTypes[0]);
        }

        [Test]
        public void ScoketSchemeConnection()
        {
            var socketInputPin = new VirtualPin<int>(this);
            var socketOutputPin = new VirtualPin<string>(this);
            var plugInputPin = new VirtualPin<int>(this);
            var plugOutputPin = new VirtualPin<string>(this);
            
            var scheme1 = new TestSocketScheme();
            scheme1.Initialize(new IPin[] { socketOutputPin }, new IPin[] { socketInputPin });
            
            var scheme2 = new TestSocketScheme();
            scheme2.Initialize(new IPin[] { plugOutputPin }, new IPin[] { plugInputPin });
            
            scheme1.ConnectPlug(scheme2);
            
            socketOutputPin.wire.SetData("hello");
            Assert.AreEqual(plugOutputPin.wire.data, "hello");
        }

        [Test]
        public void MultiTypeConnection()
        {
            var socketOutputPins = new IPin[] { new VirtualPin<string>(this), new VirtualPin<int>(this), new VirtualPin<bool>(this) };
            var socketInputPins = new IPin[] { new VirtualPin<float>(this), new VirtualPin<byte>(this) };
            var plugOutputPins = new IPin[] { new VirtualPin<string>(this), new VirtualPin<int>(this), new VirtualPin<bool>(this) };
            var plugInputPins = new IPin[] { new VirtualPin<float>(this), new VirtualPin<byte>(this) };
            
            var scheme1 = new MultiTypeSocketScheme();
            scheme1.Initialize(socketOutputPins, socketInputPins);
            
            var scheme2 = new MultiTypeSocketScheme();
            scheme2.Initialize(plugOutputPins, plugInputPins);
            
            scheme1.ConnectPlug(scheme2);
            
            ((VirtualPin<string>)socketOutputPins[0]).wire.SetData("hello");
            Assert.AreEqual(((VirtualPin<string>)plugOutputPins[0]).wire.data, "hello");
            
            ((VirtualPin<int>)socketOutputPins[1]).wire.SetData(1);
            Assert.AreEqual(((VirtualPin<int>)plugOutputPins[1]).wire.data, 1);
            
            ((VirtualPin<bool>)socketOutputPins[2]).wire.SetData(true);
            Assert.AreEqual(((VirtualPin<bool>)plugOutputPins[2]).wire.data, true);
            
            ((VirtualPin<float>)plugInputPins[0]).wire.SetData(1.0f);
            Assert.AreEqual(((VirtualPin<float>)socketInputPins[0]).wire.data, 1.0f);
            
            ((VirtualPin<byte>)plugInputPins[1]).wire.SetData(1);
            Assert.AreEqual(((VirtualPin<byte>)socketInputPins[1]).wire.data, 1);
        }

        [Test]
        public void ConnectMultiplePlugsToInputlessSocket()
        {
            var socketOutputPins = new IPin[] { new VirtualPin<int>(this), new VirtualPin<float>(this) };
            var socketInputPins = new IPin[] { };
            var plugOutputPins = new IPin[] { new VirtualPin<int>(this), new VirtualPin<float>(this) };
            var plugInputPins = new IPin[] { };
            var plug2OutputPins = new IPin[] { new VirtualPin<int>(this), new VirtualPin<float>(this) };
            var plug2InputPins = new IPin[] { };
            
            var scheme1 = new InputLessSocketScheme();
            scheme1.Initialize(socketOutputPins, socketInputPins);
            
            var scheme2 = new InputLessSocketScheme();
            scheme2.Initialize(plugOutputPins, plugInputPins);
            
            var scheme3 = new InputLessSocketScheme();
            scheme3.Initialize(plug2OutputPins, plug2InputPins);
            
            scheme1.ConnectPlug(scheme2);
            scheme1.ConnectPlug(scheme3);
            
            ((VirtualPin<int>)socketOutputPins[0]).wire.SetData(1);
            Assert.AreEqual(((VirtualPin<int>)plugOutputPins[0]).wire.data, 1);
            Assert.AreEqual(((VirtualPin<int>)plug2OutputPins[0]).wire.data, 1);
            
            ((VirtualPin<float>)socketOutputPins[1]).wire.SetData(1.0f);
            Assert.AreEqual(((VirtualPin<float>)plugOutputPins[1]).wire.data, 1.0f);
            Assert.AreEqual(((VirtualPin<float>)plug2OutputPins[1]).wire.data, 1.0f);
        }

        [Test]
        public void ConnectAndDisconnectPlug()
        {
            var socketInputPin = new VirtualPin<int>(this);
            var socketOutputPin = new VirtualPin<string>(this);
            var plugInputPin = new VirtualPin<int>(this);
            var plugOutputPin = new VirtualPin<string>(this);
            
            var scheme1 = new TestSocketScheme();
            scheme1.Initialize(new IPin[] { socketOutputPin }, new IPin[] { socketInputPin });
            
            var scheme2 = new TestSocketScheme();
            scheme2.Initialize(new IPin[] { plugOutputPin }, new IPin[] { plugInputPin });
            
            scheme1.ConnectPlug(scheme2);
            
            socketOutputPin.wire.SetData("hello");
            Assert.AreEqual(plugOutputPin.wire.data, "hello");
            
            scheme1.DisconnectPlug(scheme2);
            
            socketOutputPin.wire.SetData("hello2");
            Assert.AreNotEqual(plugOutputPin.wire.data, "hello2");
        }

        [Test]
        public void LoopbackConnection()
        {
            var socketInputPins = new IPin[] { new VirtualPin<int>(this) };
            var socketOutputPins = new IPin[] { new VirtualPin<int>(this) };
            
            var scheme1 = new MonoTypeSocketScheme();
            scheme1.Initialize(socketOutputPins, socketInputPins, true);
            
            ((VirtualPin<int>)socketOutputPins[0]).wire.SetData(1);
            Assert.AreEqual(1, ((VirtualPin<int>)socketInputPins[0]).wire.data);
        }

        [Test]
        public void LoopbackConnectionByDefaultThenConnectPlugAndThenDisconnectAgain()
        {
            var socketInputPins = new IPin[] { new VirtualPin<int>(this) };
            var socketOutputPins = new IPin[] { new VirtualPin<int>(this) };
            var plugInputPins = new IPin[] { new VirtualPin<int>(this) };
            var plugOutputPins = new IPin[] { new VirtualPin<int>(this) };
            
            var scheme1 = new MonoTypeSocketScheme();
            scheme1.Initialize(socketOutputPins, socketInputPins, true);
            
            var scheme2 = new MonoTypeSocketScheme();
            scheme2.Initialize(plugOutputPins, plugInputPins);
            
            scheme1.ConnectPlug(scheme2);
            
            ((VirtualPin<int>)socketOutputPins[0]).wire.SetData(1);
            Assert.AreEqual(1, ((VirtualPin<int>)plugOutputPins[0]).wire.data);
            ((VirtualPin<int>)plugInputPins[0]).wire.SetData(3);
            Assert.AreEqual(3, ((VirtualPin<int>)socketInputPins[0]).wire.data);
            
            scheme1.DisconnectPlug(scheme2);
            
            ((VirtualPin<int>)socketOutputPins[0]).wire.SetData(2);
            Assert.AreNotEqual(2, ((VirtualPin<int>)plugInputPins[0]).wire.data);
            Assert.AreEqual(2, ((VirtualPin<int>)socketInputPins[0]).wire.data);
        }

        [Test]
        public void Loopchain()
        {
            var plug1OutputPin = new VirtualPin<int>(this);
            var plug1InputPin = new VirtualPin<int>(this);
            var plug2OutputPin = new VirtualPin<int>(this);
            var plug2InputPin = new VirtualPin<int>(this);
            
            var socketInputPins = new IPin[] { new VirtualPin<int>(this) };
            var socketOutputPins = new IPin[] { new VirtualPin<int>(this) };
            var plug1InputPins = new IPin[] { plug1InputPin };
            var plug1OutputPins = new IPin[] { plug1OutputPin };
            var plug2InputPins = new IPin[] { plug2InputPin };
            var plug2OutputPins = new IPin[] { plug2OutputPin };

            var plug1Reader = new VirtualPinReader<int>(this, plug1OutputPin);
            var plug1Writer = new VirtualPinWriter<int>(this, plug1InputPin);
            
            var plug2Reader = new VirtualPinReader<int>(this, plug2OutputPin);
            var plug2Writer = new VirtualPinWriter<int>(this, plug2InputPin);

            plug1Reader.Subscribe(data =>
            {
                plug1Writer.SetData(data + 1);
            });
            
            plug2Reader.Subscribe(data =>
            {
                plug2Writer.SetData(data + 1);
            });
            
            var scheme1 = new MonoTypeSocketScheme();
            scheme1.Initialize(socketOutputPins, socketInputPins, true);
            
            var scheme2 = new MonoTypeSocketScheme();
            scheme2.Initialize(plug1OutputPins, plug1InputPins);
            
            var scheme3 = new MonoTypeSocketScheme();
            scheme3.Initialize(plug2OutputPins, plug2InputPins);
            
            scheme1.ConnectPlug(scheme2);
            scheme1.ConnectPlug(scheme3);
            
            ((VirtualPin<int>)socketOutputPins[0]).wire.SetData(1);
            Assert.AreEqual(3, ((VirtualPin<int>)socketInputPins[0]).wire.data);
            
            /* Disconnect plug 2 */
            scheme1.DisconnectPlug(scheme3);
            
            ((VirtualPin<int>)socketOutputPins[0]).wire.SetData(1);
            Assert.AreEqual(2, ((VirtualPin<int>)socketInputPins[0]).wire.data);
            
            /* Disconnect plug 1 */
            scheme1.DisconnectPlug(scheme2);
            
            ((VirtualPin<int>)socketOutputPins[0]).wire.SetData(1);
            Assert.AreEqual(1, ((VirtualPin<int>)socketInputPins[0]).wire.data);
        }
        
        [Test]
        public void LoopbackConnectionAndDispose()
        {
            var socketInputPins = new IPin[] { new VirtualPin<int>(this) };
            var socketOutputPins = new IPin[] { new VirtualPin<int>(this) };
            
            var scheme1 = new MonoTypeSocketScheme();
            scheme1.Initialize(socketOutputPins, socketInputPins, true);
            
            ((VirtualPin<int>)socketOutputPins[0]).wire.SetData(1);
            Assert.AreEqual(1, ((VirtualPin<int>)socketInputPins[0]).wire.data);
            
            scheme1.Dispose();
            
            ((VirtualPin<int>)socketOutputPins[0]).wire.SetData(2);
            Assert.AreEqual(0, ((VirtualPin<int>)socketInputPins[0]).wire.data);
        }
    }
}