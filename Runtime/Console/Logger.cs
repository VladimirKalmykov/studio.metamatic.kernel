using System;
using System.IO;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public static class ConsoleLogger
    {
        [Flags]
        public enum LogLevel
        {
            None = 0,
            Info = 1 << 1,
            Warning = 1 << 2,
            Error = 1 << 3,
        }
#if PERSISTENT_LOGGING
        private static StreamWriter logFileWriter;
        private static readonly object lockObj = new object();
        public static LogLevel UnityApplicationLogLevelWriteToFile = LogLevel.Info | LogLevel.Warning | LogLevel.Error;
        public static Console.LogLevel MetamaticConsolePersistentLogLevel = Console.LogLevel.Full;
#endif
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
#if PERSISTENT_LOGGING
            if (logFileWriter != null)
            {
                lock (lockObj)
                {
                    logFileWriter.Close();
                    logFileWriter = null;
                }
            }
            string logFilePath = GenerateLogFilePath();
            if (logFilePath == null)
            {
                Debug.LogError("Failed to generate log file path");
                return;
            }
            Debug.Log($"[LOGGER] Log file path: {logFilePath}");
            /* Create directory if it doesn't exist */
            string directoryPath = Path.GetDirectoryName(logFilePath);
            if (directoryPath != null && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            lock (lockObj)
            {
                logFileWriter = new StreamWriter(logFilePath, true);
                logFileWriter.AutoFlush = true;
            }
            Application.logMessageReceived += OnBuiltInLogReceived;
            Console.OnLog += OnMetamaticLogReceived;
#else
            Console.Info("[LOGGER] Persistent logging is disabled. To enable ir, define PERSISTENT_LOGGING in the project settings.");            
#endif
        }
        
#if PERSISTENT_LOGGING
        public static string GenerateLogFilePath()
        {
            string dateTime = DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss");
            string projectPath = Directory.GetParent(Application.dataPath)?.FullName ?? null;
            if (projectPath == null)
            {
                return null;
            }
            return Path.Combine(projectPath, "Logs", "Metamatic", $"Log_Metamatic_{dateTime}.txt");
        }
        
        public static void WriteLog(string message)
        {
            if (logFileWriter == null)
            {
                Debug.LogError("Log file writer is null");
                return;
            }
            UniTask.RunOnThreadPool(() => 
            {
                var finalMessage = $"{DateTime.Now:dd-MM-yyyy HH:mm:ss} {message}";
                lock (lockObj)
                {
                    logFileWriter.WriteLine(finalMessage);
                }
            }).Forget();
        }
        
        private static void OnMetamaticLogReceived(string text, string stackTrace, Console.LogLevel logLevel)
        {
            if ((MetamaticConsolePersistentLogLevel & logLevel) != logLevel)
            {
                return;
            }
            WriteLog($"[${logLevel.ToString()}] {text}");
        }
        
        private static void OnBuiltInLogReceived(string condition, string stackTrace, LogType type)
        {
            switch (type)
            {
                case LogType.Error:
                    if ((UnityApplicationLogLevelWriteToFile & LogLevel.Error) == LogLevel.Error)
                    {
                        WriteLog($"[ERROR] {condition}");
                    }
                    break;
                case LogType.Assert:
                    if ((UnityApplicationLogLevelWriteToFile & LogLevel.Error) == LogLevel.Error)
                    {
                        WriteLog($"[ASSERT] {condition}");
                    }
                    break;
                case LogType.Warning:
                    if ((UnityApplicationLogLevelWriteToFile & LogLevel.Warning) == LogLevel.Warning)
                    {
                        WriteLog($"[WARNING] {condition}");
                    }
                    break;
                case LogType.Log:
                    if ((UnityApplicationLogLevelWriteToFile & LogLevel.Info) == LogLevel.Info)
                    {
                        WriteLog($"[INFO] {condition}");
                    }
                    break;
                case LogType.Exception:
                    if ((UnityApplicationLogLevelWriteToFile & LogLevel.Error) == LogLevel.Error)
                    {
                        WriteLog($"[EXCEPTION] {condition}");
                    }
                    break;
            }
        }
#endif
    }
}