using Metamatic;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class CancellationSignalTests
    {
        [Test]
        public void SimpleUse()
        {
            var css = SignalSource.Create();
            var signal = css.Signal;
            css.TryRaise();
            Assert.IsTrue(signal.IsCancellationRequested);
        }
        
        [Test]
        public void LinkedUse()
        {
            var css1 = SignalSource.Create();
            var css2 = SignalSource.Create();
            var css3 = SignalSource.CreateLinked(css1.Signal, css2.Signal);
            Assert.IsFalse(css3.Signal.IsCancellationRequested);
            css1.TryRaise();
            Assert.IsTrue(css3.Signal.IsCancellationRequested);
            css2.TryRaise();
            Assert.IsTrue(css3.Signal.IsCancellationRequested);
        }
    }
}