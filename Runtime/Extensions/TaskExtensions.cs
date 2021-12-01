using System.Threading.Tasks;

namespace Metamatic
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;

    public static class TaskExtensions
    {
        public static async Task<int> WhenAnySignal(this Task task, Task[] tasks)
        {
            var resolvedTask = await Task.WhenAny(tasks);

            for (var i = 0; i < tasks.Length; i++)
            {
                if (resolvedTask == tasks[i])
                {
                    return i;
                }
            }

            return -1;
        }
    }
}