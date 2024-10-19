using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public interface IMindThread: IDisposable
    {
        public string name { get; }
        /// <summary>
        /// Refers to the direct active child thread
        /// </summary>
        public IMindThread activeThread { get; }
        /// <summary>
        /// Refers to the top active thread in a nested thread structure
        /// </summary>
        public IMindThread topThread { get; }
        public IEnumerable<IMindThread> children { get; }
        public float currentWeight { get; }
        public bool isActive { get; }
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
        public DisposableAction WatchLoopEnd(Action<IMindThread> action);
    }
}