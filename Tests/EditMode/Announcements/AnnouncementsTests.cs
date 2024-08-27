using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Metamatic;
using NUnit.Framework;

namespace Tests.EditMode.Announcements
{
    [TestFixture]
    public class AnnouncementsTests
    {
        [Test]
        public void AnnounceAndSubscribe()
        {
            List<int> observedValues = new List<int>();
            List<bool> observedValuesCancelled = new List<bool>();
            
            var dispatcher = new Dispatcher();
            var release = dispatcher.Announce("test", 42);
            dispatcher.ObserveAnnouncementEach<int>("test", async (value, token) =>
            {
                observedValues.Add(value);
                await token;
                observedValuesCancelled.Add(token.IsCancellationRequested);
            });
            Assert.AreEqual(1, observedValues.Count);
            Assert.AreEqual(42, observedValues[0]);
            Assert.AreEqual(0, observedValuesCancelled.Count);
            
            /* Release the announcement */
            release.Dispose();
            
            Assert.AreEqual(1, observedValues.Count);
            Assert.AreEqual(1, observedValuesCancelled.Count);
        }

        [Test]
        public void TwoAnnouncements()
        {
            List<int> observedValues = new List<int>();
            List<bool> observedValuesCancelled = new List<bool>();
            
            var dispatcher = new Dispatcher();
            var release = dispatcher.Announce("test", 42);
            dispatcher.ObserveAnnouncementEach<int>("test", async (value, token) =>
            {
                observedValues.Add(value);
                await token;
                observedValuesCancelled.Add(token.IsCancellationRequested);
            });
            Assert.AreEqual(1, observedValues.Count);
            Assert.AreEqual(42, observedValues[0]);
            Assert.AreEqual(0, observedValuesCancelled.Count);
            
            var release2 = dispatcher.Announce("test", 43);
            
            Assert.AreEqual(2, observedValues.Count);
            Assert.AreEqual(43, observedValues[1]);
            Assert.AreEqual(0, observedValuesCancelled.Count);
            
            /* Release the announcement */
            release.Dispose();
            
            Assert.AreEqual(1, observedValuesCancelled.Count);
            
            /* Release the announcement */
            release2.Dispose();
            
            Assert.AreEqual(2, observedValuesCancelled.Count);
        }
        
        [Test]
        public void CancelAnnouncementWhenDisposeObserver()
        {
            List<int> observedValues = new List<int>();
            List<bool> observedValuesCancelled = new List<bool>();
            
            var dispatcher = new Dispatcher();
            var release = dispatcher.Announce("test", 42);
            var disposeObserver = dispatcher.ObserveAnnouncementEach<int>("test", async (value, token) =>
            {
                observedValues.Add(value);
                await token;
                observedValuesCancelled.Add(token.IsCancellationRequested);
            });
            Assert.AreEqual(1, observedValues.Count);
            Assert.AreEqual(42, observedValues[0]);
            Assert.AreEqual(0, observedValuesCancelled.Count);
            
            var release2 = dispatcher.Announce("test", 43);
            
            Assert.AreEqual(2, observedValues.Count);
            Assert.AreEqual(43, observedValues[1]);
            Assert.AreEqual(0, observedValuesCancelled.Count);
            
            disposeObserver.Dispose();
            
            Assert.AreEqual(2, observedValuesCancelled.Count);
        }
    }
}