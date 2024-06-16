using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using UniTask = Cysharp.Threading.Tasks.UniTask;
using UniTaskCompletionSource = Cysharp.Threading.Tasks.UniTaskCompletionSource;

namespace Metamatic
{
    [PlatformCompatible(RuntimePlatform.WindowsPlayer)]
    [PlatformCompatible(RuntimePlatform.WebGLPlayer)]
    public class HttpStorage: Storage
    {
        public override Cysharp.Threading.Tasks.UniTask<(T, Storage.Metadata)> Get<T>(string address, CancellationToken token)
        {
            var taskCompletionSource = new Cysharp.Threading.Tasks.UniTaskCompletionSource<(T, Storage.Metadata)>();
            UniTask.RunOnThreadPool(async delegate () 
            {
                try
                {
                    using (UnityWebRequest webRequest = UnityWebRequest.Get(address))
                    {
                        await webRequest.SendWebRequest();
                        if (token.IsCancellationRequested)
                        {
                            await UniTask.SwitchToMainThread();
                            taskCompletionSource.TrySetException(new OperationCanceledException());
                            return;
                        }

                        if (webRequest.result != UnityWebRequest.Result.Success)
                        {
                            await UniTask.SwitchToMainThread();
                            taskCompletionSource.TrySetException(new Exception(webRequest.error));
                        }
                        else
                        {
                            string fileContent = webRequest.downloadHandler.text;
                            await UniTask.SwitchToMainThread();
                            taskCompletionSource.TrySetResult(Deserialize<T>(fileContent));
                        }
                    }
                }
                catch (Exception e)
                {
                    await UniTask.SwitchToMainThread();
                    taskCompletionSource.TrySetException(e);
                }
            });
            return taskCompletionSource.Task;
        }

        public override UniTask Set<T>(string address, T value, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}