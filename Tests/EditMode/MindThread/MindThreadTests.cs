using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Metamatic.Tests.PlayMode
{
    [TestFixture]
    public class MindThreadTests
    {
        public class TestAnimal
        {
            public float hunger = 0f;
            public float fatigue = 0f;
            public float boredom = 0f;
        }
        public class SleepMindThread : MindThread<TestAnimal>
        {
            public SleepMindThread(TestAnimal context) : base(context)
            {
            }

            public override float UpdateWeight()
            {
                return Context.fatigue > 0.5f ? (Context.fatigue - 0.5f) / 0.5f : 0f;
            }

            protected override UniTask OnActivate(Signal cancellationSignal)
            {
                return UniTask.CompletedTask;
            }
        }
        
        public class EatMindThread : MindThread<TestAnimal>
        {
            public EatMindThread(TestAnimal context) : base(context)
            {
            }

            public override float UpdateWeight()
            {
                return Context.hunger > 0.5f ? (Context.hunger - 0.5f) / 0.5f : 0f;
            }

            protected override UniTask OnActivate(Signal cancellationSignal)
            {
                return UniTask.CompletedTask;
            }
        }
        
        public class LazeMindThread : MindThread<TestAnimal>
        {
            public LazeMindThread(TestAnimal context) : base(context)
            {
            }

            public override float UpdateWeight()
            {
                return 0.25f;
            }

            protected override UniTask OnActivate(Signal cancellationSignal)
            {
                return UniTask.CompletedTask;
            }
        }
        
        public class FunHuntingMindThread : MindThread<TestAnimal>
        {
            public FunHuntingMindThread(TestAnimal context) : base(context)
            {
            }

            public override float UpdateWeight()
            {
                // Animal prefer to hunt when it's bored and hungry
                return Context.hunger;
            }

            protected override UniTask OnActivate(Signal cancellationSignal)
            {
                return UniTask.CompletedTask;
            }
        }
        
        public class PlayWithOwnTailMindThread : MindThread<TestAnimal>
        {
            public PlayWithOwnTailMindThread(TestAnimal context) : base(context)
            {
            }

            public override float UpdateWeight()
            {
                // Animal prefer to play with own tail when it's bored and not hungry
                return 0.1f;
            }

            protected override UniTask OnActivate(Signal cancellationSignal)
            {
                return UniTask.CompletedTask;
            }
        }
        
        public class BoredomMindThread : MindThread<TestAnimal>
        {
            public BoredomMindThread(TestAnimal context) : base(context)
            {
            }

            public override float UpdateWeight()
            {
                return Context.boredom > 0.5f ? (Context.boredom - 0.5f) / 0.5f : 0f;
            }

            protected override UniTask OnActivate(Signal cancellationSignal)
            {
                return UniTask.CompletedTask;
            }
            
            protected override IEnumerable<IMindThread> CreateChildrenThreads()
            {
                return new IMindThread[]
                {
                    new FunHuntingMindThread(Context),
                    new PlayWithOwnTailMindThread(Context)
                };
            }
        }
        
        public class EatSleepLazeMindThread : MindThread<TestAnimal>
        {
            public EatSleepLazeMindThread(TestAnimal context) : base(context)
            {
            }

            public override float UpdateWeight()
            {
                return 1f;
            }

            protected override UniTask OnActivate(Signal cancellationSignal)
            {
                return UniTask.CompletedTask;
            }

            protected override IEnumerable<IMindThread> CreateChildrenThreads()
            {
                return new IMindThread[]
                {
                    new SleepMindThread(Context),
                    new EatMindThread(Context),
                    new LazeMindThread(Context),
                    new BoredomMindThread(Context)
                };
            }
        }

        private float m_Time = 0f;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        [UnityTest]
        public IEnumerator EatSleepLazeTest_HungerToEatThread() => UniTask.ToCoroutine(async () =>
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var timeSource = new TimeSource(() => m_Time);
            var eventLoop = new EventLoop<PlayerLoopTiming>(timeSource);
            var animal = new TestAnimal();
            var rootThread = new EatSleepLazeMindThread(animal);
            var observableLodLevel = new Observable<LODLevel>(LODLevel.High);
            var processor = new MindThreadsProcessor(rootThread, observableLodLevel, eventLoop);
            processor.Run();
            
            // Make animal hungry
            animal.hunger = 1f;
            // Play loop to run weighting
            eventLoop.RunCycle();
            // Play loop again to activate most heaviest thread
            eventLoop.RunCycle();
            // Wait loop end
            await rootThread.WaitLoopEnd();
            
            // Active thread should be EatMindThread
            Assert.IsTrue(rootThread.activeThread is EatMindThread);
        });
        
        [UnityTest]
        public IEnumerator EatSleepLazeTest_HungerToEatAndThenToSleep() => UniTask.ToCoroutine(async () =>
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            m_Time = 0f;
            var timeSource = new TimeSource(() => m_Time);
            var eventLoop = new EventLoop<PlayerLoopTiming>(timeSource);
            var animal = new TestAnimal();
            var rootThread = new EatSleepLazeMindThread(animal);
            var observableLodLevel = new Observable<LODLevel>(LODLevel.High);
            var processor = new MindThreadsProcessor(rootThread, observableLodLevel, eventLoop);
            processor.Run();
            
            // Make animal hungry
            animal.hunger = 1f;
            // Play loop to run weighting
            eventLoop.RunCycle();
            // Play loop again to activate most heaviest thread
            eventLoop.RunCycle();
            // Wait loop end
            await rootThread.WaitLoopEnd();
            // Active thread should be EatMindThread
            Assert.IsTrue(rootThread.activeThread is EatMindThread);
            // Satisfy hunger
            animal.hunger = 0.1f;
            // But getting tired
            animal.fatigue = 0.75f;
            // Increase time to replay wighting loop
            m_Time += 0.3f;
            eventLoop.RunCycle();
            // Play loop to activate thread
            eventLoop.RunCycle();
            // Wait loop end
            await rootThread.WaitLoopEnd();
            
            // Active thread should be EatMindThread
            Assert.IsTrue(rootThread.activeThread is SleepMindThread);
            
            m_Time += 0.1f;
        });

        [UnityTest]
        public IEnumerator LazyIfNotHungryAndNotTired() => UniTask.ToCoroutine(async () =>
        {
            m_Time = 0f;
            var timeSource = new TimeSource(() => m_Time);
            var eventLoop = new EventLoop<PlayerLoopTiming>(timeSource);
            var animal = new TestAnimal();
            var rootThread = new EatSleepLazeMindThread(animal);
            var observableLodLevel = new Observable<LODLevel>(LODLevel.High);
            var processor = new MindThreadsProcessor(rootThread, observableLodLevel, eventLoop);
            processor.Run();
            
            // Make animal not hungry
            animal.hunger = 0.1f;
            // Make animal not tired
            animal.fatigue = 0.1f;
            // Play loop to run weighting
            eventLoop.RunCycle();
            // Play loop again to activate most heaviest thread
            eventLoop.RunCycle();
            // Wait loop end
            await rootThread.WaitLoopEnd();
            
            // Active thread should be LazeMindThread
            Assert.IsTrue(rootThread.activeThread is LazeMindThread);
        });
        
        [UnityTest]
        public IEnumerator BoredomAsAnExampleOfSubSubThreads_ShouldBeActivated() => UniTask.ToCoroutine(async () =>
        {
            m_Time = 0f;
            var timeSource = new TimeSource(() => m_Time);
            var eventLoop = new EventLoop<PlayerLoopTiming>(timeSource);
            var animal = new TestAnimal();
            var rootThread = new EatSleepLazeMindThread(animal);
            var observableLodLevel = new Observable<LODLevel>(LODLevel.High);
            var processor = new MindThreadsProcessor(rootThread, observableLodLevel, eventLoop);
            processor.Run();
            
            // Make animal not hungry
            animal.hunger = 0.2f;
            // Make animal not tired
            animal.fatigue = 0.1f;
            // Make animal bored
            animal.boredom = 1f;
            // Play loop to run weighting
            eventLoop.RunCycle();
            // Play loop again to activate most heaviest thread
            eventLoop.RunCycle();
            // Play loop to weighting sub threads
            eventLoop.RunCycle();
            // Play loop again to activate most heaviest thread
            eventLoop.RunCycle();
            // Wait loop end
            await rootThread.WaitLoopEnd();
            
            // Active thread should be BoredomMindThread
            Assert.IsTrue(rootThread.activeThread is BoredomMindThread, "Direct active thread should be BoredomMindThread");
            // Active thread should have FunHuntingMindThread as active sub thread
            Assert.IsTrue(rootThread.topThread is FunHuntingMindThread, "Top active thread should be FunHuntingMindThread");
        });
    }
}