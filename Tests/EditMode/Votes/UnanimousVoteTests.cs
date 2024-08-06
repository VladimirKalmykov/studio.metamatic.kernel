using Metamatic;
using NUnit.Framework;

namespace Tests.EditMode.Votes
{
    [TestFixture]
    public class UnanimousVoteTests
    {
        
        [Test]
        public void UnanimousVote_WhenAllVotesAreTrue_ResultIsFalse()
        {
            var vote = new UnanimousVote(false);
            using var disposable = vote.Vote(true);
            using var disposable2 = vote.Vote(false);
            Assert.IsFalse(vote.result);
        }
        
        [Test]
        public void UnanimousVote_WhenAllVotesAreFalse_ResultIsFalse()
        {
            var vote = new UnanimousVote(false);
            using var disposable = vote.Vote(false);
            using var disposable2 = vote.Vote(false);
            Assert.IsFalse(vote.result);
        }
        
        [Test]
        public void UnanimousVote_WhenAllVotesAreFalseButFallbackIsTrue_ResultIsFalse()
        {
            var vote = new UnanimousVote(true);
            using var disposable = vote.Vote(false);
            using var disposable2 = vote.Vote(false);
            Assert.IsFalse(vote.result);
        }
        
        [Test]
        public void UnanimousVote_WhenSomeVotesAreFalseButFallbackIsTrue_ResultIsFalse()
        {
            var vote = new UnanimousVote(true);
            using var disposable = vote.Vote(true);
            using var disposable2 = vote.Vote(false);
            Assert.IsTrue(vote.result);
        }
        
        [Test]
        public void UnanimousVote_WhenAllVotesAreTrue_ResultIsTrue()
        {
            var vote = new UnanimousVote(false);
            using var disposable = vote.Vote(true);
            using var disposable2 = vote.Vote(true);
            Assert.IsTrue(vote.result);
        }
    }
}