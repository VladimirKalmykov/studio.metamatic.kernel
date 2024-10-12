using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public interface IMindThread: IDisposable
    {
        /// <summary>
        /// Thread doesn't removed when reached zero weight
        /// </summary>
        public bool longLived { get; set; }
        public IMindThread rootThread { get; set; }
        public UniTask Activate(CancellationSignal cancellationSignal);
        public void Update();
        public float GetWeight();
        public void Weighting();
        /// <summary>
        /// Determines that the thread nested weighting is finished
        /// </summary>
        /// <param name="topThread">Top thread that has no children threads</param>
        public void LoopEnd(IMindThread topThread);
        public UniTask WaitLoopEnd(CancellationSignal cancellationSignal);
    }
}