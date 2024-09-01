namespace Metamatic
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Cysharp.Threading.Tasks;

    public class LoadService : Service
    {
        public readonly struct LoadingTask : IDisposable
        {
            private readonly LoadService m_Service;
            private readonly string m_TaskId;
            public readonly string name;

            public LoadingTask(LoadService loadService, string name)
            {
                m_Service = loadService;
                this.name = name;
                m_TaskId = Guid.NewGuid().ToString();
                loadService.m_LoadingTasksInProgress.Add(this);
                loadService.OnLoadingTasksChanged?.Invoke();
                loadService.m_TotalLoadingTasksCount++;
            }

            public void Dispose()
            {
                if (m_Service.m_LoadingTasksInProgress.Contains(this))
                {
                    m_Service.m_LoadingTasksInProgress.Remove(this);
                    m_Service.m_LoadedTaskNames.Add(name);
                    m_Service.OnLoadingTasksChanged?.Invoke();
                }
            }
        }

        private int m_TotalLoadingTasksCount = 0;
        private readonly List<LoadingTask> m_LoadingTasksInProgress = new List<LoadingTask>();
        private readonly List<string> m_LoadedTaskNames = new List<string>();
        private event Action OnLoadingTasksChanged;

        public (int, int) loadingProgress => (m_LoadingTasksInProgress.Count, m_TotalLoadingTasksCount);

        /// <summary>
        /// Create loading task at initialization to allow to create splash screen loader
        /// </summary>
        /// <returns></returns>
        public static async UniTask<LoadingTask> CreateLoadingTask(string name, CancellationSignal signal)
        {
            var instance = await Resolve<LoadService>(signal);
            return new LoadingTask(instance, name);
        }

#if DEBUG
        private IDisposable DisplayLoadingTasksInTerminal()
        {
            var row = Terminal.CreateUpdatableLog();
            Action handler = () =>
            {
                List<string> tasks = new List<string>();
                foreach (var task in m_LoadingTasksInProgress)
                {
                    tasks.Add(task.name + ": <color=yellow>in progress</color>");
                }

                foreach (var taskName in m_LoadedTaskNames)
                {
                    tasks.Add(taskName + ": <color=green>DONE</color>");
                }

                row.Update(
                    $"Loading tasks {loadingProgress.Item1}/{loadingProgress.Item2}:\n{string.Join("\n", tasks)}");
            };
            OnLoadingTasksChanged += handler;


            return new DisposableAction(() =>
            {
                OnLoadingTasksChanged -= handler;
                row.Dispose();
            });
        }
#endif
        protected override async UniTask RunService(Ready ready, CancellationSignal signal)
        {
#if DEBUG
            using var displayLoadingTasks = DisplayLoadingTasksInTerminal();
#endif
            await ready();
        }
    }

}