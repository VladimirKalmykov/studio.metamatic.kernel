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
        public readonly bool IsBuiltInLog;
        public readonly string Id;
        /* Timestamp */
        public readonly DateTime TimeStamp;
            
        public LogMetadata(string text, string[] tags, bool isBuiltInLog, string stackTrace)
        {
            TimeStamp = DateTime.Now;
            Text = text;
            Tags = tags;
            IsBuiltInLog = isBuiltInLog;
            StackTrace = stackTrace;
            Id = Guid.NewGuid().ToString();
        }
    }
}