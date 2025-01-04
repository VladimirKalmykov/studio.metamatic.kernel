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
        public readonly Console.StackSource StackSource;
        public readonly bool IsBuiltInLog;
        public readonly string Id;
        /* Timestamp */
        public readonly DateTime TimeStamp;
            
        public LogMetadata(string text, string[] tags, bool isBuiltInLog, Console.StackSource stackSource)
        {
            TimeStamp = DateTime.Now;
            Text = text;
            Tags = tags;
            IsBuiltInLog = isBuiltInLog;
            StackSource = stackSource;
            Id = Guid.NewGuid().ToString();
        }
    }
}