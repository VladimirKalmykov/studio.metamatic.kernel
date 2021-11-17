using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public class TasksQueue: IDisposable
    {
        private List<Task> m_UnresolvedTasks;

        public Task currentTask
        {
            get;
            private set;
        }
        public TasksQueue(IEnumerable<Task> tasks)
        {
            m_UnresolvedTasks = tasks.ToList();
        }

        public async Task<bool> Next()
        {
            if (m_UnresolvedTasks.Count > 0)
            {
                var resolvedTask = await Task.WhenAny(m_UnresolvedTasks);
                var index = m_UnresolvedTasks.FindIndex(t => t.Id == resolvedTask.Id);
                if (index < 0)
                {
                    throw new InvalidOperationException("Resolved task is out of index");
                }
                m_UnresolvedTasks.RemoveAt(index);
                currentTask = resolvedTask;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Dispose()
        {
            m_UnresolvedTasks.Clear();
        }
    }
}