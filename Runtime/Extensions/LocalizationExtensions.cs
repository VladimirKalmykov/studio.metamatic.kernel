using System;
using Cysharp.Threading.Tasks;
using Metamatic;
using UnityEngine;
using UnityEngine.Localization;

namespace Extensions
{
    public static class LocalizationExtensions
    {
        public static DisposableAction Watch(this LocalizedString localizedString, LocalizedString.ChangeHandler handler)
        {
            localizedString.StringChanged += handler;
            
            /* Immediate update */
            if (localizedString.IsEmpty)
            {
                handler.Invoke("");
            }
            else
            {
                handler.Invoke(localizedString.GetLocalizedString());
            }
            
            return DisposableAction.Create(() =>
            {
                localizedString.StringChanged -= handler;
            });
        }
        
        public static UniTask<string> WhenHasValue(this LocalizedString localizedString, Signal cancellationSignal)
        {
            if (localizedString.IsEmpty)
            {
                return UniTask.FromResult(localizedString.GetLocalizedString());
            }

            var tcs = SafeUniTaskCompletionSource<string>.Create();
            var operationHandle = localizedString.GetLocalizedStringAsync();
            var cancellationSignalRegistration = cancellationSignal.Register(() =>
            {
                operationHandle.Release();
                if (!tcs.TrySetCanceled())
                {
                    Debug.LogWarning("Localization operation was cancelled but the task was already completed.");
                }
            });
            operationHandle.Completed += handle =>
            {
                cancellationSignalRegistration.Dispose();
                if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    tcs.TrySetResult(handle.Result);
                }
                else
                {
                    tcs.TrySetException(new Exception("Failed to get localized string."));
                }
            };
            return tcs.Task;
        }
    }
}