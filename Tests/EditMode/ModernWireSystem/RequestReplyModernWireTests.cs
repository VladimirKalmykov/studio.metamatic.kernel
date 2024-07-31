using Metamatic;
using NUnit.Framework;

namespace Tests.EditMode.ModernWireSystem
{
    [TestFixture]
    public class RequestReplyModernWireTests
    {
        [Test]
        public void ConnectionTest()
        {
            var requester = new RequesterModernWire<int, int>();
            var responder = new ResponderModernWire<int, int>();
            
            responder.Assign(request => request + 1);
            
            requester.ConnectOut(responder);

            Assert.IsTrue(requester.IsConnectedOut(responder));
            Assert.IsTrue(responder.IsConnectedIn(requester));
            
            var response = requester.Request(1);
            
            Assert.AreEqual(2, response);
        }
        
        [Test]
        public void ConnectMultipleRequestersTest()
        {
            var requester1 = new RequesterModernWire<int, int>();
            var requester2 = new RequesterModernWire<int, int>();
            var responder = new ResponderModernWire<int, int>();
            
            responder.Assign(request => request + 1);
            
            requester1.ConnectOut(responder);
            requester2.ConnectOut(responder);

            Assert.IsTrue(requester1.IsConnectedOut(responder));
            Assert.IsTrue(responder.IsConnectedIn(requester1));
            
            Assert.IsTrue(requester2.IsConnectedOut(responder));
            Assert.IsTrue(responder.IsConnectedIn(requester2));
            
            var response1 = requester1.Request(1);
            var response2 = requester2.Request(1);
            
            Assert.AreEqual(2, response1);
            Assert.AreEqual(2, response2);
        }
        
        [Test]
        public void ConnectAndDisconnectTest()
        {
            var requester = new RequesterModernWire<int, int>();
            var responder = new ResponderModernWire<int, int>();
            
            responder.Assign(request => request + 1);
            
            var disposable = requester.ConnectOut(responder);

            Assert.IsTrue(requester.IsConnectedOut(responder));
            Assert.IsTrue(responder.IsConnectedIn(requester));
            
            disposable.Dispose();
            
            Assert.IsFalse(requester.IsConnectedOut(responder));
            Assert.IsFalse(responder.IsConnectedIn(requester));
            
            var response = requester.Request(1);
            
            Assert.AreEqual(0, response);
        }
    }
}