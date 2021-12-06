using System.Threading.Tasks;

namespace Metamatic
{
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