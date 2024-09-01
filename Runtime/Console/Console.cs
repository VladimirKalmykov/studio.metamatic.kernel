using System;
using System.Diagnostics;
using System.IO;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace Metamatic
{
    public static class Console
    {
        [Flags]
        public enum LogLevel
        {
            None = 0,
            Verbose = 1 << 1,
            Debug = 1 << 2,
            Info = 1 << 3,
            Warning = 1 << 4,
            Error = 1 << 5,
            Critical = 1 << 6,
            Minimal = Error | Critical,
            Standard = Info | Warning | Error | Critical,
            Full = Verbose | Debug | Info | Warning | Error | Critical
        }
        
        #region Public Properties and Methods
        
        /// <summary>
        /// Default log level for Unity console
        /// </summary>
        public static LogLevel LogLevelTranslateToUnityConsole = LogLevel.None;
        public static event Action<string, string, LogLevel> OnLog;
        
        [UsedImplicitly]
        [DebuggerHidden]
        public static void Log(string message, LogLevel logLevel = LogLevel.Info)
        {
            if (logLevel == LogLevel.None)
            {
                return;
            }
            var stackTrace = new StackTrace();
            LogOnTheMainThread(message, logLevel, stackTrace).Forget();
        }
        
        private static async UniTaskVoid LogOnTheMainThread(string message, LogLevel logLevel, StackTrace stackTrace)
        {
            /* Get current thread */
            var currentThread = System.Threading.Thread.CurrentThread;
            if (currentThread.ManagedThreadId != 1)
            {
                await UniTask.SwitchToMainThread();
                return;
            }
            
            OnLog?.Invoke(message, stackTrace.ToString(), logLevel);
            if ((LogLevelTranslateToUnityConsole & logLevel) == logLevel)
            {
                if (logLevel == LogLevel.Warning)
                {
                    UnityEngine.Debug.LogWarning(message);
                }
                else if (logLevel == LogLevel.Error)
                {
                    UnityEngine.Debug.LogError(message);
                }
                else
                {
                    UnityEngine.Debug.Log(message);
                }
            }
        }
        
        [UsedImplicitly]
        public static void Critical(string message)
        {
            Log(message, LogLevel.Critical);
        }
        
        [UsedImplicitly]
        public static void Error(string message)
        {
            Log(message, LogLevel.Error);
        }
        
        [UsedImplicitly]
        public static void Warning(string message)
        {
            Log(message, LogLevel.Warning);
        }
        
        [UsedImplicitly]
        public static void Info(string message)
        {
            Log(message, LogLevel.Info);
        }
        
        [UsedImplicitly]
        public static void Debug(string message)
        {
            Log(message, LogLevel.Debug);
        }
        
        [UsedImplicitly]
        public static void Verbose(string message)
        {
            Log(message, LogLevel.Verbose);
        }
        #endregion
    }
}