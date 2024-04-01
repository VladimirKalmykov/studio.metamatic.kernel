using Cysharp.Threading.Tasks;

namespace Metamatic
{
    using System;

    public class RepositoryAsyncOperation
    {
        private UniTaskCompletionSource<object> tcs = new UniTaskCompletionSource<object>();
        public bool isDone { get; private set; }
        public float progress { get; private set; }
        
        public UniTask task => tcs.Task;

        public RepositoryAsyncOperation(Func<UniTask> taskFunction)
        {
            ExecuteAsyncOperationFromTask(taskFunction);
        }
        
        public RepositoryAsyncOperation(out Action<float> setProgress, out Action<Exception> setError)
        {
            setProgress = SetProgress;
            setError = SetError;
        }
        
        // TODO: 
        public void OnCompleted(Action continuation)
        {
            tcs.Task.GetAwaiter().OnCompleted(continuation);
        }
        
        private void SetProgress(float progress)
        {
            if (tcs.Task.Status.IsCompleted()) return;
            this.progress = progress;
            if (progress >= 1.0f)
            {
                isDone = true;
                tcs.TrySetResult(null);
            }
        }
        
        private void SetError(Exception exception)
        {
            if (tcs.Task.Status.IsCompleted()) return;
            isDone = true;
            tcs.TrySetException(exception);
        }

        private async void ExecuteAsyncOperationFromTask(Func<UniTask> taskFunction)
        {
            await taskFunction();
            isDone = true;
            progress = 1.0f;
            tcs.TrySetResult(null);
        }

        public static RepositoryAsyncOperation Create(Func<UniTask> taskFunction)
        {
            return new RepositoryAsyncOperation(taskFunction);
        }
    }
}