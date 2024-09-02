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
            Exception = 1 << 6,
            Minimal = Error | Exception,
            Standard = Info | Warning | Error | Exception,
            Full = Verbose | Debug | Info | Warning | Error | Exception
        }
        
        #region Public Properties and Methods
        
        /// <summary>
        /// Default log level for Unity console
        /// </summary>
        public static LogLevel LogLevelTranslateToUnityConsole = LogLevel.Error | LogLevel.Exception;
        public static event Action<string, string, LogLevel> OnLog;
        
        [UsedImplicitly]
        [DebuggerHidden]
        public static void LogWithStackTrance(string message, LogLevel logLevel = LogLevel.Info, StackTrace stackTrace = null)
        {
            if (logLevel == LogLevel.None)
            {
                return;
            }
            var finalStackTrace = stackTrace ?? new StackTrace();
            LogOnTheMainThread(message, logLevel, finalStackTrace.ToString()).Forget();
        }
        
        [UsedImplicitly]
        [DebuggerHidden]
        public static void Log(string message, LogLevel logLevel = LogLevel.Info, string stackTrace = null)
        {
            if (logLevel == LogLevel.None)
            {
                return;
            }
            var finalStackTrace = stackTrace ?? new StackTrace().ToString();
            LogOnTheMainThread(message, logLevel, finalStackTrace).Forget();
        }
        
        private static async UniTaskVoid LogOnTheMainThread(string message, LogLevel logLevel, string stackTrace)
        {
            /* Get current thread */
            var currentThread = System.Threading.Thread.CurrentThread;
            if (currentThread.ManagedThreadId != 1)
            {
                await UniTask.SwitchToMainThread();
                return;
            }
            
            if ((LogLevelTranslateToUnityConsole & logLevel) == logLevel)
            {
                if (logLevel == LogLevel.Warning)
                {
                    UnityEngine.Debug.LogWarning(message);
                }
                else if (logLevel == LogLevel.Error || logLevel == LogLevel.Exception)
                {
                    UnityEngine.Debug.LogError(message);
                }
                else
                {
                    UnityEngine.Debug.Log(message);
                }
            }
            else
            {
                OnLog?.Invoke(message, stackTrace, logLevel);
            }
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Exception(string message)
        {
            Log(message, LogLevel.Exception);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Exception(Exception exception)
        {
            Log(exception.ToString(), LogLevel.Exception, exception.StackTrace);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Error(string message)
        {
            Log(message, LogLevel.Error);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Error(Exception exception)
        {
            Log(exception.ToString(), LogLevel.Error, exception.StackTrace);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Warning(string message)
        {
            Log(message, LogLevel.Warning);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Info(string message)
        {
            Log(message, LogLevel.Info);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Debug(string message)
        {
            Log(message, LogLevel.Debug);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Verbose(string message)
        {
            Log(message, LogLevel.Verbose);
        }
        #endregion
    }
}