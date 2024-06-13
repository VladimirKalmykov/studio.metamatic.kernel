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
    public class HttpSource: IOSource
    {
        public override Cysharp.Threading.Tasks.UniTask<T> Read<T>(string name, CancellationToken token)
        {
            var taskCompletionSource = new Cysharp.Threading.Tasks.UniTaskCompletionSource<T>();
            var ioOperation = new IOOperation();
            UniTask.RunOnThreadPool(async delegate () 
            {
                try
                {
                    using (UnityWebRequest webRequest = UnityWebRequest.Get(name))
                    {
                        ioOperation.progress = 0f;
                        await webRequest.SendWebRequest();
                        if (token.IsCancellationRequested)
                        {
                            taskCompletionSource.TrySetException(new OperationCanceledException());
                            return;
                        }

                        if (webRequest.result != UnityWebRequest.Result.Success)
                        {
                            taskCompletionSource.TrySetException(new Exception(webRequest.error));
                        }
                        else
                        {
                            string fileContent = webRequest.downloadHandler.text;
                            taskCompletionSource.TrySetResult(Deserialize<T>(fileContent));
                        }
                    }
                }
                catch (Exception e)
                {
                    taskCompletionSource.TrySetException(e);
                }
            });
            return taskCompletionSource.Task;
        }

        public override UniTask Write<T>(string name, T value, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}