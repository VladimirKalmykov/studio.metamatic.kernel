using Cysharp.Threading.Tasks;

namespace Metamatic.MindThreads
{
    public class IdlenessMindThread: MindThread<object>
    {
        private float m_FixedWeight;
        public IdlenessMindThread(object context, float fixedWeight) : base(context)
        {
            m_FixedWeight = fixedWeight;
        }

        public override float UpdateWeight()
        {
            return m_FixedWeight;
        }

        protected override UniTask OnActivate(CancellationSignal cancellationSignal)
        {
            return UniTask.CompletedTask;
        }
    }
}