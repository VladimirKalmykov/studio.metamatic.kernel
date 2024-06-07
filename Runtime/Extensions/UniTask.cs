using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public static class UniTaskExtensions
    {
        public static async UniTask<T> ForgetCancellation<T>(this UniTask<T> task)
        {
            try
            {
                return await task;
            }
            catch (OperationCanceledException)
            {
                return default(T);
            }
        }
    }
}