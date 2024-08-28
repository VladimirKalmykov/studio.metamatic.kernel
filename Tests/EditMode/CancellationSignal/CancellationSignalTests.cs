using Metamatic;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Tests.EditMode.CancellationSignal
{
    [TestFixture]
    public class CancellationSignalTests
    {
        [Test]
        public void SimpleUse()
        {
            var css = CancellationSignalSource.Create();
            var signal = css.Signal;
            css.TryCancel();
            Assert.IsTrue(signal.IsCancellationRequested);
        }
        
        [Test]
        public void LinkedUse()
        {
            var css1 = CancellationSignalSource.Create();
            var css2 = CancellationSignalSource.Create();
            var css3 = CancellationSignalSource.CreateLinked(css1.Signal, css2.Signal);
            Assert.IsFalse(css3.Signal.IsCancellationRequested);
            css1.TryCancel();
            Assert.IsTrue(css3.Signal.IsCancellationRequested);
            css2.TryCancel();
            Assert.IsTrue(css3.Signal.IsCancellationRequested);
        }
    }
}