using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace Metamatic.Tests.PlayMode
{
    [TestFixture]
    public class MindThreadTests
    {
        public class TestAnimal
        {
            public float hunger = 0f;
            public float fatigue = 0f;
        }
        public class SleepMindThread : MindThread<TestAnimal>
        {
            public SleepMindThread(TestAnimal context) : base(context)
            {
            }

            public override float GetWeight()
            {
                return Context.fatigue > 0.5f ? (Context.fatigue - 0.5f) / 0.5f : 0f;
            }

            protected override UniTask OnActivate(CancellationSignal cancellationSignal)
            {
                return UniTask.CompletedTask;
            }
        }
        
        public class EatMindThread : MindThread<TestAnimal>
        {
            public EatMindThread(TestAnimal context) : base(context)
            {
            }

            public override float GetWeight()
            {
                return Context.hunger > 0.5f ? (Context.hunger - 0.5f) / 0.5f : 0f;
            }

            protected override UniTask OnActivate(CancellationSignal cancellationSignal)
            {
                return UniTask.CompletedTask;
            }
        }
        
        public class LazeMindThread : MindThread<TestAnimal>
        {
            public LazeMindThread(TestAnimal context) : base(context)
            {
            }

            public override float GetWeight()
            {
                return 0.25f;
            }

            protected override UniTask OnActivate(CancellationSignal cancellationSignal)
            {
                return UniTask.CompletedTask;
            }
        }
        
        public class EatSleepLazeMindThread : MindThread<TestAnimal>
        {
            public EatSleepLazeMindThread(TestAnimal context) : base(context)
            {
            }

            public override float GetWeight()
            {
                return 1f;
            }

            protected override UniTask OnActivate(CancellationSignal cancellationSignal)
            {
                return UniTask.CompletedTask;
            }

            protected override IEnumerable<IMindThread> CreateChildrenThreads()
            {
                return new IMindThread[3]
                {
                    new SleepMindThread(Context),
                    new EatMindThread(Context),
                    new LazeMindThread(Context)
                };
            }
        }

        public IEnumerator EatSleepLazeTest() => UniTask.ToCoroutine(async () =>
        {
            var eventLoop = new EventLoop<PlayerLoopTiming>();
            var animal = new TestAnimal();
            var rootThread = new EatSleepLazeMindThread(animal);
            var observableLodLevel = new Observable<LODLevel>(LODLevel.High);
            var processor = new MindThreadsProcessor(rootThread, observableLodLevel);
            processor.Run();
        });
    }
}