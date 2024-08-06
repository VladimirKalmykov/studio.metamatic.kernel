using Metamatic;
using NUnit.Framework;

namespace Tests.EditMode.Votes
{
    [TestFixture]
    public class VetoVoteTests
    {
        [Test]
        public void VetoVote_DefaultFalse_WhenOneVotesAreTrue_ResultIsTrue()
        {
            var vote = new VetoVote(false);
            using var disposable = vote.Vote(true);
            using var disposable2 = vote.Vote(false);
            Assert.IsTrue(vote.result);
        }
        
        [Test]
        public void VetoVote_DefaultFalse_WhenAllVotesFalse_ResultIsFalse()
        {
            var vote = new VetoVote(false);
            using var disposable = vote.Vote(false);
            using var disposable2 = vote.Vote(false);
            Assert.IsFalse(vote.result);
        }
        
        [Test]
        public void VetoVote_DefaultTrue_WhenOneVotesAreFalse_ResultIsFalse()
        {
            var vote = new VetoVote(true);
            using var disposable = vote.Vote(true);
            using var disposable2 = vote.Vote(false);
            Assert.IsFalse(vote.result);
        }
        
        [Test]
        public void VetoVote_DefaultTrue_WhenAllVotesAreTrue_ResultIsTrue()
        {
            var vote = new VetoVote(true);
            using var disposable = vote.Vote(true);
            using var disposable2 = vote.Vote(true);
            Assert.IsTrue(vote.result);
        }
        
        [Test]
        public void VetoVote_DefaultTrue_WhenNoOneVotes_ResultIsTrue()
        {
            var vote = new VetoVote(true);
            Assert.IsTrue(vote.result);
        }
    }
}