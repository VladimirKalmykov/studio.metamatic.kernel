using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UniTask = Cysharp.Threading.Tasks.UniTask;

namespace Metamatic
{
    public class HttpSource: IOSource
    {
        public override AsyncReadOperation<T> Read<T>(string uri, CancellationToken token)
        {
            var operationSource = new AsyncReadOperationSource<T>();
            UniTask.Run(async delegate () 
            {
                try
                {
                    using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
                    {
                        operationSource.SetProgress(0);
                        await webRequest.SendWebRequest();
                        if (token.IsCancellationRequested)
                        {
                            operationSource.SetError(new OperationCanceledException());
                            return;
                        }

                        if (webRequest.result != UnityWebRequest.Result.Success)
                        {
                            operationSource.SetError(new Exception(webRequest.error));
                        }
                        else
                        {
                            string fileContent = webRequest.downloadHandler.text;
                            operationSource.SetRawValue(fileContent);
                        }
                    }
                }
                catch (Exception e)
                {
                    operationSource.SetError(e);
                }
            });
            return operationSource.Operation;
        }

        public override AsyncWriteOperation<T> Write<T>(string uri, T value, CancellationToken token)
        {
            var operationSource = new AsyncWriteOperationSource<T>();
            SaveFile(value, operationSource, uri, token);
            return operationSource.Operation;
        }

        private async void SaveFile<T>(T value, AsyncWriteOperationSource<T> operationSource, string uri, CancellationToken token)
        {
            var package = new SerializableDataPackage<T>(value);
            string json = JsonUtility.ToJson(package);
            try
            {
                using (UnityWebRequest webRequest = new UnityWebRequest(uri, "PUT"))
                {
                    byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                    webRequest.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
                    webRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                    webRequest.SetRequestHeader("Content-Type", "application/json");
                    operationSource.SetProgress(0);
                    await webRequest.SendWebRequest();
                    if (token.IsCancellationRequested)
                    {
                        operationSource.SetError(new OperationCanceledException());
                    }
                    else
                    {
                        if (webRequest.result != UnityWebRequest.Result.Success)
                        {
                            operationSource.SetError(new Exception(webRequest.error));
                        }
                        else
                        {
                            operationSource.SetCompleted();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                operationSource.SetError(e);
            }
        }
    }
}