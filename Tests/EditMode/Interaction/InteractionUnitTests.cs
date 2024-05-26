using System;
using NUnit.Framework;

namespace Metamatic
{
    
    public class InteractionUnitTests
    {
        [Test]
        public void InteractionAtoBSendValues()
        {
            var interaction = new Interaction<int, string>(1);
            var handshake = interaction.ToHandshake();
            int value2 = 0;
            handshake.Subscribe((value =>
            {
                value2 = value;
            }));
            Assert.AreEqual(1, value2);
            interaction.Send(2);
            Assert.AreEqual(2, value2);
            interaction.Send(3);
            Assert.AreEqual(3, value2);
            handshake.Dispose();
            interaction.Send(4);
            Assert.AreEqual(3, value2);
        }
        
        [Test]
        public void InteractionBtoASendValues()
        {
            var interaction = new Interaction<int, string>(1);
            string incomingValue = "";
            interaction.Subscribe((string value) =>
            {
                incomingValue = value;
            });
            
            var handshake = interaction.ToHandshake();
            handshake.Send("Hello");
            Assert.AreEqual("Hello", incomingValue);
            interaction.DismissHandshake();
            handshake.Send("Hello 2");
            Assert.AreEqual("Hello", incomingValue);
        }
        
        [Test]
        public void InteractionHandshakeDismissByHandshake()
        {
            var interaction = new Interaction<int, string>(1);
            int handshaked = 0;
            int dismissed = 0;
            interaction.OnHandshake += () =>
            {
                handshaked++;
            };
            interaction.OnHandshakeDismiss += () =>
            {
                dismissed++;
            };
            var handshake = interaction.ToHandshake();
            Assert.AreEqual(1, handshaked);
            handshake.Dispose();
            Assert.AreEqual(1, dismissed);
            handshake.Dispose();
            Assert.AreEqual(1, dismissed);
        }
        
        [Test]
        public void InteractionHandshakeDismissByInitiator()
        {
            var interaction = new Interaction<int, string>(1);
            int handshaked = 0;
            int dismissed = 0;
            interaction.OnHandshake += () =>
            {
                handshaked++;
            };
            interaction.OnHandshakeDismiss += () =>
            {
                dismissed++;
            };
            var handshake = interaction.ToHandshake();
            Assert.AreEqual(1, handshaked);
            interaction.DismissHandshake();
            Assert.AreEqual(1, dismissed);
            Assert.That(() => interaction.DismissHandshake(), 
                Throws.TypeOf<Exception>());
            Assert.AreEqual(1, dismissed);
            /* Try to subscribe dismissed handshake */
            Assert.That(() => handshake.Subscribe(value => { }), Throws.TypeOf<Exception>());
            interaction.Send(11);
            var nextHandshake = interaction.ToHandshake();
            int nextShakehandPayloadValue = -1;
            nextHandshake.Subscribe(value =>
            {
                nextShakehandPayloadValue = value;
            });
            Assert.AreEqual(2, handshaked);
            Assert.AreEqual(11, nextShakehandPayloadValue);
            /* Create second handshake */
            Assert.That(() => interaction.ToHandshake(), Throws.TypeOf<Exception>());
        }
    }
}