using System;

namespace Metamatic.EditorInspector
{
    /// <summary>
    /// Describes log metadata.
    /// </summary>
    public readonly struct LogMetadata
    {
        public readonly string Text;
        public readonly string[] Tags;
        public readonly string StackTrace;
        public readonly bool IsUnityLog;
        public readonly string Id;
            
        public LogMetadata(string text, string[] tags, bool isUnityLog, string stackTrace)
        {
            Text = text;
            Tags = tags;
            IsUnityLog = isUnityLog;
            StackTrace = stackTrace;
            Id = Guid.NewGuid().ToString();
        }
    }
}