using System;
using System.Collections.Generic;
using Metamatic;
using NUnit.Framework;

namespace Tests.EditMode.Announcements
{
    public class MockAutoAnnouncement : IDisposable
    {
        public static readonly Dispatcher Dispatcher = new Dispatcher();
        public int Value;

        private Action m_DisposeHandler;

        public MockAutoAnnouncement()
        {
            m_DisposeHandler += Dispatcher.Announce(this, true);
        }

        public void Dispose()
        {
            m_DisposeHandler?.Invoke();
        }
    }

    public class PlayerAnnouncement : MockAutoAnnouncement
    {
        public PlayerAnnouncement() : base()
        {
        }
    }

    [TestFixture]
    public class AnnouncementsTests
    {
        [Test]
        public void AnnounceAndSubscribe()
        {
            List<int> observedValues = new List<int>();
            List<bool> observedValuesCancelled = new List<bool>();

            var dispatcher = new Dispatcher();
            var release = dispatcher.Announce(42);
            dispatcher.ObserveAnnouncementEach<int>(async (value, token) =>
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
            var release1 = dispatcher.Announce(42);
            dispatcher.ObserveAnnouncementEach<int>(async (value, token) =>
            {
                observedValues.Add(value);
                await token;
                observedValuesCancelled.Add(token.IsCancellationRequested);
            });
            Assert.AreEqual(1, observedValues.Count);
            Assert.AreEqual(42, observedValues[0]);
            Assert.AreEqual(0, observedValuesCancelled.Count);

            var release2 = dispatcher.Announce(43);

            Assert.AreEqual(2, observedValues.Count);
            Assert.AreEqual(43, observedValues[1]);
            Assert.AreEqual(0, observedValuesCancelled.Count);

            /* Release the first announcement */
            release1.Dispose();

            Assert.AreEqual(1, observedValuesCancelled.Count);

            /* Release the second announcement */
            release2.Dispose();

            Assert.AreEqual(2, observedValuesCancelled.Count);
        }

        [Test]
        public void CancelAnnouncementWhenDisposeObserver()
        {
            List<int> observedValues = new List<int>();
            List<bool> observedValuesCancelled = new List<bool>();

            var dispatcher = new Dispatcher();
            var release1 = dispatcher.Announce(42);
            var disposeObserver = dispatcher.ObserveAnnouncementEach<int>(async (value, token) =>
            {
                observedValues.Add(value);
                await token;
                observedValuesCancelled.Add(token.IsCancellationRequested);
            });
            Assert.AreEqual(1, observedValues.Count);
            Assert.AreEqual(42, observedValues[0]);
            Assert.AreEqual(0, observedValuesCancelled.Count);

            var release2 = dispatcher.Announce(43);

            Assert.AreEqual(2, observedValues.Count);
            Assert.AreEqual(43, observedValues[1]);
            Assert.AreEqual(0, observedValuesCancelled.Count);

            disposeObserver.Dispose();

            Assert.AreEqual(2, observedValuesCancelled.Count);
        }

        [Test]
        public void AutoAnnouncement()
        {
            var mockAutoAnnouncement = new MockAutoAnnouncement();
            var objHistory = new List<Type>();
            var releaseHistory = new List<object>();

            MockAutoAnnouncement.Dispatcher.ObserveAnnouncementEach<MockAutoAnnouncement>(async (obj, token) =>
            {
                objHistory.Add(obj.GetType());
                await token;
                releaseHistory.Add(obj);
            });

            Assert.AreEqual(1, objHistory.Count);
            Assert.AreEqual(typeof(MockAutoAnnouncement), objHistory[0]);

            mockAutoAnnouncement.Dispose();

            Assert.AreEqual(1, releaseHistory.Count);
            Assert.AreEqual(mockAutoAnnouncement, releaseHistory[0]);
        }

        [Test]
        public void AutoAnnouncementDerivedClass()
        {
            var playerAnnouncement = new PlayerAnnouncement();
            var objHistory = new List<Type>();
            var releaseHistory = new List<object>();

            MockAutoAnnouncement.Dispatcher.ObserveAnnouncementEach<PlayerAnnouncement>(async (obj, token) =>
            {
                objHistory.Add(obj.GetType());
                await token;
                releaseHistory.Add(obj);
            });

            Assert.AreEqual(1, objHistory.Count);
            Assert.AreEqual(typeof(PlayerAnnouncement), objHistory[0]);

            playerAnnouncement.Dispose();

            Assert.AreEqual(1, releaseHistory.Count);
            Assert.AreEqual(playerAnnouncement, releaseHistory[0]);
        }
    }
}
