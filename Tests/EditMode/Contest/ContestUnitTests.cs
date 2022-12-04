using System;
using NUnit.Framework;

namespace Metamatic
{
    public class ContestUnitTests
    {
        public class CompetitionMember : ICompetitive, IDisposable
        {
            public int priority { get; }
            private event Action OnDispose;

            public CompetitionMember(int priority = 0)
            {
                this.priority = priority;
            }
            public Action SubscribeDispose(Action handler)
            {
                OnDispose += handler;

                return () =>
                {
                    OnDispose -= handler;
                };
            }

            public void Dispose()
            {
                OnDispose?.Invoke();
            }
        }
        [Test]
        public void ContestGivesRigthCompetitiveLeader()
        {
            var memeber1 = new CompetitionMember(1);
            var memeber2 = new CompetitionMember(2);
            var memeber3 = new CompetitionMember(3);
            var memeber4 = new CompetitionMember(4);
            var memeber5 = new CompetitionMember(5);
            CompetitionMember bestMemeber = null;
            var contest = new Contest<CompetitionMember>((value =>
            {
                bestMemeber = value;
            }));
            contest.Add(memeber1);
            Assert.AreEqual(bestMemeber,memeber1);
            contest.Add(memeber2);
            Assert.AreEqual(bestMemeber, memeber2);
            contest.Add(memeber3);
            Assert.AreEqual(bestMemeber, memeber3);
            contest.Add(memeber5);
            Assert.AreEqual(bestMemeber,memeber5);
            contest.Add(memeber4);
            Assert.AreEqual(bestMemeber,memeber5);
        }
        
        [Test]
        public void ContestGivesRigthCompetitiveLeaderWithRemoval()
        {
            var memeber1 = new CompetitionMember(1);
            var memeber2 = new CompetitionMember(2);
            var memeber3 = new CompetitionMember(3);
            var memeber4 = new CompetitionMember(4);
            var memeber5 = new CompetitionMember(5);
            CompetitionMember bestMemeber = null;
            var contest = new Contest<CompetitionMember>((value =>
            {
                bestMemeber = value;
            }));
            contest.Add(memeber1);
            Assert.AreEqual(bestMemeber,memeber1);
            contest.Add(memeber2);
            Assert.AreEqual(bestMemeber, memeber2);
            contest.Add(memeber3);
            Assert.AreEqual(bestMemeber, memeber3);
            contest.Add(memeber5);
            Assert.AreEqual(bestMemeber,memeber5);
            contest.Add(memeber4);
            Assert.AreEqual(bestMemeber,memeber5);
            contest.Remove(memeber5);
            Assert.AreEqual(bestMemeber,memeber4);
            contest.Remove(memeber1);
            Assert.AreEqual(bestMemeber,memeber4);
        }
        
        [Test]
        public void ContestGivesRigthCompetitiveLeaderWithDispose()
        {
            var memeber1 = new CompetitionMember(1);
            var memeber2 = new CompetitionMember(2);
            var memeber3 = new CompetitionMember(3);
            var memeber4 = new CompetitionMember(4);
            var memeber5 = new CompetitionMember(5);
            CompetitionMember bestMemeber = null;
            var contest = new Contest<CompetitionMember>((value =>
            {
                bestMemeber = value;
            }));
            contest.Add(memeber1);
            Assert.AreEqual(bestMemeber,memeber1);
            contest.Add(memeber2);
            Assert.AreEqual(bestMemeber, memeber2);
            contest.Add(memeber3);
            Assert.AreEqual(bestMemeber, memeber3);
            contest.Add(memeber5);
            Assert.AreEqual(bestMemeber,memeber5);
            contest.Add(memeber4);
            Assert.AreEqual(bestMemeber,memeber5);
            memeber5.Dispose();
            Assert.AreEqual(4, bestMemeber.priority);
            memeber1.Dispose();
            Assert.AreEqual(4, bestMemeber.priority);
            memeber2.Dispose();
            Assert.AreEqual(4, bestMemeber.priority);
            memeber4.Dispose();
            Assert.AreEqual(3, bestMemeber.priority);
            memeber3.Dispose();
            Assert.AreEqual(null, bestMemeber);
        }
    }
}