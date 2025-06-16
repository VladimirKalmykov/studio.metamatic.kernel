using System;
using Cysharp.Threading.Tasks;
using Metamatic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Extensions
{
    public static class LocalizationExtensions
    {
        /// <summary>
        /// Отслеживает изменение локализованной строки и сразу же выдаёт текущее значение (асинхронно).
        /// </summary>
        public static DisposableAction Watch(this LocalizedString localizedString, UnityEngine.Localization.LocalizedString.ChangeHandler handler)
        {
            // Подписываемся на будущие изменения
            localizedString.StringChanged += handler;

            // Асинхронно запрашиваем текущее значение
            AsyncOperationHandle<string> handle = localizedString.GetLocalizedStringAsync();
            if (handle.IsDone)
            {
                // Если уже готово — сразу вызываем
                handler(handle.Result);
            }
            else
            {
                // Иначе ждём завершения
                handle.Completed += op => handler(op.Result);
            }

            // Возвращаем IDisposable для отписки от StringChanged
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