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
        public static event Action<string, string[], string, LogLevel> OnLog;
        
        [UsedImplicitly]
        [DebuggerHidden]
        public static void LogWithStackTrance(string message, string[] tags, LogLevel logLevel = LogLevel.Info, StackTrace stackTrace = null)
        {
            if (logLevel == LogLevel.None)
            {
                return;
            }
            var finalStackTrace = stackTrace ?? new StackTrace();
            LogOnTheMainThread(message, tags, logLevel, finalStackTrace.ToString()).Forget();
        }
        
        [UsedImplicitly]
        [DebuggerHidden]
        public static void Log(string message, string[] tags, LogLevel logLevel = LogLevel.Info, string stackTrace = null)
        {
            if (logLevel == LogLevel.None)
            {
                return;
            }
            var finalStackTrace = stackTrace ?? new StackTrace().ToString();
            LogOnTheMainThread(message, tags, logLevel, finalStackTrace).Forget();
        }
        
        [UsedImplicitly]
        [DebuggerHidden]
        public static void Log(string tag1, string message, LogLevel logLevel = LogLevel.Info, string stackTrace = null)
        {
            if (logLevel == LogLevel.None)
            {
                return;
            }
            var finalStackTrace = stackTrace ?? new StackTrace().ToString();
            LogOnTheMainThread(message, new []{ tag1 }, logLevel, finalStackTrace).Forget();
        }
        
        [UsedImplicitly]
        [DebuggerHidden]
        public static void Log(string tag1, string tag2, string message, LogLevel logLevel = LogLevel.Info, string stackTrace = null)
        {
            if (logLevel == LogLevel.None)
            {
                return;
            }
            var finalStackTrace = stackTrace ?? new StackTrace().ToString();
            LogOnTheMainThread(message, new []{ tag1, tag2 }, logLevel, finalStackTrace).Forget();
        }
        
        [UsedImplicitly]
        [DebuggerHidden]
        public static void Log(string tag1, string tag2, string tag3, string message, LogLevel logLevel = LogLevel.Info, string stackTrace = null)
        {
            if (logLevel == LogLevel.None)
            {
                return;
            }
            var finalStackTrace = stackTrace ?? new StackTrace().ToString();
            LogOnTheMainThread(message, new []{ tag1, tag2, tag3 }, logLevel, finalStackTrace).Forget();
        }
        
        [UsedImplicitly]
        [DebuggerHidden]
        public static void Log(string tag1, string tag2, string tag3, string tag4, string message, LogLevel logLevel = LogLevel.Info, string stackTrace = null)
        {
            if (logLevel == LogLevel.None)
            {
                return;
            }
            var finalStackTrace = stackTrace ?? new StackTrace().ToString();
            LogOnTheMainThread(message, new []{ tag1, tag2, tag3, tag4 }, logLevel, finalStackTrace).Forget();
        }
        
        private static async UniTaskVoid LogOnTheMainThread(string message, string[] tags, LogLevel logLevel, string stackTrace)
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
                    UnityEngine.Debug.LogWarning(CompileMessageWithTags(tags, message));
                }
                else if (logLevel == LogLevel.Error || logLevel == LogLevel.Exception)
                {
                    UnityEngine.Debug.LogError(CompileMessageWithTags(tags, message));
                }
                else
                {
                    UnityEngine.Debug.Log(CompileMessageWithTags(tags, message));
                }
            }
            else
            {
                OnLog?.Invoke(message, tags, stackTrace, logLevel);
            }
        }
        
        public static string CompileMessageWithTags(string[] tags, string message)
        {
            if (tags.Length == 0)
            {
                return message;
            }
            return $"[{string.Join("][", tags)}] {message}";
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Exception(string tag1, Exception exception)
        {
            Log(tag1, exception.ToString(), LogLevel.Exception, exception.StackTrace);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Exception(string tag1, string message)
        {
            Log(tag1, message, LogLevel.Exception);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Exception(string tag1, string tag2, string message)
        {
            Log(tag1, tag2, message, LogLevel.Exception);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Exception(string tag1, string tag2, string tag3, string message)
        {
            Log(tag1, tag2, tag3, message, LogLevel.Exception);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Error(string tag1, Exception exception)
        {
            Log(tag1, exception.ToString(), LogLevel.Error, exception.StackTrace);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Error(string tag1, string message)
        {
            Log(tag1, message, LogLevel.Error);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Error(string tag1, string tag2, string message)
        {
            Log(tag1, tag2, message, LogLevel.Error);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Error(string tag1, string tag2, string tag3, string message)
        {
            Log(tag1, tag2, tag3, message, LogLevel.Error);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Warning(string tag1, string message)
        {
            Log(tag1, message, LogLevel.Warning);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Warning(string tag1, string tag2, string message)
        {
            Log(tag1, tag2, message, LogLevel.Warning);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Warning(string tag1, string tag2, string tag3, string message)
        {
            Log(tag1, tag2, tag3, message, LogLevel.Warning);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Info(string tag1, string message)
        {
            Log(tag1, message, LogLevel.Info);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Info(string tag1, string tag2, string message)
        {
            Log(tag1, tag2, message, LogLevel.Info);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Info(string tag1, string tag2, string tag3, string message)
        {
            Log(tag1, tag2, tag3, message, LogLevel.Info);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Debug(string tag1, string message)
        {
            Log(tag1, message, LogLevel.Debug);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Debug(string tag1, string tag2, string message)
        {
            Log(tag1, tag2, message, LogLevel.Debug);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Debug(string tag1, string tag2, string tag3, string message)
        {
            Log(tag1, tag2, tag3, message, LogLevel.Debug);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Verbose(string tag1, string message)
        {
            Log(tag1, message, LogLevel.Verbose);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Verbose(string tag1, string tag2, string message)
        {
            Log(tag1, tag2, message, LogLevel.Verbose);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Verbose(string tag1, string tag2, string tag3, string message)
        {
            Log(tag1, tag2, tag3, message, LogLevel.Verbose);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Verbose(string tag1, string tag2, string tag3, string tag4, string message)
        {
            Log(tag1, tag2, tag3, tag4, message, LogLevel.Verbose);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Verbose(string message, string[] tags, string stackTrace = null)
        {
            Log(message, tags, LogLevel.Verbose, stackTrace);
        }
        #endregion
    }
}